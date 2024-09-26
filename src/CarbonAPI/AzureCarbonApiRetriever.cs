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

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = payload == null
            ? await _client.GetAsync(uri)
            : await _client.PostAsync("https://management.azure.com/providers/Microsoft.Carbon/carbonEmissionReports?api-version=2023-04-01-preview",
            content);

        if (includeDebugOutput)
        {
            AnsiConsole.WriteLine($"Response status code is {response.StatusCode} and got payload size of {response.Content.Headers.ContentLength}");
            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.WriteLine($"Response content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<IEnumerable<CarbonResourceItem>> RetrieveCarbonForResources(
        Guid subscriptionId,
        bool includeDebugOutput,
        Scope scope,
        string[] filter,
        TimeframeType timeFrame,
        int year,
        int month)
    {
        var uri = new Uri("/providers/Microsoft.Carbon/carbonEmissionReports?api-version=2023-04-01-preview", UriKind.Relative);
        var subscriptions = new[] { subscriptionId.ToString() };
        var payload = new
        {
            carbonScopeList = new[] { "Scope1", "Scope2", "Scope3" },
            categoryType = "Resource",
            dateRange = new
            {
                start = new DateOnly(year, month, 1).ToString("yyyy-MM-dd"),
                end = new DateOnly(year, month, 1).ToString("yyyy-MM-dd")
            },
            orderBy = "TotalCarbonEmission",
            pageSize = 10,
            reportType = "ItemDetailsReport",
            resourceGroupUrlList = Array.Empty<string>(),
            sortDirection = "Asc",
            subscriptionList = new[] { subscriptionId },
            skipToken = string.Empty,
            groupCategory = string.Empty
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

        var groupedItems = items.GroupBy(x => x.ResourceId);

        return groupedItems.Select(groupedItem => new CarbonResourceItem(
                groupedItem.Sum(x => x.Carbon),
                groupedItem.First().SubscriptionId,
                groupedItem.Key,
                groupedItem.First().ResourceType,
                string.Join(", ", groupedItem.Select(x => x.ResourceLocation)),
                groupedItem.First().ResourceGroupName, groupedItem.First().PublisherType,
                null,
                null,
                groupedItem.First().Tags));
    }
}


