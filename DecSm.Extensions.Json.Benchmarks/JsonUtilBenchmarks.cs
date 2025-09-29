namespace DecSm.Extensions.Json.Benchmarks;

public class JsonUtilBenchmarks
{
    private Dictionary<string, string?> _flattened = null!;

    private JsonNode _jsonNode = null!;
    private JsonObject _jsonObject = null!;

    private Dictionary<string, string?> _replacements = null!;

    // Size parameter to generate larger deterministic payloads
    [Params(2, 10, 100)]
    [UsedImplicitly]
    public int Scale { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Dynamically (but deterministically) generate a payload whose size depends on Scale
        _jsonObject = CreateSamplePayload(Scale);

        _jsonNode = _jsonObject.DeepClone();

        // Precompute flattened form for unflatten benchmark
        _flattened = JsonExtensions
            .Flatten(_jsonObject)
            .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))
            .ToDictionary();

        // Replacements used by Replace and ReplaceMany (paths are guaranteed to exist)
        _replacements = new()
        {
            ["user:name"] = "Jane",
            ["user:tags:[0]"] = "owner",
            ["user:addresses:[1]:zip"] = "02199",
            ["user:extras:p0"] = "zero",
            ["user:extras:p1"] = "one",
            ["user:extras:p2"] = "two",
            ["user:extras:p3"] = "three",
            ["user:extras:p4"] = "four",
            ["user:extras:p5"] = "five",
            ["user:extras:p6"] = "six",
            ["user:extras:p7"] = "seven",
            ["user:extras:p8"] = "eight",
            ["user:extras:p9"] = "nine",
        };
    }

    private static JsonObject CreateSamplePayload(int scale)
    {
        // Ensure we always have at least 2 addresses and 1 tag for replacements to target
        var addressesCount = Math.Max(2, scale);
        var tagsCount = Math.Max(1, scale * 2);
        var extrasCount = scale * 5;

        var user = new JsonObject
        {
            ["name"] = $"User_{scale}",
        };

        // Addresses: array of objects with deterministic values
        var addresses = new JsonArray();

        for (var i = 0; i < addressesCount; i++)
            addresses.Add(new JsonObject
            {
                ["city"] = $"City_{i}",
                ["zip"] = (10_000 + i).ToString(),
            });

        user["addresses"] = addresses;

        // Tags: array of simple strings
        var tags = new JsonArray();

        for (var i = 0; i < tagsCount; i++)
            tags.Add($"tag_{i}");

        user["tags"] = tags;

        // Extras: large object with many properties to scale object-branch fanout
        var extras = new JsonObject();

        for (var i = 0; i < extrasCount; i++)
            extras[$"p{i}"] = i.ToString();

        user["extras"] = extras;

        return new()
        {
            ["user"] = user,
        };
    }

    [Benchmark(Description = "Flatten: JsonNode -> pairs")]
    public IDictionary<string, string?> Flatten_Benchmark() =>
        JsonExtensions.Flatten(_jsonNode);

    [Benchmark(Description = "Unflatten: pairs -> JsonObject")]
    public JsonObject Unflatten_Benchmark() =>
        JsonExtensions.Unflatten(_flattened);

    [Benchmark(Description = "Replace single value by path")]
    public JsonObject Replace_Single_Benchmark() =>
        _jsonObject.ReplaceValue("user:name", "Jane");

    [Benchmark(Description = "Replace many values by dictionary")]
    public JsonObject Replace_Many_Benchmark() =>
        _jsonObject
            .DeepClone()
            .AsObject()
            .ReplaceValues(_replacements);
}
