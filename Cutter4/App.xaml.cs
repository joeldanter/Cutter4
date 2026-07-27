using CommunityToolkit.Mvvm.Messaging;
using Cutter4.Domain;
using Cutter4.Persistence;
using Cutter4.Persistence.Repositories;
using Cutter4.View;
using Cutter4.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Cutter4
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; }

        public App()
        { 
            Services = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();

            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
            string connStr = $"Data Source={dbPath}";
            services.AddDbContextFactory<CutterContext>(options =>
                options.UseSqlite(connStr)
                .AddInterceptors(new SqlitePragmaInterceptor()));

            services.AddSingleton<IMessenger, WeakReferenceMessenger>();

            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddTransient<IStorageRepository, StorageRepository>();

            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<EditStorageWindow>();
            services.AddTransient<EditStorageViewModel>();
            services.AddTransient<TruckDeliveryWindow>();
            services.AddTransient<TruckDeliveryViewModel>();

            services.AddSingleton<IDialogService, MessageBoxDialogService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();

            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

            return services.BuildServiceProvider();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Show();

            if (!System.Diagnostics.Debugger.IsAttached)
            {
                DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleFatalException(e.Exception);
            e.Handled = true;
            Shutdown(-1); // graceful shutdown
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleFatalException(e.ExceptionObject as Exception);
            // we are cooked anyways and the apps gonna shut down
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs? e)
        {
            HandleFatalException(e?.Exception);
            e?.SetObserved();
            Shutdown(-1);
        }

        private void HandleFatalException(Exception? ex)
        {
            try
            {
                File.AppendAllText("crash.log", $"{DateTime.Now}: {ex}\n\n");
            }
            catch { /* we are cooked */ }

            MessageBox.Show($"An unexpected error occurred and the application must close.\n\n{ex?.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class SqlitePragmaInterceptor : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = @"
                PRAGMA journal_mode=WAL;
                PRAGMA foreign_keys=ON;
                PRAGMA busy_timeout=1000;";
            cmd.ExecuteNonQuery();
        }
    }
}
