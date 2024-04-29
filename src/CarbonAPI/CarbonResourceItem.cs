namespace AzureCarbonCli.CarbonApi;

public record CarbonResourceItem(double Carbon, string SubscriptionId, string ResourceId, string ResourceType,
    string ResourceLocation, string ResourceGroupName, string PublisherType, string?
        ServiceName, string? ServiceTier, Dictionary<string, string> Tags)
{
    public static CarbonResourceItem From(CarbonEmissionItemDetailData data, ResourcesBySubscriptionId? root)
    {
        var resourceProperties = root?.Value?.Find(x => x.Id.Equals(data.ResourceId, StringComparison.OrdinalIgnoreCase));
        var tags = resourceProperties?.Tags ?? new Dictionary<string, string>();
        return new CarbonResourceItem(data.TotalCarbonEmission, data.SubscriptionId, data.ResourceId, data.ResourceType,
            resourceProperties?.Location ?? string.Empty, data.ResourceGroup, string.Empty, string.Empty, string.Empty, tags);
    }
}

public static class CarbonResourceItemExtensions
{
    // Function to extract the name of the resource from the resource id
    public static string GetResourceName(this CarbonResourceItem resource)
    {
        var parts = resource.ResourceId.Split('/');
        return parts.Last();
    }
}