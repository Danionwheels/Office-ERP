using System.Text;

namespace SafarSuite.StagingPreflight;

internal sealed record DotEnvParseResult(
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<ValidationFailure> Failures);

internal static class DotEnvParser
{
    public static DotEnvParseResult Parse(string path)
    {
        string content;

        try
        {
            content = File.ReadAllText(path, new UTF8Encoding(false, true));
        }
        catch
        {
            return Failed("ENV_FILE", "The staging environment file could not be read.");
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var failures = new List<ValidationFailure>();
        using var reader = new StringReader(content);
        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            var trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith("export ", StringComparison.Ordinal))
            {
                trimmed = trimmed[7..].TrimStart();
            }

            var equalsIndex = trimmed.IndexOf('=');

            if (equalsIndex <= 0)
            {
                failures.Add(FormatFailure(lineNumber));
                continue;
            }

            var key = trimmed[..equalsIndex].Trim();

            if (!IsValidKey(key) || values.ContainsKey(key))
            {
                failures.Add(new ValidationFailure(
                    values.ContainsKey(key) ? "ENV_DUPLICATE" : "ENV_FORMAT",
                    $"The staging environment file has an invalid entry at line {lineNumber}."));
                continue;
            }

            if (!TryParseValue(trimmed[(equalsIndex + 1)..], out var value))
            {
                failures.Add(FormatFailure(lineNumber));
                continue;
            }

            values.Add(key, value);
        }

        return new DotEnvParseResult(values, failures);
    }

    private static bool TryParseValue(string input, out string value)
    {
        var candidate = input.Trim();

        if (candidate.EndsWith('\\'))
        {
            value = string.Empty;
            return false;
        }

        if (candidate.Length == 0)
        {
            value = string.Empty;
            return true;
        }

        if (candidate[0] == '\'')
        {
            var closing = candidate.IndexOf('\'', 1);

            if (closing < 0 || !HasOnlyTrailingComment(candidate[(closing + 1)..]))
            {
                value = string.Empty;
                return false;
            }

            value = candidate[1..closing];
            return true;
        }

        if (candidate[0] == '"')
        {
            return TryParseDoubleQuotedValue(candidate, out value);
        }

        var commentIndex = FindUnquotedComment(candidate);
        value = (commentIndex >= 0 ? candidate[..commentIndex] : candidate).TrimEnd();
        return true;
    }

    private static bool TryParseDoubleQuotedValue(string candidate, out string value)
    {
        var builder = new StringBuilder();
        var escaped = false;

        for (var index = 1; index < candidate.Length; index++)
        {
            var character = candidate[index];

            if (escaped)
            {
                var decoded = character switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    '\\' => '\\',
                    '"' => '"',
                    _ => '\0'
                };

                if (decoded == '\0')
                {
                    value = string.Empty;
                    return false;
                }

                builder.Append(decoded);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                if (!HasOnlyTrailingComment(candidate[(index + 1)..]))
                {
                    value = string.Empty;
                    return false;
                }

                value = builder.ToString();
                return true;
            }

            builder.Append(character);
        }

        value = string.Empty;
        return false;
    }

    private static bool HasOnlyTrailingComment(string remainder)
    {
        var trimmed = remainder.TrimStart();
        return trimmed.Length == 0 || trimmed.StartsWith('#');
    }

    private static int FindUnquotedComment(string value)
    {
        for (var index = 1; index < value.Length; index++)
        {
            if (value[index] == '#' && char.IsWhiteSpace(value[index - 1]))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsValidKey(string key)
    {
        if (key.Length == 0 || !(key[0] == '_' || char.IsAsciiLetter(key[0])))
        {
            return false;
        }

        return key.All(character =>
            character == '_' || char.IsAsciiLetterOrDigit(character));
    }

    private static ValidationFailure FormatFailure(int lineNumber) =>
        new("ENV_FORMAT", $"The staging environment file has an invalid entry at line {lineNumber}.");

    private static DotEnvParseResult Failed(string code, string message) =>
        new(
            new Dictionary<string, string>(StringComparer.Ordinal),
            [new ValidationFailure(code, message)]);
}
