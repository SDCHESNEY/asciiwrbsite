using AsciiSite.Client.Components;
using AsciiSite.Shared.Blog;
using AsciiSite.Client.Services;
using AsciiSite.Shared.Configuration;
using AsciiSite.Shared.Content;
using AsciiSite.Shared.GitHub;
using AsciiSite.Shared.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMemoryCache();

builder.Services.Configure<AsciiArtOptions>(builder.Configuration.GetSection(AsciiArtOptions.SectionName));
builder.Services.AddSingleton<IValidateOptions<AsciiArtOptions>, AsciiArtOptionsValidator>();
builder.Services.AddScoped<IAsciiArtProvider, AsciiArtProvider>();
builder.Services.AddScoped<IAboutContentProvider, FileSystemAboutContentProvider>();
builder.Services.AddSingleton<IBlogPostProvider, FileSystemBlogPostProvider>();
builder.Services.Configure<GitHubRepoOptions>(builder.Configuration.GetSection(GitHubRepoOptions.SectionName));
builder.Services.Configure<LocalizationOptions>(builder.Configuration.GetSection(LocalizationOptions.SectionName));
builder.Services.AddHttpClient<IGitHubRepoService, GitHubRepoService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AsciiSite.Client/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
});
builder.Services.AddScoped<ILocalizationProvider, LocalizationProvider>();
builder.Services.AddScoped<PreferencesStore>();
builder.Services.AddScoped<ThemeManager>();
builder.Services.AddScoped<LocalizationState>();

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

public partial class Program;
