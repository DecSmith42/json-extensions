namespace DecSm.Extensions.Json;

public static class JsonUtil
{
    public static JsonObject ToFlattenedJsonObject(this JsonObject jsonObject, string separator = ":")
    {
        var output = new JsonObject();

        // Iterative DFS traversal using an explicit stack to preserve DFS order
        var stack = new Stack<(JsonNode? Node, string Path)>();

        // Seed stack with root object's properties in reverse insertion order
        var children = new List<KeyValuePair<string, JsonNode?>>();

        children.AddRange(jsonObject);

        for (var i = children.Count - 1; i >= 0; i--)
        {
            var (key, value) = (children[i].Key, children[i].Value);
            stack.Push((value, key));
        }

        while (stack.Count > 0)
        {
            var (node, path) = stack.Pop();

            if (node is null)
            {
                output[path] = null;

                continue;
            }

            switch (node)
            {
                case JsonObject obj:
                {
                    // Push properties in reverse to process them in insertion order
                    children.Clear();
                    children.AddRange(obj);

                    for (var i = children.Count - 1; i >= 0; i--)
                    {
                        var (key, value) = (children[i].Key, children[i].Value);
                        stack.Push((value, string.Concat(path, separator, key)));
                    }

                    break;
                }

                case JsonArray arr:
                {
                    // Push elements in reverse index order so 0..N are processed in order
                    for (var i = arr.Count - 1; i >= 0; i--)
                        stack.Push((arr[i], string.Concat(path, separator, i.ToString())));

                    break;
                }

                default:
                {
                    // Leaf value: clone to keep original types and avoid sharing references
                    output[path] = node.DeepClone();

                    break;
                }
            }
        }

        return output;
    }

    public static Dictionary<string, string?> ToFlattenedDictionary(this JsonObject jsonObject, string separator = ":")
    {
        var output = new Dictionary<string, string?>();

        // Iterative DFS traversal using an explicit stack to preserve DFS order
        var stack = new Stack<(JsonNode? Node, string Path)>();

        // Seed stack with root object's properties in reverse insertion order
        var children = new List<KeyValuePair<string, JsonNode?>>();
        children.AddRange(jsonObject);

        for (var i = children.Count - 1; i >= 0; i--)
        {
            var (key, value) = (children[i].Key, children[i].Value);
            stack.Push((value, key));
        }

        while (stack.Count > 0)
        {
            var (node, path) = stack.Pop();

            if (node is null)
            {
                // Note: assigning null to Dictionary<string,string> will produce a nullable warning in nullable-enabled contexts
                // but is allowed at runtime and preserves parity with Flatten(JsonObject) which retains nulls.
                output[path] = null!;

                continue;
            }

            switch (node)
            {
                case JsonObject obj:
                {
                    // Push properties in reverse to process them in insertion order
                    children.Clear();
                    children.AddRange(obj);

                    for (var i = children.Count - 1; i >= 0; i--)
                    {
                        var (key, value) = (children[i].Key, children[i].Value);
                        stack.Push((value, string.Concat(path, separator, key)));
                    }

                    break;
                }

                case JsonArray arr:
                {
                    // Push elements in reverse index order so 0..N are processed in order
                    for (var i = arr.Count - 1; i >= 0; i--)
                        stack.Push((arr[i], string.Concat(path, separator, i.ToString())));

                    break;
                }

                default:
                {
                    output[path] = node.GetValueKind() switch
                    {
                        JsonValueKind.Undefined => throw new InvalidOperationException(
                            "Undefined JsonNode cannot be converted to string"),
                        JsonValueKind.Object => throw new InvalidOperationException(
                            "Object JsonNode cannot be converted to string"),
                        JsonValueKind.Array => throw new InvalidOperationException(
                            "Array JsonNode cannot be converted to string"),
                        JsonValueKind.String => node.ToString(),
                        JsonValueKind.Number => node.ToJsonString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => null,
                        _ => throw new InvalidOperationException("Unknown JsonNode kind"),
                    };

                    break;
                }
            }
        }

        return output;
    }

    public static bool HasNestedObjects(this JsonObject jsonObject)
    {
        foreach (var child in jsonObject)
            if (child.Value is JsonObject or JsonArray)
                return true;

        return false;
    }

    public static JsonObject ToUnflattenedJsonObject(this JsonObject jsonObject, string separator = ":")
    {
        // Root of the reconstructed tree
        var root = new JsonObject();

        foreach (var (path, value) in jsonObject)
        {
            // Split by the provided separator (can be multi-character)
            var segments = path.Split([separator], StringSplitOptions.None);

            if (segments.Length == 0)
                continue;

            JsonNode current = root;

            // Traverse/create intermediate containers
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var seg = segments[i];
                var nextSeg = segments[i + 1];
                var nextIsIndex = int.TryParse(nextSeg, out _);

                switch (current)
                {
                    case JsonObject obj:
                    {
                        var child = obj[seg];

                        if (child is null)
                        {
                            child = nextIsIndex
                                ? new JsonArray()
                                : new JsonObject();

                            obj[seg] = child;
                        }

                        current = child;

                        break;
                    }

                    case JsonArray arr:
                    {
                        if (!int.TryParse(seg, out var index))
                            throw new InvalidOperationException($"Segment '{seg}' is not a valid array index.");

                        // Ensure capacity
                        while (arr.Count <= index)
                            arr.Add(null);

                        var child = arr[index];

                        if (child is null)
                        {
                            child = nextIsIndex
                                ? new JsonArray()
                                : new JsonObject();

                            arr[index] = child;
                        }

                        current = child;

                        break;
                    }

                    default:
                    {
                        // We hit a primitive where a container is expected; replace it with an object/array.
                        // This scenario shouldn't occur with valid flattened input, but we guard defensively.
                        JsonNode replacement = nextIsIndex
                            ? new JsonArray()
                            : new JsonObject();

                        // We cannot directly replace without knowing the parent; however, with valid input this won't happen.
                        // So just set current to the replacement to continue building (structure will be reachable from root only in valid inputs).
                        current = replacement;

                        break;
                    }
                }
            }

            // Assign the final value at the last segment
            var lastSeg = segments[^1];

            switch (current)
            {
                case JsonObject lastObj:
                    lastObj[lastSeg] = value?.DeepClone();

                    break;

                case JsonArray lastArr:
                {
                    if (!int.TryParse(lastSeg, out var index))
                        throw new InvalidOperationException($"Segment '{lastSeg}' is not a valid array index.");

                    while (lastArr.Count <= index)
                        lastArr.Add(null);

                    lastArr[index] = value?.DeepClone();

                    break;
                }

                default:
                    // As above, this should not occur for valid flattened input
                    throw new InvalidOperationException("Unexpected non-container node while assigning final segment.");
            }
        }

        return root;
    }
}
