namespace Vulindlela.Portal.Mvc.Models
{
    public sealed record ApiResult<T>(bool Available, T? Value, int? StatusCode = null, string? Error = null)
    {
        public bool Succeeded => Available && Value is not null && StatusCode is >= 200 and < 300;

        public static ApiResult<T> Unavailable() => new(false, default);
    }

    public sealed record BranchDto(int Id, string Name);

    public sealed record SlotDto(int Id, int BranchId, DateTimeOffset StartsAt);

    public sealed record AppointmentDto(
        int Id,
        int SlotId,
        int? BranchId,
        DateTimeOffset? StartsAt,
        string CitizenId,
        string ServiceType,
        string Status);

    public sealed record BookAppointmentDto(int SlotId, string CitizenId, string ServiceType);

    public sealed record RequirementDto(string DocumentType, bool MustBeCertified);

    public sealed record ClearanceDto(
        int AppointmentId,
        string Status,
        List<string> Problems,
        List<RequirementDto> RequiredDocuments,
        DateTime? VerifiedAt);

    public sealed record SubmittedDocumentDto(string DocumentType, DateOnly? CertifiedOn, DateOnly? ExpiresOn);

    public sealed record SubmissionDto(int SubmissionId, int AppointmentId, string Status);

    public sealed record AccessLogEntryDto(DateTimeOffset AccessedAt, string AccessedBy, string AccessedByRole, string Reason);

    public sealed record ProblemDto(string? Title);
}
