namespace DecSm.Extensions.Json;

public static partial class JsonExtensions
{
    /// <summary>
    ///     Replaces the value at a specified path within a JSON object, returning a new object.
    /// </summary>
    /// <param name="root">The source JSON object to read from.</param>
    /// <param name="path">
    ///     The property path to replace. Use <see ref="separator" />-separated segments for nested objects
    ///     (e.g., "user:address:city"). Array indices are not supported by this method.
    ///     If the path contains no <see ref="separator" />s, it is treated as a simple, root-level property name.
    /// </param>
    /// <param name="value">The new value to assign at the specified path. Use <c>null</c> for JSON null.</param>
    /// <param name="separator">The separator used to split path segments. Defaults to colon (":").</param>
    /// <returns>
    ///     A new <see cref="JsonObject" /> with the requested change applied, leaving the original object unchanged.
    ///     For simple paths (no <see ref="separator" />s), the property is only replaced if it already exists; missing
    ///     properties are not added.
    ///     For <see ref="separator" />-separated paths, only existing intermediate objects are traversed; missing segments are
    ///     not created.
    ///     If a segment is missing, the value is set on the last navigated object (potentially the root) under the final
    ///     segment name.
    /// </returns>
    /// <remarks>
    ///     This method is intentionally conservative about creating structure. If you need conditional creation and
    ///     support for arrays, see
    ///     <see cref="M:DecSm.Extensions.Json.JsonExtensions.ReplaceValues(JsonObject,Dictionary{string,string},string)" />.
    ///     Setting <paramref name="value" /> to null results in a JSON null at the target path.
    ///     If <paramref name="root" /> is null, a <see cref="NullReferenceException" /> will be thrown by extension method
    ///     invocation semantics.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var obj = JsonNode.Parse("""{ "user": { "name": "John" } }""").AsObject();
    /// var updated = obj.ReplaceValue("user:name", "Jane");
    /// // {"user":{"name":"Jane"}}
    /// var rootOnly = obj.ReplaceValue("unknown", "x");
    /// // Unchanged structure; returns a clone with no new property added.
    /// ]]></code>
    /// </example>
    public static JsonObject ReplaceValue(this JsonObject root, string path, string? value, string separator = ":")
    {
        ArgumentNullException.ThrowIfNull(path);

        // Ignore empty paths for consistency with batch Replace
        if (path.Length is 0)
            return root;

        // If the path doesn't contain separator, handle as a simple key replacement
        if (!path.Contains(separator))
        {
            if (root.ContainsKey(path))
                root[path] = JsonValue.Create(value);

            return root;
        }

        // Handle nested path replacement
        var pathSegments = path.Split(separator);
        var current = root;

        // Navigate to the parent of the target key
        for (var i = 0; i < pathSegments.Length - 1; i++)
        {
            var segment = pathSegments[i];

            if (current[segment] is JsonObject nestedObj)
                current = nestedObj;
        }

        // Set the value at the final path segment
        var finalKey = pathSegments[^1];
        current[finalKey] = JsonValue.Create(value);

        return root;
    }
}
