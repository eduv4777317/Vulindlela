# ADR 0002: Prevent double booking with a unique index in the database

- **Status:** Accepted
- **Date:** 2026-11-01

## Context

A slot has a capacity of exactly one. When booking opens, many citizens request the same popular slot in the same second. The obvious implementation checks whether the slot is taken and then inserts the appointment. Two requests can both pass the check before either inserts, so both proceed.

The legacy `BookAppointment` has the same shape: a `SELECT COUNT(*)`, then an `INSERT`, in separate statements with no transaction and no constraint. The guarantee here has to hold whatever the application code does, under load, and across more than one running instance.

## Decision

We make the database refuse the second booking. `Appointments` has a unique index on `SlotId`, filtered to active appointments so that a cancelled appointment releases its slot. In `BookingDBContext`:

```csharp
appointment.HasIndex(a => a.SlotId)
    .IsUnique()
    .HasFilter($"\"Status\" = {(int)AppointmentStatus.Active}");
```

`Status` is stored as its integer value. The filter is built from `AppointmentStatus.Active` rather than a bare number, and the enum values are pinned (`Active = 0`, `Cancelled = 1`), so the filter and the enum cannot drift apart.

The booking code does not rely on a prior check for correctness. `BookingService` keeps an "is it taken?" check only to give a friendlier answer in the common case. `BookingRepository.InsertAppointmentAsync` inserts and saves in one step, recognises a unique violation by the SQLite error code (`SqliteErrorCode` 19, extended code 2067), never by matching message text, and returns `SlotAlreadyTaken`. The controller returns that as `409 Conflict`.

## Alternatives considered

**Check, then insert, inside a transaction.** Rejected. A transaction does not close the window by itself: two transactions can both read "free" before either writes. On SQLite the loser may instead fail with a lock error, which is a different bug rather than a fix.

**An in-process lock (`lock` or `SemaphoreSlim`).** Rejected. It only protects one process, and the target architecture runs several instances, so a second instance is not covered. A single global lock would also serialise unrelated bookings, and a lock per slot needs its own cleanup.

**Optimistic concurrency with a version token on the slot row.** Rejected. It works, but SQLite has no `rowversion`, so the column has to be maintained by hand, and it needs a retry policy. Every unrelated update to the slot row also becomes a possible conflict. That is more moving parts than the problem needs.

## Consequences

**What we gain.** The guarantee is enforced where the data lives, so it holds for every code path, including ones written later by someone who has not read this ADR.

**What we accept.** The rule lives in the schema, not in the business logic layer, so a reader of the service code will not see it. It needs a comment at the call site and the concurrency test to protect it. The guarantee is only as real as the test database: a fake repository that ignores constraints would pass while the real system fails, so the tests run against real SQLite. A unique index cannot express a capacity above one, so if slots ever hold several citizens this design is replaced by an atomic conditional update on a counter. Losing requests pay for a failed insert and an exception, which is acceptable at this volume but is exception-driven control flow. Finally, the filter depends on the stored number for `Active`: if someone reorders the enum or removes the pinned values, the index silently counts the wrong rows, and the `Status` column shows 0 and 1 rather than words when you inspect `booking.db`.

## Evidence

`DoubleBookingTests` in `tests/Vulindlela.Booking.Tests` runs against a real in-memory SQLite database:

- `ConcurrentBookingsForOneSlot_ExactlyOneSucceeds` releases 2, 20 and 50 simultaneous bookings at one slot and asserts that exactly one succeeds and exactly one active appointment exists.
- `SecondActiveAppointmentForSameSlot_IsRefusedByTheDatabase` skips the service's check and inserts through the repository twice, so only the unique index can stop the second insert.
- `CancelledAppointment_ReleasesTheSlot` fails if the index loses its "active only" filter.
