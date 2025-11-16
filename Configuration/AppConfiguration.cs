using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace GrapheneSensore.Configuration
{
    /// <summary>
    /// Centralized configuration management for the application
    /// </summary>
    public class AppConfiguration
    {
        private static AppConfiguration? _instance;
        private static readonly object _lock = new object();
        private readonly IConfiguration _configuration;

        private AppConfiguration()
        {
            // Get the directory where the executable is located
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(exePath) ?? Directory.GetCurrentDirectory();
            
            var builder = new ConfigurationBuilder()
                .SetBasePath(exeDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true);

            _configuration = builder.Build();
        }

        public static AppConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AppConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        public string ConnectionString => _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");

        public int MatrixSize => _configuration.GetValue<int>("AppSettings:MatrixSize", 32);
        public int PressureThreshold => _configuration.GetValue<int>("AppSettings:PressureThreshold", 150);
        public int MinAreaPixels => _configuration.GetValue<int>("AppSettings:MinAreaPixels", 10);
        public int LowerThreshold => _configuration.GetValue<int>("AppSettings:LowerThreshold", 20);
        public int PlaybackIntervalMs => _configuration.GetValue<int>("AppSettings:PlaybackIntervalMs", 500);
        public int MaxAlertDisplay => _configuration.GetValue<int>("AppSettings:MaxAlertDisplay", 50);
        public int DataImportBatchSize => _configuration.GetValue<int>("AppSettings:DataImportBatchSize", 100);

        public int PasswordMinLength => _configuration.GetValue<int>("Security:PasswordMinLength", 8);
        public bool RequireUppercase => _configuration.GetValue<bool>("Security:RequireUppercase", true);
        public bool RequireDigit => _configuration.GetValue<bool>("Security:RequireDigit", true);
        public bool RequireSpecialChar => _configuration.GetValue<bool>("Security:RequireSpecialChar", true);
        public int SessionTimeoutMinutes => _configuration.GetValue<int>("Security:SessionTimeoutMinutes", 60);

        public bool EnableCaching => _configuration.GetValue<bool>("Performance:EnableCaching", true);
        public int CacheDurationMinutes => _configuration.GetValue<int>("Performance:CacheDurationMinutes", 5);
        public int MaxConcurrentImports => _configuration.GetValue<int>("Performance:MaxConcurrentImports", 3);

        public string LogFilePath => _configuration.GetValue<string>("Logging:File:Path") ?? "Logs/graphene-sensore-.log";
        public string LogLevel => _configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";
    }
}
