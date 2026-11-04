using System.ComponentModel.DataAnnotations;

namespace Vulindlela.Clearance.Api.Contracts
{
    public sealed class AppointmentCreatedRequest
    {
        [Required]
        public int? AppointmentId { get; init; }

        [Required]
        public string? CitizenId { get; init; }

        [Required]
        public string? ServiceType { get; init; }

        [Required]
        public DateTime? StartsAt { get; init; }
    }

    public sealed class SubmitDocumentsRequest
    {
        [Required]
        public List<SubmittedDocumentRequest>? Documents { get; init; }
    }

    public sealed class SubmittedDocumentRequest
    {
        [Required]
        public string? DocumentType { get; init; }

        public DateOnly? CertifiedOn { get; init; }

        public DateOnly? ExpiresOn { get; init; }
    }
}
