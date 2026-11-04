using System.Security.Claims;

namespace Vulindlela.Portal.Mvc.Security
{
    public static class PortalClaims
    {
        public const string CitizenId = "citizen_id";

        public static string? GetCitizenId(this ClaimsPrincipal user) => user.FindFirstValue(CitizenId);
    }
}
