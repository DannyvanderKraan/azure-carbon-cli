using System.Globalization;
using AzureCarbonCli.Commands.CarbonByResource;
using AzureCarbonCli.CarbonApi;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace AzureCarbonCli.OutputFormatters;

public class CsvOutputFormatter : BaseOutputFormatter
{
 
    public override Task WriteCarbonByResource(CarbonByResourceSettings settings, IEnumerable<CarbonResourceItem> resources)
    {
        return ExportToCsv(settings.SkipHeader, resources);
    }
    
    private static Task ExportToCsv(bool skipHeader, IEnumerable<object> resources)
    {
        var config = new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            HasHeaderRecord = skipHeader == false
        };
        
        using (var writer = new StringWriter())
        using (var csv = new CsvWriter(writer, config))
        {
            csv.Context.TypeConverterCache.AddConverter<double>(new CustomDoubleConverter());
            csv.Context.TypeConverterCache.AddConverter<Dictionary<string, string>>(new TagsConverter());
            csv.WriteRecords(resources);

            Console.Write(writer.ToString());
        }

        return Task.CompletedTask;
    }
    
}

public class TagsConverter : DefaultTypeConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value == null)
            return string.Empty;
        var tags = (Dictionary<string, string>)value;
        return string.Join(";", tags.Select(a => $"{a.Key}:{a.Value}"));
    }
}

public class CustomDoubleConverter : DoubleConverter
{
    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        double number = (double)value;
        return number.ToString("F8", CultureInfo.InvariantCulture);
    }
}