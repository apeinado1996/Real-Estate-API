namespace PropertiesInformation.Api.Class
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public ApiError? Error { get; init; }
        public string TraceId { get; init; } = string.Empty;

        public static ApiResponse<T> Ok(T data, HttpContext http) =>
            new() { Success = true, Data = data, TraceId = http.TraceIdentifier };

        public static ApiResponse<T> Fail(int code, string message, HttpContext http, object? details = null) =>
            new()
            {
                Success = false,
                Error = new ApiError { Code = code, Message = message, Details = details },
                TraceId = http.TraceIdentifier
            };
    }

    public sealed class ApiError
    {
        public int Code { get; init; }
        public string Message { get; init; } = string.Empty;
        public object? Details { get; init; }
    }
}
