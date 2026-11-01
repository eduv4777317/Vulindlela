# ADR 0002: Prevent double booking with a database unique index

- Status: Accepted
- Date: 2026-11-01
- Deciders: [your name]

## Context

A slot has a capacity of exactly one. When booking opens, many citizens request the same popular slot in the same second. The obvious implementation checks whether the slot is taken and then inserts the appointment. Two requests can both pass the check before either inserts, so both proceed. The legacy `BookAppointment` method has the same shape (`SELECT COUNT(*)`, then `INSERT`, in separate statements with no constraint).

The guarantee has to hold whatever the application code does, including under load and across more than one running instance.

## Decision

Make the database refuse the second booking.

- `Appointments` has a unique index on `SlotId`, filtered to active appointments only, so a cancelled appointment releases its slot. In EF Core this is `HasIndex(a => a.SlotId).IsUnique().HasFilter(...)`, with `Status` stored as text so the filter is readable.
- The booking code inserts directly and does not rely on a prior check for correctness. A pre-check may be kept only to give a friendlier message in the common case.
- A unique violation is recognised by the SQLite error code (`SqliteErrorCode` 19 with extended code 2067), never by matching message text, and is translated into a domain result `SlotAlreadyTaken`, which the controller returns as `409 Conflict`.
- The concurrency test (Deliverable 2.4) fires many simultaneous requests at one slot against a real SQLite database and asserts that exactly one appointment exists.

## Alternatives considered and rejected

1. Check, then insert inside a transaction. A transaction does not close the window by itself. Two transactions can both read "free" before either writes, and on SQLite the loser may instead fail with a lock error, which is a different bug.
2. An in-process lock (`lock` or `SemaphoreSlim`). It only protects a single process. A second instance, or a second branch deployment, is not covered, and a single global lock would serialise unrelated bookings.
3. Optimistic concurrency with a version token on the slot row. It works, but it needs a version column maintained by hand on SQLite, a retry policy, and it makes every unrelated update to the slot row a possible conflict. More moving parts than the problem needs.

## Consequences

Benefits: the guarantee is enforced where the data lives, so it holds for every code path, including ones written later by someone who has not read this ADR.

Costs accepted:

- The rule lives in the schema, not in the business logic layer. A reader of the service code will not see it, so it needs a comment at the call site and the concurrency test to protect it.
- The guarantee is only as real as the test database. Testing against a fake repository that ignores constraints would pass while the real system fails, so the tests must use a real SQLite database (see the fixture in `SETUP.md`).
- A unique index cannot express a capacity greater than one. If slots ever hold several citizens, this design is replaced by an atomic conditional update on a counter.
- Losing requests pay for a failed insert and an exception. That is acceptable at this volume, but it is exception-driven control flow.
- The filtered index and the `Status` conversion have to stay in step. Forgetting the filter means cancelled appointments hold slots forever.
