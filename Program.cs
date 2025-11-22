using Invoqs.Components;
using Invoqs.Interfaces;
using Invoqs.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Invoqs Blazor");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Invoqs.Blazor"));

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    var apiBaseUrl = builder.Configuration["API_BASE_URL"] ?? "http://localhost:5126";
    builder.Services.AddHttpClient("InvoiceAPI", client =>
    {
        client.BaseAddress = new Uri($"{apiBaseUrl}/api/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // Scoped HTTP client for dependency injection
    builder.Services.AddScoped(sp =>
    {
        var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
        return clientFactory.CreateClient("InvoiceAPI");
    });

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ICustomerService, CustomerService>();
    builder.Services.AddScoped<IJobService, JobService>();
    builder.Services.AddScoped<IInvoiceService, InvoiceService>();
    builder.Services.AddScoped<IReceiptService, ReceiptService>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStaticFiles();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}