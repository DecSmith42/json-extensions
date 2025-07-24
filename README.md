# DecSm.Extensions.Json

A C# library providing JSON utility extensions and helper methods.

## Overview

DecSm.Extensions.Json is a .NET library that extends JSON functionality with useful utilities and helper methods.

## Requirements

- .NET 9.0 or later

## Getting Started

Install the package via NuGet:

```bash
dotnet add package DecSm.Extensions.Json
```

## Features

### JSON Flattening and Unflattening

Convert between hierarchical JSON structures and flat key-value pairs using colon-separated path notation.

#### Flatten JSON Structure

Transform nested JSON objects and arrays into flat key-value pairs:

```csharp
using DecSm.Extensions.Json;

var json = new
{
    Name = "John",
    Address = new
    {
        Street = "123 Main St",
        City = "Anytown"
    },
    Tags = new[] { "tag1", "tag2" }
};

var flattened = json.Flatten();

// Result:
// {
//     "Name": "John",
//     "Address:Street": "123 Main St",
//     "Address:City": "Anytown",
//     "Tags:0": "tag1",
//     "Tags:1": "tag2"
// }
```

#### Unflatten JSON Structure

Convert flat key-value pairs back into a hierarchical JSON structure:

```csharp
using DecSm.Extensions.Json;

var flatJson = new Dictionary<string, object>
{
    { "Name", "John" },
    { "Address:Street", "123 Main St" },
    { "Address:City", "Anytown" },
    { "Tags:0", "tag1" },
    { "Tags:1", "tag2" }
};

var unflattened = flatJson.Unflatten();

// Result:
// {
//     "Name": "John",
//     "Address": {
//         "Street": "123 Main St",
//         "City": "Anytown"
//     },
//     "Tags": ["tag1", "tag2"]
// }
```

### JSON Value Replacement

Replace values in a JSON object based on a specified key path:

```csharp
using DecSm.Extensions.Json;

var json = new
{
    Name = "John",
    Address = new
    {
        Street = "123 Main St",
        City = "Anytown"
    }
};

var updatedJson = json.ReplaceValue("Address:City", "Newtown");

// Result:
// {
//     "Name": "John",
//     "Address": {
//         "Street": "123 Main St",
//         "City": "Newtown"
//     }
// }
```

## Development

### Prerequisites

- .NET 9.0 SDK
- A compatible IDE (Visual Studio, JetBrains Rider, VS Code)
- [Atom](https://github.com/decsm/atom) Tool (`dotnet tool install -g decsm.atom`)

### Building the Project

This project uses [Atom](https://github.com/decsm/atom) for building and managing dependencies. To build the project,
run:

```bash
atom PackJsonExtensions
```

### Running Tests

To run the tests, use the following command:

```bash
atom TestJsonExtensions
```

### Code Style

This project follows standard C# coding conventions and uses an `.editorconfig` file to maintain consistent formatting
across different editors.

### Versioning

This project uses GitVersion for automatic versioning based on Git history. The configuration is defined in
`GitVersion.yml`.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add or update tests as necessary
5. Ensure all tests pass
6. Submit a pull request

## Project Configuration

- **Target Framework**: .NET 9.0
- **Language Version**: C# 14.0
- **Build Configuration**: Defined in `Directory.Build.props`
- **Global Settings**: Configured in `global.json`

## License

This project is licensed under the MIT License. [See the LICENSE file for more details.](LICENSE.txt).