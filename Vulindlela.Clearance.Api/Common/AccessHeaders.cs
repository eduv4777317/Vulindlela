using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Common
{
    public static class AccessHeaders
    {
        public const string AccessedBy = "X-Accessed-By";
        public const string AccessedByRole = "X-Accessed-By-Role";
        public const string Reason = "X-Access-Reason";

        public static AccessContext FromRequest(HttpRequest request) =>
            new(Read(request, AccessedBy, "Unknown caller"),
                Read(request, AccessedByRole, "Unknown"),
                Read(request, Reason, "No reason given"));

        private static string Read(HttpRequest request, string header, string fallback)
        {
            var value = Uri.UnescapeDataString(request.Headers[header].ToString()).Trim();
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
    }
}
