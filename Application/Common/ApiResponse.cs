namespace SamanMobileInsurance.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public PaginationMeta? Pagination { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null, PaginationMeta? pagination = null) =>
        new() { Success = true, Data = data, Message = message, Pagination = pagination };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse OkMessage(string? message = null) =>
        new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}

public class PaginationMeta
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public int TotalPages { get; init; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public PaginationMeta Pagination { get; init; } = new();
}
