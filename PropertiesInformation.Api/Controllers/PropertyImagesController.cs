using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Interface;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/properties/{idProperty:int}/images")]
    [Authorize]
    public sealed class PropertyImagesController : ControllerBase
    {
        private readonly IPropertyImageRepository _repo;
        private readonly IPropertyRepository _propRepo;

        public PropertyImagesController(IPropertyImageRepository repo, IPropertyRepository propRepo)
        {
            _repo = repo;
            _propRepo = propRepo;
        }

        /// <summary>
        /// List Property Images
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> List(int idProperty, CancellationToken ct)
        {
            var prop = await _propRepo.GetByIdAsync(idProperty, ct);
            if (prop is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            var items = await _repo.ListByPropertyAsync(idProperty, ct);
            var dto = items.Select(i => new PropertyImageResponse(i.Id, i.IdProperty, i.FileName, i.ContentType, i.Enabled));
            return Ok(ApiResponse<IEnumerable<PropertyImageResponse>>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Get property images
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="imageId"></param>
        /// <param name="includeBase64"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("{imageId:int}")]
        public async Task<IActionResult> Get(int idProperty, int imageId, [FromQuery] bool includeBase64, CancellationToken ct)
        {
            var meta = await _repo.GetByIdAsync(imageId, ct);
            if (meta is null || meta.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property image not found.", HttpContext));

            if (!includeBase64)
            {
                var r = new PropertyImageResponse(meta.Id, meta.IdProperty, meta.FileName, meta.ContentType, meta.Enabled);
                return Ok(ApiResponse<PropertyImageResponse>.Ok(r, HttpContext));
            }

            var (bytes, fileName, contentType) = await _repo.GetFileAsync(imageId, ct);
            if (bytes is null)
                return NotFound(ApiResponse<object>.Fail(404, "File not found.", HttpContext));

            var base64 = Convert.ToBase64String(bytes);
            var r64 = new PropertyImageWithBase64Response(meta.Id, meta.IdProperty, fileName, contentType, meta.Enabled, base64);
            return Ok(ApiResponse<PropertyImageWithBase64Response>.Ok(r64, HttpContext));
        }

        /// <summary>
        /// Create property Images
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(int idProperty, [FromForm] PropertyImageUploadRequest req, CancellationToken ct)
        {
            var prop = await _propRepo.GetByIdAsync(idProperty, ct);
            if (prop is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            if (req.File is null || req.File.Length == 0)
                return BadRequest(ApiResponse<object>.Fail(400, "File is required.", HttpContext));

            using var ms = new MemoryStream();
            await req.File.CopyToAsync(ms, ct);
            var newId = await _repo.AddAsync(idProperty, ms.ToArray(), req.File.FileName, req.File.ContentType, req.Enabled, ct);

            var meta = await _repo.GetByIdAsync(newId, ct)!;
            var dto = new PropertyImageResponse(meta!.Id, meta.IdProperty, meta.FileName, meta.ContentType, meta.Enabled);
            return CreatedAtAction(nameof(Get), new { idProperty, imageId = newId }, ApiResponse<PropertyImageResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Update Property images
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="imageId"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{imageId:int}/file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceFile(int idProperty, int imageId, [FromForm] PropertyImageReplaceFileRequest req, CancellationToken ct)
        {
            var meta = await _repo.GetByIdAsync(imageId, ct);
            if (meta is null || meta.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property image not found.", HttpContext));

            if (req.File is null || req.File.Length == 0)
                return BadRequest(ApiResponse<object>.Fail(400, "File is required.", HttpContext));

            using var ms = new MemoryStream();
            await req.File.CopyToAsync(ms, ct);
            await _repo.ReplaceFileAsync(imageId, ms.ToArray(), req.File.FileName, req.File.ContentType, ct);

            var updated = await _repo.GetByIdAsync(imageId, ct)!;
            var dto = new PropertyImageResponse(updated!.Id, updated.IdProperty, updated.FileName, updated.ContentType, updated.Enabled);
            return Ok(ApiResponse<PropertyImageResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Enable property images
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="imageId"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{imageId:int}/enable")]
        [Consumes("application/json")]
        public async Task<IActionResult> SetEnabled(int idProperty, int imageId, [FromBody] PropertyImageEnableRequest req, CancellationToken ct)
        {
            var meta = await _repo.GetByIdAsync(imageId, ct);
            if (meta is null || meta.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property image not found.", HttpContext));

            await _repo.SetEnabledAsync(imageId, req.Enabled, ct);

            var updated = await _repo.GetByIdAsync(imageId, ct)!;
            var dto = new PropertyImageResponse(updated!.Id, updated.IdProperty, updated.FileName, updated.ContentType, updated.Enabled);
            return Ok(ApiResponse<PropertyImageResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Delete property image
        /// </summary>
        /// <param name="idProperty"></param>
        /// <param name="imageId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpDelete("{imageId:int}")]
        public async Task<IActionResult> Delete(int idProperty, int imageId, CancellationToken ct)
        {
            var meta = await _repo.GetByIdAsync(imageId, ct);
            if (meta is null || meta.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property image not found.", HttpContext));

            await _repo.DeleteAsync(imageId, ct);
            return Ok(ApiResponse<object>.Ok(new { id = imageId, deleted = true }, HttpContext));
        }
    }
}
