using System.Text.Json;
using static AppTranslator.Utils.AppTranslatorConstants;
namespace AppTranslator.Extensions;

public static class StringExtensions
{
    public static TType Deserialize<TType>(this string value, bool formatted = true)
    {
        try
        {
            return JsonSerializer.Deserialize<TType>(value, GetSerializeOptions(formatted));
        }
        catch
        {
            return default;
        }
    }        
}