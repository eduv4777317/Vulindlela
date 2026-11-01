# ADR 0003: Notify Clearance on a best-effort basis and record the outcome

- Status: Accepted
- Date: 2026-11-01
- Deciders: [your name]

## Context

When an appointment is created, the Booking service must tell the Clearance service, which needs it to accept document submissions. The Agency's constraint is that a failure in clearance must not stop a citizen booking. The legacy code shows the opposite mistake: its SMS and export steps sit in empty `catch` blocks, so failures disappear and nobody learns that a booking was lost.

## Decision

- Booking commits the appointment first, then calls Clearance through `IClearanceNotifier`, a typed `HttpClient` behind an interface, so the booking logic depends on the interface and not on HTTP.
- The call has a short timeout, read from configuration. `HttpRequestException` and timeout exceptions are caught at that one boundary and never reach the citizen.
- The outcome is recorded. Appointments carry a `ClearanceNotifiedAt` value, null until Clearance has acknowledged. A failure is logged with the appointment id.
- The booking response succeeds either way. The portal's composed view shows clearance status as unavailable when it cannot be fetched.
- Clearance accepts the notification idempotently, keyed on the appointment id, so a repeated notification does nothing harmful.

## Alternatives considered and rejected

1. Block the booking until Clearance acknowledges. This directly violates the Agency constraint and makes Clearance a single point of failure for booking.
2. Fire and forget, with exceptions caught and ignored. This is the legacy pattern. It satisfies "booking keeps working" but destroys the record that a notification is owed.
3. A message broker with an outbox. This is the correct long-term answer, but it needs infrastructure the Phase 1 stack deliberately excludes.
4. A circuit-breaker library such as Polly. It would stop repeated slow calls during a sustained outage, but it is an extra package that needs its own justification, and a timeout is enough for Phase 1.

## Consequences

Benefits: booking is isolated from Clearance, and no failure is silent. The flag makes "owed but not yet delivered" a queryable fact.

Costs accepted:

- While Clearance is down, each booking waits up to the timeout before the response returns. Moving the call to a background queue would remove that delay but adds machinery.
- An appointment can exist that Clearance has never heard of. Until it is retried, that citizen cannot submit documents against it.
- The flag is only useful if something retries on it. A sweep of appointments where `ClearanceNotifiedAt` is null is the minimum, and if it is not built it must be stated under Known Limitations.
- There is a dual-write window: Booking can commit and then crash before it notifies. The flag covers this, but only because it is written in the same transaction as the appointment.
- Clearance has to implement idempotent handling, which is extra code on the receiving side.
