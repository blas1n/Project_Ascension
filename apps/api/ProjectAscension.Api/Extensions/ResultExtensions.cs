using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller) =>
        result.IsSuccess
            ? controller.Ok(result.Value)
            : result.Error.Code switch
            {
                "NOT_FOUND" => controller.NotFound(result.Error),
                "CONFLICT" => controller.Conflict(result.Error),
                "INVALID" => controller.BadRequest(result.Error),
                _ => controller.StatusCode(500, result.Error)
            };
}
