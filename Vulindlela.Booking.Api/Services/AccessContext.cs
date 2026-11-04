namespace Vulindlela.Booking.Api.Services
{
    public sealed record AccessContext(string AccessedBy, string AccessedByRole, string Reason);
}
