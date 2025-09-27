using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
    public class PropertyTracesServiceTests
    {
        private Mock<IPropertyTraceRepository> _traceRepo = default!;
        private Mock<IPropertyRepository> _propRepo = default!;
        private PropertyTracesController _ctrl = default!;

        [SetUp]
        public void SetUp()
        {
            _traceRepo = new Mock<IPropertyTraceRepository>(MockBehavior.Strict);
            _propRepo = new Mock<IPropertyRepository>(MockBehavior.Strict);

            _ctrl = new PropertyTracesController(_traceRepo.Object, _propRepo.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _traceRepo.VerifyAll();
            _propRepo.VerifyAll();
        }

        [Test]
        public async Task List_should_return_ok_with_items()
        {
            _propRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 1, Name = "P" });

            _traceRepo.Setup(r => r.ListByPropertyAsync(1, null, null, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<PropertyTrace> {
                      new() { Id=100, IdProperty=1, Name="SALE", DateSale=new DateTime(2025,1,1), Value=1000m, Tax=10m }
                      });

            var result = await _ctrl.List(1, null, null, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<IEnumerable<PropertyTraceResponse>>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Should().HaveCount(1);
        }

        [Test]
        public async Task Create_should_add_trace_and_return_created()
        {
            _propRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 2, Name = "P2" });

            var req = new PropertyTraceCreateRequest(
                DateSale: new DateTime(2025, 2, 2),
                Name: "INITIAL",
                Value: 5000m,
                Tax: 100m
            );

            _traceRepo.Setup(r => r.AddAsync(It.Is<PropertyTrace>(t =>
                t.IdProperty == 2 && t.DateSale == req.DateSale.Date && t.Name == "INITIAL" && t.Value == 5000m && t.Tax == 100m
            ), It.IsAny<CancellationToken>())).ReturnsAsync(77);

            _traceRepo.Setup(r => r.GetByIdAsync(77, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new PropertyTrace { Id = 77, IdProperty = 2, DateSale = req.DateSale.Date, Name = "INITIAL", Value = 5000m, Tax = 100m });

            var result = await _ctrl.Create(2, req, CancellationToken.None);

            result.Should().BeOfType<CreatedAtActionResult>();
            var env = (ApiResponse<PropertyTraceResponse>)((CreatedAtActionResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(77);
            env.Data!.Name.Should().Be("INITIAL");
        }

        [Test]
        public async Task Update_should_modify_trace_and_return_ok()
        {
            _traceRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new PropertyTrace { Id = 5, IdProperty = 3, DateSale = new DateTime(2025, 1, 1), Name = "X", Value = 1m, Tax = 0m });

            var req = new PropertyTraceUpdateRequest(
                DateSale: new DateTime(2025, 3, 3),
                Name: "UPDATED",
                Value: 9000m,
                Tax: 300m
            );

            _traceRepo.Setup(r => r.UpdateAsync(It.Is<PropertyTrace>(t =>
                t.Id == 5 && t.IdProperty == 3 && t.DateSale == req.DateSale.Date && t.Name == "UPDATED" && t.Value == 9000m && t.Tax == 300m
            ), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _traceRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new PropertyTrace { Id = 5, IdProperty = 3, DateSale = req.DateSale.Date, Name = "UPDATED", Value = 9000m, Tax = 300m });

            var result = await _ctrl.Update(3, 5, req, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyTraceResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Name.Should().Be("UPDATED");
        }

        [Test]
        public async Task Delete_should_remove_trace_and_return_ok()
        {
            _traceRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new PropertyTrace { Id = 9, IdProperty = 4, Name = "DEL", DateSale = DateTime.Today, Value = 1m, Tax = 0m });

            _traceRepo.Setup(r => r.DeleteAsync(9, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask);

            var result = await _ctrl.Delete(4, 9, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            ((ApiResponse<object>)((OkObjectResult)result).Value!).Success.Should().BeTrue();
        }

        [Test]
        public async Task Summary_should_return_totals()
        {
            _propRepo.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 6 });

            _traceRepo.Setup(r => r.SummaryByPropertyAsync(6, null, null, It.IsAny<CancellationToken>()))
                      .ReturnsAsync((CountTraces: 3, TotalValue: 10000m, TotalTax: 500m));

            var result = await _ctrl.Summary(6, null, null, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyTraceSummaryResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.CountTraces.Should().Be(3);
            env.Data!.TotalValue.Should().Be(10000m);
            env.Data!.TotalTax.Should().Be(500m);
        }
    }
}
