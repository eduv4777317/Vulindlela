using Microsoft.EntityFrameworkCore;
using Vulindlela.Booking.Api.Clients;
using Vulindlela.Booking.Api.Common;
using Vulindlela.Booking.Api.Data;
using Vulindlela.Booking.Api.Data.Repositories;
using Vulindlela.Booking.Api.Data.Seedings;
using Vulindlela.Booking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5101");

var bookingConnectionString = builder.Configuration.GetConnectionString("BookingDB")
    ?? throw new InvalidOperationException("Connection string 'BookingDB' is not configured.");

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddDbContext<BookingDBContext>(options =>
{
    options.UseSqlite(bookingConnectionString);
});
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingSeeder, BookingSeeder>();

var clearanceBaseUrl = builder.Configuration["Clearance:BaseUrl"]
    ?? throw new InvalidOperationException("Setting 'Clearance:BaseUrl' is not configured.");
var clearanceTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Clearance:TimeoutSeconds", 2));

builder.Services.AddHttpClient<IClearanceNotifier, ClearanceNotifier>(client =>
{
    client.BaseAddress = new Uri(clearanceBaseUrl);
    client.Timeout = clearanceTimeout;
});

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    BookingDBContext context = scope.ServiceProvider.GetRequiredService<BookingDBContext>();
    context.Database.EnsureCreated();

    IBookingSeeder seeder = scope.ServiceProvider.GetRequiredService<IBookingSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Booking API"));
}

app.UseAuthorization();

app.MapControllers();

app.Run();
