using Repository.Enums;
using System.Text.Json;

namespace BusinessLogic.Helpers
{
    public class JsonExtractHelper
    {
        public JsonExtractHelper() { }
        public bool? GetBoolFromFilter(Dictionary<string, object> filters, string key)
        {
            if (!filters.TryGetValue(key, out var raw)) return null;

            if (raw is JsonElement e)
            {
                if (e.ValueKind == JsonValueKind.True) return true;
                if (e.ValueKind == JsonValueKind.False) return false;

                // If body sends "true" / "false" as strings
                if (e.ValueKind == JsonValueKind.String &&
                    bool.TryParse(e.GetString(), out var parsed))
                    return parsed;
            }

            return null;
        }
        public string? GetStringFromFilter(Dictionary<string, object> filters, string key)
        {
            if (!filters.TryGetValue(key, out var raw))
                return null;
            if (raw is JsonElement e && e.ValueKind == JsonValueKind.String)
            {
                return e.GetString();
            }
            return null;
        }
        public QuizSetTypeEnum? GetEnumFromFilter(Dictionary<string, object> filters, string key)
        {
            if (!filters.TryGetValue(key, out var raw))
                return null;

            if (raw is JsonElement e)
            {
                // If client sends a string: "1" or "MultipleChoice"
                if (e.ValueKind == JsonValueKind.String)
                {
                    var str = e.GetString();
                    if (Enum.TryParse<QuizSetTypeEnum>(str, true, out var parsed))
                        return parsed;

                    // If string contains a number → "1"
                    if (int.TryParse(str, out var num) && Enum.IsDefined(typeof(QuizSetTypeEnum), num))
                        return (QuizSetTypeEnum)num;
                }

                // If client sends a number directly: 1
                if (e.ValueKind == JsonValueKind.Number)
                {
                    if (e.TryGetInt32(out var num) &&
                        Enum.IsDefined(typeof(QuizSetTypeEnum), num))
                    {
                        return (QuizSetTypeEnum)num;
                    }
                }
            }

            return null;
        }
    }
}
