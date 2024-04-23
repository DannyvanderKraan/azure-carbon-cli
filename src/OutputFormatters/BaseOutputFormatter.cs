using AzureCarbonCli.Commands.CarbonByResource;
using AzureCarbonCli.CarbonApi;

namespace AzureCarbonCli.OutputFormatters;

public abstract class BaseOutputFormatter
{

    public abstract Task WriteCarbonByResource(CarbonByResourceSettings settings, IEnumerable<CarbonResourceItem> resources);
    

}