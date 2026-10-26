using System.Globalization;
using System.Text;
using GoIPGateway.Application;
using GoIPGateway.Application.Common.Interfaces;
using GoIPGateway.Infrastructure;
using GoIPGateway.Infrastructure.Data;
using GoIPGateway.Infrastructure.Services;
using GoIPGateway.Web.Middleware;
using GoIPGateway.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Layers DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. HTTP Context & Tenancy Providers
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// 3. SignalR Real-Time Hub
builder.Services.AddSignalR();

// 4. Localization Configuration (ar-EG & en-US)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// 5. Authentication (Cookies for Dashboards + JWT Bearer for REST API)
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "GoIP_Gateway_Super_Secret_Key_For_Jwt_Generation_2026_Secure_Default!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GoIPGatewayPlatform";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GoIPGatewayClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "SmartScheme";
    options.DefaultChallengeScheme = "SmartScheme";
})
.AddPolicyScheme("SmartScheme", "Bearer or Cookie", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// 6. Controllers with Views & Bilingual View Localization
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// 7. Swagger Documentation for Companies
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GoIP Gateway Middleware REST API",
        Version = "v1",
        Description = "منصة الوساطة المتكاملة لأجهزة GoIP والشرايح الذكية - تنفيذ USSD، التحويل، دفع QR، واستعلام الأرصدة لحظياً.",
        Contact = new OpenApiContact
        {
            Name = "GoIP Gateway Platform Team",
            Email = "support@goipgateway.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Company API Key Header (X-Api-Key)",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityDefinition("SecretKey", new OpenApiSecurityScheme
    {
        Description = "Company Secret Key Header (X-Secret-Key)",
        Name = "X-Secret-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 8. Auto-Seed Initial Database Data
await InitialDataSeed.SeedAsync(app.Services);

// 9. HTTP Pipeline Configuration
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GoIP Gateway REST API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseStaticFiles();

// 10. Localization Middleware
var supportedCultures = new[] { new CultureInfo("ar-EG"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ar-EG"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures,
    RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    }
});

app.UseRouting();

// 11. Custom Middlewares
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

// 12. Endpoint Mappings
app.MapHub<GatewayHub>("/hubs/gateway");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
