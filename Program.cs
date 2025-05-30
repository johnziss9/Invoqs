using Invoqs.Components;
using Invoqs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("InvoiceAPI", client =>
{
    client.BaseAddress = new Uri("https://localhost:7001/api/"); // TODO Update with API URL later
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Scoped HTTP client for dependency injection
builder.Services.AddScoped(sp =>
{
    var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return clientFactory.CreateClient("InvoiceAPI");
});

builder.Services.AddScoped<ICustomerService, MockCustomerService>();
builder.Services.AddScoped<IJobService, MockJobService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();