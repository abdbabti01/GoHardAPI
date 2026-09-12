using Asp.Versioning;
using GoHardAPI.Data;
using GoHardAPI.Models;
using GoHardAPI.Services;
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
        /// Add a food item to a meal entry. The insert and the resulting meal
        /// entry/meal log totals recompute commit as ONE atomic, retried transaction -
        /// see <see cref="MealLogTotalsRecalculator"/> for why: committing them
        /// separately is exactly what let a completed food change leave a stale total
        /// behind under a concurrent write or an exhausted retry.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<FoodItem>> CreateFoodItem([FromBody] FoodItem foodItem)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<FoodItem> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var mealEntry = await _context.MealEntries
                        .Include(me => me.MealLog)
                        .FirstOrDefaultAsync(me => me.Id == foodItem.MealEntryId, ct);

                    if (mealEntry == null || mealEntry.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<FoodItem>.NotFound();
                    }

                    // A fresh entity every attempt - never add the client-supplied
                    // `foodItem` parameter itself. If an earlier attempt's intermediate
                    // SaveChangesAsync below already assigned it a real database Id before
                    // that attempt was rolled back, re-adding the SAME instance on retry
                    // would insert with an explicit (stale) key instead of generating a
                    // new one - safe by accident on this schema's identity column, but
                    // fragile and inconsistent with every other action in this file, which
                    // all re-read or freshly construct their entity inside the delegate.
                    var newItem = new FoodItem
                    {
                        MealEntryId = foodItem.MealEntryId,
                        FoodTemplateId = foodItem.FoodTemplateId,
                        Name = foodItem.Name,
                        Brand = foodItem.Brand,
                        Quantity = foodItem.Quantity,
                        ServingSize = foodItem.ServingSize,
                        ServingUnit = foodItem.ServingUnit,
                        Calories = foodItem.Calories,
                        Protein = foodItem.Protein,
                        Carbohydrates = foodItem.Carbohydrates,
                        Fat = foodItem.Fat,
                        Fiber = foodItem.Fiber,
                        Sugar = foodItem.Sugar,
                        Sodium = foodItem.Sodium,
                        CreatedAt = DateTime.UtcNow,
                    };

                    _context.FoodItems.Add(newItem);
                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, mealEntry.MealLog.Id, ct);

                    return MealMutationOutcome<FoodItem>.Success(newItem);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not save this food item due to a conflicting update. Please try again." });
            }

            return outcome.Found
                ? CreatedAtAction(nameof(GetFoodItem), new { id = outcome.Value!.Id }, outcome.Value)
                : NotFound(new { message = "Meal entry not found" });
        }

        /// <summary>
        /// Quick add food from a template. Atomic with its totals recompute - see
        /// <see cref="CreateFoodItem"/>'s doc comment.
        /// </summary>
        [HttpPost("quick")]
        public async Task<ActionResult<FoodItem>> QuickAddFood([FromBody] QuickAddFoodRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<FoodItem> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var mealEntry = await _context.MealEntries
                        .Include(me => me.MealLog)
                        .FirstOrDefaultAsync(me => me.Id == request.MealEntryId, ct);

                    if (mealEntry == null || mealEntry.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<FoodItem>.NotFound();
                    }

                    var template = await _context.FoodTemplates.FindAsync(new object?[] { request.FoodTemplateId }, ct);
                    if (template == null)
                    {
                        return MealMutationOutcome<FoodItem>.NotFound();
                    }

                    var newItem = new FoodItem
                    {
                        MealEntryId = request.MealEntryId,
                        CreatedAt = DateTime.UtcNow
                    };
                    newItem.CalculateFromTemplate(template, request.Quantity);

                    _context.FoodItems.Add(newItem);
                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, mealEntry.MealLog.Id, ct);

                    return MealMutationOutcome<FoodItem>.Success(newItem);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not save this food item due to a conflicting update. Please try again." });
            }

            if (!outcome.Found)
            {
                return NotFound(new { message = "Meal entry or food template not found" });
            }

            return CreatedAtAction(nameof(GetFoodItem), new { id = outcome.Value!.Id }, outcome.Value);
        }

        /// <summary>
        /// Update a food item. Atomic with its totals recompute - see
        /// <see cref="CreateFoodItem"/>'s doc comment.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFoodItem(int id, [FromBody] FoodItem foodItem)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<bool> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var existing = await _context.FoodItems
                        .Include(fi => fi.MealEntry)
                            .ThenInclude(me => me!.MealLog)
                        .FirstOrDefaultAsync(fi => fi.Id == id, ct);

                    if (existing == null || existing.MealEntry?.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<bool>.NotFound();
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

                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, existing.MealEntry!.MealLog!.Id, ct);

                    return MealMutationOutcome<bool>.Success(true);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not save this food item due to a conflicting update. Please try again." });
            }

            return outcome.Found ? NoContent() : NotFound();
        }

        /// <summary>
        /// Update food item quantity and recalculate nutrition. Atomic with its totals
        /// recompute - see <see cref="CreateFoodItem"/>'s doc comment.
        /// </summary>
        [HttpPut("{id}/quantity")]
        public async Task<ActionResult<FoodItem>> UpdateQuantity(int id, [FromBody] decimal quantity)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<FoodItem> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var foodItem = await _context.FoodItems
                        .Include(fi => fi.FoodTemplate)
                        .Include(fi => fi.MealEntry)
                            .ThenInclude(me => me!.MealLog)
                        .FirstOrDefaultAsync(fi => fi.Id == id, ct);

                    if (foodItem == null || foodItem.MealEntry?.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<FoodItem>.NotFound();
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

                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, foodItem.MealEntry!.MealLog!.Id, ct);

                    return MealMutationOutcome<FoodItem>.Success(foodItem);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not save this food item due to a conflicting update. Please try again." });
            }

            return outcome.Found ? Ok(outcome.Value) : NotFound();
        }

        /// <summary>
        /// Replace a food item with a suggested alternative. Atomic with its totals
        /// recompute - see <see cref="CreateFoodItem"/>'s doc comment.
        /// </summary>
        [HttpPut("{id}/replace")]
        public async Task<ActionResult<FoodItem>> ReplaceWithAlternative(int id, [FromBody] ReplaceFoodRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<FoodItem> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var existing = await _context.FoodItems
                        .Include(fi => fi.MealEntry)
                            .ThenInclude(me => me!.MealLog)
                        .FirstOrDefaultAsync(fi => fi.Id == id, ct);

                    if (existing == null || existing.MealEntry?.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<FoodItem>.NotFound();
                    }

                    // Try to find a matching food template by name
                    FoodTemplate? matchingTemplate = null;
                    if (!string.IsNullOrEmpty(request.Name))
                    {
                        matchingTemplate = await _context.FoodTemplates
                            .Where(ft => ft.Name.ToLower() == request.Name.ToLower())
                            .FirstOrDefaultAsync(ct);
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

                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, existing.MealEntry!.MealLog!.Id, ct);

                    return MealMutationOutcome<FoodItem>.Success(existing);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not save this food item due to a conflicting update. Please try again." });
            }

            return outcome.Found ? Ok(outcome.Value) : NotFound();
        }

        /// <summary>
        /// Delete a food item. Atomic with its totals recompute - see
        /// <see cref="CreateFoodItem"/>'s doc comment.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFoodItem(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            MealMutationOutcome<bool> outcome;
            try
            {
                outcome = await MealLogTotalsRecalculator.ExecuteAtomicallyAsync(_context, async ct =>
                {
                    var foodItem = await _context.FoodItems
                        .Include(fi => fi.MealEntry)
                            .ThenInclude(me => me!.MealLog)
                        .FirstOrDefaultAsync(fi => fi.Id == id, ct);

                    if (foodItem == null || foodItem.MealEntry?.MealLog?.UserId != userId)
                    {
                        return MealMutationOutcome<bool>.NotFound();
                    }

                    var mealLogId = foodItem.MealEntry.MealLog.Id;

                    _context.FoodItems.Remove(foodItem);
                    await _context.SaveChangesAsync(ct);

                    await MealLogTotalsRecalculator.StageRecalculationAsync(_context, mealLogId, ct);

                    return MealMutationOutcome<bool>.Success(true);
                });
            }
            catch (MealLogTotalsRecalculator.ConcurrencyRetriesExhaustedException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Could not delete this food item due to a conflicting update. Please try again." });
            }

            return outcome.Found ? NoContent() : NotFound();
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
