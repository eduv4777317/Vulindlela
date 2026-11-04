using Microsoft.AspNetCore.Authorization;

namespace Vulindlela.Portal.Mvc.Security
{
    public sealed class CitizenRecordAccessHandler : AuthorizationHandler<CitizenRecordAccessRequirement, string>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CitizenRecordAccessRequirement requirement,
            string requestedCitizenId)
        {
            if (context.User.IsInRole(Roles.Official))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var sessionCitizenId = context.User.GetCitizenId();
            if (!string.IsNullOrEmpty(sessionCitizenId) && sessionCitizenId == requestedCitizenId)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
