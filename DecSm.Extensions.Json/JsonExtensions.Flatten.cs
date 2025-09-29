namespace DecSm.Extensions.Json;

/// <summary>
///     Extension methods for working with System.Text.Json nodes using a human-readable path notation.
///     Path conventions:
///     - Object properties are separated by colons (e.g., "user:address:city").
///     - Arrays are addressed with bracketed indices (e.g., "users:[0]:name") in flattened/unflattened keys.
///     - For in-place replacement via <see cref="ReplaceValues" />, array steps use bare numeric segments (e.g., "users:0:name").
///     These helpers are allocation‑conscious and designed for clarity when manipulating JSON in config and ETL scenarios.
/// </summary>
public static partial class JsonExtensions
{
    /// <summary>
    ///     Flattens a hierarchical JSON structure into a flat dictionary of path/value pairs.
    ///     Nested objects are represented using colon-separated paths, and array elements
    ///     use bracket notation with zero-based indices.
    /// </summary>
    /// <param name="node">The JSON node to flatten. Can be a JsonObject, JsonArray, or primitive value.</param>
    /// <returns>
    ///     A read-only dictionary mapping each path to its string representation.
    ///     - Keys use colons for object properties and [index] notation for arrays.
    ///     - Values are the string representation of the JSON values (null for JSON null values).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var json = JsonNode.Parse("""{ "user": { "name": "John", "tags": ["admin", "user"] } }""");
    /// var flattened = JsonExtensions.Flatten(json);
    /// // Result: [("user:name", "John"), ("user:tags:[0]", "admin"), ("user:tags:[1]", "user")]
    /// ]]></code>
    /// </example>
    public static IReadOnlyDictionary<string, string?> Flatten(JsonNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var keyLookup = new Dictionary<string, int>();
        var flattened = new List<KeyValuePair<string, string?>>();
        var sb = new StringBuilder(64);

        Flatten(node, flattened, keyLookup, sb);

        return flattened.ToDictionary();
    }

    /// <summary>
    ///     Recursively flattens a JSON node into key-value pairs with path-based keys.
    ///     This is a helper method that performs the actual flattening logic.
    /// </summary>
    /// <param name="node">The current JSON node to process (can be null).</param>
    /// <param name="flattened">The list to add flattened key-value pairs to.</param>
    /// <param name="keyLookup">Dictionary for O(1) key lookups to avoid duplicates.</param>
    /// <param name="sb">A reusable StringBuilder holding the current path being built.</param>
    /// <remarks>
    ///     The method handles three cases:
    ///     - JsonArray: Iterates through elements with [index] notation
    ///     - JsonObject: Iterates through properties with colon-separated paths
    ///     - Primitive values: Adds the value with the current prefix as key
    ///     If a key already exists in the flattened list, it replaces the existing value.
    ///     This can happen with complex nested structures during the flattening process.
    /// </remarks>
    private static void Flatten(
        this JsonNode? node,
        List<KeyValuePair<string, string?>> flattened,
        Dictionary<string, int> keyLookup,
        StringBuilder sb)
    {
        switch (node)
        {
            case JsonArray array:
            {
                var baseLen = sb.Length;
                sb.Append(":[");
                var indexStart = sb.Length;

                for (var i = 0; i < array.Count; i++)
                {
                    sb.Length = indexStart;
                    sb.Append(i);
                    sb.Append(']');
                    Flatten(array[i], flattened, keyLookup, sb);
                }

                sb.Length = baseLen;

                break;
            }
            case JsonObject obj:
            {
                var baseLen = sb.Length;

                if (baseLen > 0)
                    sb.Append(':');

                var keyStart = sb.Length;

                foreach (var pair in obj)
                {
                    sb.Length = keyStart;
                    sb.Append(pair.Key);
                    Flatten(pair.Value, flattened, keyLookup, sb);
                }

                sb.Length = baseLen;

                break;
            }
            default:
            {
                var key = sb.ToString();
                var kvp = new KeyValuePair<string, string?>(key, node?.ToString());

                if (keyLookup.TryGetValue(key, out var existingIndex))
                {
                    flattened[existingIndex] = kvp;
                }
                else
                {
                    keyLookup[key] = flattened.Count;
                    flattened.Add(kvp);
                }

                break;
            }
        }
    }
}
