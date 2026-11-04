# ADR 0001: Use SQLite rather than SQL Server for Phase 1

> **This is a worked example showing the expected standard.**
> It covers a decision you are *not* asked to make, so that you can see the
> shape of a good ADR without being handed one of your answers.

- **Status:** Accepted
- **Date:** Phase 1, week 1

## Context

Each service in Phase 1 must own its own database. Phase 1 is built and marked
on laptops, and demonstrated on a laptop, with no guarantee of a database server
or an internet connection in the room.

The Agency's production estate runs SQL Server, so a case exists for developing
against it from the start.

## Decision

Each service uses SQLite through EF Core, with one `.db` file per service.

## Alternatives considered

**SQL Server LocalDB.** Rejected. It matches production more closely, which is a
genuine advantage. It is also Windows-only, which excludes anyone on macOS or
Linux, and it makes "clone the repository and run it" a multi-step install
rather than one command. The mismatch is manageable because EF Core is the
abstraction: the provider is one line in `Program.cs`.

**PostgreSQL in Docker.** Rejected for Phase 1. It is closest to how the target
architecture would really run, and it requires Docker, an image pull and a
running container before a marker sees anything. That is a poor trade for a
six-week project.

**In-memory storage with no database.** Rejected. It removes the ability to
demonstrate a unique index, a transaction or anything else the database is
responsible for, and everything is lost on restart.

## Consequences

**What we gain.** Cloning and running takes one command. One file per service
makes "no shared database" a physical fact: you can see three files. SQLite is
fully ACID, so the unique index that guarantees no double booking is a real
guarantee.

**What we accept.** SQLite handles one writer at a time, so it would not carry
the Agency's real write volume. Some SQL Server features are unavailable,
notably `rowversion` for optimistic concurrency without a hand-written trigger.
Phase 2 would move to SQL Server or PostgreSQL, changing the provider line and
the connection string.

## Evidence

`dotnet run` on a clean clone produces a working system with seeded data in
under thirty seconds, on Windows, macOS and Linux.
