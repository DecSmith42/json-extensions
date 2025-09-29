namespace DecSm.Extensions.Json;

public static partial class JsonExtensions
{
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="flattened" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
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
    /// ]]></code>
    /// </example>
    /// <remarks>
    ///     The method handles common scenarios including:
    ///     - Creating nested objects and arrays as needed
    ///     - Managing transitions between object and array contexts
    ///     - Overwriting existing values when the same path is encountered multiple times
    ///     - Note: Array indices are applied in append order; sparse/non-sequential indices are not padded.
    ///     Path parsing rules:
    ///     - Colons (':') separate object property names
    ///     - Square brackets with numbers ([0], [1], etc.) indicate array indices
    ///     - Mixed object/array paths are supported (e.g., "users:[0]:name")
    /// </remarks>
    public static JsonObject Unflatten(IDictionary<string, string?> flattened)
    {
        ArgumentNullException.ThrowIfNull(flattened);

        var obj = new JsonObject();

        foreach (var (key, value) in flattened)
        {
            // Split the flattened key into path segments using colon as delimiter
            var path = key.Split(':');

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
                        if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
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
                            currentArray[finalIndex] = value;

                            continue;
                        }

                        currentArray.Add(value);

                        continue;
                    }

                    if (currentObject![pathPart] is not null)
                    {
                        currentObject[pathPart] = value;

                        continue;
                    }

                    currentObject.Add(pathPart, value);

                    continue;
                }

                if (currentArray is not null)
                {
                    currentArray.Add(value);

                    continue;
                }

                if (currentObject!.ContainsKey(pathPart))
                {
                    currentObject[pathPart] = value;

                    continue;
                }

                currentObject.Add(pathPart, value);
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
}
