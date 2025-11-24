using AsciiSite.Client.Components;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<AsciiArtOptions>(builder.Configuration.GetSection(AsciiArtOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AsciiArtOptions>, AsciiArtOptionsValidator>();
builder.Services.AddScoped<IAsciiArtProvider, AsciiArtProvider>();
builder.Services.AddScoped<IAboutContentProvider, FileSystemAboutContentProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
