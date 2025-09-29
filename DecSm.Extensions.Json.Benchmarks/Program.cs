using DecSm.Extensions.Json.Benchmarks;

BenchmarkRunner.Run<JsonUtilBenchmarks>(DefaultConfig
    .Instance
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(MarkdownExporter.GitHub)
    .AddExporter(JsonExporter.Brief)
    .AddExporter(HtmlExporter.Default));
