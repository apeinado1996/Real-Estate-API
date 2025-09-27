using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/owners")]
    [Authorize]
    public sealed class OwnersController : ControllerBase
    {
        private readonly IOwnerRepository _repo;
        public OwnersController(IOwnerRepository repo) => _repo = repo;

        /// <summary>
        /// List Ownes
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var items = await _repo.ListAsync(ct);
            var data = items.Select(o => new OwnerResponse(o.Id, o.Name, o.Address, o.Birthday));
            return Ok(ApiResponse<IEnumerable<OwnerResponse>>.Ok(data, HttpContext));
        }

        /// <summary>
        /// Get owner by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var o = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            if (o is null)
                return NotFound(ApiResponse<object>.Fail(404, "Owner not found.", HttpContext));

            var dto = new OwnerResponse(o.Id, o.Name, o.Address, o.Birthday);
            return Ok(ApiResponse<OwnerResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Create owner
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("form")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] OwnerCreateRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));

            byte[]? photoBytes = null;
            if (req.Photo is not null && req.Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await req.Photo.CopyToAsync(ms, ct);
                photoBytes = ms.ToArray();
            }

            var id = await _repo.AddAsync(new Owner
            {
                Name = req.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim(),
                Birthday = req.Birthday?.Date,
                Photo = photoBytes
            }, ct);

            var created = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            var dto = new OwnerResponse(created!.Id, created.Name, created.Address, created.Birthday);
            return CreatedAtAction(nameof(Get), new { id }, ApiResponse<OwnerResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Update Owner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] OwnerUpdateRequest req, CancellationToken ct)
        {
            var o = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            if (o is null)
                return NotFound(ApiResponse<object>.Fail(404, "Owner not found.", HttpContext));

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));

            o.Name = req.Name.Trim();
            o.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
            o.Birthday = req.Birthday?.Date;

            await _repo.UpdateAsync(o, ct);

            var updated = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            var dto = new OwnerResponse(updated!.Id, updated.Name, updated.Address, updated.Birthday);
            return Ok(ApiResponse<OwnerResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// update Photo
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePhoto(int id, [FromForm] OwnerFileUploadRequest req, CancellationToken ct)
        {
            var o = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            if (o is null)
                return NotFound(ApiResponse<object>.Fail(404, "Owner not found.", HttpContext));

            if (req.Photo is null || req.Photo.Length == 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Photo file is required.", HttpContext));

            using var ms = new MemoryStream();
            await req.Photo.CopyToAsync(ms, ct);
            await _repo.UpdatePhotoAsync(id, ms.ToArray(), ct);

            return Ok(ApiResponse<object>.Ok(new { id, photoUpdated = true }, HttpContext));
        }

        /// <summary>
        /// Delete Owner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var o = await _repo.GetByIdAsync(id, includePhoto: false, ct);
            if (o is null)
                return NotFound(ApiResponse<object>.Fail(404, "Owner not found.", HttpContext));

            await _repo.DeleteAsync(id, ct);
            return Ok(ApiResponse<object>.Ok(new { id, deleted = true }, HttpContext));
        }
    }
}
