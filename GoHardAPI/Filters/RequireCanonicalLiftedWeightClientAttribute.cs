using GoHardAPI.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace GoHardAPI.Filters
{
    /// <summary>
    /// Rejects lifted-weight writes from clients that do not send
    /// X-Lifted-Weight-Unit: kg while the guard is enabled. Old builds sent
    /// numbers typed under an "lbs" label; they must never be stored as kg.
    /// </summary>
    public sealed class RequireCanonicalLiftedWeightClientAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var options = context.HttpContext.RequestServices
                .GetRequiredService<IOptionsMonitor<LiftedWeightOptions>>().CurrentValue;
            if (!options.GuardEnabled) return;

            var declared = context.HttpContext.Request.Headers[LiftedWeightOptions.HeaderName].ToString();
            if (string.Equals(declared, LiftedWeightOptions.CanonicalUnit, StringComparison.OrdinalIgnoreCase)) return;

            context.Result = new BadRequestObjectResult(new
            {
                code = "LIFTED_WEIGHT_UNIT_REQUIRED",
                message = "Please update GoHard to keep logging workouts.",
            });
        }
    }
}
