using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Data;
using Vulindlela.Clearance.Api.Data.Repositories;
using Vulindlela.Clearance.Api.Data.Seedings;
using Vulindlela.Clearance.Api.Queue;
using Vulindlela.Clearance.Api.Services;
using Vulindlela.Clearance.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5102");

var clearanceConnectionString = builder.Configuration.GetConnectionString("ClearanceDB")
    ?? throw new InvalidOperationException("Connection string 'ClearanceDB' is not configured.");
var simulatedDelay = TimeSpan.FromSeconds(builder.Configuration.GetValue("Verification:SimulatedDelaySeconds", 3));

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<ClearanceDBContext>(options =>
{
    options.UseSqlite(clearanceConnectionString);
});
builder.Services.AddScoped<IClearanceRepository, ClearanceRepository>();
builder.Services.AddScoped<IClearanceService, ClearanceService>();
builder.Services.AddScoped<IClearanceSeeder, ClearanceSeeder>();
builder.Services.AddSingleton<IVerificationQueue, ChannelVerificationQueue>();
builder.Services.AddSingleton<IDocumentScanner>(new SimulatedDocumentScanner(simulatedDelay));
builder.Services.AddHostedService<VerificationWorker>();

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    ClearanceDBContext context = scope.ServiceProvider.GetRequiredService<ClearanceDBContext>();
    context.Database.EnsureCreated();

    IClearanceSeeder seeder = scope.ServiceProvider.GetRequiredService<IClearanceSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Clearance API"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
