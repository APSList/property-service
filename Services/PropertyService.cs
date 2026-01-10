using Mapster;
using Microsoft.EntityFrameworkCore;
using property_service.Database;
using property_service.Enums;
using property_service.Interfaces;
using property_service.Models.PropertyAmenityModels;
using property_service.Models.PropertyImageModels;
using property_service.Models.PropertyModels;

namespace property_service.Services;

public class PropertyService : IPropertyService
{
    private readonly PropertyDbContext _context;
    private readonly ISupabaseStorageService _storage;
    private readonly IOrganizationContext _org;

    public PropertyService(PropertyDbContext context, ISupabaseStorageService storage, IOrganizationContext orgContext)
    {
        _context = context;
        _storage = storage;
        _org = orgContext;
    }

    public async Task<List<Property>> GetPropertiesAsync()
    {
        var orgId = _org.OrganizationId;

        var properties = await _context.Properties
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Include(p => p.PropertyImages)
            .ToListAsync();

        foreach (var property in properties)
        {
            foreach (var image in property.PropertyImages)
            {
                image.StoragePath = await _storage.GetSignedUrlAsync(image.StoragePath);
            }
        }

        return properties;
    }

    public async Task<Property?> GetPropertyByIdAsync(int id)
    {
        var orgId = _org.OrganizationId;

        var property = await _context.Properties
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Include(p => p.PropertyImages)
            .Include(p => p.PropertyAmenities)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        foreach (var image in property.PropertyImages)
        {
            image.StoragePath = await _storage.GetSignedUrlAsync(image.StoragePath);
        }

        return property;
    }

    public async Task<int> InsertPropertyAsync(PropertyCreateRequestDTO dto)
    {
        var orgId = _org.OrganizationId;
        var email = _org.Email ?? "USER";

        var property = dto.Adapt<Property>();

        property.OrganizationId = orgId;
        property.CreatedBy = email;
        property.PropertyImages = [];
        property.PropertyAmenities = [];

        await _context.Properties.AddAsync(property);
        await _context.SaveChangesAsync();

        dto.Amenities = [AmenityTypeEnum.Pool, AmenityTypeEnum.Wifi];
        AddAmenities(dto, property);
        await AddImagesAsync(dto, property);

        await _context.SaveChangesAsync();

        return property.Id!.Value;
    }

    public async Task<int?> UpdatePropertyAsync(int id, PropertyCreateRequestDTO dto)
    {
        var orgId = _org.OrganizationId;
        var email = _org.Email ?? "USER";

        var property = await _context.Properties
            .Where(p => p.OrganizationId == orgId)
            .Include(p => p.PropertyAmenities)
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        dto.Adapt(property);
        property.UpdatedBy = email;

        AddAmenities(dto, property);
        await AddImagesAsync(dto, property);

        await _context.SaveChangesAsync();
        return property.Id;
    }

    public async Task<int?> DeletePropertyByIdAsync(int id)
    {
        var orgId = _org.OrganizationId;

        var property = await _context.Properties
            .Where(p => p.OrganizationId == orgId)
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        foreach (var image in property.PropertyImages)
        {
            await _storage.DeleteImageAsync(image.StoragePath);
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        return id;
    }

    private async Task AddImagesAsync(PropertyCreateRequestDTO dto, Property property)
    {
        if (dto.Images is not { Count: > 0 }) return;

        foreach (var file in dto.Images)
        {
            var path = await _storage.UploadPropertyImageAsync(property.Id!.Value, file);

            property.PropertyImages.Add(new PropertyImage
            {
                StoragePath = path,
                Property = property,
                PropertyId = property.Id
            });
        }
    }

    private void AddAmenities(PropertyCreateRequestDTO dto, Property property)
    {
        var incoming = dto.Amenities?.Distinct().ToList() ?? new List<AmenityTypeEnum>();
        if (incoming.Count == 0) return;

        foreach (var a in incoming)
        {
            property.PropertyAmenities.Add(new PropertyAmenity
            {
                AmenityName = a,
                Property = property,
                PropertyId = property.Id
            });
        }
    }
}
