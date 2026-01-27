using System.Text.Json;
using GoHardAPI.Models;

namespace GoHardAPI.Services
{
    public interface IOpenFoodFactsService
    {
        Task<FoodTemplate?> GetFoodByBarcodeAsync(string barcode);
    }

    public class OpenFoodFactsService : IOpenFoodFactsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenFoodFactsService> _logger;

        public OpenFoodFactsService(HttpClient httpClient, ILogger<OpenFoodFactsService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<FoodTemplate?> GetFoodByBarcodeAsync(string barcode)
        {
            try
            {
                var url = $"https://world.openfoodfacts.org/api/v2/product/{barcode}?fields=code,product_name,brands,serving_size,nutriments,categories_tags";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Open Food Facts API returned {StatusCode} for barcode {Barcode}",
                        response.StatusCode, barcode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonDocument.Parse(json);
                var root = data.RootElement;

                // Check if product was found
                if (!root.TryGetProperty("status", out var status) || status.GetInt32() != 1)
                {
                    _logger.LogInformation("Product not found in Open Food Facts for barcode {Barcode}", barcode);
                    return null;
                }

                if (!root.TryGetProperty("product", out var product))
                {
                    return null;
                }

                return ParseProduct(product, barcode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching product from Open Food Facts for barcode {Barcode}", barcode);
                return null;
            }
        }

        private FoodTemplate? ParseProduct(JsonElement product, string barcode)
        {
            try
            {
                // Get product name
                if (!product.TryGetProperty("product_name", out var nameElement))
                {
                    return null;
                }
                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }

                // Get brand (optional)
                string? brand = null;
                if (product.TryGetProperty("brands", out var brandElement))
                {
                    brand = brandElement.GetString();
                }

                // Parse serving size (default to 100g)
                double servingSize = 100;
                string servingUnit = "g";

                if (product.TryGetProperty("serving_size", out var servingSizeElement))
                {
                    var servingSizeStr = servingSizeElement.GetString();
                    if (!string.IsNullOrEmpty(servingSizeStr))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(
                            servingSizeStr, @"(\d+(?:\.\d+)?)\s*([a-zA-Z]+)?");
                        if (match.Success)
                        {
                            if (double.TryParse(match.Groups[1].Value, out var size))
                            {
                                servingSize = size;
                            }
                            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                            {
                                servingUnit = match.Groups[2].Value;
                            }
                        }
                    }
                }

                // Get nutriments
                JsonElement nutriments = default;
                product.TryGetProperty("nutriments", out nutriments);

                decimal caloriesPer100 = GetNutrimentValue(nutriments, "energy-kcal_100g", "energy-kcal");
                decimal proteinPer100 = GetNutrimentValue(nutriments, "proteins_100g", "proteins");
                decimal carbsPer100 = GetNutrimentValue(nutriments, "carbohydrates_100g", "carbohydrates");
                decimal fatPer100 = GetNutrimentValue(nutriments, "fat_100g", "fat");
                decimal? fiberPer100 = GetNutrimentValueNullable(nutriments, "fiber_100g", "fiber");
                decimal? sugarPer100 = GetNutrimentValueNullable(nutriments, "sugars_100g", "sugars");
                decimal? sodiumPer100 = GetNutrimentValueNullable(nutriments, "sodium_100g", "sodium");
                decimal? saturatedFatPer100 = GetNutrimentValueNullable(nutriments, "saturated-fat_100g", "saturated-fat");

                // Calculate per-serving values
                decimal multiplier = (decimal)servingSize / 100m;
                decimal calories = caloriesPer100 * multiplier;
                decimal protein = proteinPer100 * multiplier;
                decimal carbs = carbsPer100 * multiplier;
                decimal fat = fatPer100 * multiplier;
                decimal? fiber = fiberPer100.HasValue ? fiberPer100.Value * multiplier : null;
                decimal? sugar = sugarPer100.HasValue ? sugarPer100.Value * multiplier : null;
                decimal? sodium = sodiumPer100.HasValue ? sodiumPer100.Value * multiplier : null;
                decimal? saturatedFat = saturatedFatPer100.HasValue ? saturatedFatPer100.Value * multiplier : null;

                // Parse category
                string category = "Uncategorized";
                if (product.TryGetProperty("categories_tags", out var categoriesElement) &&
                    categoriesElement.ValueKind == JsonValueKind.Array &&
                    categoriesElement.GetArrayLength() > 0)
                {
                    var firstCategory = categoriesElement[0].GetString();
                    if (!string.IsNullOrEmpty(firstCategory))
                    {
                        // Clean up category: remove "en:" prefix, replace dashes with spaces, capitalize words
                        category = firstCategory
                            .Replace("en:", "")
                            .Replace("-", " ");
                        category = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(category);
                    }
                }

                return new FoodTemplate
                {
                    Name = name,
                    Brand = brand,
                    Barcode = barcode,
                    Category = category,
                    ServingSize = (decimal)servingSize,
                    ServingUnit = servingUnit,
                    Calories = calories,
                    Protein = protein,
                    Carbohydrates = carbs,
                    Fat = fat,
                    Fiber = fiber,
                    Sugar = sugar,
                    Sodium = sodium,
                    SaturatedFat = saturatedFat,
                    IsCustom = false,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Open Food Facts product for barcode {Barcode}", barcode);
                return null;
            }
        }

        private decimal GetNutrimentValue(JsonElement nutriments, string primaryKey, string fallbackKey)
        {
            if (nutriments.ValueKind == JsonValueKind.Undefined)
                return 0;

            if (nutriments.TryGetProperty(primaryKey, out var primary) &&
                primary.ValueKind == JsonValueKind.Number)
            {
                return primary.GetDecimal();
            }

            if (nutriments.TryGetProperty(fallbackKey, out var fallback) &&
                fallback.ValueKind == JsonValueKind.Number)
            {
                return fallback.GetDecimal();
            }

            return 0;
        }

        private decimal? GetNutrimentValueNullable(JsonElement nutriments, string primaryKey, string fallbackKey)
        {
            if (nutriments.ValueKind == JsonValueKind.Undefined)
                return null;

            if (nutriments.TryGetProperty(primaryKey, out var primary) &&
                primary.ValueKind == JsonValueKind.Number)
            {
                return primary.GetDecimal();
            }

            if (nutriments.TryGetProperty(fallbackKey, out var fallback) &&
                fallback.ValueKind == JsonValueKind.Number)
            {
                return fallback.GetDecimal();
            }

            return null;
        }
    }
}
