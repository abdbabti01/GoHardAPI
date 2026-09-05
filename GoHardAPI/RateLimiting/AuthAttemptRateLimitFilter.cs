using GoHardAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoHardAPI.RateLimiting
{
    /// <summary>
    /// Endpoint-metadata marker: this action is an anonymous authentication attempt
    /// (login / signup). The app-wide <c>GlobalLimiter</c> keys such a request by the
    /// anonymous (socket-IP) aggregate ceiling even when it carries a bearer token, so
    /// an attacker holding one real account cannot use the authenticated per-user
    /// ceiling to probe rotating victim identities.
    /// </summary>
    public interface IAuthAttemptEndpoint
    {
    }

    /// <summary>
    /// Marks a login / signup action for the per-credential-identity attempt limiter.
    /// <paramref name="scope"/> is <see cref="AuthAttemptIdentity.LoginScope"/> or
    /// <see cref="AuthAttemptIdentity.SignupScope"/> — the two get independent partitions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AuthAttemptRateLimitAttribute : TypeFilterAttribute, IAuthAttemptEndpoint
    {
        public AuthAttemptRateLimitAttribute(string scope)
            : base(typeof(AuthAttemptRateLimitFilter))
        {
            Arguments = new object[] { scope };
        }
    }

    /// <summary>
    /// Runs <b>after</b> MVC model binding (so it reads the bound DTO, not the raw
    /// body), acquires one <see cref="AuthAttemptRateLimiter"/> permit for the
    /// submitted identity's partition, and only then invokes the action.
    ///
    /// <list type="bullet">
    ///   <item>Admitted: the action runs exactly once; the lease is disposed after.</item>
    ///   <item>Rejected: the action never runs; the response is a bare <c>429</c>
    ///     (<see cref="EmptyResult"/> + status code, no body) — the shape the old
    ///     <c>auth</c> limiter returned.</item>
    ///   <item>Cancelled (client aborted while queued): <see cref="OperationCanceledException"/>
    ///     propagates and the action never runs.</item>
    /// </list>
    /// </summary>
    public sealed class AuthAttemptRateLimitFilter : IAsyncActionFilter
    {
        private readonly string _scope;
        private readonly AuthAttemptRateLimiter _limiter;

        public AuthAttemptRateLimitFilter(string scope, AuthAttemptRateLimiter limiter)
        {
            _scope = scope;
            _limiter = limiter;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var partitionKey = AuthAttemptIdentity.PartitionKey(_scope, ExtractEmail(context));

            using var lease = await _limiter.AcquireAsync(partitionKey, context.HttpContext.RequestAborted);

            if (!lease.IsAcquired)
            {
                // Bare 429 — the shape the old middleware-level "auth" limiter returned.
                // An EmptyResult (not StatusCodeResult) so [ApiController]'s client-error
                // mapping does not turn it into a ProblemDetails body.
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Result = new EmptyResult();
                return;
            }

            await next();
        }

        /// <summary>The email from the bound login / signup DTO, or <c>null</c> if none is present.</summary>
        private static string? ExtractEmail(ActionExecutingContext context)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                switch (argument)
                {
                    case LoginRequest login:
                        return login.Email;
                    case SignupRequest signup:
                        return signup.Email;
                }
            }

            return null;
        }
    }
}
