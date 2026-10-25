using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;

namespace GZCTF.Middlewares;

/// <summary>
/// Localized API responses for antiforgery failures
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiAntiforgeryFailureAttribute : Attribute, IAlwaysRunResultFilter, IOrderedFilter
{
    public int Order => int.MinValue;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not IAntiforgeryValidationFailedResult)
            return;

        var localizer = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
        context.Result = RequestResponse.Result(localizer[nameof(Resources.Program.Auth_InvalidRequestToken)]);
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
