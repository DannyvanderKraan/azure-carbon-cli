using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureCarbonCli.Commands;

public class LogCommandSettings : CommandSettings
{
    [CommandOption("--debug")]
    [Description("Increase logging verbosity to show all debug logs.")]
    [DefaultValue(false)]
    public bool Debug { get; set; }
    
}