using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Minecraft_Monitor.Helpers
{
    /// <summary>
    /// Minimum parsing of the NBT format returned by the Minecraft commands.
    /// </summary>
    public static class NBTParser
    {
        public static string GetJsonFromNBT(string nbtString)
        {
            if (!string.IsNullOrWhiteSpace(nbtString))
            {
                try
                {
                    nbtString = nbtString.Replace("'", string.Empty);
                    nbtString = Regex.Replace(nbtString, @"([{,])(\s*)([A-Za-z0-9_\-]+?)\s*(:\s*)", "$1\"$3\":"); // Quotes around keys.
                    nbtString = Regex.Replace(nbtString, @"(:)(\s*)([A-Za-z0-9_\-]+?)([,}])(\s*)", "$1\"$3\"$4"); // Quotes around values.
                    nbtString = Regex.Replace(nbtString, "[\"]([-\\d]\\d*)[bslfd][\"]", "$1", RegexOptions.IgnoreCase); // Remove numeric NBT tags.
                    nbtString = Regex.Replace(nbtString, "(\\d*.[\\d*])d", "$1", RegexOptions.IgnoreCase); // Remove numeric NBT tags for NBT without quotes.

                    JsonSerializer.Deserialize<List<Item>>(nbtString, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    });

                    return nbtString;
                }
                catch { }
            }

            return null;
        }
    }

    public class Item
    {
        public string Id { get; set; }
        public int Slot { get; set; }
        public int Count { get; set; }
        public object Tag { get; set; }
    }

    public class Enchantment
    {
        public string Id { get; set; }
        public int Lvl { get; set; }
    }

    public class Display
    {
        public string Name { get; set; }
    }

    public class Name
    {
        public string Text { get; set; }
    }
}