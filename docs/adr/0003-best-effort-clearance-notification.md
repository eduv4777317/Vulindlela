# ADR 0003: Tell Clearance about new appointments on a best-effort basis and record the outcome

- **Status:** Accepted
- **Date:** 2026-11-01

## Context

When an appointment is created, Booking must tell Clearance, which needs that fact before it can accept document submissions. The Agency's constraint is that a failure in clearance must not stop a citizen booking.

The legacy code shows the opposite mistake. Its SMS and export steps sit in empty `catch` blocks, so failures disappear, and the code's own comment admits that a locked export file means head office never learns about the booking. Surviving a failure is not enough; a failure that leaves no trace is a lost booking nobody can find.

## Decision

`BookingService` inserts the appointment first, then calls Clearance through `IClearanceNotifier`. The implementation, `ClearanceNotifier`, is an `HttpClient` registered as a typed client, so the booking logic depends on the interface and not on HTTP. The base address and a short timeout (2 seconds) are read from `appsettings.json`. `HttpRequestException` and timeouts are caught in the notifier, logged with the appointment id, and returned as `false`; they never reach the citizen.

The outcome is recorded: each appointment carries a `ClearanceNotifiedAt` value. It is empty when the appointment is inserted and is set only once Clearance has accepted the notification. The booking response succeeds either way, and the portal shows clearance status as unavailable when it cannot be fetched. Clearance accepts the notification idempotently, keyed on the appointment id, so a repeated notification creates nothing new.

```csharp
if (await _clearanceNotifier.NotifyAppointmentCreatedAsync(appointment, slot))
{
    appointment.ClearanceNotifiedAt = DateTime.UtcNow;
    await _bookingRepository.SaveChangesAsync();
}

return BookingResult.Booked(appointment);
```

## Alternatives considered

**Block the booking until Clearance acknowledges.** Rejected. It breaks the Agency's second constraint outright and makes Clearance a single point of failure for booking, which is the March incident again.

**Fire and forget, with exceptions caught and ignored.** Rejected. It keeps booking working, but it is the legacy pattern: it destroys the record that a notification is owed, so a lost notification is invisible until a citizen is turned away.

**A message broker with an outbox.** Rejected for Phase 1. It is the right long-term answer, but it needs infrastructure the Phase 1 stack deliberately excludes. The notifier sits behind an interface so that only one class would change.

**A circuit-breaker library such as Polly.** Rejected for Phase 1. It would stop repeated slow calls during a sustained outage, but it is an extra package that needs its own justification, and a timeout is enough at this scale.

## Consequences

**What we gain.** Booking is isolated from Clearance, and no failure is silent. The flag turns "owed but not yet delivered" into something we can query.

**What we accept.** While Clearance is down, each booking waits up to the 2-second timeout before the response returns; moving the call to a background queue would remove that delay but adds machinery. An appointment can exist that Clearance has never heard of, and until it is sent again that citizen cannot submit documents against it. The flag only helps if something retries on it; Phase 1 does not build that retry, so it is stated under Known Limitations in the README. If Booking crashes after inserting the appointment but before notifying, the empty flag still marks the appointment as not yet sent. Clearance has to handle repeated notifications, which is extra code on the receiving side.

## Evidence

`ClearanceFailureTests` in `Vulindlela.Tests/Booking`:

- `Booking_StillSucceeds_WhenClearanceCannotBeReached` books with a notifier that fails, and asserts the booking succeeds and `ClearanceNotifiedAt` stays empty.
- `Booking_IsSentToClearance_AndMarkedAsNotified` asserts the flag is set when Clearance accepts.
- `Notifier_ReturnsFalse_InsteadOfThrowing_WhenClearanceIsStopped` points the real `ClearanceNotifier` at an address where nothing is listening.

The report shows the same behaviour with the Clearance service stopped: the booking is confirmed, and My Appointments shows that document checks are unavailable instead of an error page.
