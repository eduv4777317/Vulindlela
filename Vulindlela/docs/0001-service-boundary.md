# ADR 0001: Split Booking and Clearance into two services

- Status: Accepted
- Date: 2026-11-01
- Deciders: [your name]

## Context

Phase 1 of Vulindlela lets a citizen book a counter slot and submit supporting documents before travelling. The Agency has set three constraints:

1. Booking and document clearance are separate services with separate databases, and neither may read the other's tables.
2. A failure in document clearance must not stop a citizen booking.
3. Every read of a citizen's record must be logged, and the citizen must be able to see that log.

The two concerns behave differently. Booking is a short transaction that must be atomic (see ADR 0002). Document verification is slow (OCR, a population register lookup, a virus scan) and can fail on its own.

## Decision

Two ASP.NET Core Web APIs, each with its own SQLite file, plus an MVC portal that composes the citizen view.

- The Booking service owns branches, slots, appointments, citizen accounts, and the access log for reads of booking data.
- The Clearance service owns the document requirements per service type, submissions, verification outcomes, a minimal copy of each appointment it has been told about (appointment id, opaque citizen id, service type, appointment date), and the access log for reads of its own data.
- Booking tells Clearance about a new appointment with one HTTP call carrying exactly that data. Clearance never calls Booking.
- The portal calls both services at read time and composes the result. If one service is unavailable it shows what it has and says plainly what is missing.
- No shared database, connection string or table access crosses the boundary.

## Alternatives considered and rejected

1. One service with internal modules. Simpler to build, but the two parts would share a process and a fate. Slow or crashing verification work could take booking down, which breaks constraint 2, and it does not satisfy the requirement for separate databases.
2. Four or more services (separate Citizen, Notification and Audit services). Rejected for Phase 1. Each extra service adds a deployment, a failure mode and a network hop, and no stated constraint needs it. An Audit service is the strongest Phase 2 candidate.
3. Clearance reading Booking's database, or calling Booking for appointment details at submission time. The first is forbidden and couples the two schemas. The second makes Clearance depend on Booking being available and adds a call to every submission.

## Consequences

Benefits: booking keeps working when Clearance is down; the slow work is isolated from the atomic work; each service can change its schema without coordinating with the other.

Costs accepted:

- Clearance holds a copy of appointment facts. The copy can go stale, for example if an appointment is cancelled and Clearance is never told.
- Booking and Clearance can disagree about which appointments exist until the notification succeeds (see ADR 0003).
- A citizen's access log is split across two databases, so the portal has to compose it and degrade gracefully when one half is unavailable.
- Citizen accounts live in the Booking database for Phase 1, which makes Booking the de facto identity authority. This is the weakest placement in the design, and moving it later is a data migration.
- Two deployables to run, configure and test instead of one.
