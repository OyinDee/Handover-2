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
        sqlOptions.CommandTimeout(30);
        sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
    }));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = true;
    
    // Add lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Replace the problematic configuration with this:
builder.Services.Configure<IdentityOptions>(options => { });

// Add Identity event handlers for logging
builder.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, UserClaimsPrincipalFactory<IdentityUser, IdentityRole>>();
builder.Services.Configure<SecurityStampValidatorOptions>(options => {
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

// Add event handlers
builder.Services.AddScoped<SignInManager<IdentityUser>>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
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
builder.Services.AddSignalR();

var app = builder.Build();

// Add role and admin user seeding
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // Create roles
    var roles = new[] { "Supervisor", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Create default admin user
    var adminEmail = "admin001@admin.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = "admin001",
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@001"); // Password is "Admin@001"
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Supervisor");
        }
    }

    // Change user with ID 1 to Supervisor
    var userWithId1 = userManager.Users.FirstOrDefault(u => u.Id == "1");
    if (userWithId1 != null && !await userManager.IsInRoleAsync(userWithId1, "Supervisor"))
    {
        await userManager.AddToRoleAsync(userWithId1, "Supervisor");
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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<NotificationHub>("/notificationHub");

// Clean up logger on application shutdown
app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                await SeedData.Initialize(services);
                await SeedData.AddAdminUser(services); // Add this line to seed another admin user
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred seeding the DB.");
            }
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

public class NotificationHub : Hub
{
}