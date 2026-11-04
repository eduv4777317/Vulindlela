using Vulindlela.Portal.Mvc.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5100");

var bookingBaseUrl = builder.Configuration["BookingApi:BaseUrl"]
    ?? throw new InvalidOperationException("Setting 'BookingApi:BaseUrl' is not configured.");
var clearanceBaseUrl = builder.Configuration["ClearanceApi:BaseUrl"]
    ?? throw new InvalidOperationException("Setting 'ClearanceApi:BaseUrl' is not configured.");

builder.Services.AddControllersWithViews();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
