# Vulindlela

Every weekday, people queue outside National Civic Services Agency branches from before dawn, and about one in three reaches the counter only to be sent home for a missing, expired or out-of-date document. Vulindlela moves the two things that cause this to before the journey: a citizen books a real counter slot at a specific branch, and submits their supporting documents in advance so they find out before they travel whether anything is wrong. Every read of a citizen's record is logged, and the citizen can see who looked and why.

This is Phase 1: a booking service, a document clearance service, and a web portal, built with ASP.NET Core on .NET 10.

## Architecture

```
                         Citizen / Official (browser)
                                    |
                                    v
                    +-------------------------------+
                    |  Portal (ASP.NET Core MVC)    |
                    |  http://localhost:5100        |
                    |  sign-in, roles, policy,      |
                    |  composes both access logs    |
                    +-------------------------------+
                       |          portal.db          |
                       |    (accounts, PBKDF2 hashes) |
          HTTP + who/why headers               HTTP + who/why headers
                       |                             |
                       v                             v
       +---------------------------+     +-------------------------------+
       |  Booking API              |     |  Clearance API                |
       |  http://localhost:5101    |---->|  http://localhost:5102        |
       |  branches, slots,         | new |  document rules, submissions, |
       |  appointments, access log | appt|  background verification,     |
       +---------------------------+     |  access log                   |
                    |                    +-------------------------------+
               booking.db                               |
                                                  clearance.db
```

- **Three processes, three databases.** Booking and Clearance never read each other's tables. Booking tells Clearance about a new appointment over HTTP; if Clearance is down, the booking still succeeds (best effort, 2 second timeout).
- **Double booking is prevented by the database**, with a unique index on the slot that only counts active appointments. Two requests for the same slot at the same moment: exactly one wins.
- **Document checks run in the background.** A submission is queued (`System.Threading.Channels`) and a hosted worker verifies it, so the citizen gets `202 Accepted` immediately and the page updates by itself.
- **Each API logs reads of its own data.** The portal passes who is reading and why with every request, and the access log page merges both halves.

Each service is split into Controllers, Services (business rules) and Data (EF Core DbContext, repositories, seeding), with every dependency behind an interface. The reasoning behind the main decisions is in [`docs/adr`](docs/adr):

- [0001 Service boundary](docs/adr/0001-service-boundary.md)
- [0002 Preventing double booking](docs/adr/0002-prevent-double-booking.md)
- [0003 Best-effort clearance notification](docs/adr/0003-best-effort-clearance-notification.md)

## How to run it

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Nothing else: the databases are SQLite files that are created and seeded on first run.

```bash
git clone <this repository>
cd Vulindlela
dotnet build
```

Start the three services, each in its own terminal. Start the two APIs first:

```bash
dotnet run --project Vulindlela.Booking.Api      # http://localhost:5101
dotnet run --project Vulindlela.Clearance.Api    # http://localhost:5102
dotnet run --project Vulindlela.Portal.Mvc       # http://localhost:5100
```

Then open **http://localhost:5100**.

In Visual Studio, set all three projects as startup projects (Solution > Configure Startup Projects > Multiple startup projects).

| Service   | URL                     | Swagger UI (Development only)   |
|-----------|-------------------------|---------------------------------|
| Portal    | http://localhost:5100   |                                 |
| Booking   | http://localhost:5101   | http://localhost:5101/swagger   |
| Clearance | http://localhost:5102   | http://localhost:5102/swagger   |

Run the tests with:

```bash
dotnet test
```

### Configuration and environments

