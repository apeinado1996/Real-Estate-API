using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertiesInformation.Api.Class;
using PropertiesInformation.Api.DTOs;
using PropertiesInformation.Core.Entities;
using PropertiesInformation.Core.Interface;

namespace PropertiesInformation.Api.Controllers
{
    [ApiController]
    [Route("api/properties/{idProperty:int}/traces")]
    [Authorize]
    public sealed class PropertyTracesController : ControllerBase
    {
        private readonly IPropertyTraceRepository _repo;
        private readonly IPropertyRepository _propRepo;

        public PropertyTracesController(IPropertyTraceRepository repo, IPropertyRepository propRepo)
        {
            _repo = repo;
            _propRepo = propRepo;
        }

        // LIST
        [HttpGet]
        public async Task<IActionResult> List(int idProperty, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        {
            var prop = await _propRepo.GetByIdAsync(idProperty, ct);
            if (prop is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            var items = await _repo.ListByPropertyAsync(idProperty, fromDate, toDate, ct);
            var dto = items.Select(t => new PropertyTraceResponse(t.Id, t.DateSale, t.Name, t.Value, t.Tax, t.IdProperty));
            return Ok(ApiResponse<IEnumerable<PropertyTraceResponse>>.Ok(dto, HttpContext));
        }

        // GET BY ID
        [HttpGet("{traceId:int}")]
        public async Task<IActionResult> Get(int idProperty, int traceId, CancellationToken ct)
        {
            var t = await _repo.GetByIdAsync(traceId, ct);
            if (t is null || t.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property trace not found.", HttpContext));

            var dto = new PropertyTraceResponse(t.Id, t.DateSale, t.Name, t.Value, t.Tax, t.IdProperty);
            return Ok(ApiResponse<PropertyTraceResponse>.Ok(dto, HttpContext));
        }

        // CREATE
        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Create(int idProperty, [FromBody] PropertyTraceCreateRequest req, CancellationToken ct)
        {
            var prop = await _propRepo.GetByIdAsync(idProperty, ct);
            if (prop is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));
            if (req.Value < 0 || req.Tax < 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Value and Tax must be non-negative.", HttpContext));

            var id = await _repo.AddAsync(new PropertyTrace
            {
                IdProperty = idProperty,
                DateSale = req.DateSale.Date,
                Name = req.Name.Trim(),
                Value = req.Value,
                Tax = req.Tax
            }, ct);

            var created = await _repo.GetByIdAsync(id, ct);
            var dto = new PropertyTraceResponse(created!.Id, created.DateSale, created.Name, created.Value, created.Tax, created.IdProperty);
            return CreatedAtAction(nameof(Get), new { idProperty, traceId = id }, ApiResponse<PropertyTraceResponse>.Ok(dto, HttpContext));
        }

        // UPDATE
        [HttpPut("{traceId:int}")]
        [Consumes("application/json")]
        public async Task<IActionResult> Update(int idProperty, int traceId, [FromBody] PropertyTraceUpdateRequest req, CancellationToken ct)
        {
            var t = await _repo.GetByIdAsync(traceId, ct);
            if (t is null || t.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property trace not found.", HttpContext));

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(ApiResponse<object>.Fail(400, "Name is required.", HttpContext));
            if (req.Value < 0 || req.Tax < 0)
                return BadRequest(ApiResponse<object>.Fail(400, "Value and Tax must be non-negative.", HttpContext));

            t.DateSale = req.DateSale.Date;
            t.Name = req.Name.Trim();
            t.Value = req.Value;
            t.Tax = req.Tax;

            await _repo.UpdateAsync(t, ct);

            var updated = await _repo.GetByIdAsync(traceId, ct);
            var dto = new PropertyTraceResponse(updated!.Id, updated.DateSale, updated.Name, updated.Value, updated.Tax, updated.IdProperty);
            return Ok(ApiResponse<PropertyTraceResponse>.Ok(dto, HttpContext));
        }

        // DELETE
        [HttpDelete("{traceId:int}")]
        public async Task<IActionResult> Delete(int idProperty, int traceId, CancellationToken ct)
        {
            var t = await _repo.GetByIdAsync(traceId, ct);
            if (t is null || t.IdProperty != idProperty)
                return NotFound(ApiResponse<object>.Fail(404, "Property trace not found.", HttpContext));

            await _repo.DeleteAsync(traceId, ct);
            return Ok(ApiResponse<object>.Ok(new { id = traceId, deleted = true }, HttpContext));
        }

        // SUMMARY
        [HttpGet("summary")]
        public async Task<IActionResult> Summary(int idProperty, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        {
            var prop = await _propRepo.GetByIdAsync(idProperty, ct);
            if (prop is null)
                return NotFound(ApiResponse<object>.Fail(404, "Property not found.", HttpContext));

            var s = await _repo.SummaryByPropertyAsync(idProperty, fromDate, toDate, ct);
            var dto = new PropertyTraceSummaryResponse(s.CountTraces, s.TotalValue, s.TotalTax);
            return Ok(ApiResponse<PropertyTraceSummaryResponse>.Ok(dto, HttpContext));
        }
    }
}
