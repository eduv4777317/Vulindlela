# ADR 0001: Split Booking and Clearance into two services that never read each other's data

- **Status:** Accepted
- **Date:** 2026-11-01

## Context

Phase 1 lets a citizen book a counter slot and submit supporting documents before travelling. The Agency has set three constraints: booking and document clearance are separate services with separate databases and neither may read the other's tables; a failure in clearance must not stop a citizen booking; and every read of a citizen's record must be logged and visible to that citizen.

The two concerns behave differently. Booking is a short transaction that must be atomic, because one citizen holds one slot (see ADR 0002). Verification is slow, since in production it means OCR, a population register lookup and a virus scan, and it can fail on its own. The legacy `BookAppointment` method does all of this in one place, which is why one failing step can lose or corrupt the others.

## Decision

We build two ASP.NET Core Web APIs, `Vulindlela.Booking.Api` and `Vulindlela.Clearance.Api`, each with its own SQLite file, and an MVC portal, `Vulindlela.Portal.Mvc`, that composes the citizen view. Each runs as its own process on its own port, so either can be stopped without the other.

The Booking service owns branches, slots, appointments, citizen accounts, and the access log for reads of booking data. The Clearance service owns the document requirements per service type, submissions, verification outcomes, a minimal copy of each appointment it has been told about (appointment id, citizen id, service type, appointment time), and the access log for reads of its own data.

Booking tells Clearance about a new appointment with one HTTP call carrying exactly that data. Clearance never calls Booking. The portal calls both services at read time and composes the result. No database, connection string or table is shared across the boundary.

Each API publishes its contract at `/openapi/v1.json` and, in Development only, a Swagger UI page at `/swagger`, which is where each API opens when started. The page comes from `Swashbuckle.AspNetCore.SwaggerUI`, a package beyond the prescribed stack. It is added because the boundary is only useful if people can see it: a marker or a developer can browse every endpoint of each service and call it from the browser, without writing JSON requests by hand.

## Alternatives considered

**One service with internal modules.** Rejected. It is simpler to build, but the modules share a process and a fate. A hung OCR call or a crash in verification takes booking down with it, which breaks the Agency's second constraint, and one service cannot satisfy the requirement for separate databases.

**Four or more services (separate Citizen, Notification and Audit services).** Rejected for Phase 1. Each adds a deployment, a failure mode and a network hop, and no stated constraint needs them. An Audit service is the strongest candidate for Phase 2, because keeping the log apart from the data owner is the Tallinn idea.

**Clearance reads Booking's database, or calls Booking for appointment details when a document arrives.** Rejected. The first is forbidden and couples the two schemas. The second makes Clearance depend on Booking being up and adds a call to every submission, which is exactly the chatter this boundary is meant to avoid.

## Consequences

**What we gain.** Booking keeps working when Clearance is down. The slow, failure-prone work is isolated from the atomic work. Each service can change its schema without coordinating with the other, and "no shared database" is a physical fact you can see as two `.db` files.

**What we accept.** Clearance holds a copy of appointment facts, and that copy can go stale, for example if an appointment is cancelled and Clearance is never told. Booking and Clearance can disagree about which appointments exist until the notification succeeds (ADR 0003). A citizen's access log is split across two databases, so the portal has to compose it and degrade gracefully when one half is unavailable. Citizen accounts live in the Booking database for Phase 1, which makes Booking the de facto identity authority; this is the weakest placement in the design and moving it later is a data migration. We run, configure and test three deployables instead of one. Finally, each API carries one extra package for the Swagger UI page, which has to be kept up to date; it is limited to Development and only renders the description that `AddOpenApi` already produces, so removing it later changes one line and one package reference per service.
