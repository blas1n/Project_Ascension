namespace ProjectAscension.Shared;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NotFound = new("NOT_FOUND", "Resource not found.");
    public static readonly Error Conflict = new("CONFLICT", "Resource already exists.");
    public static readonly Error Invalid = new("INVALID", "Invalid request.");
}
