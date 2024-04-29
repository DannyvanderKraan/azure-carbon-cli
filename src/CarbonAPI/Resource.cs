using Newtonsoft.Json;

namespace AzureCarbonCli.CarbonApi;

public class Resource
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }

    [JsonProperty("sku")]
    public Sku Sku { get; set; }

    [JsonProperty("kind")]
    public string Kind { get; set; }

    [JsonProperty("tags")]
    public Dictionary<string, string> Tags { get; set; }

    [JsonProperty("identity")]
    public Identity Identity { get; set; }

    [JsonProperty("managedBy")]
    public string ManagedBy { get; set; }
}

public class Sku
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("tier")]
    public string Tier { get; set; }

    [JsonProperty("size")]
    public string Size { get; set; }

    [JsonProperty("family")]
    public string Family { get; set; }

    [JsonProperty("capacity")]
    public int Capacity { get; set; }
}

public class Identity
{
    [JsonProperty("principalId")]
    public string PrincipalId { get; set; }

    [JsonProperty("tenantId")]
    public string TenantId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

public class ResourcesBySubscriptionId
{
    [JsonProperty("value")]
    public List<Resource> Value { get; set; }
}