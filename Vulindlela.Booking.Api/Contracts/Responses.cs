using Vulindlela.Booking.Api.Common;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Booking.Api.Contracts
{
    public sealed record BranchResponse(int Id, string Name);

    public sealed record SlotResponse(int Id, int BranchId, DateTimeOffset StartsAt);

    public sealed record AppointmentResponse(int Id, int SlotId, int? BranchId, DateTimeOffset? StartsAt, string CitizenId, string ServiceType, string Status);

    public sealed record AccessLogEntryResponse(DateTimeOffset AccessedAt, string AccessedBy, string AccessedByRole, string Reason);

    public static class ResponseMapping
    {
        public static BranchResponse ToResponse(this Branch branch) =>
            new(branch.Id, branch.Name);

        public static SlotResponse ToResponse(this Slot slot) =>
            new(slot.Id, slot.BranchId, Utility.ToLocal(slot.StartsAt));

        public static AppointmentResponse ToResponse(this Appointment appointment, Slot? slot) =>
            new(appointment.Id, appointment.SlotId, slot?.BranchId,
                slot is null ? null : Utility.ToLocal(slot.StartsAt),
                appointment.CitizenId, appointment.ServiceType, appointment.Status.ToString());

        public static AccessLogEntryResponse ToResponse(this AccessLogEntry entry) =>
            new(Utility.ToLocal(entry.AccessedAt), entry.AccessedBy, entry.AccessedByRole, entry.Reason);
    }
}
