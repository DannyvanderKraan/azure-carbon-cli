using System.Globalization;
using System.Text;
using AzureCarbonCli.Commands.CarbonByResource;
using AzureCarbonCli.CarbonApi;
using AzureCarbonCli.Infrastructure;

namespace AzureCarbonCli.OutputFormatters;

public class MarkdownOutputFormatter : BaseOutputFormatter
{
    public override Task WriteCarbonByResource(CarbonByResourceSettings settings, IEnumerable<CarbonResourceItem> resources)
    {

            if (settings.SkipHeader == false)
            {
                Console.WriteLine("# Azure Carbon by Resource");
                Console.WriteLine();
                Console.WriteLine(
                    "| ResourceName | ResourceType | Location | ResourceGroupName | Carbon |");
                Console.WriteLine("|---|---|---|---|---|---|---|---:|");
            }

            foreach (var carbon in resources)
            {
                Console.WriteLine(
                    $"|{carbon.ResourceId.Split('/').Last()} | {carbon.ResourceType} | {carbon.ResourceLocation} | {carbon.ResourceGroupName} | {carbon.Carbon} |");
            }
        return Task.CompletedTask;
    }
}