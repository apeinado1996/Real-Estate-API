namespace PropertiesInformation.Api.DTOs
{
    public sealed record PropertyTraceCreateRequest(DateTime DateSale, string Name, decimal Value, decimal Tax);
    public sealed record PropertyTraceUpdateRequest(DateTime DateSale, string Name, decimal Value, decimal Tax);
    public sealed record PropertyTraceResponse(int Id, DateTime DateSale, string Name, decimal Value, decimal Tax, int IdProperty);
    public sealed record PropertyTraceSummaryResponse(int CountTraces, decimal TotalValue, decimal TotalTax);
}
