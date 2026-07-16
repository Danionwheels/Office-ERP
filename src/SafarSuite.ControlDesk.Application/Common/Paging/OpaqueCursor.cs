using System.Text;
using System.Text.Json;

namespace SafarSuite.ControlDesk.Application.Common.Paging;

public static class OpaqueCursor
{
    public static string Encode<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode<T>(string? cursor, out T? value)
        where T : class
    {
        value = null;

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            var normalized = cursor.Trim().Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(
                normalized.Length + ((4 - normalized.Length % 4) % 4),
                '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            value = JsonSerializer.Deserialize<T>(json);

            return value is not null;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
