using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WashTrack.Data;
using WashTrack.MVVM.Views;
using WashTrack.MVVM.ViewModels;
using WashTrack.Models;

namespace WashTrack
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });



            // Register DbContext
            builder.Services.AddDbContext<WashTrackContext>(
            contextLifetime: ServiceLifetime.Transient,
            optionsLifetime: ServiceLifetime.Transient);

            // Services & Pricing
            builder.Services.AddTransient<ServicesPage>();
            builder.Services.AddTransient<ServicesViewModel>();
            builder.Services.AddTransient<ServiceDetailPage>();
            builder.Services.AddTransient<ServiceDetailViewModel>();

            // Customers
            builder.Services.AddTransient<CustomersPage>();
            builder.Services.AddTransient<CustomersViewModel>();
            builder.Services.AddTransient<CustomerDetailPage>();
            builder.Services.AddTransient<CustomerDetailViewModel>();

            // Transactions
            builder.Services.AddTransient<TransactionsPage>();
            builder.Services.AddTransient<TransactionsViewModel>();
            builder.Services.AddTransient<TransactionDetailPage>();
            builder.Services.AddTransient<TransactionDetailViewModel>();

            // Inventory
            builder.Services.AddTransient<InventoryPage>();
            builder.Services.AddTransient<InventoryViewModel>();
            builder.Services.AddTransient<InventoryDetailPage>();
            builder.Services.AddTransient<InventoryDetailViewModel>();

            // Dashboard
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DashboardViewModel>();

            // Reports
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<ReportsViewModel>();



#if DEBUG
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();

            // Auto-create database on first run
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<WashTrackContext>();
                context.Database.EnsureCreated();

                // Seed initial services if the table is empty
                if (!context.Services.Any())
                {
                    context.Services.AddRange(
                        new Service
                        {
                            ServiceName = "Wash and Fold",
                            PricePerKilo = 30,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Wash and Pressed",
                            PricePerKilo = 60,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Hand Wash",
                            PricePerKilo = 50,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Linens/Blanket",
                            PricePerKilo = 60,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Towel",
                            PricePerKilo = 60,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Comforter",
                            FlatRate = 300,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        },
                        new Service
                        {
                            ServiceName = "Set/Cover",
                            MinKilo = 3,
                            MinKiloCharge = 200,
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        }
                    );
                    context.SaveChanges();
                }
            }

            return app;
        }
    }
}
