using AppInsightExceptionsMailer.Mail.Implementations;
using AppInsightExceptionsMailer.Mail.Interfaces;
using AppInsightExceptionsMailer.Mail.Pocos;
using AppInsightsReport.Mail.Pocos;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.ApplicationInsights.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace AppInsightExceptionsMailer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var basePath = GetBasePath();
            var host = new HostBuilder()
                   .ConfigureHostConfiguration(configurationBuilder =>
                   {
                       configurationBuilder.AddCommandLine(args)
                       //.SetBasePath(basePath)
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)  // common settings go here.
                       .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")}.json", optional: true, reloadOnChange: false)  // environment specific settings go here
                       .AddJsonFile("local.settings.json", optional: true, reloadOnChange: false)  // secrets go here. This file is excluded from source control.
                       .AddEnvironmentVariables()
                       .Build();
                   })
                .ConfigureFunctionsWorkerDefaults()
                 .ConfigureServices((context, services) =>
                 {
                     var config = context.Configuration;
                     services.AddSingleton<TelemetryConfiguration>(sp =>
                     {
                         var key = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                         if (!string.IsNullOrWhiteSpace(key))
                         {
                             return new TelemetryConfiguration(key);
                         }
                         return new TelemetryConfiguration();
                     });
                     var credentials = new ApiKeyClientCredentials(config.GetSection("AppOptions:ApplicationInsights:ApiKey").Value);
                     var baseUrl = config.GetSection("AppOptions:ApplicationInsights:BaseUrl").Value;
                     services.AddHttpClient();
                     services.AddLogging();
                     services.AddOptions();
                     //services.AddSingleton<ITelemetryInitializer>();
                     services.AddSingleton<ApplicationInsightsDataClient>(sp => new ApplicationInsightsDataClient(new Uri(baseUrl), credentials));
                     services.Configure<MailSettings>(config.GetSection("MailSettings"));
                     services.AddTransient<IMailService, MailService>();
                     services.Configure<ApplicationInsightsSection>(config.GetSection("AppOptions:ApplicationInsights"));
                 })
                .Build();

            host.Run();
        }


        private static string? GetBasePath()
        {
            var azureWebJobsScriptRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot"); // local_root
            var azureRoot = Environment.GetEnvironmentVariable("HOME") != null ? $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot" : null; //azure root
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var actualRoot = azureWebJobsScriptRoot ?? azureRoot ?? assemblyLocation;

            return actualRoot;
        }
    }
}