using System;
using System.Text;
using Newtonsoft.Json;

namespace RustyWatcher.Extensions;

public static class StringEx
{
    public static string[] SplitInParts(this string s, int partLength)
    {
        if (s == null)
            throw new ArgumentNullException(nameof(s));
        
        if (partLength <= 0)
            throw new ArgumentException("Part length has to be positive.", nameof(partLength));

        var splitResult = new string[Convert.ToInt32(Math.Ceiling(s.Length / (float)partLength))];
        for (int i = 0, c = 0; i < s.Length; i += partLength, c++)
            splitResult[c] = s.Substring(i, Math.Min(partLength, s.Length - i));

        return splitResult;
    }

    public static string TrimWhitespaces(this string value)
    {
        var builder = new StringBuilder();
        var previousWhitespace = false;

        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                if (previousWhitespace)
                {
                    continue;
                }

                previousWhitespace = true;
            }
            else
            {
                previousWhitespace = false;
            }

            builder.Append(value[i]);
        }

        return builder.ToString();
    }
    
    public static bool TryParseJson<T>(this string obj, out T? result)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            result = JsonConvert.DeserializeObject<T>(obj, settings);
            return true;
        }
        catch (Exception)
        {
            result = default(T);
            return false;
        }
    }
    
    public static string GetCompleteHex(this string input)
    {
        var colour = string.Empty;
        if (input.Length < 6)
        {
            var multiplier = 6 / input.Length;
            foreach (char c in input)
            {
                for (int i = 0; i < multiplier; i++)
                {
                    colour += c;
                }
            }
        }
        else 
            colour = input;

        return colour;
    }
    
    public static int LevenshteinDistanceRate(this string s, string? c)
    {
        if (string.IsNullOrEmpty(c))
            return 0;
        
        var maxLength = Math.Max(s.Length, c.Length);
        return (int)((1.0 - (double)s.LevenshteinDistance(c) / maxLength) * 100);
    }
    
    public static int LevenshteinDistance(this string input, string t)
    {
        var n = input.Length;
        var m = t.Length;

        // Step 1
        if (n == 0)
            return m;

        if (m == 0)
            return n;
            
        var data = new int[n + 1, m + 1];

        for (var i = 0; i <= n; data[i, 0] = i++)
        {
        }

        for (var j = 0; j <= m; data[0, j] = j++)
        {
        }

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = t[j - 1] == input[i - 1] ? 0 : 1;

                data[i, j] = Math.Min(
                    Math.Min(data[i - 1, j] + 1, data[i, j - 1] + 1),
                    data[i - 1, j - 1] + cost);
            }
        }

        return data[n, m];
    }
}