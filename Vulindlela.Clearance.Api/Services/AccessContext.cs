namespace Vulindlela.Clearance.Api.Services
{
    public sealed record AccessContext(string AccessedBy, string AccessedByRole, string Reason);
}
