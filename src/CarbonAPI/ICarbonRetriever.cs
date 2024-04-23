using AzureCarbonCli.Commands;

namespace AzureCarbonCli.CarbonApi;

public interface ICarbonRetriever
{

    public string CarbonApiAddress { get; set; }

    Task<IEnumerable<CarbonResourceItem>> RetrieveCarbonForResources(bool settingsDebug, Scope scope, string[] filter, 
        TimeframeType settingsTimeframe, DateOnly from, DateOnly to);
}



