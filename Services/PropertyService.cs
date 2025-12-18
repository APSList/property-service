using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using property_service.Database;
using property_service.Interfaces;
using property_service.Models.PropertyAmenityModels;
using property_service.Models.PropertyImageModels;
using property_service.Models.PropertyModels;

namespace property_service.Services;

public class PropertyService : IPropertyService
{
    private readonly PropertyDbContext _context;
    private readonly ISupabaseStorageService _storage;

    public PropertyService(
        PropertyDbContext context,
        ISupabaseStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<List<Property>> GetPropertiesAsync(PropertyFilter filter)
    {
        var properties = await _context.Properties
            .Include(p => p.Images)
            .ToListAsync();

        foreach (var property in properties)
        {
            foreach (var image in property.Images)
            {
                image.StoragePath = await _storage.GetSignedUrlAsync(image.StoragePath);
            }
        }

        return properties;
    }

    public async Task<Property?> GetPropertyByIdAsync(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null)
        {
            return null;
        }

        property.Images.ToList().ForEach(async (image) =>
        {
            image.StoragePath = await _storage.GetSignedUrlAsync(image.StoragePath);
        });

        return property;
    }

    public async Task<int> InsertPropertyAsync(PropertyCreateRequestDTO dto)
    {
        //TODO VALIDATION ON PropertyCreateRequestDTO

        var property = dto.Adapt<Property>();
        property.OrganizationId = 1; //TODO
        property.CreatedBy = "TODO"; //TODO

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        await HandleImages(dto, property);
        await HandleAmenities(dto, property);

        return property.Id;
    }

    public async Task<int?> UpdatePropertyAsync(int id, PropertyCreateRequestDTO dto)
    {
        //TODO VALIDATION ON PropertyCreateRequestDTO

        var property = await _context.Properties.FirstOrDefaultAsync(p => p.Id == id);
        if (property == null) return null;

        property = dto.Adapt<Property>();
        property.UpdatedBy = "TODO"; //TODO

        await _context.SaveChangesAsync();


        await HandleImages(dto, property);
        await HandleAmenities(dto, property);
        return property.Id;
    }

    public async Task<int?> DeletePropertyByIdAsync(int id)
    {
        var property = await _context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property == null) return null;

        foreach (var image in property.Images)
        {
            await _storage.DeleteImageAsync(image.StoragePath);
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        return id;
    }

    private async Task HandleImages(PropertyCreateRequestDTO dto, Property property)
    {
        if (dto.Images != null && dto.Images.Count != 0)
        {
            foreach (var image in dto.Images)
            {
                var path = await _storage.UploadPropertyImageAsync(property.Id, image);

                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = property.Id,
                    StoragePath = path,
                });
            }

            await _context.SaveChangesAsync();
        }

    }

    private async Task HandleAmenities(PropertyCreateRequestDTO dto, Property property)
    {
        if (dto.Amenities != null && dto.Amenities.Count != 0)
        {
            foreach (var amenity in dto.Amenities)
            {
                _context.PropertyAmenities.Add(new PropertyAmenity
                {
                    PropertyId = property.Id,
                    AmenityName = amenity,
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
