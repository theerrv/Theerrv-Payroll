using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PayrollSaaS.Application.Interfaces;
using PayrollSaaS.Domain.Calculators;
using PayrollSaaS.Infrastructure.Documents;
using PayrollSaaS.Infrastructure.Persistence;
using PayrollSaaS.Infrastructure.Persistence.Interceptors;
using PayrollSaaS.Shared.Json;
using PayrollSaaS.API.Auth;
using PayrollSaaS.API.Errors;
using Hangfire;
using Hangfire.PostgreSql;
using PayrollSaaS.Infrastructure.Jobs;
using Swashbuckle.AspNetCore.SwaggerUI;
using Serilog;

// ── QuestPDF Community licence (free under $1M annual revenue) ──
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ── Serilog bootstrap ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, cfg) => cfg
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());

    // ── Database ──
    var connectionString = builder.Configuration.GetConnectionString("Payroll")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Payroll is not configured. " +
            "Set the 'ConnectionStrings__Payroll' environment variable on Render " +
            "or use dotnet user-secrets locally.");

    builder.Services.AddScoped<AuditInterceptor>();
    builder.Services.AddDbContext<PayrollDbContext>((sp, options) =>
    {
        options.UseNpgsql(connectionString,
                   npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
               .UseSnakeCaseNamingConvention()
               .AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
    });
    builder.Services.AddScoped<IPayrollDbContext>(sp => sp.GetRequiredService<PayrollDbContext>());

    // ── Auth ──
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "DevKey-Change-In-Production-Must-Be-32-Bytes!!");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"] ?? "PayrollSaaS",
                ValidAudience = jwtSection["Audience"] ?? "PayrollSaaS",
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"))
        .AddPolicy("SchoolAdmin", p => p.RequireRole("SuperAdmin", "SchoolAdmin"))
        .AddPolicy("Hr", p => p.RequireRole("SuperAdmin", "SchoolAdmin", "Hr"))
        .AddPolicy("Finance", p => p.RequireRole("SuperAdmin", "SchoolAdmin", "Finance"))
        .AddPolicy("Employee", p => p.RequireRole("SuperAdmin", "SchoolAdmin", "Hr", "Finance", "Employee"));

    // ── Current user context (from JWT claims) ──
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

    // ── Domain services ──
    builder.Services.AddSingleton<PayrollCalculationService>();
    builder.Services.AddSingleton<IDocumentService, PayrollSaaS.Infrastructure.Documents.DocumentService>();

    // ── Hangfire (doc step 11) ──
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer();
    builder.Services.AddScoped<PfEligibilityJob>();

    // ── CORS — allow the React dev server (and production origin) ──
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FrontendPolicy", policy =>
        {
            var origins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>()
                ?? ["http://localhost", "http://localhost:5173", "http://localhost:5174", "http://localhost:3000"];

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // ── Controllers + JSON ──
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Money serialised as string to avoid floating-point drift (doc §6 API Standards)
            options.JsonSerializerOptions.Converters.Add(new MoneyJsonConverter());
            options.JsonSerializerOptions.Converters.Add(new NullableMoneyJsonConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        });

    // ── OpenAPI + Scalar (replaces Swashbuckle) ──
    builder.Services.AddOpenApi();

    // ── ProblemDetails (RFC 7807) ──
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<PayrollExceptionHandler>();

    var app = builder.Build();

    // ── Middleware pipeline ──
    app.UseCors("FrontendPolicy");   // must be first — before exception handler and auth
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "School Payroll SaaS API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "School Payroll SaaS API";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
        });
    }

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ── Hangfire dashboard (super_admin only) ──
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAuthFilter()]
    });

    // ── Migrate on every startup (dev + production) ──
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
        await db.Database.MigrateAsync();
        if (app.Environment.IsDevelopment())
            await SeedData.SeedAsync(db);
    }

    // ── Recurring jobs ──
    RecurringJob.AddOrUpdate<PfEligibilityJob>(
        "pf-eligibility-daily",
        job => job.RunAsync(),
        Cron.Daily(hour: 2)); // 2 AM UTC

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

// Make the Program class visible for integration tests (WebApplicationFactory)
public partial class Program { }
