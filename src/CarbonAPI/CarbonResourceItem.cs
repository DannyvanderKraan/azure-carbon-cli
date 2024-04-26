namespace AzureCarbonCli.CarbonApi;

public record CarbonResourceItem(double Carbon, string SubscriptionId, string ResourceId, string ResourceType,
    string ResourceLocation, string ResourceGroupName, string PublisherType, string? 
        ServiceName, string? ServiceTier, Dictionary<string, string> Tags);

public static class CarbonResourceItemExtensions
{
    // Function to extract the name of the resource from the resource id
    public static string GetResourceName(this CarbonResourceItem resource)
    {
        var parts = resource.ResourceId.Split('/');
        return parts.Last();
    }

    
}