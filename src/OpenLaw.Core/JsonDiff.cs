using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;

namespace Clarius.OpenLaw;

public static class JsonDiff
{
    public static JsonPatch[] Diff(string first, string second)
    {
        if (JsonDiffPatcher.Diff(first, second, new JsonPatchDeltaFormatter()) is not JsonArray diff ||
            diff.Count == 0)
            return [];

        return JsonSerializer.Deserialize<JsonPatch[]>(diff) ?? [];
    }
}
