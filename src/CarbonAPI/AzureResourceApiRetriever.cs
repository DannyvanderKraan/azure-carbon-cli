using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace AzureCarbonCli.CarbonApi;

public class AzureResourceApiRetriever {
    private readonly HttpClient _client;

    public AzureResourceApiRetriever(IHttpClientFactory httpClientFactory) {
        _client = httpClientFactory.CreateClient("ResourceApi");
    }

    private async Task RetrieveToken() {
        var tokenCredential = new ChainedTokenCredential(
            new AzureCliCredential(),
            new DefaultAzureCredential());
        var tokenRequestContext = new TokenRequestContext(new[] { "https://management.azure.com/.default" });
        var accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
    }

    public async Task<Root?> GetResourcesBySubscriptionId(string subscriptionId){
        var uri = new Uri($"https://management.azure.com/subscriptions/{subscriptionId}/resources?api-version=2021-04-01");
        var result = await ExecuteCall(false, null, uri);
        var jsonResult = await result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Root>(jsonResult);
    }

    private async Task<HttpResponseMessage> ExecuteCall(bool includeDebugOutput, object? payload, Uri uri)
    {
        await RetrieveToken();

        if (includeDebugOutput)
        {
            AnsiConsole.WriteLine($"Retrieving data from {uri} using the following payload:");
            AnsiConsole.Write(new JsonText(JsonConvert.SerializeObject(payload)));
            AnsiConsole.WriteLine();
        }

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
}