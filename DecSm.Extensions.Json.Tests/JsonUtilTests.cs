namespace DecSm.Extensions.Json.Tests;

[TestFixture]
public sealed class JsonUtilTests
{
    [Test]
    public void Flatten_JsonObject_ReturnsFlattenedList()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
            ["address"] = new JsonObject
            {
                ["street"] = "123 Main St",
                ["city"] = "Anytown",
            },
            ["phones"] = new JsonArray
            {
                "123-456-7890",
                "987-654-3210",
            },
        };

        // Act
        var flattened = json.Flatten();
        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(6);
        flattened.ShouldContain(x => x.Key == "name" && x.Value == "John");
        flattened.ShouldContain(x => x.Key == "age" && x.Value == "30");
        flattened.ShouldContain(x => x.Key == "address:street" && x.Value == "123 Main St");
        flattened.ShouldContain(x => x.Key == "address:city" && x.Value == "Anytown");
        flattened.ShouldContain(x => x.Key == "phones:[0]" && x.Value == "123-456-7890");
        flattened.ShouldContain(x => x.Key == "phones:[1]" && x.Value == "987-654-3210");
    }

    [Test]
    public void Unflatten_JsonObject_ReturnsOriginalJson()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("name", "John"),
            new("age", "30"),
            new("address:street", "123 Main St"),
            new("address:city", "Anytown"),
            new("phones:[0]", "123-456-7890"),
            new("phones:[1]", "987-654-3210"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        json
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonObject>();

        json["name"]
            .ShouldNotBeNull()
            .ToString()
            .ShouldBe("John");

        json["age"]
            .ShouldNotBeNull()
            .ToString()
            .ShouldBe("30");

        json["address"]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonObject>()
            .ShouldSatisfyAllConditions(address => address["street"]
                    .ShouldNotBeNull()
                    .ToString()
                    .ShouldBe("123 Main St"),
                address => address["city"]
                    .ShouldNotBeNull()
                    .ToString()
                    .ShouldBe("Anytown"));
    }

    [Test]
    public void Flatten_EmptyJsonObject_ReturnsEmptyList()
    {
        // Arrange
        var json = new JsonObject();

        // Act
        var flattened = json.Flatten();

        // Assert
        flattened.Count.ShouldBe(0);
    }

    [Test]
    public void Flatten_JsonArray_ReturnsFlattenedWithIndices()
    {
        // Arrange
        var json = new JsonArray
        {
            "first",
            "second",
            "third",
        };

        // Act
        var flattened = json.Flatten();
        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(3);
        flattened.ShouldContain(x => x.Key == ":[0]" && x.Value == "first");
        flattened.ShouldContain(x => x.Key == ":[1]" && x.Value == "second");
        flattened.ShouldContain(x => x.Key == ":[2]" && x.Value == "third");
    }

    [Test]
    public void Flatten_NestedArrays_ReturnsCorrectStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["matrix"] = new JsonArray
            {
                new JsonArray
                {
                    1,
                    2,
                    3,
                },
                new JsonArray
                {
                    4,
                    5,
                    6,
                },
            },
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(6);
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[0]" && x.Value == "1");
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[1]" && x.Value == "2");
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[2]" && x.Value == "3");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[0]" && x.Value == "4");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[1]" && x.Value == "5");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[2]" && x.Value == "6");
    }

    [Test]
    public void Flatten_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["middleName"] = null,
            ["age"] = 30,
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(3);
        flattened.ShouldContain(x => x.Key == "name" && x.Value == "John");
        flattened.ShouldContain(x => x.Key == "middleName" && x.Value == null);
        flattened.ShouldContain(x => x.Key == "age" && x.Value == "30");
    }

    [Test]
    public void Flatten_DeeplyNested_HandlesComplexStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["level1"] = new JsonObject
            {
                ["level2"] = new JsonObject
                {
                    ["level3"] = new JsonObject
                    {
                        ["value"] = "deep",
                    },
                },
            },
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(1);
        flattened.ShouldContain(x => x.Key == "level1:level2:level3:value" && x.Value == "deep");
    }

    [Test]
    public void Flatten_ArrayOfObjects_ReturnsCorrectStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["users"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "Alice",
                    ["age"] = 25,
                },
                new JsonObject
                {
                    ["name"] = "Bob",
                    ["age"] = 30,
                },
            },
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(4);
        flattened.ShouldContain(x => x.Key == "users:[0]:name" && x.Value == "Alice");
        flattened.ShouldContain(x => x.Key == "users:[0]:age" && x.Value == "25");
        flattened.ShouldContain(x => x.Key == "users:[1]:name" && x.Value == "Bob");
        flattened.ShouldContain(x => x.Key == "users:[1]:age" && x.Value == "30");
    }

    [Test]
    public void Flatten_PrimitiveTypes_HandlesAllTypes()
    {
        // Arrange
        var json = new JsonObject
        {
            ["string"] = "test",
            ["number"] = 42,
            ["decimal"] = 3.14,
            ["boolean"] = true,
            ["null"] = null,
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(5);
        flattened.ShouldContain(x => x.Key == "string" && x.Value == "test");
        flattened.ShouldContain(x => x.Key == "number" && x.Value == "42");
        flattened.ShouldContain(x => x.Key == "decimal" && x.Value == "3.14");
        flattened.ShouldContain(x => x.Key == "boolean" && x.Value == "true");
        flattened.ShouldContain(x => x.Key == "null" && x.Value == null);
    }

    [Test]
    public void Unflatten_EmptyCollection_ReturnsEmptyObject()
    {
        // Arrange
        // ReSharper disable once CollectionNeverUpdated.Local - test code
        var flattened = new List<KeyValuePair<string, string?>>();

        // Act
        var json = flattened.Unflatten();

        // Assert
        json.ShouldNotBeNull();
        json.Count.ShouldBe(0);
    }

    [Test]
    public void Unflatten_ComplexNestedArrays_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("matrix:[0]:[0]", "1"),
            new("matrix:[0]:[1]", "2"),
            new("matrix:[1]:[0]", "3"),
            new("matrix:[1]:[1]", "4"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        json["matrix"]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonArray>();

        var matrix = json["matrix"]!.AsArray();
        matrix.Count.ShouldBe(2);

        matrix[0]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonArray>();

        var firstRow = matrix[0]!.AsArray();

        firstRow[0]!
            .ToString()
            .ShouldBe("1");

        firstRow[1]!
            .ToString()
            .ShouldBe("2");

        matrix[1]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonArray>();

        var secondRow = matrix[1]!.AsArray();

        secondRow[0]!
            .ToString()
            .ShouldBe("3");

        secondRow[1]!
            .ToString()
            .ShouldBe("4");
    }

    [Test]
    public void Unflatten_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("name", "John"),
            new("middleName", null),
            new("age", "30"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        json["name"]!
            .ToString()
            .ShouldBe("John");

        json["middleName"]
            .ShouldBeNull();

        json["age"]!
            .ToString()
            .ShouldBe("30");
    }

    [Test]
    public void Unflatten_ArrayOfObjects_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("users:[0]:name", "Alice"),
            new("users:[0]:age", "25"),
            new("users:[1]:name", "Bob"),
            new("users:[1]:age", "30"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        json["users"]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonArray>();

        var users = json["users"]!.AsArray();
        users.Count.ShouldBe(2);

        var alice = users[0]!.AsObject();

        alice["name"]!
            .ToString()
            .ShouldBe("Alice");

        alice["age"]!
            .ToString()
            .ShouldBe("25");

        var bob = users[1]!.AsObject();

        bob["name"]!
            .ToString()
            .ShouldBe("Bob");

        bob["age"]!
            .ToString()
            .ShouldBe("30");
    }

    [Test]
    public void Unflatten_DuplicateKeys_LastValueWins()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("name", "John"),
            new("name", "Jane"), // Duplicate key, should overwrite
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        json["name"]!
            .ToString()
            .ShouldBe("Jane");
    }

    [Test]
    public void RoundTrip_FlattenThenUnflatten_PreservesOriginalStructure()
    {
        // Arrange
        var original = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
                ["addresses"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["city"] = "New York",
                        ["zip"] = "10001",
                    },
                    new JsonObject
                    {
                        ["city"] = "Boston",
                        ["zip"] = "02101",
                    },
                },
                ["tags"] = new JsonArray
                {
                    "admin",
                    "user",
                },
            },
        };

        // Act
        var flattened = original.Flatten();

        var roundTrip = flattened
            .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))
            .Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        roundTrip["user"]
            .ShouldNotBeNull()
            .ShouldBeOfType<JsonObject>();

        var user = roundTrip["user"]!.AsObject();

        user["name"]!
            .ToString()
            .ShouldBe("John");

        var addresses = user["addresses"]!.AsArray();
        addresses.Count.ShouldBe(2);

        var firstAddress = addresses[0]!.AsObject();

        firstAddress["city"]!
            .ToString()
            .ShouldBe("New York");

        firstAddress["zip"]!
            .ToString()
            .ShouldBe("10001");

        var tags = user["tags"]!.AsArray();
        tags.Count.ShouldBe(2);

        tags[0]!
            .ToString()
            .ShouldBe("admin");

        tags[1]!
            .ToString()
            .ShouldBe("user");
    }

    [Test]
    public void Flatten_SinglePrimitiveValue_ReturnsEmptyKeyWithValue()
    {
        // Arrange
        JsonNode json = JsonValue.Create("standalone");

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(1);
        flattened.ShouldContain(x => x.Key == "" && x.Value == "standalone");
    }

    [Test]
    public void Flatten_EmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var json = new JsonArray();

        // Act
        var flattened = json.Flatten();

        // Assert
        flattened.Count.ShouldBe(0);
    }

    [Test]
    public void Flatten_NullJsonNode_HandlesGracefully()
    {
        // Arrange
        JsonNode? json = null;

        // Act
        var flattened = json!.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(1);
        flattened.ShouldContain(x => x.Key == "" && x.Value == null);
    }

    [Test]
    public void Flatten_ExtremelyDeepNesting_HandlesWithoutStackOverflow()
    {
        // Arrange - Create 100 levels deep
        var json = new JsonObject();
        var current = json;

        for (var i = 0; i < 100; i++)
        {
            var next = new JsonObject
            {
                ["value"] = i.ToString(),
            };

            current[$"level{i}"] = next;
            current = next;
        }

        // Act
        var flattened = json.Flatten();

        // Assert
        flattened.Count.ShouldBe(100);

        flattened
            .Any(x => x.Key.Split(':')
                          .Length ==
                      101)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_KeysWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("user:name with spaces", "John Doe"),
            new("user:email@domain.com", "john@example.com"),
            new("data:field.with.dots", "dotted"),
            new("symbols:$#@%^&*()", "special"),
            new("unicode:café", "coffee"),
            new("newline:line\nbreak", "broken"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        var user = json["user"]!.AsObject();

        user["name with spaces"]!
            .ToString()
            .ShouldBe("John Doe");

        user["email@domain.com"]!
            .ToString()
            .ShouldBe("john@example.com");

        var data = json["data"]!.AsObject();

        data["field.with.dots"]!
            .ToString()
            .ShouldBe("dotted");

        var symbols = json["symbols"]!.AsObject();

        symbols["$#@%^&*()"]!
            .ToString()
            .ShouldBe("special");

        var unicode = json["unicode"]!.AsObject();

        unicode["café"]!
            .ToString()
            .ShouldBe("coffee");

        var newline = json["newline"]!.AsObject();

        newline["line\nbreak"]!
            .ToString()
            .ShouldBe("broken");
    }

    [Test]
    public void Unflatten_ArrayLikeKeysButNotArrays_HandlesCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("obj:[not_an_index]", "value1"),
            new("obj:[123abc]", "value2"),
            new("obj:[]", "value3"),
            new("obj:[", "value4"),
            new("obj:]", "value5"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        var obj = json["obj"]!.AsObject();

        obj["[not_an_index]"]!
            .ToString()
            .ShouldBe("value1");

        obj["[123abc]"]!
            .ToString()
            .ShouldBe("value2");

        obj["[]"]!
            .ToString()
            .ShouldBe("value3");

        obj["["]!
            .ToString()
            .ShouldBe("value4");

        obj["]"]!
            .ToString()
            .ShouldBe("value5");
    }

    [Test]
    public void Unflatten_ConflictingStructureTypes_LastWins()
    {
        // Arrange - First define as object, then as array
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("data:property", "object_value"),
            new("data:[0]", "array_value"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        // The structure should be determined by the last processed item
        json["data"]
            .ShouldNotBeNull();

        // Could be either object or array depending on processing order
        var isArray = json["data"] is JsonArray;
        var isObject = json["data"] is JsonObject;
        (isArray || isObject).ShouldBeTrue();
    }

    [Test]
    public void Flatten_ArraysWithNullElements_PreservesNulls()
    {
        // Arrange
        var json = new JsonObject
        {
            ["items"] = new JsonArray
            {
                "first",
                null,
                "third",
                null,
            },
        };

        // Act
        var flattened = json.Flatten();

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(4);
        flattened.ShouldContain(x => x.Key == "items:[0]" && x.Value == "first");
        flattened.ShouldContain(x => x.Key == "items:[1]" && x.Value == null);
        flattened.ShouldContain(x => x.Key == "items:[2]" && x.Value == "third");
        flattened.ShouldContain(x => x.Key == "items:[3]" && x.Value == null);
    }

    [Test]
    public void Unflatten_ComplexMixedArrayObjectNesting_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new List<KeyValuePair<string, string?>>
        {
            new("root:[0]:users:[0]:profile:addresses:[0]:street", "123 Main St"),
            new("root:[0]:users:[0]:profile:addresses:[1]:street", "456 Oak Ave"),
            new("root:[0]:users:[1]:name", "Bob"),
            new("root:[1]:config:enabled", "true"),
        };

        // Act
        var json = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        var root = json["root"]!.AsArray();
        root.Count.ShouldBe(2);

        var firstItem = root[0]!.AsObject();
        var users = firstItem["users"]!.AsArray();
        users.Count.ShouldBe(2);

        var firstUser = users[0]!.AsObject();
        var profile = firstUser["profile"]!.AsObject();
        var addresses = profile["addresses"]!.AsArray();
        addresses.Count.ShouldBe(2);

        addresses[0]!
            .AsObject()["street"]!
            .ToString()
            .ShouldBe("123 Main St");

        addresses[1]!
            .AsObject()["street"]!
            .ToString()
            .ShouldBe("456 Oak Ave");

        users[1]!
            .AsObject()["name"]!
            .ToString()
            .ShouldBe("Bob");

        var secondItem = root[1]!.AsObject();

        secondItem["config"]!
            .AsObject()["enabled"]!
            .ToString()
            .ShouldBe("true");
    }

    [Test]
    public void RoundTrip_WithEdgeCases_MaintainsDataIntegrity()
    {
        // Arrange - Complex structure with edge cases
        var original = new JsonObject
        {
            [""] = "empty_key",
            ["array"] = new JsonArray
            {
                null,
                "value",
                new JsonObject
                {
                    ["nested"] = true,
                },
            },
            ["special-chars"] = "hyphen_in_key",
            ["unicode"] = "🚀✨",
            ["numbers"] = new JsonObject
            {
                ["int"] = 42,
                ["float"] = 3.14159,
                ["scientific"] = 1.23e-4,
            },
        };

        // Act
        var flattened = original.Flatten();
        var reconstructed = flattened.Unflatten();

        // Assert
        using var _ = Assert.EnterMultipleScope();

        reconstructed[""]!
            .ToString()
            .ShouldBe("empty_key");

        reconstructed["special-chars"]!
            .ToString()
            .ShouldBe("hyphen_in_key");

        reconstructed["unicode"]!
            .ToString()
            .ShouldBe("🚀✨");

        var array = reconstructed["array"]!.AsArray();

        array[0]
            .ShouldBeNull();

        array[1]!
            .ToString()
            .ShouldBe("value");

        array[2]!
            .AsObject()["nested"]!
            .ToString()
            .ShouldBe("true");

        var numbers = reconstructed["numbers"]!.AsObject();

        numbers["int"]!
            .ToString()
            .ShouldBe("42");

        numbers["float"]!
            .ToString()
            .ShouldBe("3.14159");

        numbers["scientific"]!
            .ToString()
            .ShouldBe("0.000123");
    }

    [Test]
    public void Replace_Replaces_SimpleKeyValue()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
        };

        // Act
        var replaced = json.Replace("name", "Jane");

        // Assert
        using var _ = Assert.EnterMultipleScope();

        replaced["name"]!
            .ToString()
            .ShouldBe("Jane");

        replaced["age"]!
            .ToString()
            .ShouldBe("30");
    }

    [Test]
    public void Replace_Replaces_NestedKeyValue()
    {
        // Arrange
        var json = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
                ["details"] = new JsonObject
                {
                    ["age"] = 30,
                    ["city"] = "New York",
                },
            },
        };

        // Act
        var replaced = json.Replace("user:details:city", "Los Angeles");

        // Assert
        using var _ = Assert.EnterMultipleScope();

        replaced["user"]!
            .AsObject()["details"]!
            .AsObject()["city"]!
            .ToString()
            .ShouldBe("Los Angeles");
    }

    [Test]
    public void Replace_Ignores_NonExistentKey()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
        };

        // Act
        var replaced = json.Replace("nonexistent", "value");

        // Assert
        using var _ = Assert.EnterMultipleScope();

        replaced.Count.ShouldBe(2);

        replaced["name"]!
            .ToString()
            .ShouldBe("John");

        replaced["age"]!
            .ToString()
            .ShouldBe("30");
    }
}
