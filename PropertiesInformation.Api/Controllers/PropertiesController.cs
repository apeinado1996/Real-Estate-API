using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/properties")]
    [Authorize]
    public sealed class PropertiesController : ControllerBase
    {
        private readonly IPropertyRepository _repo;
        public PropertiesController(IPropertyRepository repo) => _repo = repo;

        /// <summary>
        /// Lis Properties
        /// </summary>
        /// <param name="codeInternal"></param>
        /// <param name="ownerId"></param>
        /// <param name="minPrice"></param>
        /// <param name="maxPrice"></param>
        /// <param name="address"></param>
        /// <param name="year"></param>
        /// <param name="name"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? codeInternal, [FromQuery] int? ownerId, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, string? address, int? year, string? name, CancellationToken ct)
        {
            var items = await _repo.ListAsync(codeInternal, ownerId, minPrice, maxPrice, address, year, name, ct);
            var data = items.Select(p => new PropertyResponse(p.Id, p.Name, p.Address, p.Price, p.CodeInternal, p.Year, p.IdOwner));
            return Ok(ApiResponse<IEnumerable<PropertyResponse>>.Ok(data, HttpContext));
        }

        /// <summary>
        /// Get Property by Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            var dto = new PropertyResponse(p.Id, p.Name, p.Address, p.Price, p.CodeInternal, p.Year, p.IdOwner);
            return Ok(ApiResponse<PropertyResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Create Property
        /// </summary>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Create([FromBody] PropertyCreateRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));
            if (string.IsNullOrWhiteSpace(req.CodeInternal))
                return BadRequest(ApiResponse<object>.Fail(400, "CodeInternal is required.", HttpContext));
            if (req.Price < 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Price must be non-negative.", HttpContext));

            var id = await _repo.AddAsync(new Property
            {
                Name = req.Name.Trim(),
                Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim(),
                Price = req.Price,
                CodeInternal = req.CodeInternal.Trim(),
                Year = req.Year,
                IdOwner = req.IdOwner
            }, ct);

            var created = await _repo.GetByIdAsync(id, ct);
            var dto = new PropertyResponse(created!.Id, created.Name, created.Address, created.Price, created.CodeInternal, created.Year, created.IdOwner);
            return CreatedAtAction(nameof(Get), new { id }, ApiResponse<PropertyResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Update Property
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update(int id, [FromBody] PropertyUpdateRequest req, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));
            if (req.Price < 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Price must be non-negative.", HttpContext));

            p.Name = req.Name.Trim();
            p.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
            p.Price = req.Price;
            p.Year = req.Year;
            p.IdOwner = req.IdOwner;

            await _repo.UpdateAsync(p, ct);

            var updated = await _repo.GetByIdAsync(id, ct);
            var dto = new PropertyResponse(updated!.Id, updated.Name, updated.Address, updated.Price, updated.CodeInternal, updated.Year, updated.IdOwner);
            return Ok(ApiResponse<PropertyResponse>.Ok(dto, HttpContext));
        }

        /// <summary>
        /// Delete Property
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            await _repo.DeleteAsync(id, ct);
            return Ok(ApiResponse<object>.Ok(new { id, deleted = true }, HttpContext));
        }

        
        /// <summary>
        /// Update price
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{id:int}/price")]
        [Consumes("application/json")]
        public async Task<IActionResult> ChangePrice(int id, [FromBody] PropertyChangePriceRequest req, CancellationToken ct)
        {
            if (req.NewPrice < 0)
                return BadRequest(ApiResponse<object>.Fail(400, "NewPrice must be non-negative.", HttpContext));

            var p = await _repo.GetByIdAsync(id, ct);
            if (p is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            var traceName = string.IsNullOrWhiteSpace(req.TraceName) ? "PRICE UPDATE" : req.TraceName!.Trim();
            var traceTax = req.TraceTax ?? 0m;

            await _repo.ChangePriceAsync(id, req.NewPrice, traceName, traceTax, ct);

            var updated = await _repo.GetByIdAsync(id, ct);
            var dto = new PropertyResponse(updated!.Id, updated.Name, updated.Address, updated.Price, updated.CodeInternal, updated.Year, updated.IdOwner);
            return Ok(ApiResponse<PropertyResponse>.Ok(dto, HttpContext));
        }
    }
}
