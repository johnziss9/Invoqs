using Invoqs.Components;
using Invoqs.Interfaces;
using Invoqs.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("InvoiceAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5126/api/");
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