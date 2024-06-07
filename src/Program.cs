using AzureCarbonCli.CarbonApi;
using AzureCarbonCli.Commands.CarbonByResource;
using AzureCarbonCli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

// Setup the DI
var registrations = new ServiceCollection();

// Register a http client so we can make requests to the Azure Carbon Optimization API
registrations.AddHttpClient("CarbonApi", client =>
{
    client.BaseAddress = new Uri("https://management.azure.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(PollyExtensions.GetRetryAfterPolicy());
registrations.AddHttpClient("ResourceApi", client =>
{
    client.BaseAddress = new Uri("https://management.azure.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).AddPolicyHandler(PollyExtensions.GetRetryAfterPolicy());

registrations.AddTransient<ICarbonRetriever, AzureCarbonApiRetriever>();
registrations.AddTransient<AzureResourceApiRetriever>();

var registrar = new TypeRegistrar(registrations);

// Setup the application itself
var app = new CommandApp(registrar);

// We default to the CarbonByResourceCommand
app.SetDefaultCommand<CarbonByResourceCommand>(); 

app.Configure(config =>
{
    config.SetApplicationName("azure-carbon");

    config.AddExample(new[] { "carbonByResource", "-s", "00000000-0000-0000-0000-000000000000", "-o", "text" });

#if DEBUG
    config.PropagateExceptions();
#endif

    config.AddCommand<CarbonByResourceCommand>("carbonByResource")
      .WithDescription("Show the carbon by resource.");

    config.ValidateExamples();
});

// Run the application
return await app.RunAsync(args);