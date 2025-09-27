using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.Controllers;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;
using System.Text;

namespace PropertiesInformation.UnitTests.Test
{

    [TestFixture]
    public class OwnersServiceTests
    {
        private Mock<IOwnerRepository> _repo = default!;
        private OwnersController _ctrl = default!;

        [SetUp]
        public void SetUp()
        {
            _repo = new Mock<IOwnerRepository>(MockBehavior.Strict);

            _ctrl = new OwnersController(_repo.Object)
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
            // Arrange
            var items = new List<Owner>
            {
                new() { Id=1, Name="Andres Owner", Address="Cra 16 66 75", Birthday = new DateTime(1996,11,14) }
            };
            _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);

            // Act
            var result = await _ctrl.List(CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeOfType<ApiResponse<IEnumerable<OwnerResponse>>>();

            var envelope = (ApiResponse<IEnumerable<OwnerResponse>>)ok.Value!;
            envelope.Success.Should().BeTrue();
            envelope.Data!.Should().HaveCount(1);
        }

        [Test]
        public async Task Get_should_return_notfound_when_owner_does_not_exist()
        {
            _repo.Setup(r => r.GetByIdAsync(99, false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Owner?)null);

            var result = await _ctrl.Get(99, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
            var nf = (NotFoundObjectResult)result;
            nf.Value.Should().BeOfType<ApiResponse<object>>();
            ((ApiResponse<object>)nf.Value!).Success.Should().BeFalse();
        }

        [Test]
        public async Task Get_should_return_ok_with_owner()
        {
            var entity = new Owner { Id = 2, Name = "Felipe Owner", Address = "cra 25", Birthday = new DateTime(1990, 1, 1) };
            _repo.Setup(r => r.GetByIdAsync(2, false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

            var result = await _ctrl.Get(2, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeOfType<ApiResponse<OwnerResponse>>();
            var env = (ApiResponse<OwnerResponse>)ok.Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(2);
            env.Data!.Name.Should().Be("Felipe Owner");
        }

        [Test]
        public async Task Create_form_should_create_owner_with_photo_and_return_created()
        {
            // Arrange
            var newId = 10;
            var reqBytes = Encoding.UTF8.GetBytes("fake-image");
            var file = new FormFile(new MemoryStream(reqBytes), 0, reqBytes.Length, "Photo", "photo.jpg")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/jpeg"
            };

            var createReq = new OwnerCreateRequest("Andres", "Cra 20 # 30", file, new DateTime(1985, 5, 5));

            _repo.Setup(r => r.AddAsync(It.Is<Owner>(o =>
                    o.Name == "Andres" &&
                    o.Address == "Cra 20 # 30" &&
                    o.Birthday == new DateTime(1985, 5, 5) &&
                    o.Photo != null && o.Photo.Length == reqBytes.Length
                ), It.IsAny<CancellationToken>()))
                .ReturnsAsync(newId);

            _repo.Setup(r => r.GetByIdAsync(newId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Owner { Id = newId, Name = "Andres", Address = "Cra 20 # 30", Birthday = new DateTime(1985, 5, 5) });

            // Act
            var result = await _ctrl.Create(createReq, CancellationToken.None);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var created = (CreatedAtActionResult)result;
            created.Value.Should().BeOfType<ApiResponse<OwnerResponse>>();
            var env = (ApiResponse<OwnerResponse>)created.Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(newId);
            env.Data!.Name.Should().Be("Andres");
        }
       
        [Test]
        public async Task Update_should_modify_owner_and_return_ok()
        {
            Owner? passed = null;

            _repo.Setup(r => r.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Owner { Id = 5, Name = "Andres owner", Address = "cra 16 #66-75", Birthday = new DateTime(2000, 1, 1) });

            _repo.Setup(r => r.UpdateAsync(It.IsAny<Owner>(), It.IsAny<CancellationToken>()))
                .Callback<Owner, CancellationToken>((o, _) => passed = o)
                .Returns(Task.CompletedTask);

            _repo.Setup(r => r.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Owner { Id = 5, Name = "New Name", Address = "New Addr", Birthday = new DateTime(1999, 9, 9) });

            // Act
            var req = new OwnerUpdateRequest("Felipe Owner", "cra 14 # 70-98", new DateTime(1999, 9, 9));
            var result = await _ctrl.Update(5, req, CancellationToken.None);

            // Assert
            passed.Should().NotBeNull();
            passed!.Id.Should().Be(5);
            passed.Name.Should().Be("Felipe Owner");
            passed.Address.Should().Be("cra 14 # 70-98");
            passed.Birthday.Should().Be(new DateTime(1999, 9, 9));
        }

        [Test]
        public async Task UpdatePhoto_should_save_bytes_and_return_ok()
        {
            var id = 3;
            var owner = new Owner { Id = id, Name = "Photo Owner" };
            _repo.Setup(r => r.GetByIdAsync(id, false, It.IsAny<CancellationToken>())).ReturnsAsync(owner);

            var bytes = Encoding.UTF8.GetBytes("img");
            var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "x.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            _repo.Setup(r => r.UpdatePhotoAsync(id, It.Is<byte[]>(b => b.SequenceEqual(bytes)), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            var ownerFileUploadRequest = new OwnerFileUploadRequest(file);

            var result = await _ctrl.UpdatePhoto(id, ownerFileUploadRequest, CancellationToken.None);
            
            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeOfType<ApiResponse<object>>();
            var env = (ApiResponse<object>)ok.Value!;
            env.Success.Should().BeTrue();
        }

        [Test]
        public async Task Delete_should_remove_owner_and_return_ok()
        {
            var id = 8;
            _repo.Setup(r => r.GetByIdAsync(id, false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Owner { Id = id, Name = "ToDelete" });
            _repo.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

            var result = await _ctrl.Delete(id, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var ok = (OkObjectResult)result;
            ok.Value.Should().BeOfType<ApiResponse<object>>();
            ((ApiResponse<object>)ok.Value!).Success.Should().BeTrue();
        }
    }
}
