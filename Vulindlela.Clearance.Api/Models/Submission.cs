namespace Vulindlela.Clearance.Api.Models
{
    public enum SubmissionStatus
    {
        Queued = 0,
        Verifying = 1,
        Cleared = 2,
        MissingDocuments = 3
    }

    public class Submission
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Queued;
        public DateTime SubmittedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string Problems { get; set; } = "";
        public List<SubmittedDocument> Documents { get; set; } = new();
    }

    public class SubmittedDocument
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string DocumentType { get; set; } = "";
        public DateOnly? CertifiedOn { get; set; }
        public DateOnly? ExpiresOn { get; set; }
    }
}
