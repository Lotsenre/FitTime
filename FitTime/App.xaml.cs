using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using FitTime.Data;
using FitTime.Services;
using FitTime.ViewModels;
using FitTime.Views;

namespace FitTime;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Allow DateTime with Kind=Local for Npgsql 6+ (legacy timestamp behavior)
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "fittime-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("FitTime application starting...");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<FitTimeDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ClientsViewModel>();
        services.AddTransient<ClientDialogViewModel>();
        services.AddTransient<ScheduleViewModel>();
        services.AddTransient<ClassDialogViewModel>();
        services.AddTransient<AttendanceViewModel>();
        services.AddTransient<MembershipTypesViewModel>();
        services.AddTransient<TrainersViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<UsersViewModel>();
        services.AddTransient<MembershipTypeDialogViewModel>();
        services.AddTransient<MembershipDialogViewModel>();
        services.AddTransient<TrainerDialogViewModel>();
        services.AddTransient<UserDialogViewModel>();
        services.AddTransient<PasswordResetDialogViewModel>();

        Services = services.BuildServiceProvider();

        var loginWindow = new LoginWindow
        {
            DataContext = Services.GetRequiredService<LoginViewModel>()
        };
        loginWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("FitTime application shutting down.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
