using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureCarbonCli.Commands;

public interface ICarbonSettings
{
    bool SkipHeader { get; set; }
    OutputFormat Output { get; set; }
    string Query { get; set; }
}

public class CarbonSettings : LogCommandSettings, ICarbonSettings
{
    [CommandOption("-s|--subscription")]
    [Description("The subscription id to use. Will try to fetch the active id if not specified.")]
    public Guid? Subscription { get; set; }

    [CommandOption("-g|--resource-group")]
    [Description("The resource group to scope the request to. Need to be used in combination with the subscription id.")]
    public string? ResourceGroup { get; set; }


    [CommandOption("-o|--output")]
    [Description("The output format to use. Defaults to Console (Console, Json, JsonC, Text, Markdown, Csv)")]
    public OutputFormat Output { get; set; } = OutputFormat.Console;

    [CommandOption("-t|--timeframe")]
    [Description("The timeframe to use for the carbon. Defaults to BillingMonthToDate. When set to Custom, specify the from and to dates using the --from and --to options")]
    public TimeframeType Timeframe { get; set; } = TimeframeType.BillingMonthToDate;

    [CommandOption("--from")]
    [Description("The start date to use for the carbon. Defaults to the first day of the previous month.")]
    public DateOnly From { get; set; } = DateOnly.FromDateTime(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1));

    [CommandOption("--to")]
    [Description("The end date to use for the carbon. Defaults to the current date.")]
    public DateOnly To { get; set; } = DateOnly.FromDateTime(new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1));

    [CommandOption("--others-cutoff")]
    [Description("The number of items to show before collapsing the rest into an 'Others' item.")]
    [DefaultValue(10)]
    public int OthersCutoff { get; set; } = 10;

    [CommandOption("--query")]
    [Description("JMESPath query string, applicable for the Json output only. See http://jmespath.org/ for more information and examples.")]
    public string Query { get; set; } = string.Empty;

    [CommandOption("--skipHeader")]
    [Description("Skip header creation for specific output formats. Useful when appending the output from multiple runs into one file. Defaults to false.")]
    [DefaultValue(false)]
    public bool SkipHeader { get; set; }

    [CommandOption("--filter")]
    [Description("Filter the output by the specified properties. Defaults to no filtering and can be multiple values.")]
    public string[] Filter { get; set; } = [];

    [CommandOption("--includeTags")]
    [Description("Include Tags from the selected dimension.")]
    [DefaultValue(false)]
    public bool IncludeTags { get; set; }

    [CommandOption("--carbonApiBaseAddress <BASE_ADDRESS>")]
    [Description("The base address for the Carbon API. Defaults to https://management.azure.com/")]
    public string CarbonApiAddress { get; set; } = "https://management.azure.com/";

    public Scope GetScope => Scope.Subscription(Subscription.GetValueOrDefault(Guid.Empty));
}

/// <summary>
/// The scope associated with query and export operations.
/// This includes '/subscriptions/{subscriptionId}/' for subscription scope,
/// '/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}' for resourceGroup scope,
///
/// Note; not all are implemented
/// </summary>
public class Scope
{
    public static Scope Subscription(Guid subscriptionId) => new("Subscription", "/subscriptions/" + subscriptionId, true);
    public static Scope ResourceGroup(Guid subscriptionId, string resourceGroup) => new("ResourceGroup", $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}", true);

    private Scope(string name, string path, bool isSubscriptionBased)
    {
        Name = name;
        ScopePath = path;
        IsSubscriptionBased = isSubscriptionBased;
    }

    public string Name { get; init; }

    public string ScopePath
    {
        get;
        init;
    }

    public bool IsSubscriptionBased { get; set; }
}