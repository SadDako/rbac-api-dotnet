using Microsoft.AspNetCore.Mvc;

namespace Rbac.Api.Infrastructure.Http;

public static class ControllerProblemExtensions
{
    public static ObjectResult ToApiProblem(this ControllerBase controller, int statusCode, string code, string message)
    {
        var payload = ApiProblemDetailsFactory.Create(controller.HttpContext, statusCode, code, message);
        var result = new ObjectResult(payload)
        {
            StatusCode = statusCode
        };

        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
