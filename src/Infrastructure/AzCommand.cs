using System.Diagnostics;
using System.Text.Json;

namespace AzureCarbonCli.Infrastructure;

public static class AzCommand
{
    public static string GetDefaultAzureSubscriptionId()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "az",
            Arguments = "account show",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new Exception($"Error executing 'az account show': {error}");
        }

        using var jsonDocument = JsonDocument.Parse(output);
        var root = jsonDocument.RootElement;
        if (root.TryGetProperty("id", out var idElement))
        {
            var subscriptionId = idElement.GetString()!;
            return subscriptionId;
        }
        else
        {
            throw new Exception("Unable to find the 'id' property in the JSON output.");
        }
    }
}