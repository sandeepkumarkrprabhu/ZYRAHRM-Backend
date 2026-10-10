using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZYRAHRM.IntegrationApp.DTOs.AssetManagement;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Route("api/asset-categories")]
public sealed class AssetCategoriesController : ControllerBase
{
    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<AssetCategoriesController> _logger;

    public AssetCategoriesController(
        AttendanceDbContext dbContext,
        ILogger<AssetCategoriesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AssetCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetCategoryResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AssetCategories.AsNoTracking();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var categories = await query
            .OrderBy(x => x.CategoryName)
            .Select(x => new AssetCategoryResponse
            {
                AssetCategoryId = x.AssetCategoryId,
                CategoryName = x.CategoryName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssetCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetCategoryResponse>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.AssetCategories
            .AsNoTracking()
            .Where(x => x.AssetCategoryId == id)
            .Select(x => new AssetCategoryResponse
            {
                AssetCategoryId = x.AssetCategoryId,
                CategoryName = x.CategoryName,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetCategoryResponse>> Create(
        [FromBody] AssetCategoryWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoryName = request.CategoryName.Trim();
        if (await _dbContext.AssetCategories.AnyAsync(
                x => x.CategoryName == categoryName, cancellationToken))
        {
            return Conflict(new { message = "An asset category with this name already exists." });
        }

        var category = new AssetCategory
        {
            CategoryName = categoryName,
            Description = NormalizeOptionalText(request.Description),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentActor()
        };

        _dbContext.AssetCategories.Add(category);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to create asset category {CategoryName}.", categoryName);
            return Conflict(new { message = "The category could not be created; verify that its name is unique." });
        }

        var response = ToResponse(category);
        return CreatedAtAction(nameof(GetById), new { id = category.AssetCategoryId }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssetCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetCategoryResponse>> Update(
        int id,
        [FromBody] AssetCategoryWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.AssetCategories
            .SingleOrDefaultAsync(x => x.AssetCategoryId == id, cancellationToken);

        if (category is null)
            return NotFound();

        var categoryName = request.CategoryName.Trim();
        if (await _dbContext.AssetCategories.AnyAsync(
                x => x.AssetCategoryId != id && x.CategoryName == categoryName,
                cancellationToken))
        {
            return Conflict(new { message = "An asset category with this name already exists." });
        }

        category.CategoryName = categoryName;
        category.Description = NormalizeOptionalText(request.Description);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to update asset category {AssetCategoryId}.", id);
            return Conflict(new { message = "The category could not be updated; verify that its name is unique." });
        }

        return Ok(ToResponse(category));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.AssetCategories
            .SingleOrDefaultAsync(x => x.AssetCategoryId == id, cancellationToken);

        if (category is null)
            return NotFound();

        if (!category.IsActive)
            return NoContent();

        var hasActiveAssets = await _dbContext.Assets.AnyAsync(
            x => x.AssetCategoryId == id && x.IsActive, cancellationToken);

        if (hasActiveAssets)
        {
            return Conflict(new
            {
                message = "This category has active assets. Move or deactivate those assets before deactivating the category."
            });
        }

        category.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? CurrentActor() => User.Identity?.Name;

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AssetCategoryResponse ToResponse(AssetCategory category) => new()
    {
        AssetCategoryId = category.AssetCategoryId,
        CategoryName = category.CategoryName,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt
    };
}
