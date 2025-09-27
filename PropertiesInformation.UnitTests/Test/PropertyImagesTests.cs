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
    public class PropertyImagesTests
    {
        private Mock<IPropertyImageRepository> _imgRepo = default!;
        private Mock<IPropertyRepository> _propRepo = default!;
        private PropertyImagesController _ctrl = default!;

        [SetUp]
        public void SetUp()
        {
            _imgRepo = new Mock<IPropertyImageRepository>(MockBehavior.Strict);
            _propRepo = new Mock<IPropertyRepository>(MockBehavior.Strict);

            _ctrl = new PropertyImagesController(_imgRepo.Object, _propRepo.Object)
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
            _imgRepo.VerifyAll();
            _propRepo.VerifyAll();
        }

        [Test]
        public async Task List_should_return_404_when_property_not_found()
        {
            _propRepo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Property?)null);

            var result = await _ctrl.List(99, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
            ((ApiResponse<object>)((NotFoundObjectResult)result).Value!).Success.Should().BeFalse();
        }

        [Test]
        public async Task List_should_return_ok_with_items()
        {
            _propRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 1, Name = "Prop" });

            _imgRepo.Setup(r => r.ListByPropertyAsync(1, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<PropertyImage>
                    {
                    new() { Id = 10, IdProperty = 1, FileName = "a.png", ContentType = "image/png", Enabled = false }
                    });

            var result = await _ctrl.List(1, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<IEnumerable<PropertyImageResponse>>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Should().HaveCount(1);
            env.Data!.First().FileName.Should().Be("a.png");
        }

        [Test]
        public async Task Get_should_return_404_when_image_not_found_or_wrong_property()
        {            
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((PropertyImage?)null);

            var r1 = await _ctrl.Get(1, 5, includeBase64: false, CancellationToken.None);
            r1.Should().BeOfType<NotFoundObjectResult>();

            _imgRepo.Reset();
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 5, IdProperty = 2 });
            var r2 = await _ctrl.Get(1, 5, includeBase64: false, CancellationToken.None);
            r2.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task Get_should_return_ok_without_base64()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 7, IdProperty = 3, FileName = "f.jpg", ContentType = "image/jpeg", Enabled = true });

            var result = await _ctrl.Get(3, 7, includeBase64: false, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyImageResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(7);
            env.Data!.Enabled.Should().BeTrue();
            env.Data!.ContentType.Should().Be("image/jpeg");
        }

        [Test]
        public async Task Get_should_return_ok_with_base64_and_file_metadata()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 7, IdProperty = 3, Enabled = false });

            var bytes = Encoding.UTF8.GetBytes("img-bytes");
            _imgRepo.Setup(r => r.GetFileAsync(7, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((bytes, "x.png", "image/png"));

            var result = await _ctrl.Get(3, 7, includeBase64: true, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyImageWithBase64Response>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.FileName.Should().Be("x.png");
            env.Data!.ContentType.Should().Be("image/png");
            env.Data!.FileBase64.Should().Be(Convert.ToBase64String(bytes));
        }

        [Test]
        public async Task Get_should_return_404_when_includeBase64_and_file_missing()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 7, IdProperty = 3, Enabled = false });

            _imgRepo.Setup(r => r.GetFileAsync(7, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((bytes: (byte[]?)null, fileName: "", contentType: ""));

            var result = await _ctrl.Get(3, 7, includeBase64: true, CancellationToken.None);
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task Create_should_return_404_when_property_not_found()
        {
            _propRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Property?)null);

            var req = new PropertyImageUploadRequest();
            req.File = null!;
            req.Enabled = false;
            var result = await _ctrl.Create(9, req, CancellationToken.None);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task Create_should_return_400_when_file_missing()
        {
            _propRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 1 });

            var req = new PropertyImageUploadRequest();
            req.File = null!;
            req.Enabled = false;

            var result = await _ctrl.Create(1, req, CancellationToken.None);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task Create_should_add_and_return_created()
        {
            _propRepo.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Property { Id = 2, Name = "P" });

            var bytes = Encoding.UTF8.GetBytes("file");
            var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", "images.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
            var req = new PropertyImageUploadRequest();
            req.File = file;
            req.Enabled = true;

            _imgRepo.Setup(r => r.AddAsync(2,
                                           It.Is<byte[]>(b => b.SequenceEqual(bytes)),
                                           "images.png",
                                           "image/png",
                                           true,
                                           It.IsAny<CancellationToken>()))
                    .ReturnsAsync(100);

            _imgRepo.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 100, IdProperty = 2, FileName = "images.png", ContentType = "image/png", Enabled = true });

            var result = await _ctrl.Create(2, req, CancellationToken.None);

            result.Should().BeOfType<CreatedAtActionResult>();
            var env = (ApiResponse<PropertyImageResponse>)((CreatedAtActionResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Id.Should().Be(100);
            env.Data!.Enabled.Should().BeTrue();
        }

        [Test]
        public async Task ReplaceFile_should_return_404_when_image_not_found_or_wrong_property()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((PropertyImage?)null);

            var file = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("x")), 0, 1, "File", "x.png");
            var req = new PropertyImageReplaceFileRequest();
            req.File = file;

            var r1 = await _ctrl.ReplaceFile(1, 5, req, CancellationToken.None);
            r1.Should().BeOfType<NotFoundObjectResult>();

            _imgRepo.Reset();
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 5, IdProperty = 99 });

            var r2 = await _ctrl.ReplaceFile(1, 5, req, CancellationToken.None);
            r2.Should().BeOfType<NotFoundObjectResult>();
        }

        [Test]
        public async Task ReplaceFile_should_return_400_when_file_missing()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 5, IdProperty = 1 });

            var req = new PropertyImageReplaceFileRequest();
            req.File = null;
            var result = await _ctrl.ReplaceFile(1, 5, req, CancellationToken.None);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Test]
        public async Task ReplaceFile_should_update_and_return_ok()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 5, IdProperty = 1, FileName = "old.jpg", ContentType = "image/jpeg", Enabled = false });

            var bytes = Encoding.UTF8.GetBytes("new");
            var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", "new.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var req = new PropertyImageReplaceFileRequest();
            req.File = file;

            _imgRepo.Setup(r => r.ReplaceFileAsync(5,
                                                   It.Is<byte[]>(b => b.SequenceEqual(bytes)),
                                                   "new.png",
                                                   "image/png",
                                                   It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            _imgRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 5, IdProperty = 1, FileName = "new.png", ContentType = "image/png", Enabled = false });

            var result = await _ctrl.ReplaceFile(1, 5, req, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyImageResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.FileName.Should().Be("new.png");
            env.Data!.ContentType.Should().Be("image/png");
        }
        
        [Test]
        public async Task SetEnabled_should_update_flag_and_return_ok()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 9, IdProperty = 2, Enabled = false });

            _imgRepo.Setup(r => r.SetEnabledAsync(9, true, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            _imgRepo.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 9, IdProperty = 2, Enabled = true, FileName = "f.png", ContentType = "image/png" });

            var result = await _ctrl.SetEnabled(2, 9, new PropertyImageEnableRequest(true), CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<PropertyImageResponse>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
            env.Data!.Enabled.Should().BeTrue();
        }

        [Test]
        public async Task Delete_should_remove_and_return_ok()
        {
            _imgRepo.Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new PropertyImage { Id = 8, IdProperty = 4, Enabled = false });

            _imgRepo.Setup(r => r.DeleteAsync(8, It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            var result = await _ctrl.Delete(4, 8, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>();
            var env = (ApiResponse<object>)((OkObjectResult)result).Value!;
            env.Success.Should().BeTrue();
        }
    }
}
