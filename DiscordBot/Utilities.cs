using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RustyWatcher
{
    public static class StringExt
    {
        public static IEnumerable<String> SplitInParts(this String s, Int32 partLength)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", nameof(partLength));

            for (var i = 0; i < s.Length; i += partLength)
                yield return s.Substring(i, Math.Min(partLength, s.Length - i));
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
    }

    public class RGB
    {
        [JsonProperty("Red")]
        public int Red = 44;
        [JsonProperty("Green")]
        public int Green = 47;
        [JsonProperty("Blue")]
        public int Blue = 51;

        public Color ToColor()
        {
            return new Color(Red, Green, Blue);
        }
    }
}
