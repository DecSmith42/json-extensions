namespace DecSm.Extensions.Json;

/// <summary>
///     Provides utility methods for flattening and unflattening JSON structures.
///     This class enables conversion between hierarchical JSON objects and flat key-value pairs
///     using a colon-separated path notation for nested properties and bracket notation for array indices.
/// </summary>
[PublicAPI]
public static partial class JsonUtil
{
    /// <summary>
    ///     Regular expression pattern to identify array index notation in flattened keys.
    ///     Matches strings that start with '[', contain one or more digits, and end with ']'.
    ///     Examples: [0], [42], [123]
    /// </summary>
    [GeneratedRegex(@"^\[\d+\]$", RegexOptions.IgnoreCase)]
    private static partial Regex JsonCollectionRegex { get; }

    public static JsonObject Replace(this JsonObject obj, string key, string value)
    {
        // If the key doesn't contain colons, handle as a simple key replacement
        if (!key.Contains(':'))
        {
            var simpleResult = new JsonObject();

            // Copy all existing key-value pairs
            foreach (var kvp in obj)
                simpleResult[kvp.Key] = kvp.Value?.DeepClone();

            // Replace the specified key with the new value
            // Don't add it if it doesn't already exist
            if (obj.ContainsKey(key))
                simpleResult[key] = value;

            return simpleResult;
        }

        // Handle nested path replacement
        var complexResult = obj
            .DeepClone()
            .AsObject();

        var pathSegments = key.Split(':');
        var current = complexResult;

        // Navigate to the parent of the target key
        for (var i = 0; i < pathSegments.Length - 1; i++)
        {
            var segment = pathSegments[i];

            if (current[segment] is JsonObject nestedObj)
                current = nestedObj;
        }

        // Set the value at the final path segment
        var finalKey = pathSegments[^1];
        current[finalKey] = value;

        return complexResult;
    }

    /// <summary>
    ///     Flattens a hierarchical JSON structure into a flat list of key-value pairs.
    ///     Nested objects are represented using colon-separated paths, and array elements
    ///     use bracket notation with zero-based indices.
    /// </summary>
    /// <param name="node">The JSON node to flatten. Can be a JsonObject, JsonArray, or primitive value.</param>
    /// <returns>
    ///     A read-only list of key-value pairs where:
    ///     - Keys represent the path to each value using colon separation for objects and [index] for arrays
    ///     - Values are the string representation of the JSON values (null for JSON null values)
    /// </returns>
    /// <example>
    ///     <code>
    /// var json = JsonNode.Parse("""{"user":{"name":"John","tags":["admin","user"]}}""");
    /// var flattened = json.Flatten();
    /// // Result: [("user:name", "John"), ("user:tags:[0]", "admin"), ("user:tags:[1]", "user")]
    /// </code>
    /// </example>
    public static IReadOnlyList<KeyValuePair<string, string?>> Flatten(this JsonNode node)
    {
        var keyLookup = new Dictionary<string, int>();
        var flattened = new List<KeyValuePair<string, string?>>();

        Flatten(node, flattened, keyLookup, string.Empty);

        return flattened;
    }

