using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZYRAHRM.IntegrationApp.DTOs.AssetManagement;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Authorize]
[Route("api/assets")]
public sealed class AssetsController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly AttendanceDbContext _dbContext;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(
        AttendanceDbContext dbContext,
        ILogger<AssetsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AssetResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AssetResponse>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] AssetStatus? status = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            return BadRequest(new { message = "pageNumber must be greater than zero." });

        if (pageSize < 1 || pageSize > MaxPageSize)
            return BadRequest(new { message = $"pageSize must be between 1 and {MaxPageSize}." });

        if (status.HasValue && !Enum.IsDefined(status.Value))
            return BadRequest(new { message = "The supplied asset status is invalid." });

        var query = _dbContext.Assets.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (categoryId.HasValue)
            query = query.Where(x => x.AssetCategoryId == categoryId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.AssetCode.Contains(term) ||
                x.AssetName.Contains(term) ||
                (x.SerialNumber != null && x.SerialNumber.Contains(term)) ||
                (x.Manufacturer != null && x.Manufacturer.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.AssetCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(AssetResponseProjection)
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AssetResponse>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetResponse>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.AssetId == id)
            .Select(AssetResponseProjection)
            .SingleOrDefaultAsync(cancellationToken);

        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Create(
        [FromBody] AssetWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.OwnershipType))
            return BadRequest(new { message = "The supplied ownership type is invalid." });

        if (request.OwnershipType == AssetOwnershipType.ClientOwned)
        {
            if (!request.ClientId.HasValue)
                return BadRequest(new { message = "ClientId is required for client-owned assets." });

            var clientIsActive = await _dbContext.ClientMasters.AnyAsync(
                x => x.ClientId == request.ClientId.Value && x.IsActive, cancellationToken);
            if (!clientIsActive)
                return BadRequest(new { message = "ClientId must reference an existing active client." });
        }
        else if (request.ClientId.HasValue)
        {
            return BadRequest(new { message = "ClientId can only be set for client-owned assets." });
        }

        var assetCode = request.AssetCode.Trim();
        if (await _dbContext.Assets.AnyAsync(x => x.AssetCode == assetCode, cancellationToken))
            return Conflict(new { message = "An asset with this asset code already exists." });

        var categoryIsActive = await _dbContext.AssetCategories.AnyAsync(
            x => x.AssetCategoryId == request.AssetCategoryId && x.IsActive,
            cancellationToken);

        if (!categoryIsActive)
            return BadRequest(new { message = "AssetCategoryId must reference an existing active category." });

        var asset = new Asset
        {
            AssetCode = assetCode,
            AssetName = request.AssetName.Trim(),
            Description = NormalizeOptionalText(request.Description),
            AssetCategoryId = request.AssetCategoryId,
            ClientId = request.ClientId,
            Manufacturer = NormalizeOptionalText(request.Manufacturer),
            ModelNumber = NormalizeOptionalText(request.ModelNumber),
            SerialNumber = NormalizeOptionalText(request.SerialNumber),
            OwnershipType = request.OwnershipType,
            Status = AssetStatus.Available,
            PurchaseDate = request.PurchaseDate,
            PurchaseCost = request.PurchaseCost,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            Location = NormalizeOptionalText(request.Location),
            Remarks = NormalizeOptionalText(request.Remarks),
            DeviceAdminAccountName = NormalizeOptionalText(request.DeviceAdminAccountName),
            DeviceAdminCredentialSecretReference = NormalizeOptionalText(request.DeviceAdminCredentialSecretReference),
            DeviceAdminAccountNotes = NormalizeOptionalText(request.DeviceAdminAccountNotes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentActor()
        };

        _dbContext.Assets.Add(asset);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to create asset with code {AssetCode}.", assetCode);
            return Conflict(new { message = "The asset could not be created; verify the asset code and category." });
        }

        var response = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.AssetId == asset.AssetId)
            .Select(AssetResponseProjection)
            .SingleAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = asset.AssetId }, response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AssetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetResponse>> Update(
        int id,
        [FromBody] AssetWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.OwnershipType))
            return BadRequest(new { message = "The supplied ownership type is invalid." });

        var asset = await _dbContext.Assets
            .SingleOrDefaultAsync(x => x.AssetId == id, cancellationToken);

        if (asset is null)
            return NotFound();

        var assetCode = request.AssetCode.Trim();
        if (await _dbContext.Assets.AnyAsync(
                x => x.AssetId != id && x.AssetCode == assetCode,
                cancellationToken))
        {
            return Conflict(new { message = "An asset with this asset code already exists." });
        }

        var categoryIsActive = await _dbContext.AssetCategories.AnyAsync(
            x => x.AssetCategoryId == request.AssetCategoryId && x.IsActive,
            cancellationToken);

        if (!categoryIsActive)
            return BadRequest(new { message = "AssetCategoryId must reference an existing active category." });

        asset.AssetCode = assetCode;
        asset.AssetName = request.AssetName.Trim();
        asset.Description = NormalizeOptionalText(request.Description);
        asset.AssetCategoryId = request.AssetCategoryId;
        asset.ClientId = request.ClientId;
        asset.Manufacturer = NormalizeOptionalText(request.Manufacturer);
        asset.ModelNumber = NormalizeOptionalText(request.ModelNumber);
        asset.SerialNumber = NormalizeOptionalText(request.SerialNumber);
        asset.OwnershipType = request.OwnershipType;
        asset.PurchaseDate = request.PurchaseDate;
        asset.PurchaseCost = request.PurchaseCost;
        asset.WarrantyExpiryDate = request.WarrantyExpiryDate;
        asset.Location = NormalizeOptionalText(request.Location);
        asset.Remarks = NormalizeOptionalText(request.Remarks);
        asset.DeviceAdminAccountName = NormalizeOptionalText(request.DeviceAdminAccountName);
        asset.DeviceAdminCredentialSecretReference = NormalizeOptionalText(request.DeviceAdminCredentialSecretReference);
        asset.DeviceAdminAccountNotes = NormalizeOptionalText(request.DeviceAdminAccountNotes);
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = CurrentActor();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to update asset {AssetId}.", id);
            return Conflict(new { message = "The asset could not be updated; verify the asset code and category." });
        }

        var response = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.AssetId == id)
            .Select(AssetResponseProjection)
            .SingleAsync(cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.Assets
            .SingleOrDefaultAsync(x => x.AssetId == id, cancellationToken);

        if (asset is null)
            return NotFound();

        if (!asset.IsActive)
            return NoContent();

        var hasActiveAssignment = await _dbContext.AssetAssignments.AnyAsync(
            x => x.AssetId == id && x.ReturnedAt == null,
            cancellationToken);

        if (hasActiveAssignment || asset.Status == AssetStatus.Assigned)
        {
            return Conflict(new
            {
                message = "An assigned asset cannot be deactivated. Return the asset first."
            });
        }

        asset.IsActive = false;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = CurrentActor();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private string? CurrentActor() => User.Identity?.Name;

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly Expression<Func<Asset, AssetResponse>> AssetResponseProjection = asset => new AssetResponse
    {
        AssetId = asset.AssetId,
        AssetCode = asset.AssetCode,
        AssetName = asset.AssetName,
        Description = asset.Description,
        AssetCategoryId = asset.AssetCategoryId,
        ClientId = asset.ClientId,
        ClientName = asset.Client == null ? null : asset.Client.ClientName,
        CategoryName = asset.Category!.CategoryName,
        Manufacturer = asset.Manufacturer,
        ModelNumber = asset.ModelNumber,
        SerialNumber = asset.SerialNumber,
        OwnershipType = asset.OwnershipType,
        Status = asset.Status,
        PurchaseDate = asset.PurchaseDate,
        PurchaseCost = asset.PurchaseCost,
        WarrantyExpiryDate = asset.WarrantyExpiryDate,
        Location = asset.Location,
        Remarks = asset.Remarks,
        DeviceAdminAccountName = asset.DeviceAdminAccountName,
        HasDeviceAdminCredentialReference = asset.DeviceAdminCredentialSecretReference != null,
        DeviceAdminAccountNotes = asset.DeviceAdminAccountNotes,
        IsActive = asset.IsActive,
        CreatedAt = asset.CreatedAt,
        CreatedBy = asset.CreatedBy,
        UpdatedAt = asset.UpdatedAt,
        UpdatedBy = asset.UpdatedBy
    };
}
