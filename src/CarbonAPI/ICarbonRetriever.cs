using AzureCarbonCli.Commands;

namespace AzureCarbonCli.CarbonApi;

public interface ICarbonRetriever
{

    public string CarbonApiAddress { get; set; }

    Task<IEnumerable<CarbonResourceItem>> RetrieveCarbonForResources(
        Guid subscriptionId,
        bool settingsDebug, 
        Scope scope, 
        string[] filter, 
        TimeframeType settingsTimeframe, 
        DateOnly firstDayOfMonth);
}



