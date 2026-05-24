using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using RewardSystem.Data;
using System.Globalization;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddDbContext<RewardSystemDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsHistoryTable("__EFMigrationsHistory", "belek_reward_system")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Giris";
        options.LogoutPath = "/Account/Cikis";
        options.AccessDeniedPath = "/Account/Giris";
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<RewardSystem.Services.IEmailService, RewardSystem.Services.EmailService>();

var app = builder.Build();

// Supported cultures
var supportedCultures = new[] {
    new CultureInfo("tr"),
    new CultureInfo("en")
};

app.UseRequestLocalization(new RequestLocalizationOptions {
    DefaultRequestCulture     = new RequestCulture("tr"),
    SupportedCultures         = supportedCultures,
    SupportedUICultures       = supportedCultures,
    RequestCultureProviders   = new List<IRequestCultureProvider> {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Language switch endpoint
app.MapGet("/Language/Set/{culture}", (string culture, string returnUrl, HttpContext ctx) => {
    var cookieOptions = new CookieOptions {
        Expires  = DateTimeOffset.UtcNow.AddYears(1),
        IsEssential = true,
        SameSite = SameSiteMode.Lax
    };
    ctx.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        cookieOptions
    );
    return Results.Redirect(returnUrl ?? "/");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Giris}/{id?}");

app.Run();
