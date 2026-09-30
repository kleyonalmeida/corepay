using BuildingBlocks.Results;

namespace Core.Application.Admin;

public static class GuidIdValidator
{
    public static Result ValidateGuidString(string? id, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure(Error.Validation(
                "admin.invalid_id",
                $"{fieldName} is required."));
        }

        if (!Guid.TryParse(id, out _))
        {
            return Result.Failure(Error.Validation(
                "admin.invalid_id",
                $"{fieldName} must be a valid GUID."));
        }

        return Result.Success();
    }
}
