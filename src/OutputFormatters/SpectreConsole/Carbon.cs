using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AzureCostCli.OutputFormatters.SpectreConsole;

public class Carbon : Renderable
{
    private const string currency = "tCO2e";

    private readonly Markup _paragraph;

    public Carbon(double amount, int precision=2, Style? style = null, Justify justify = Justify.Right)
    {
        _paragraph = new Markup(FormatCarbon(amount, precision), style)
        {
            Justification = justify
        };
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        return ((IRenderable)_paragraph).Render(options, maxWidth);
    }

   public static string FormatCarbon(double amount, int precision = 2)
{
    // Get current culture info
    var cultureInfo = CultureInfo.CurrentCulture;
    // Get culture specific decimal separator
    var decimalSeparator = cultureInfo.NumberFormat.NumberDecimalSeparator;

    // Format the amount with the specified precision
    string formattedAmount = amount.ToString($"N{precision}", cultureInfo);

    // Split the formatted amount into integer and fraction parts
    var amountParts = formattedAmount.Split(decimalSeparator);
    string amountInteger = amountParts[0];
    string amountFraction = amountParts.Length > 1 ? amountParts[1] : new string('0', precision);

    // Prepare styled string
    string styledAmount =
        $"[bold dim]{amountInteger}[/]{decimalSeparator}[dim]{amountFraction}[/] [green]{currency}[/]";

    return styledAmount;
}

}