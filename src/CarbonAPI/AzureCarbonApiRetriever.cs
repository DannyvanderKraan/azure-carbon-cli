using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Azure.Core;
using Azure.Identity;
using AzureCarbonCli.Commands;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace AzureCarbonCli.CarbonApi;

public class AzureCarbonApiRetriever : ICarbonRetriever
{
    private readonly AzureResourceApiRetriever _resourceApiRetriever;
    private readonly HttpClient _client;
    private bool _tokenRetrieved;

    public string CarbonApiAddress { get; set; }

    public AzureCarbonApiRetriever(IHttpClientFactory httpClientFactory, AzureResourceApiRetriever azureResourceApiRetriever)
    {
        _client = httpClientFactory.CreateClient("CarbonApi");
        _resourceApiRetriever = azureResourceApiRetriever;
    }

    private async Task RetrieveToken(bool includeDebugOutput)
    {
        if (_tokenRetrieved)
            return;

        // Get the token by using the DefaultAzureCredential, but try the AzureCliCredential first
        var tokenCredential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new DefaultAzureCredential());

        if (includeDebugOutput)
            AnsiConsole.WriteLine($"Using token credential: {tokenCredential.GetType().Name} to fetch a token.");

        var token = await tokenCredential.GetTokenAsync(new TokenRequestContext([$"{CarbonApiAddress}.default"]));

        if (includeDebugOutput)
            AnsiConsole.WriteLine($"Token retrieved and expires at: {token.ExpiresOn}");

        // Set as the bearer token for the HTTP client
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        _tokenRetrieved = true;
    }

    private async Task<HttpResponseMessage> ExecuteCallToCarbonApi(bool includeDebugOutput, object? payload, Uri uri)
    {
        await RetrieveToken(includeDebugOutput);

        if (includeDebugOutput)
        {
            AnsiConsole.WriteLine($"Retrieving data from {uri} using the following payload:");
            AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(payload)));
            AnsiConsole.WriteLine();
        }

        if (!string.Equals(_client.BaseAddress?.ToString(), CarbonApiAddress))
        {
            _client.BaseAddress = new Uri(CarbonApiAddress);
        }

        string json = JsonConvert.SerializeObject(payload);
        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = payload == null
            ? await _client.GetAsync(uri)
            : await _client.PostAsync("https://management.azure.com/providers/Microsoft.Carbon/carbonEmissionReports?api-version=2023-04-01-preview",
            content);

        if (includeDebugOutput)
        {
            AnsiConsole.WriteLine(
                $"Response status code is {response.StatusCode} and got payload size of {response.Content.Headers.ContentLength}");
            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.WriteLine($"Response content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<IEnumerable<CarbonResourceItem>> RetrieveCarbonForResources(bool includeDebugOutput,
        Scope scope, string[] filter, TimeframeType timeFrame,
        DateOnly from,
        DateOnly to)
    {
        var uri = new Uri("/providers/Microsoft.Carbon/carbonEmissionReports?api-version=2023-04-01-preview", UriKind.Relative);
        var subscriptions = new[] { "2193e77b-d7ae-498b-9a28-14abbd97dfe2", "aeaddd47-153c-436f-9e3e-5fd9b42f737d" };
        var payload = new
        {
            carbonScopeList = new[] { "Scope1", "Scope2", "Scope3" },
            categoryType = "Resource",
            dateRange = new { start = "2024-01-01", end = "2024-01-01" },
            orderBy = "TotalCarbonEmission",
            pageSize = 10,
            reportType = "ItemDetailReport",
            resourceGroupUrlList = Array.Empty<string>(),
            sortDirection = "Asc",
            subscriptionList = subscriptions,
            skipToken = string.Empty
        };

        var subscriptionWithResources = new Dictionary<string, ResourcesBySubscriptionId?>();
        foreach (var subscription in subscriptions)
        {
            var resourcesBySubscriptionId = await _resourceApiRetriever.GetResourcesBySubscriptionId(subscription);
            subscriptionWithResources.Add(subscription, resourcesBySubscriptionId);
        }

        var response = await ExecuteCallToCarbonApi(includeDebugOutput, payload, uri);

        CarbonEmissionDataListResult? content = await response.Content.ReadFromJsonAsync<CarbonEmissionDataListResult>();

        var items = new List<CarbonResourceItem>();
        if (content?.Value != null)
        {
            foreach (CarbonEmissionItemDetailData row in content.Value)
            {
                items.Add(CarbonResourceItem.From(row, subscriptionWithResources[row.SubscriptionId]));
            }
        }

        var aggregatedItems = new List<CarbonResourceItem>();
        var groupedItems = items.GroupBy(x => x.ResourceId);
        foreach (var groupedItem in groupedItems)
        {
            var aggregatedItem = new CarbonResourceItem(groupedItem.Sum(x => x.Carbon),
                groupedItem.First().SubscriptionId,
                groupedItem.Key, groupedItem.First().ResourceType,
                string.Join(", ", groupedItem.Select(x => x.ResourceLocation)),
                groupedItem.First().ResourceGroupName, groupedItem.First().PublisherType, null, null,
                groupedItem.First().Tags);
            aggregatedItems.Add(aggregatedItem);
        }

        return aggregatedItems;
    }
}


