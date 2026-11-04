using Vulindlela.Clearance.Api.Models;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Clearance.Api.Contracts
{
    public sealed record RequirementResponse(string DocumentType, bool MustBeCertified);

    public sealed record SubmissionResponse(int SubmissionId, int AppointmentId, string Status);

    public sealed record ClearanceResponse(
        int AppointmentId,
        string Status,
        IReadOnlyList<string> Problems,
        IReadOnlyList<RequirementResponse> RequiredDocuments,
        DateTime? VerifiedAt);

    public sealed record AccessLogEntryResponse(DateTimeOffset AccessedAt, string AccessedBy, string AccessedByRole, string Reason);

    public static class ResponseMapping
    {
        public static RequirementResponse ToResponse(this DocumentRequirement requirement) =>
            new(requirement.DocumentType, requirement.MustBeCertified);

        public static SubmissionResponse ToResponse(this Submission submission) =>
            new(submission.Id, submission.AppointmentId, submission.Status.ToString());

        public static AccessLogEntryResponse ToResponse(this AccessLogEntry entry) =>
            new(new DateTimeOffset(DateTime.SpecifyKind(entry.AccessedAt, DateTimeKind.Utc)),
                entry.AccessedBy, entry.AccessedByRole, entry.Reason);

        public static ClearanceResponse ToResponse(this ClearanceStatus status)
        {
            var submission = status.LatestSubmission;
            var state = !status.AppointmentKnown ? "UnknownAppointment"
                : submission is null ? "NotSubmitted"
                : submission.Status.ToString();

            var problems = submission is null || submission.Problems.Length == 0
                ? Array.Empty<string>()
                : submission.Problems.Split('|');

            return new ClearanceResponse(
                status.AppointmentId,
                state,
                problems,
                status.Requirements.Select(r => r.ToResponse()).ToList(),
                submission?.VerifiedAt);
        }
    }
}
