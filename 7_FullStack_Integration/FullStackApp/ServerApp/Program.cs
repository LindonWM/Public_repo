using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = System
        .Text
        .Json
        .Serialization
        .JsonIgnoreCondition
        .WhenWritingNull;
});

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

var defaultCacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

app.MapGet(
    "/api/productlist",
    IResult (IMemoryCache cache) =>
    {
        const string cacheKey = "productlist";

        if (cache.TryGetValue<ProductResponse[]>(cacheKey, out var cached) && cached is not null)
        {
            return Results.Ok(cached);
        }

        var products = new[]
        {
            new ProductResponse
            {
                Id = 1,
                Name = "Laptop",
                Price = 1200.50m,
                Stock = 25,
                Category = new CategoryResponse { Id = 101, Name = "Electronics" },
            },
            new ProductResponse
            {
                Id = 2,
                Name = "Headphones",
                Price = 50.00m,
                Stock = 100,
                Category = new CategoryResponse { Id = 102, Name = "Accessories" },
            },
        };

        var validationErrors = ValidateProducts(products);
        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        cache.Set(cacheKey, products, defaultCacheOptions);

        return Results.Ok(products);
    }
);

app.Run();

static Dictionary<string, string[]> ValidateProducts(IEnumerable<ProductResponse> products)
{
    var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);

    foreach (var product in products)
    {
        ValidateModel(product, errors, "product");
        ValidateModel(product.Category, errors, "product.category");
    }

    return errors.ToDictionary(
        pair => pair.Key,
        pair => pair.Value.ToArray(),
        StringComparer.Ordinal
    );
}

static void ValidateModel(object model, Dictionary<string, List<string>> errors, string prefix)
{
    var context = new ValidationContext(model);
    var validationResults = new List<ValidationResult>();

    if (Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true))
    {
        return;
    }

    foreach (var validationResult in validationResults)
    {
        var members =
            validationResult.MemberNames?.Any() == true
                ? validationResult.MemberNames
                : new[] { string.Empty };

        foreach (var member in members)
        {
            var key = string.IsNullOrWhiteSpace(member) ? prefix : $"{prefix}.{member}";
            if (!errors.TryGetValue(key, out var messages))
            {
                messages = new List<string>();
                errors[key] = messages;
            }

            messages.Add(validationResult.ErrorMessage ?? "Invalid value.");
        }
    }
}

internal sealed class ProductResponse
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }

    [Required]
    public required CategoryResponse Category { get; init; }
}

internal sealed class CategoryResponse
{
    [Range(1, int.MaxValue)]
    public int Id { get; init; }

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public required string Name { get; init; }
}