    /// <summary>
    ///     Reconstructs a hierarchical JSON object from flattened key-value pairs.
    ///     This method reverses the flattening process by parsing colon-separated paths
    ///     and bracket notation to rebuild the original nested structure.
    /// </summary>
    /// <param name="flattened">
    ///     An enumerable of tuples containing flattened key-value pairs where:
    ///     - Key: Path string using colon separation for objects and [index] notation for arrays
    ///     - Value: String representation of the value (null for JSON null values)
    /// </param>
    /// <returns>
    ///     A JsonObject representing the reconstructed hierarchical structure.
    ///     Arrays and nested objects are created as needed based on the path notation.
    /// </returns>
    /// <example>
    ///     <code>
    /// var flattened = new[]
    /// {
    ///     ("user:name", "John"),
    ///     ("user:addresses:[0]:city", "New York"),
    ///     ("user:addresses:[0]:zip", "10001"),
    ///     ("user:tags:[0]", "admin"),
    ///     ("user:tags:[1]", "user")
    /// };
    /// var json = flattened.Unflatten();
    /// // Result: {"user":{"name":"John","addresses":[{"city":"New York","zip":"10001"}],"tags":["admin","user"]}}
    /// </code>
    /// </example>
    /// <remarks>
    ///     The method handles complex scenarios including:
    ///     - Creating nested objects and arrays as needed
    ///     - Managing transitions between object and array contexts
    ///     - Handling array indices that may not be sequential
    ///     - Overwriting existing values when the same path is encountered multiple times
    ///     Path parsing rules:
    ///     - Colons (':') separate object property names
    ///     - Square brackets with numbers ([0], [1], etc.) indicate array indices
    ///     - Mixed object/array paths are supported (e.g., "users:[0]:name")
    /// </remarks>
    public static JsonObject Unflatten(this IEnumerable<KeyValuePair<string, string?>> flattened)
    {
        var obj = new JsonObject();

        foreach (var pair in flattened)
        {
            // Split the flattened key into path segments using colon as delimiter
            var path = pair.Key.Split(':');

            // Track the current object context during path traversal
            var currentObject = obj;

            // Track the current array context (null when not in an array)
            JsonArray? currentArray = null;

            // Traverse each segment of the path to build the hierarchical structure
            for (var i = 0; i < path.Length; i++)
            {
                var pathPart = path[i];

                // Process intermediate path segments (not the final value)
                if (i < path.Length - 1)
                {
                    // Handle array index notation [0], [1], etc.
                    if (TryParseArrayIndex(pathPart, out var index))
                    {
                        var array = new JsonArray();

                        if (currentArray is not null)
                        {
                            if (currentArray.Count > index)
                            {
                                currentObject = currentArray[index] as JsonObject;
                                currentArray = currentArray[index] as JsonArray;

                                continue;
                            }

                            if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
                            {
                                currentArray.Add(array);
                                currentArray = array;

                                continue;
                            }

                            var newObject = new JsonObject();
                            currentArray.Add(newObject);
                            currentObject = newObject;
                            currentArray = null;

                            continue;
                        }

                        if (currentObject![pathPart] is not null)
                        {
                            currentArray = currentObject[pathPart] as JsonArray;
                            currentObject = currentObject[pathPart] as JsonObject;

                            continue;
                        }

                        currentObject.Add(pathPart, array);
                        currentArray = array;

                        continue;
                    }

                    if (currentArray is not null)
                    {
                        var section = new JsonObject();
                        currentArray.Add(section);
                        currentObject = section;
                        currentArray = null;

                        continue;
                    }

                    if (currentObject!.ContainsKey(pathPart))
                    {
                        if (JsonCollectionRegex.Match(path[i + 1])
                            .Success)
                            currentArray = currentObject[pathPart] as JsonArray;

                        currentObject = currentObject[pathPart] as JsonObject;

                        continue;
                    }

                    if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
                    {
                        currentArray = [];
                        currentObject.Add(pathPart, currentArray);

                        continue;
                    }

                    var newSection = new JsonObject();
                    currentObject.Add(pathPart, newSection);
                    currentObject = newSection;

                    continue;
                }

                // Process the final path segment (the actual value assignment)
                if (TryParseArrayIndex(pathPart, out var finalIndex))
                {
                    if (currentArray is not null)
                    {
                        if (currentArray.Count > finalIndex)
                        {
                            currentArray[finalIndex] = pair.Value;

                            continue;
                        }

                        currentArray.Add(pair.Value);

                        continue;
                    }

                    if (currentObject![pathPart] is not null)
                    {
                        currentObject[pathPart] = pair.Value;

                        continue;
                    }

                    currentObject.Add(pathPart, pair.Value);

                    continue;
                }

                if (currentArray is not null)
                {
                    currentArray.Add(pair.Value);

                    continue;
                }

                if (currentObject!.ContainsKey(pathPart))
                {
                    currentObject[pathPart] = pair.Value;

                    continue;
                }

                currentObject.Add(pathPart, pair.Value);
            }
        }

        return obj;
    }

    /// <summary>
    ///     Efficiently parses array index from bracket notation using Span operations.
    ///     This method avoids regex overhead and string allocations for better performance.
    /// </summary>
    /// <param name="input">The input string to parse (e.g., "[42]")</param>
    /// <param name="index">The parsed index value if successful</param>
    /// <returns>True if the input represents a valid array index, false otherwise</returns>
    private static bool TryParseArrayIndex(string input, out int index)
    {
        index = 0;

        if (input.Length < 3 || input[0] != '[' || input[^1] != ']')
            return false;

        var span = input.AsSpan(1, input.Length - 2);

        return int.TryParse(span, out index);
    }

    /// <summary>
    ///     Recursively flattens a JSON node into key-value pairs with path-based keys.
    ///     This is a helper method that performs the actual flattening logic.
    /// </summary>
    /// <param name="node">The current JSON node to process (can be null).</param>
    /// <param name="flattened">The list to add flattened key-value pairs to.</param>
    /// <param name="keyLookup">Dictionary for O(1) key lookups to avoid duplicates.</param>
    /// <param name="prefix">The current path prefix for building hierarchical keys.</param>
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
        string prefix)
    {
        switch (node)
        {
            case JsonArray array:
            {
                var sb = new StringBuilder(prefix.Length + 10);
                sb.Append(prefix);
                sb.Append(":[");
                var baseLength = sb.Length;

                for (var i = 0; i < array.Count; i++)
                {
                    sb.Length = baseLength;
                    sb.Append(i);
                    sb.Append(']');
                    Flatten(array[i], flattened, keyLookup, sb.ToString());
                }

                break;
            }
            case JsonObject obj:
            {
                var sb = new StringBuilder(prefix.Length + 50);

                if (prefix.Length > 0)
                {
                    sb.Append(prefix);
                    sb.Append(':');
                }

                var baseLength = sb.Length;

                foreach (var pair in obj)
                {
                    sb.Length = baseLength;
                    sb.Append(pair.Key);
                    Flatten(pair.Value, flattened, keyLookup, sb.ToString());
                }

                break;
            }
            default:
            {
                var kvp = new KeyValuePair<string, string?>(prefix, node?.ToString());

                if (keyLookup.TryGetValue(prefix, out var existingIndex))
                {
                    flattened[existingIndex] = kvp;
                }
                else
                {
                    keyLookup[prefix] = flattened.Count;
                    flattened.Add(kvp);
                }

                break;
            }
        }
    }
}
