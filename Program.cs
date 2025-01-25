using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Handover_2.Data;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Handover_2.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Handover_2.Services;  // Add this line for NotificationService

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/handover_.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog to the application
builder.Host.UseSerilog();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(60),
            errorNumbersToAdd: new[] { 4060, 40197, 40501, 40613, 49918, 49919, 49920 });

        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
    }));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    
    // Add lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Replace the problematic configuration with this:
builder.Services.Configure<IdentityOptions>(options => { });

// Fix the incorrect UserClaimsPrincipalFactory configuration
builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, 
    UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();

// Add Identity event handlers for logging
builder.Services.Configure<SecurityStampValidatorOptions>(options => {
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

// Add event handlers
builder.Services.AddScoped<SignInManager<IdentityUser>>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        if (context?.HttpContext?.RequestServices == null) return;
        
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user != null && await userManager.IsLockedOutAsync(user))
        {
            Log.Warning("Account locked out for user {UserName}. Lockout end: {EndDate}", 
                user.UserName, await userManager.GetLockoutEndDateAsync(user));
        }
    };
});

builder.Services.AddRazorPages();

// Add controllers and API support
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add API explorer for better route discovery
builder.Services.AddEndpointsApiExplorer();

// Update CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Be careful with this in production
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register NotificationService
builder.Services.AddScoped<NotificationService>();

// Add SignalR after CORS
builder.Services.AddSignalR();

var app = builder.Build();

// Single initialization point
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        
        await SeedData.Initialize(services);
        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while seeding the database: {Message}", ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/NotFound");
    app.UseHsts();
    
    // Add global exception handling
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unhandled exception occurred");
            throw;
        }
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add CORS middleware before routing
app.UseCors("SignalRPolicy");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Update endpoint configuration
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers(); // This should come first
    endpoints.MapRazorPages();
    endpoints.MapHub<NotificationHub>("/notificationHub");
});

// Clean up logger on application shutdown
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

public class NotificationHub : Hub
{
    private readonly NotificationService _notificationService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        NotificationService notificationService,
        UserManager<IdentityUser> userManager,
        ILogger<NotificationHub> logger)
    {
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task GetUserNotifications()
    {
        try
        {
            var user = await _userManager.GetUserAsync(Context.User);
            if (user == null) return;

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id);
            await Clients.Caller.SendAsync("ReceiveNotificationList", notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications through SignalR");
            throw;
        }
    }
}