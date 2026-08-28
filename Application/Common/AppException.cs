namespace SamanMobileInsurance.Application.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string> Errors { get; }

    public AppException(string message, int statusCode = 400, IReadOnlyList<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? [message];
    }
}

public class ValidationAppException : AppException
{
    public ValidationAppException(string message, IReadOnlyList<string>? errors = null)
        : base(message, 400, errors) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409) { }
}

public class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "دسترسی غیرمجاز است.") : base(message, 401) { }
}

public class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string message = "شما اجازه انجام این عملیات را ندارید.") : base(message, 403) { }
}

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message, 422) { }
}
