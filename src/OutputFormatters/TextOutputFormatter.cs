using System.Globalization;
using AzureCarbonCli.Commands.CarbonByResource;
using AzureCarbonCli.CarbonApi;
using AzureCarbonCli.Infrastructure;

namespace AzureCarbonCli.OutputFormatters;

public class TextOutputFormatter : BaseOutputFormatter
{
    public override Task WriteCarbonByResource(CarbonByResourceSettings settings, IEnumerable<CarbonResourceItem> resources)
    {
        if (settings.SkipHeader)
        {
            Console.WriteLine(
                $"Azure Cost Overview for {settings.Subscription} by resource");

            Console.WriteLine();
        }

        foreach (var resource in resources.OrderByDescending(a=>a.Carbon))
        {

                Console.WriteLine(
                    $"{resource.ResourceId.Split('/').Last()} \t {resource.ResourceType} \t {resource.ResourceLocation} \t {resource.ResourceGroupName} \t {resource.Carbon:N2}");

        }
      
        return Task.CompletedTask;
    }
}