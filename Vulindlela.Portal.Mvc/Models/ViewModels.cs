namespace Vulindlela.Portal.Mvc.Models
{
    public sealed class BookingIndexViewModel
    {
        public bool BookingUnavailable { get; init; }
        public List<BranchDto> Branches { get; init; } = new();
        public BranchDto? SelectedBranch { get; init; }
        public List<IGrouping<DateOnly, SlotDto>> SlotsByDay { get; init; } = new();
    }

    public sealed class BookViewModel
    {
        public int SlotId { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = "";
        public DateTimeOffset StartsAt { get; set; }
        public string ServiceType { get; set; } = "";
        public List<string> ServiceTypes { get; set; } = new();
        public string? Error { get; set; }
        public bool SlotTaken { get; set; }
    }

    public sealed class ConfirmationViewModel
    {
        public AppointmentDto Appointment { get; init; } = default!;
        public string BranchName { get; init; } = "";
    }

    public sealed class AppointmentRow
    {
        public AppointmentDto Appointment { get; init; } = default!;
        public string BranchName { get; init; } = "";
        public ClearanceDto? Clearance { get; init; }
    }

    public sealed class MyAppointmentsViewModel
    {
        public bool BookingUnavailable { get; init; }
        public bool ClearanceUnavailable { get; init; }
        public List<AppointmentRow> Rows { get; init; } = new();
        public bool AnyVerificationInProgress => Rows.Any(r => r.Clearance?.Status is "Queued" or "Verifying");
    }

    public sealed class DocumentRow
    {
        public string DocumentType { get; set; } = "";
        public bool MustBeCertified { get; set; }
        public bool Include { get; set; } = true;
        public DateOnly? CertifiedOn { get; set; }
        public DateOnly? ExpiresOn { get; set; }
    }

    public sealed class DocumentsViewModel
    {
        public int AppointmentId { get; set; }
        public bool ClearanceUnavailable { get; set; }
        public string? Error { get; set; }
        public List<DocumentRow> Rows { get; set; } = new();
    }
}
