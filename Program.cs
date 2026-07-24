using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.JobService;
using Zyra.LantimeServiceApp.Models;
using Zyra.LantimeServiceApp.Services;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;
using ZyraHangfireService;
using ZYRAHRM.IntegrationApp.HangfireService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config =>
    config
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                JobExpirationCheckInterval = TimeSpan.FromDays(1)
            })
        .WithJobExpirationTimeout(TimeSpan.FromDays(90))
        .UseConsole()
);

builder.Services.Configure<ZyraIntegrationCredentials>(
    builder.Configuration.GetSection("ZyraIntegrationCredentials"));

builder.Services.AddOptions<BiometricSyncSettings>()
    .Bind(builder.Configuration.GetSection("BiometricSync"))
    .ValidateOnStart();

builder.Services.Configure<AttendanceSettings>(
    builder.Configuration.GetSection("AttendanceSettings"));

builder.Services.Configure<HangfireSecurityOptions>(
    builder.Configuration.GetSection("HangfireSecurity"));

//builder.Services.AddDbContext<AttendanceDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("AttendanceDb")));

builder.Services.AddDbContext<AttendanceDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AttendanceDb"),
        b => b.MigrationsAssembly("ZYRA.Attendance.Infrastructure")
    ));


builder.Services.AddHangfireServer();

builder.Services.AddScoped<IDbService, DbService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IHttpService, HttpService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IAttendanceProvider, AttendanceProvider>();
builder.Services.AddScoped<IAttendanceApiService, AttendanceApiService>();
builder.Services.AddScoped<IAttendancePolicy, AttendancePolicy>();
builder.Services.AddScoped<IAttendanceProcessor, AttendanceProcessor>();
builder.Services.AddScoped<IAttendanceDbService, AttendanceDbService>();
builder.Services.AddScoped<IAttendanceLogService, AttendanceLogService>();
builder.Services.AddScoped<IEmployeeSyncProcessor,  EmployeeSyncProcessor>();
builder.Services.AddScoped<IEmployeePunchProcessor, EmployeePunchProcessor>();

builder.Services.AddScoped<ISyncAttendanceJob, JobSyncAttendanceJob>();
builder.Services.AddScoped<IAutoCheckoutJob, JobAutoCheckoutJob>();
builder.Services.AddScoped<IDirectorAttendanceJob, JobDirectorAttendance>();
builder.Services.AddScoped<IEmployeeSyncJob, JobEmployeeMasterSync>();
builder.Services.AddScoped<IEmployeePunchSyncJob, JobEmployeeBioPunchTimeUpdate>();

//builder.Services.AddScoped<AttendanceJobService>();
//builder.Services.AddScoped<UserEmployeeService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Logging services 
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();


// ✅ ADD SWAGGER SERVICES
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB migration update
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<AttendanceDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying database migrations.");
    }
}

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

// ENABLE SWAGGER MIDDLEWARE
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        Console.WriteLine("Swagger JSON being generated...");
    });
});

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hangfire API V1");
    c.RoutePrefix = "swagger"; // open at /swagger
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new AllowAllDashboardAuthorizationFilter() }
});

app.MapControllers();

// Register recurring jobs BEFORE app.Run()

var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

// Register all Hangfire jobs in one place
HangfireJobRegistration.Register(app.Services, istZone);

app.MapGet("/", () => "Hangfire Service is running...");

app.Run();