using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using ZYRAHRM.IntegrationApp.DTOs.AssetManagement;
using ZyraHangfireModels.Models.AssetManagement;

namespace ZYRAHRM.IntegrationApp.Controllers;

[ApiController]
[Route("api/vendors")]
public sealed class VendorsController : ControllerBase
{
    private readonly AttendanceDbContext _dbContext;

    public VendorsController(AttendanceDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VendorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.VendorMasters.AsNoTracking();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var vendors = await query.OrderBy(x => x.VendorName)
            .Select(x => new VendorResponse
            {
                VendorId = x.VendorId,
                VendorCode = x.VendorCode,
                VendorName = x.VendorName,
                ContactPerson = x.ContactPerson,
                PhoneNumber = x.PhoneNumber,
                EmailAddress = x.EmailAddress,
                Address = x.Address,
                TaxRegistrationNumber = x.TaxRegistrationNumber,
                Notes = x.Notes,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(vendors);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VendorResponse>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var vendor = await _dbContext.VendorMasters.AsNoTracking()
            .Where(x => x.VendorId == id)
            .Select(x => new VendorResponse
            {
                VendorId = x.VendorId,
                VendorCode = x.VendorCode,
                VendorName = x.VendorName,
                ContactPerson = x.ContactPerson,
                PhoneNumber = x.PhoneNumber,
                EmailAddress = x.EmailAddress,
                Address = x.Address,
                TaxRegistrationNumber = x.TaxRegistrationNumber,
                Notes = x.Notes,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .SingleOrDefaultAsync(cancellationToken);

        return vendor is null ? NotFound() : Ok(vendor);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendorResponse>> Create(
        [FromBody] VendorWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.VendorCode.Trim();
        var name = request.VendorName.Trim();
        if (code.Length == 0 || name.Length < 2)
            return BadRequest(new { message = "VendorCode must not be blank and VendorName must contain at least 2 non-whitespace characters." });

        if (await _dbContext.VendorMasters.AnyAsync(x => x.VendorCode == code, cancellationToken))
            return Conflict(new { message = "A vendor with this code already exists." });

        var vendor = new VendorMaster
        {
            VendorCode = code,
            VendorName = name,
            ContactPerson = Normalize(request.ContactPerson),
            PhoneNumber = Normalize(request.PhoneNumber),
            EmailAddress = Normalize(request.EmailAddress),
            Address = Normalize(request.Address),
            TaxRegistrationNumber = Normalize(request.TaxRegistrationNumber),
            Notes = Normalize(request.Notes),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = CurrentActor()
        };

        _dbContext.VendorMasters.Add(vendor);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The vendor could not be created. Verify that the vendor code is unique." });
        }

        return CreatedAtAction(nameof(GetById), new { id = vendor.VendorId }, ToResponse(vendor));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VendorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VendorResponse>> Update(
        int id,
        [FromBody] VendorWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        var vendor = await _dbContext.VendorMasters.SingleOrDefaultAsync(x => x.VendorId == id, cancellationToken);
        if (vendor is null)
            return NotFound();

        var code = request.VendorCode.Trim();
        var name = request.VendorName.Trim();
        if (code.Length == 0 || name.Length < 2)
            return BadRequest(new { message = "VendorCode must not be blank and VendorName must contain at least 2 non-whitespace characters." });

        if (await _dbContext.VendorMasters.AnyAsync(x => x.VendorId != id && x.VendorCode == code, cancellationToken))
            return Conflict(new { message = "A vendor with this code already exists." });

        vendor.VendorCode = code;
        vendor.VendorName = name;
        vendor.ContactPerson = Normalize(request.ContactPerson);
        vendor.PhoneNumber = Normalize(request.PhoneNumber);
        vendor.EmailAddress = Normalize(request.EmailAddress);
        vendor.Address = Normalize(request.Address);
        vendor.TaxRegistrationNumber = Normalize(request.TaxRegistrationNumber);
        vendor.Notes = Normalize(request.Notes);
        vendor.UpdatedAt = DateTime.UtcNow;
        vendor.UpdatedBy = CurrentActor();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "The vendor could not be updated. Verify that the vendor code is unique." });
        }

        return Ok(ToResponse(vendor));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken = default)
    {
        var vendor = await _dbContext.VendorMasters.SingleOrDefaultAsync(x => x.VendorId == id, cancellationToken);
        if (vendor is null)
            return NotFound();

        if (!vendor.IsActive)
            return NoContent();

        var hasActiveRentedAsset = await _dbContext.Assets.AnyAsync(
            x => x.VendorId == id && x.IsActive && x.OwnershipType == AssetOwnershipType.Rented,
            cancellationToken);
        if (hasActiveRentedAsset)
            return Conflict(new { message = "This vendor has active rented assets. Return or update those assets before deactivating the vendor." });

        vendor.IsActive = false;
        vendor.UpdatedAt = DateTime.UtcNow;
        vendor.UpdatedBy = CurrentActor();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? CurrentActor() => User.Identity?.Name;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static VendorResponse ToResponse(VendorMaster vendor) => new()
    {
        VendorId = vendor.VendorId,
        VendorCode = vendor.VendorCode,
        VendorName = vendor.VendorName,
        ContactPerson = vendor.ContactPerson,
        PhoneNumber = vendor.PhoneNumber,
        EmailAddress = vendor.EmailAddress,
        Address = vendor.Address,
        TaxRegistrationNumber = vendor.TaxRegistrationNumber,
        Notes = vendor.Notes,
        IsActive = vendor.IsActive,
        CreatedAt = vendor.CreatedAt,
        CreatedBy = vendor.CreatedBy,
        UpdatedAt = vendor.UpdatedAt,
        UpdatedBy = vendor.UpdatedBy
    };
}