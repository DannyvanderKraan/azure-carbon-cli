using AzureCarbonCli.CarbonApi;
using AzureCarbonCli.Infrastructure;
using AzureCarbonCli.OutputFormatters;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureCarbonCli.Commands.CarbonByResource
{
    public class CarbonByResourceCommand: AsyncCommand<CarbonByResourceSettings>
    {
        private readonly ICarbonRetriever _carbonRetriever;

        private readonly Dictionary<OutputFormat, BaseOutputFormatter> _outputFormatters = new();

        public CarbonByResourceCommand(ICarbonRetriever costRetriever)
        {
            _carbonRetriever = costRetriever;

            // Add the output formatters
            _outputFormatters.Add(OutputFormat.Console, new ConsoleOutputFormatter());
            _outputFormatters.Add(OutputFormat.Json, new JsonOutputFormatter());
            _outputFormatters.Add(OutputFormat.Jsonc, new JsonOutputFormatter());
            _outputFormatters.Add(OutputFormat.Text, new TextOutputFormatter());
            _outputFormatters.Add(OutputFormat.Markdown, new MarkdownOutputFormatter());
            _outputFormatters.Add(OutputFormat.Csv, new CsvOutputFormatter());
        }

        public override ValidationResult Validate(CommandContext context, CarbonByResourceSettings settings)
        {
            // Validate if the timeframe is set to Custom, then the from and to dates must be specified and the from date must be before the to date
            if (settings.Timeframe == TimeframeType.Custom)
            {
                if (settings.From == null)
                {
                    return ValidationResult.Error("The from date must be specified when the timeframe is set to Custom.");
                }

                if (settings.To == null)
                {
                    return ValidationResult.Error("The to date must be specified when the timeframe is set to Custom.");
                }

                if (settings.From > settings.To)
                {
                    return ValidationResult.Error("The from date must be before the to date.");
                }
            }

            return ValidationResult.Success();
        }

        public override async Task<int> ExecuteAsync(CommandContext context, CarbonByResourceSettings settings)
        {
            // Show version
            if (settings.Debug)
                AnsiConsole.WriteLine($"Version: {typeof(CarbonByResourceCommand).Assembly.GetName().Version}");

            _carbonRetriever.CarbonApiAddress = settings.CarbonApiAddress;

            // Get the subscription ID from the settings
            var subscriptionId = settings.Subscription;

            if (!subscriptionId.HasValue && (settings.GetScope.IsSubscriptionBased))
            {
                // Get the subscription ID from the Azure CLI
                try
                {
                    if (settings.Debug)
                        AnsiConsole.WriteLine(
                            "No subscription ID specified. Trying to retrieve the default subscription ID from Azure CLI.");

                    subscriptionId = Guid.Parse(AzCommand.GetDefaultAzureSubscriptionId());

                    if (settings.Debug)
                        AnsiConsole.WriteLine($"Default subscription ID retrieved from az cli: {subscriptionId}");

                    settings.Subscription = subscriptionId;
                }
                catch (Exception e)
                {
                    AnsiConsole.WriteException(new ArgumentException(
                        "Missing subscription ID. Please specify a subscription ID or login to Azure CLI.", e));
                    return -1;
                }
            }

            // Fetch the carbon footprint from the Azure Carbon Optimization API
            IEnumerable<CarbonResourceItem> resources = new List<CarbonResourceItem>();

            await AnsiConsoleExt.Status()
                .StartAsync("Fetching carbon data for resources...", async ctx =>
                {
                    resources = await _carbonRetriever.RetrieveCarbonForResources(
                        settings.Debug,
                        settings.GetScope, 
                        settings.Filter,
                        settings.Timeframe,
                        settings.From,
                        settings.To);
                });

            // Write the output
            await _outputFormatters[settings.Output]
                .WriteCarbonByResource(settings, resources);

            return 0;
        }
    }
}