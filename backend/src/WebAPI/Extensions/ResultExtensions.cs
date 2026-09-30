using BuildingBlocks.Results;

namespace WebAPI.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.Error!.Category switch
        {
            ErrorCategory.Validation => Results.BadRequest(new { error = result.Error.Code, message = result.Error.Message }),
            ErrorCategory.NotFound => Results.NotFound(new { error = result.Error.Code, message = result.Error.Message }),
            ErrorCategory.Conflict => Results.Conflict(new { error = result.Error.Code, message = result.Error.Message }),
            ErrorCategory.Unauthorized => Results.Unauthorized(),
            ErrorCategory.Forbidden => Results.Json(
                new { error = result.Error.Code, message = result.Error.Message },
                statusCode: StatusCodes.Status403Forbidden),
            ErrorCategory.UnprocessableEntity => Results.Json(
                new { error = result.Error.Code, message = result.Error.Message },
                statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Results.Problem(result.Error.Message)
        };
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsFailure)
        {
            return ((Result)result).ToHttpResult();
        }

        return onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value);
    }

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory)
    {
        if (result.IsFailure)
        {
            return result.ToHttpResult();
        }

        return Results.Created(locationFactory(result.Value), result.Value);
    }
}
