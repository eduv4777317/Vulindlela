using Vulindlela.Clearance.Api.Models;

namespace Vulindlela.Clearance.Api.Services
{
    public enum SubmitOutcome
    {
        Queued,
        AppointmentNotFound
    }

    public sealed record SubmitResult(SubmitOutcome Outcome, Submission? Submission = null);

    public sealed record DocumentInput(string DocumentType, DateOnly? CertifiedOn, DateOnly? ExpiresOn);

    public sealed record ClearanceStatus(
        int AppointmentId,
        bool AppointmentKnown,
        Submission? LatestSubmission,
        IReadOnlyList<DocumentRequirement> Requirements);
}
