namespace DecSm.Extensions.Json;

public static partial class JsonExtensions
{
    /// <summary>
    ///     Applies multiple replacements to a JSON object in-place using a consistent path notation.
    /// </summary>
    /// <param name="root">The root JSON object to modify.</param>
    /// <param name="replacements">
    ///     A mapping from path to new value. Paths can be:
    ///     - Simple property names (e.g., "name"). Only updated if the property exists on the root.
    ///     - <see ref="separator" />-separated nested paths (e.g., "user:address:city").
    ///     - Paths that step into arrays using bare numeric segments only (e.g., "users:0:name").
    ///     Note: Bracketed indices like "[0]" are ignored by this method.
    /// </param>
    /// <param name="separator">The segment separator to use for nested paths. Defaults to colon (":").</param>
    /// <returns>The same <see cref="JsonObject" /> instance passed in, after modifications.</returns>
    /// <remarks>
    ///     Behavior:
    ///     - No new properties or containers are created; only existing ones are updated.
    ///     - For <see ref="separator" />-separated paths, the method first attempts to traverse the structure. If traversal
    ///     fails
    ///     but the root contains a literal property equal to the remaining <see ref="separator" />-joined path, that property
    ///     is updated.
    ///     - Array indices must exist and be within bounds to be updated.
    ///     - Values are stored using <see cref="JsonValue.Create(string?, JsonNodeOptions?)" /> which preserves nulls.
    ///     - This method modifies the input object in-place.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replacements" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var obj = JsonNode.Parse("""{ "name": "John", "user": { "address": { "city":"NYC" } }, "users": [ { "name":"A" }, { "name":"B" } ] }""").AsObject();
    /// obj.ReplaceValues(new Dictionary<string,string?>
    /// {
    ///     ["name"] = "Jane",                  // replaces root-level property if present
    ///     ["user:address:city"] = "LA",      // traverses nested objects if present
    ///     ["users:1:name"] = "Beta"          // updates within existing arrays
    /// });
    /// ]]></code>
    /// </example>
    public static JsonObject ReplaceValues(
        this JsonObject root,
        Dictionary<string, string?> replacements,
        string separator = ":")
    {
        ArgumentNullException.ThrowIfNull(replacements);

        foreach (var (key, newValue) in replacements)
        {
            if (key.Length is 0)
                continue;

            // If key contains separator, attempt nested update first (preserve nested structure if it exists),
            // otherwise fall back to literal property update on the root if it exists.
            if (key.Contains(separator))
            {
                if (TrySetNestedIfExists(root, key, newValue, separator))
                    continue;

                if (root.TryGetPropertyValue(key, out _))
                    root[key] = JsonValue.Create(newValue);

                continue;
            }

            // Simple root-level property update if it exists.
            if (root.TryGetPropertyValue(key, out _))
                root[key] = JsonValue.Create(newValue);
        }

        return root;
    }

    /// <summary>
    ///     Helper that attempts to set a value using a separated path, only when the full path exists.
    ///     Supports stepping through objects and arrays (numeric segments). Does not create missing nodes.
    /// </summary>
    /// <param name="root">The root object to start from.</param>
    /// <param name="path">A separated path. Array positions can be specified using numeric segments.</param>
    /// <param name="value">The value to set (null for JSON null).</param>
    /// <param name="separator">The segment separator to use.</param>
    /// <returns><c>true</c> if the value was set; otherwise, <c>false</c>.</returns>
    private static bool TrySetNestedIfExists(JsonObject root, string path, string? value, string separator)
    {
        var parts = path.Split(separator);

        if (parts.Length == 0)
            return false;

        JsonNode? current = root;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var segment = parts[i];

            switch (current)
            {
                case JsonObject obj:
                    if (obj.TryGetPropertyValue(segment, out var next))
                    {
                        current = next;

                        continue;
                    }

                    // Fallback: if the nested object path doesn't exist, check if the current object
                    // contains a literal property with the remaining joined path and update it.
                    var remainingPath = string.Join(separator, parts[i..]);

                    if (!obj.TryGetPropertyValue(remainingPath, out _))
                        return false;

                    obj[remainingPath] = JsonValue.Create(value);

                    return true;

                case JsonArray arr:
                    if (!int.TryParse(segment, out var index) || index < 0 || index >= arr.Count)
                        return false;

                    current = arr[index];

                    continue;

                default:
                    return false;
            }
        }

        var last = parts[^1];

        switch (current)
        {
            case JsonObject obj:
                if (!obj.TryGetPropertyValue(last, out _))
                    return false;

                obj[last] = JsonValue.Create(value);

                return true;

            case JsonArray arr:
                if (!int.TryParse(last, out var index) || index < 0 || index >= arr.Count)
                    return false;

                arr[index] = JsonValue.Create(value);

                return true;

            default:
                return false;
        }
    }
}
