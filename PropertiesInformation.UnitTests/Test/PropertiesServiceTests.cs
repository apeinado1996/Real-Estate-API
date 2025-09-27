using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.Controllers;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesInformation.UnitTests.Test
{
    [TestFixture]
    public class PropertiesServiceTests
    {
        private Mock<IPropertyRepository> _repo = default!;
        private PropertiesController _ctrl = default!;

        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IPropertyRepository>(MockBehavior.Strict);

            _ctrl = new PropertiesController(_repo.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [TearDown]
        public void TearDown() => _repo.VerifyAll();

        [Test]
        public async Task List_should_return_ok_with_items()
        {
            var list = new List<Property>
        {
            new() { Id = 1, Name = "Depa Centro", Address = "Calle 1", Price = 100000, CodeInternal="P-001", Year=2020, IdOwner=5 }
        };

            _repo.Setup(r => r.ListAsync(null, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(list);

            var result = await _ctrl.List(codeInternal: null, ownerId: null, minPrice: null, maxPrice: null, null, null, null, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeOfType<ApiResponse<IEnumerable<PropertyResponse>>>();
            var env = (ApiResponse<IEnumerable<PropertyResponse>>)ok.Value!;
            env.Success.Should().BeTrue();
            env.Data!.Should().HaveCount(1);
        }

        [Test]
        public async Task Get_should_return_notfound_when_property_missing()
        {
            _repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Property?)null);

            var result = await _ctrl.Get(99, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
            ((NotFoundObjectResult)result).Value.Should().BeOfType<ApiResponse<object>>();
        }

        [Test]
        public async Task Create_should_return_created_property()
        {
            var id = 10;
            var req = new PropertyCreateRequest("Depto", "Calle 2", 200000m, "P-002", 2021, 7);

            _repo.Setup(r => r.AddAsync(It.Is<Property>(p =>
                    p.Name == req.Name && p.Address == req.Address && p.Price == req.Price &&
                    p.CodeInternal == req.CodeInternal && p.Year == req.Year && p.IdOwner == req.IdOwner
            ), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Property { Id = id, Name = req.Name, Address = req.Address, Price = req.Price, CodeInternal = req.CodeInternal, Year = req.Year, IdOwner = req.IdOwner });

            var result = await _ctrl.Create(req, CancellationToken.None);

            result.Should().BeOfType<CreatedAtActionResult>();
            var created = (CreatedAtActionResult)result;
            created.Value.Should().BeOfType<ApiResponse<PropertyResponse>>();
            var env = (ApiResponse<PropertyResponse>)created.Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(id);
            env.Data!.Name.Should().Be("Depto");
        }

        [Test]
        public async Task Update_should_modify_and_return_ok()
        {
            var existing = new Property { Id = 5, Name = "Old", Address = "A", Price = 1, CodeInternal = "X", Year = 2000, IdOwner = 1 };
            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existing);

            var req = new PropertyUpdateRequest("New", "Addr", 300000m, 2022, 2);

            _repo.Setup(r => r.UpdateAsync(It.Is<Property>(p =>
                p.Id == 5 && p.Name == "New" && p.Address == "Addr" && p.Price == 300000m && p.Year == 2022 && p.IdOwner == 2
            ), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _repo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Property { Id = 5, Name = "New", Address = "Addr", Price = 300000m, CodeInternal = "X", Year = 2022, IdOwner = 2 });

            var result = await _ctrl.Update(5, req, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Name.Should().Be("New");
        }

        [Test]
        public async Task ChangePrice_should_call_repo_and_return_ok()
        {
            var existing = new Property { Id = 3, Name = "Prop", Address = "A", Price = 100m, CodeInternal = "C", Year = 2010, IdOwner = 1 };
            _repo.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

            var req = new PropertyChangePriceRequest(250000m, "PRICE UPDATE", 0m);

            _repo.Setup(r => r.ChangePriceAsync(3, req.NewPrice, req.TraceName, req.TraceTax.Value, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            var result = await _ctrl.ChangePrice(3, req, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().NotBeNull();            
            ok.Value.Should().BeAssignableTo<ApiResponse<PropertyResponse>>();
            var env = (ApiResponse<PropertyResponse>)ok.Value!;
            env.Success.Should().BeTrue();
        }

        [Test]
        public async Task Delete_should_remove_and_return_ok()
        {
            _repo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Property { Id = 4, Name = "ToDel" });

            _repo.Setup(r => r.DeleteAsync(4, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            var result = await _ctrl.Delete(4, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            ((ApiResponse<object>)((OkObjectResult)result).Value!).Success.Should().BeTrue();
        }
    }
}
