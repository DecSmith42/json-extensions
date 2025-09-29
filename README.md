# DecSm.Extensions.Json

Lightweight, allocation-conscious helpers for working with System.Text.Json.Nodes. Provides a simple, human-readable path notation to:
- Flatten JSON into path/value pairs
- Unflatten path/value pairs back to JSON
- Replace a single value by path
- Apply many in-place replacements by path

The library targets .NET 8.0 and 9.0.

## Install

From your project directory:

```bash
dotnet add package DecSm.Extensions.Json
```

## Getting started

The helpers operate on System.Text.Json.Nodes (JsonObject, JsonArray, JsonValue). Add these using statements:

```csharp
using System.Text.Json.Nodes;
using DecSm.Extensions.Json;
```

### Path conventions
- Object properties are separated by colons, e.g. `user:address:city`.
- Arrays use bracketed indices when flattening/unflattening, e.g. `users:[0]:name`.
- For in-place replacement (ReplaceValues), arrays use bare numeric segments only, e.g. `users:0:name`.

### Flatten a JSON node

```csharp
var json = JsonNode.Parse("""{ "user": { "name": "John", "tags": ["admin", "user"] } }""")!;
var flattened = JsonExtensions.Flatten(json);
// flattened is an IDictionary<string, string?> like:
// [
//   ("user:name", "John"),
//   ("user:tags:[0]", "admin"),
//   ("user:tags:[1]", "user")
// ]
```

### Unflatten to a JSON object

```csharp
var flat = new Dictionary<string, string?>
{
    ["user:name"] = "John",
    ["user:tags:[0]"] = "admin",
    ["user:tags:[1]"] = "user",
};
var obj = JsonExtensions.Unflatten(flat);
// obj is a JsonObject:
// {"user":{"name":"John","tags":["admin","user"]}}
```

### Replace a single value in-place

```csharp
var root = JsonNode.Parse("""{ "user": { "details": { "city": "NYC" } } }""")!.AsObject();
var updated = root.ReplaceValue("user:details:city", "LA");
// updated: {"user":{"details":{"city":"LA"}}}
// Notes:
// - Only existing objects are traversed; missing segments are not created.
// - This method does not step into arrays.
// - Setting value to null writes a JSON null.
```

### Apply many replacements in-place

```csharp
var root2 = JsonNode.Parse("""
{
  "name": "A",
  "user": { "address": { "city": "NYC" } },
  "users": [ { "name": "Alice" }, { "name": "Bob" } ]
}
""")!.AsObject();

root2.ReplaceValues(new Dictionary<string, string?>
{
    ["name"] = "B",                 // root-level property if present
    ["user:address:city"] = "LA",   // nested object path if present
    ["users:1:name"] = "Robert",    // arrays use bare numeric segments
});
// root2 is modified in-place
```

Important notes for ReplaceValues:
- No new properties/containers are created; only existing ones are updated.
- If a nested path can’t be fully traversed, but the root contains a literal property equal to the remaining colon-joined path, that property is updated.
- Bracketed indices like `[0]` are ignored by ReplaceValues; use bare numeric segments (`users:0:name`).

## Projects in this repo
- DecSm.Extensions.Json — the library
- DecSm.Extensions.Json.Tests — unit tests (NUnit + Shouldly)
- DecSm.Extensions.Json.Benchmarks — microbenchmarks (BenchmarkDotNet)
- _atom — Atom build definition and GitHub workflow generation

## Build, test, and benchmarks with Atom
This repository uses [Atom](https://github.com/decsm/atom) for local automation and CI workflows.

Install Atom as a global .NET tool:

```bash
dotnet tool install -g decsm.atom.tool
```

Run tasks from the repository root:

```bash
# Pack the NuGet package
atom PackJsonExtensions

# Run unit tests
atom TestJsonExtensions

# Run benchmarks (BenchmarkDotNet); reports are published under _publish/DecSm.Extensions.Json.Benchmarks
atom BenchmarkJsonExtensions
```

## Build and test with the .NET CLI (without Atom)

```bash
# Build the solution
dotnet build -c Release

# Run tests
dotnet test DecSm.Extensions.Json.Tests -c Release
```

## Requirements
- .NET SDK 9.0 or later
- Library targets: net8.0; net9.0;

## License

This project is licensed under the MIT License. See LICENSE.txt for details.