`dotnet run` and Visual Studio start every service in **Development**. The connection strings and the demo accounts live only in each project's `appsettings.Development.json`. The plain `appsettings.json` holds no connection strings and no accounts, so a production deployment starts with no demo data and must be given its connection strings as environment variables:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__BookingDB=...
ConnectionStrings__ClearanceDB=...
ConnectionStrings__PortalDB=...
```

A service started without its connection string stops immediately with a clear error rather than quietly creating a local file.

No connection string, key or password appears in any `.cs` file. The one exception is `legacy/LegacyAppointmentManager.cs`, the supplied BranchDesk example, which is kept for reference, is not part of any project, and shows exactly what this codebase avoids.

### If you pull a change that adds tables

The databases are created with `EnsureCreated`, which does nothing if the file already exists. After a schema change, stop the services and delete `booking.db`, `clearance.db` and `portal.db` (and their `-shm` and `-wal` files). They are recreated and reseeded on the next start.

## Demo accounts

Seeded in Development only. Sign in with the ID number (or `official`) as the username.

| Who            | Username        | Password             | Role     |
|----------------|-----------------|----------------------|----------|
| Thandi Mokoena | `9001015009086` | `Thandi-Demo-2026`   | Citizen  |
| Sipho Dlamini  | `8502205800086` | `Sipho-Demo-2026`    | Citizen  |
| Agency Official| `official`      | `Official-Demo-2026` | Official |

The ID numbers are made up but valid (correct date and check digit). You can also register a new citizen account with any valid South African ID number.

The Booking service seeds four branches (Johannesburg Central, Pretoria Central, Cape Town City Centre, Durban Central) with 30-minute slots from 08:00 to 15:30 on the next 15 working days.

## Things to try

1. **Two people, one slot.** Sign in as Thandi in one browser and as Sipho in a private window. Open the same branch, pick the same slot in both, then press *Book this slot* in both. One gets the booking; the other is told the slot has just been taken. `DoubleBookingTests` fires 50 simultaneous bookings at one slot and checks exactly one succeeds.
2. **Find out before you travel.** As Thandi, book a `PASSPORT` slot, go to *My appointments* and choose *Submit documents*. Untick the passport photo, or give the ID copy a certification date more than three months ago, and send. The status moves from Queued to Verifying to a list of problems by itself, without a refresh.
3. **Clearance down, booking still works.** Stop the Clearance API and book a slot. The booking succeeds. *My appointments* still loads and says document checks are unavailable instead of failing.
4. **Who looked at my file?** Sign in as `official`, open *Citizen records*, search for `9001015009086` and choose *Submit documents* on the appointment Thandi booked in step 2. Sign out, sign in as Thandi and open *Access log*: the official's reads are listed with the time, their name, their role, the reason and which service recorded it.
5. **A citizen sees only their own record.** Signed in as Thandi, visit `http://localhost:5100/Appointments?citizenId=8502205800086` or `http://localhost:5100/AccessLog?citizenId=8502205800086`. Both are refused. This is a policy that compares the ID on the request with the ID on the session, not a role check, because every citizen has the same role.
6. **Look at the APIs directly** in Swagger UI on ports 5101 and 5102.

## Known limitations

- **The APIs do not authenticate their callers.** They trust that requests come through the portal. Anyone who can reach port 5101 or 5102 directly bypasses sign-in and can also fake the "who/why" headers that feed the access log. Phase 1 runs on localhost; protecting the APIs (for example with tokens issued by the portal) is Phase 2.
- **The access log is not tamper-evident.** Entries are ordinary rows in each service's own database. They are not signed or chained, and the service that owns the data also owns its log. A separate audit service is the natural next step (ADR 0001).
- **Opening the access log page is not itself logged**, and the *My appointments* page refreshes every 3 seconds while documents are being verified, which adds a log entry each time.
- **Clearance notifications are not retried.** If Clearance is down when an appointment is booked, Clearance never learns about it and documents cannot be submitted for it. Cancellations are not passed to Clearance at all, so its copy can go stale (ADR 0003).
- **Document verification is simulated.** The citizen types the certification and expiry dates; no files are uploaded and nothing checks a real document. The scanner only adds a delay.
- **Registration only checks that an ID number is valid**, not that it belongs to the person registering. There is no email or phone verification, account lockout, password policy or password reset.
- **There is no way to create official accounts.** In Development one is seeded; production starts with none. Provisioning officials is Phase 2.
- **Citizens cannot cancel or reschedule in the portal.** The Booking API supports cancelling (`DELETE api/appointments/{id}`), but there is no page for it.
- **No migrations.** The schema is created with `EnsureCreated`, so any schema change means deleting the database files.
- **SQLite and a single instance of each service.** Fine for a demonstration; it is not built to run on several servers.
- **Local HTTP only.** Ports are fixed, there is no HTTPS, and the agency time zone is fixed at UTC+2.
- **The concurrency test is probabilistic.** It releases all requests at once but cannot force them to collide every run. `SecondActiveAppointmentForSameSlot_IsRefusedByTheDatabase` covers the guarantee deterministically.
- **Windows Smart App Control can block the test assembly.** If `dotnet test` reports "An Application Control policy has blocked this file", the tests did not run. Run them in WSL, on another machine, or with Smart App Control turned off.

## Project layout

```
Vulindlela.Booking.Api/      Branches, slots, appointments, booking access log
Vulindlela.Clearance.Api/    Document rules, submissions, verification worker, clearance access log
Vulindlela.Portal.Mvc/       Accounts, sign-in, authorisation policy, pages
Vulindlela.Tests/            xUnit tests for all three, against real in-memory SQLite
docs/adr/                    Architecture decision records
legacy/                      The supplied BranchDesk class (reference only)
SETUP.md                     Environment setup notes
```
