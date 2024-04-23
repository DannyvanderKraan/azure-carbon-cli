using AzureCarbonCli.CarbonApi;
using AzureCarbonCli.Commands.CarbonByResource;
using AzureCostCli.OutputFormatters.SpectreConsole;
using Spectre.Console;
using Spectre.Console.Extensions;
using Spectre.Console.Json;
using System.Text.Json;

namespace AzureCarbonCli.OutputFormatters;

public class ConsoleOutputFormatter : BaseOutputFormatter
{


    public override Task WriteCarbonByResource(CarbonByResourceSettings settings, IEnumerable<CarbonResourceItem> resources)
    {

        var table = new Table()
            .RoundedBorder().Expand()
            .AddColumn("Resource")
            .AddColumn("Resource Type")
            .AddColumn("Location")
            .AddColumn("Resource group name")
            .AddColumn("Tags")
            .AddColumn("Carbon", column => column.Width(15).RightAligned());

      
 
        foreach (var resource in resources.OrderByDescending(a => a.Carbon))
        {
  
            table.AddRow(new Markup("[bold]" + resource.ResourceId.Split('/').Last().EscapeMarkup() + "[/]"),
                    new Markup(resource.ResourceType.EscapeMarkup()),
                    new Markup(resource.ResourceLocation.EscapeMarkup()),
                    new Markup(resource.ResourceGroupName.EscapeMarkup()),
                    resource.Tags.Any() ? new JsonText(JsonSerializer.Serialize(resource.Tags)) : new Markup(""),
                    new Carbon(resource.Carbon));
        }

        AnsiConsole.Write(table);

        return Task.CompletedTask;
    }
}