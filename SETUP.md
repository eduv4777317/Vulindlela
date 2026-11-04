# Setup

## What you need

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 8.0 or later | The only mandatory install |
| An editor | Visual Studio 2022, VS Code | **Visual Studio 2019 cannot build .NET 8** |
| Git | Any recent version | |
| A GitHub account | | A private repository is fine |

You do **not** need SQL Server, Docker, RabbitMQ or a cloud subscription. Every
requirement in this assignment is met with the SDK alone.

## Check your install

```bash
dotnet --version
```

If that prints `8.` or higher, you are ready.

## Creating the solution

This assignment requires **two services and a portal**, not one service. The
Document Clearance service in Deliverable 3.1 is a separate Web API with its
own database, so it is a separate project.

```bash
mkdir vulindlela && cd vulindlela
git init
dotnet new sln -n Vulindlela

dotnet new webapi   -o src/Vulindlela.Booking.Api
dotnet new webapi   -o src/Vulindlela.Clearance.Api
dotnet new mvc      -o src/Vulindlela.Portal
dotnet new classlib -o src/Vulindlela.Shared
dotnet new xunit    -o tests/Vulindlela.Booking.Tests

dotnet sln add src/Vulindlela.Booking.Api
dotnet sln add src/Vulindlela.Clearance.Api
dotnet sln add src/Vulindlela.Portal
dotnet sln add src/Vulindlela.Shared
dotnet sln add tests/Vulindlela.Booking.Tests
```

`Vulindlela.Shared` is a plain class library holding the DTOs that cross a
service boundary. It is optional — you may instead declare the DTOs twice, once
in each service. Sharing them is tidier; duplicating them is more honest about
the boundary. Either is acceptable, and either is worth writing up in an ADR.

Wire up the references you need:

```bash
dotnet add src/Vulindlela.Booking.Api   reference src/Vulindlela.Shared
dotnet add src/Vulindlela.Clearance.Api reference src/Vulindlela.Shared
dotnet add src/Vulindlela.Portal        reference src/Vulindlela.Shared
dotnet add tests/Vulindlela.Booking.Tests reference src/Vulindlela.Booking.Api
```

> **The Booking service must never reference the Clearance service's database
> or its DbContext.** Sharing DTOs is fine. Sharing a database is the thing
> Deliverable 3.1 exists to test.

## Packages you will need

Every project that owns a database needs the EF Core SQLite provider. That is
three of your projects, plus the test project, which runs against a real SQLite
database held in memory:

```bash
dotnet add src/Vulindlela.Booking.Api     package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/Vulindlela.Clearance.Api   package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/Vulindlela.Portal          package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/Vulindlela.Booking.Tests package Microsoft.EntityFrameworkCore.Sqlite
```

Beyond that, `BackgroundService`, `Channel<T>`, cookie authentication,
policy-based authorisation and PBKDF2 hashing are all in the framework already.

## Creating the database

You do **not** need EF Core migrations or the `dotnet-ef` tool. In `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
    context.Database.EnsureCreated();
}
```

`EnsureCreated()` builds the schema from your model on first run. Delete the
`.db` file to start over.

Each service writes its `.db` file into **its own project folder**, because
that is the content root of the running service — for example
`src/Vulindlela.Booking.Api/booking.db`. That is where to look for it, and
where to delete it from.

## Testing against a real database

Your concurrency test must run against a real SQLite database, not a fake
repository. Give EF the **connection string**, not a `SqliteConnection` object:

```csharp
string databaseName = "vulindlela-tests-" + Guid.NewGuid().ToString("N");
string connectionString = $"Data Source={databaseName};Mode=Memory;Cache=Shared";

// Held open only to keep the in-memory database alive.
var keepAlive = new SqliteConnection(connectionString);
keepAlive.Open();

var options = new DbContextOptionsBuilder<YourDbContext>()
    .UseSqlite(connectionString)
    .Options;
```

A `SqliteConnection` is **not thread-safe**. If you hand the same connection
object to every `DbContext` and then fire concurrent requests at it, the
requests collide inside the SQLite driver and the test fails for a reason that
has nothing to do with your booking logic. Passing the connection string means
each context opens its own connection to the same shared in-memory database.

## Running two services at once

Give each project a fixed port in its `Program.cs`:

```csharp
builder.WebHost.UseUrls("http://localhost:5101");
```

Then run each in its own terminal. Fixed ports mean your marker never has to
guess a URL, and your README can state them.

## Committing

Commit the `.gitignore` first, before anything else. If you commit `bin/` or a
`.db` file by mistake, remove it and say so in your reflection. Every developer
has done it.

Commit messages: `Add slot repository`, not `stuff`. Your commit history is
assessed and an employer will read it.
