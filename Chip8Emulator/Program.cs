using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SFML.Graphics;
using System;
using System.IO;

namespace Chip8Emulator
{
    class Program
    {
        public static void Main(string[] args)
        {
            ProgramInit();

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<ICPU, CPU>();
                    services.AddTransient<IChip8, Chip8>();
                })
                .UseSerilog()
                .Build();

            var chip8 = ActivatorUtilities.CreateInstance<Chip8>(host.Services);

            chip8.Init();
        }

        private static void ProgramInit()
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .Enrich.FromLogContext()
                .WriteTo.File(@"D:\\emulator.log")
                //.WriteTo.File(@$"{Directory.GetCurrentDirectory()}\emulator.log")
                .CreateLogger();

            Log.Logger.Information("Application started");
        }

        static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIROMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
        }
    }
}
