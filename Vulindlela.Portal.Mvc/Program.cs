using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Portal.Mvc.Clients;
using Vulindlela.Portal.Mvc.Data;
using Vulindlela.Portal.Mvc.Data.Repositories;
using Vulindlela.Portal.Mvc.Data.Seedings;
using Vulindlela.Portal.Mvc.Security;
using Vulindlela.Portal.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5100");

var bookingBaseUrl = builder.Configuration["BookingApi:BaseUrl"]
    ?? throw new InvalidOperationException("Setting 'BookingApi:BaseUrl' is not configured.");
var clearanceBaseUrl = builder.Configuration["ClearanceApi:BaseUrl"]
    ?? throw new InvalidOperationException("Setting 'ClearanceApi:BaseUrl' is not configured.");
var portalConnectionString = builder.Configuration.GetConnectionString("PortalDB")
    ?? throw new InvalidOperationException("Connection string 'PortalDB' is not configured.");

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<PortalDBContext>(options =>
{
    options.UseSqlite(portalConnectionString);
});
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPortalSeeder, PortalSeeder>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/SignIn";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.CanViewCitizenRecord, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CitizenRecordAccessRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, CitizenRecordAccessHandler>();

builder.Services.AddHttpClient<IBookingApiClient, BookingApiClient>(client =>
{
    client.BaseAddress = new Uri(bookingBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("BookingApi:TimeoutSeconds", 5));
});
builder.Services.AddHttpClient<IClearanceApiClient, ClearanceApiClient>(client =>
{
    client.BaseAddress = new Uri(clearanceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("ClearanceApi:TimeoutSeconds", 2));
});

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    PortalDBContext context = scope.ServiceProvider.GetRequiredService<PortalDBContext>();
    context.Database.EnsureCreated();

    IPortalSeeder seeder = scope.ServiceProvider.GetRequiredService<IPortalSeeder>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
