using Vulindlela.Booking.Api.Clients;
using Vulindlela.Booking.Api.Models;

namespace Vulindlela.Tests.Booking
{
    public sealed class FakeClearanceNotifier : IClearanceNotifier
    {
        private readonly bool _succeeds;

        public FakeClearanceNotifier(bool succeeds)
        {
            _succeeds = succeeds;
        }

        public List<int> NotifiedAppointmentIds { get; } = new();

        public Task<bool> NotifyAppointmentCreatedAsync(Appointment appointment, Slot slot)
        {
            NotifiedAppointmentIds.Add(appointment.Id);
            return Task.FromResult(_succeeds);
        }
    }
}
