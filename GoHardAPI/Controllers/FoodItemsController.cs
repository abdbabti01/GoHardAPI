using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GoHardAPI.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class FoodItemsController : ControllerBase
    {
        private readonly TrainingContext _context;

        public FoodItemsController(TrainingContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get food items for a meal entry
        /// </summary>
        [HttpGet("mealentry/{mealEntryId}")]
        public async Task<ActionResult<IEnumerable<FoodItem>>> GetFoodItems(int mealEntryId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Verify the meal entry belongs to the user
            var mealEntry = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == mealEntryId);

            if (mealEntry == null || mealEntry.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            var foodItems = await _context.FoodItems
                .Where(fi => fi.MealEntryId == mealEntryId)
                .Include(fi => fi.FoodTemplate)
                .ToListAsync();

            return Ok(foodItems);
        }

        /// <summary>
        /// Get a specific food item by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<FoodItem>> GetFoodItem(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var foodItem = await _context.FoodItems
                .Include(fi => fi.FoodTemplate)
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .FirstOrDefaultAsync(fi => fi.Id == id);

            if (foodItem == null || foodItem.MealEntry?.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            return Ok(foodItem);
        }

        /// <summary>
        /// Add a food item to a meal entry
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<FoodItem>> CreateFoodItem([FromBody] FoodItem foodItem)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Verify the meal entry belongs to the user
            var mealEntry = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == foodItem.MealEntryId);

            if (mealEntry == null || mealEntry.MealLog?.UserId != userId)
            {
                return NotFound(new { message = "Meal entry not found" });
            }

            foodItem.CreatedAt = DateTime.UtcNow;

            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(mealEntry.MealLog.Id);

            return CreatedAtAction(nameof(GetFoodItem), new { id = foodItem.Id }, foodItem);
        }

        /// <summary>
        /// Quick add food from a template
        /// </summary>
        [HttpPost("quick")]
        public async Task<ActionResult<FoodItem>> QuickAddFood([FromBody] QuickAddFoodRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            // Verify the meal entry belongs to the user
            var mealEntry = await _context.MealEntries
                .Include(me => me.MealLog)
                .FirstOrDefaultAsync(me => me.Id == request.MealEntryId);

            if (mealEntry == null || mealEntry.MealLog?.UserId != userId)
            {
                return NotFound(new { message = "Meal entry not found" });
            }

            // Get the food template
            var template = await _context.FoodTemplates.FindAsync(request.FoodTemplateId);
            if (template == null)
            {
                return NotFound(new { message = "Food template not found" });
            }

            var foodItem = new FoodItem
            {
                MealEntryId = request.MealEntryId,
                CreatedAt = DateTime.UtcNow
            };

            foodItem.CalculateFromTemplate(template, request.Quantity);

            _context.FoodItems.Add(foodItem);
            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(mealEntry.MealLog.Id);

            return CreatedAtAction(nameof(GetFoodItem), new { id = foodItem.Id }, foodItem);
        }

        /// <summary>
        /// Update a food item
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFoodItem(int id, [FromBody] FoodItem foodItem)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.FoodItems
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .FirstOrDefaultAsync(fi => fi.Id == id);

            if (existing == null || existing.MealEntry?.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            existing.Name = foodItem.Name;
            existing.Brand = foodItem.Brand;
            existing.Quantity = foodItem.Quantity;
            existing.ServingSize = foodItem.ServingSize;
            existing.ServingUnit = foodItem.ServingUnit;
            existing.Calories = foodItem.Calories;
            existing.Protein = foodItem.Protein;
            existing.Carbohydrates = foodItem.Carbohydrates;
            existing.Fat = foodItem.Fat;
            existing.Fiber = foodItem.Fiber;
            existing.Sugar = foodItem.Sugar;
            existing.Sodium = foodItem.Sodium;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(existing.MealEntry.MealLog.Id);

            return NoContent();
        }

        /// <summary>
        /// Update food item quantity and recalculate nutrition
        /// </summary>
        [HttpPut("{id}/quantity")]
        public async Task<ActionResult<FoodItem>> UpdateQuantity(int id, [FromBody] decimal quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var foodItem = await _context.FoodItems
                .Include(fi => fi.FoodTemplate)
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .FirstOrDefaultAsync(fi => fi.Id == id);

            if (foodItem == null || foodItem.MealEntry?.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            if (foodItem.FoodTemplate != null)
            {
                foodItem.CalculateFromTemplate(foodItem.FoodTemplate, quantity);
            }
            else
            {
                // For custom foods, scale the existing values
                var ratio = quantity / foodItem.Quantity;
                foodItem.Calories *= ratio;
                foodItem.Protein *= ratio;
                foodItem.Carbohydrates *= ratio;
                foodItem.Fat *= ratio;
                if (foodItem.Fiber.HasValue) foodItem.Fiber *= ratio;
                if (foodItem.Sugar.HasValue) foodItem.Sugar *= ratio;
                if (foodItem.Sodium.HasValue) foodItem.Sodium *= ratio;
                foodItem.Quantity = quantity;
            }

            foodItem.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(foodItem.MealEntry.MealLog.Id);

            return Ok(foodItem);
        }

        /// <summary>
        /// Replace a food item with a suggested alternative
        /// </summary>
        [HttpPut("{id}/replace")]
        public async Task<ActionResult<FoodItem>> ReplaceWithAlternative(int id, [FromBody] ReplaceFoodRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var existing = await _context.FoodItems
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .FirstOrDefaultAsync(fi => fi.Id == id);

            if (existing == null || existing.MealEntry?.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            // Try to find a matching food template by name
            FoodTemplate? matchingTemplate = null;
            if (!string.IsNullOrEmpty(request.Name))
            {
                matchingTemplate = await _context.FoodTemplates
                    .Where(ft => ft.Name.ToLower() == request.Name.ToLower())
                    .FirstOrDefaultAsync();
            }

            if (matchingTemplate != null)
            {
                // Use the template
                existing.CalculateFromTemplate(matchingTemplate, request.Quantity);
            }
            else
            {
                // Use the provided values from AI suggestion
                existing.FoodTemplateId = null;
                existing.Name = request.Name;
                existing.Quantity = request.Quantity;
                existing.ServingSize = request.ServingSize;
                existing.ServingUnit = request.ServingUnit;
                existing.Calories = request.Calories * request.Quantity;
                existing.Protein = request.Protein * request.Quantity;
                existing.Carbohydrates = request.Carbohydrates * request.Quantity;
                existing.Fat = request.Fat * request.Quantity;
                existing.Fiber = null;
                existing.Sugar = null;
                existing.Sodium = null;
            }

            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(existing.MealEntry.MealLog.Id);

            return Ok(existing);
        }

        /// <summary>
        /// Delete a food item
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            var foodItem = await _context.FoodItems
                .Include(fi => fi.MealEntry)
                    .ThenInclude(me => me!.MealLog)
                .FirstOrDefaultAsync(fi => fi.Id == id);

            if (foodItem == null || foodItem.MealEntry?.MealLog?.UserId != userId)
            {
                return NotFound();
            }

            var mealLogId = foodItem.MealEntry.MealLog.Id;

            _context.FoodItems.Remove(foodItem);
            await _context.SaveChangesAsync();

            // Recalculate meal entry and meal log totals
            await RecalculateTotals(mealLogId);

            return NoContent();
        }

        private async Task RecalculateTotals(int mealLogId)
        {
            var mealLog = await _context.MealLogs
                .Include(ml => ml.MealEntries)
                    .ThenInclude(me => me.FoodItems)
                .FirstOrDefaultAsync(ml => ml.Id == mealLogId);

            if (mealLog != null)
            {
                foreach (var entry in mealLog.MealEntries)
                {
                    entry.RecalculateTotals();
                }
                mealLog.RecalculateTotals();
                await _context.SaveChangesAsync();
            }
        }
    }

    public class QuickAddFoodRequest
    {
        public int MealEntryId { get; set; }
        public int FoodTemplateId { get; set; }
        public decimal Quantity { get; set; } = 1;
    }

    public class ReplaceFoodRequest
    {
        public string Name { get; set; } = "";
        public decimal ServingSize { get; set; } = 100;
        public string ServingUnit { get; set; } = "g";
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fat { get; set; }
        public decimal Quantity { get; set; } = 1;
    }
}
