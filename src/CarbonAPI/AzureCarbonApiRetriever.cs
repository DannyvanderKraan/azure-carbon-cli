using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using AzureCarbonCli.Commands;
using Spectre.Console;
using Spectre.Console.Json;

namespace AzureCarbonCli.CarbonApi;

public class AzureCarbonApiRetriever : ICarbonRetriever
{
    private readonly HttpClient _client;
    private bool _tokenRetrieved;

    public string CarbonApiAddress { get; set; }

    public enum DimensionNames
    {
        PublisherType,
        ResourceGroupName,
        ResourceLocation,
        ResourceId,
        ServiceName,
        ServiceTier,
        ServiceFamily,
        InvoiceId,
        CustomerName,
        PartnerName,
        ResourceType,
        ChargeType,
        BillingPeriod,
        MeterCategory,
        MeterSubCategory,
        // Add more dimension names as needed
    }

    public AzureCarbonApiRetriever(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("CarbonApi");
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

        var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[]
            { $"{CarbonApiAddress}.default" }));

        if (includeDebugOutput)
            AnsiConsole.WriteLine($"Token retrieved and expires at: {token.ExpiresOn}");

        // Set as the bearer token for the HTTP client
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        _tokenRetrieved = true;
    }


    private object? GenerateFilters(string[]? filterArgs)
    {
        if (filterArgs == null || filterArgs.Length == 0)
            return null;

        var filters = new List<object>();
        foreach (var arg in filterArgs)
        {
            var filterParts = arg.Split('=');
            var name = filterParts[0];
            var values = filterParts[1].Split(';');

            // Define default filter dictionary
            var filterDict = new Dictionary<string, object>()
            {
                { "Name", name },
                { "Operator", "In" },
                { "Values", new List<string>(values) }
            };

            // Decide if this is a Dimension or a Tag filter
            if (Enum.IsDefined(typeof(DimensionNames), name))
            {
                filters.Add(new { Dimensions = filterDict });
            }
            else
            {
                filters.Add(new { Tags = filterDict });
            }
        }

        if (filters.Count > 1)
            return new
            {
                And = filters
            };
        else
            return filters[0];
    }

    private Uri DeterminePath(Scope scope, string path)
    {
        // return the scope.ScopePath combined with the path
        return new Uri(scope.ScopePath + path, UriKind.Relative);

    }

    private async Task<HttpResponseMessage> ExecuteCallToCarbonApi(bool includeDebugOutput, object? payload, Uri uri)
    {
        await RetrieveToken(includeDebugOutput);

        if (includeDebugOutput)
        {
            AnsiConsole.WriteLine($"Retrieving data from {uri} using the following payload:");
            AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(payload)));
            AnsiConsole.WriteLine();
        }

        if (!string.Equals(_client.BaseAddress?.ToString(), CarbonApiAddress))
        {
            _client.BaseAddress = new Uri(CarbonApiAddress);
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var response = payload == null
            ? await _client.GetAsync(uri)
            : await _client.PostAsJsonAsync(uri, payload, options);

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
        var uri = DeterminePath(scope, "/providers/Microsoft.CarbonManagement/query?api-version=2023-03-01&$top=5000");

        object grouping = new[]
            {
                new
                {
                    type = "Dimension",
                    name = "ResourceId"
                },
                new
                {
                    type = "Dimension",
                    name = "ResourceType"
                },
                new
                {
                    type = "Dimension",
                    name = "ResourceLocation"
                },
                new
                {
                    type = "Dimension",
                    name = "ResourceGroupName"
                }
            };
  
        var payload = new
        {
            timeframe = timeFrame.ToString(),
            timePeriod = timeFrame == TimeframeType.Custom
                ? new
                {
                    from = from.ToString("yyyy-MM-dd"),
                    to = to.ToString("yyyy-MM-dd")
                }
                : null,
            dataSet = new
            {
                granularity = "None",
                aggregation = new
                {
                    totalCarbon = new
                    {
                        name = "Carbon",
                        function = "Sum"
                    }
                },
                include = new[] { "Tags" },
                filter = GenerateFilters(filter),
                grouping = grouping,
            }
        };
        var response = await ExecuteCallToCarbonApi(includeDebugOutput, payload, uri);

        CarbonQueryResponse? content = await response.Content.ReadFromJsonAsync<CarbonQueryResponse>();

        var items = new List<CarbonResourceItem>();
        foreach (JsonElement row in content.properties.rows)
        {
            double Carbon = row[0].GetDouble();
            string resourceId = row[2].GetString();
            string resourceType = row[3].GetString();
            string resourceLocation = row[4].GetString();
            string resourceGroupName = row[6].GetString();
            string publisherType = row[7].GetString();
            string serviceName = row[8].GetString();
            string serviceTier = row[9].GetString();

            int tagsColumn = 8;
            // Assuming row[tagsColumn] contains the tags array
            var tagsArray = row[tagsColumn].EnumerateArray().ToArray();

            Dictionary<string, string> tags = new Dictionary<string, string>();

            foreach (var tagString in tagsArray)
            {
                var parts = tagString.GetString().Split(':');
                if (parts.Length == 2) // Ensure the string is in the format "key:value"
                {
                    var key = parts[0].Trim('"'); // Remove quotes from the key
                    var value = parts[1].Trim('"'); // Remove quotes from the value
                    tags[key] = value;
                }
            }

            CarbonResourceItem item = new CarbonResourceItem(Carbon, resourceId, resourceType, resourceLocation,
                resourceGroupName, publisherType, serviceName, serviceTier, tags);

            items.Add(item);
        }

            var aggregatedItems = new List<CarbonResourceItem>();
            var groupedItems = items.GroupBy(x => x.ResourceId);
            foreach (var groupedItem in groupedItems)
            {
                var aggregatedItem = new CarbonResourceItem(groupedItem.Sum(x => x.Carbon), 
                    groupedItem.Key, groupedItem.First().ResourceType,
                    string.Join(", ", groupedItem.Select(x => x.ResourceLocation)), 
                    groupedItem.First().ResourceGroupName, groupedItem.First().PublisherType, null, null,
                    groupedItem.First().Tags);
                aggregatedItems.Add(aggregatedItem);
            }

            return aggregatedItems;
    }

}


