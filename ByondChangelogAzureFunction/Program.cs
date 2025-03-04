using ByondChangelogAzureFunction.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ByondChangelogAzureFunction {
    public class Program {
        public static void Main(string[] args) {
            // Create our builder
            FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

            // Configure insights for Azure
            builder.ConfigureFunctionsWebApplication();
            builder.Services.AddApplicationInsightsTelemetryWorkerService().ConfigureFunctionsApplicationInsights();

            // Add our data service
            builder.Services.AddSingleton<IDataService, DataService>();

            // And send it
            builder.Build().Run();
        }
    }
}

