using Mapster;
using Microsoft.EntityFrameworkCore;
using property_service.Database;
using property_service.Enums;
using property_service.Interfaces;
using property_service.Models.PropertyAmenityModels;
using property_service.Models.PropertyImageModels;
using property_service.Models.PropertyModels;
using System.Runtime.CompilerServices;

namespace property_service.Services;

public class PropertyService : IPropertyService
{
    private readonly PropertyDbContext _context;
    private readonly ISupabaseStorageService _storage;

    public PropertyService(PropertyDbContext context, ISupabaseStorageService storage)
    {
        _context = context;
        _storage = storage;
    }

    public async Task<List<Property>> GetPropertiesAsync(PropertyFilter filter)
    {
        // AsNoTracking: da ne "trackaš" in po nesreči ne shranjuješ signed URL-jev v DB
        var properties = await _context.Properties
            .AsNoTracking()
            .Include(p => p.PropertyImages)
            .ToListAsync();

        // Če želiš hitreje, lahko narediš Task.WhenAll, ampak pazi na rate-limite storage-a.
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
        var property = await _context.Properties
            .AsNoTracking()
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
        var property = dto.Adapt<Property>();

        property.OrganizationId = 1;
        property.CreatedBy = "USER";
        property.PropertyImages = [];

        await _context.Properties.AddAsync(property);

        await _context.SaveChangesAsync();

        dto.Amenities = [AmenityTypeEnum.Pool, AmenityTypeEnum.Wifi];
        await AddImagesAsync(dto, property);

        await _context.SaveChangesAsync();

        return property.Id!.Value;
    }


    public async Task<int?> UpdatePropertyAsync(int id, PropertyCreateRequestDTO dto)
    {
        // Naloži obstoječ entity (tracked)
        var property = await _context.Properties
            .Include(p => p.PropertyAmenities)
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        dto.Adapt(property);
        property.UpdatedBy = "USER"; // TODO: iz auth konteksta

        await AddImagesAsync(dto, property);

        await _context.SaveChangesAsync();
        return property.Id;
    }

    public async Task<int?> DeletePropertyByIdAsync(int id)
    {
        var property = await _context.Properties
            .Include(p => p.PropertyImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (property is null) return null;

        // Najprej pobriši iz storage
        foreach (var image in property.PropertyImages)
        {
            await _storage.DeleteImageAsync(image.StoragePath);
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        return id;
    }

    // -----------------------
    // Helpers
    // -----------------------

    private async Task AddImagesAsync(PropertyCreateRequestDTO dto, Property property)
    {
        if (dto.Images is not { Count: > 0 }) return;

        foreach (var file in dto.Images)
        {
            // ✅ MUST await (če ne, bo stream disposed in dobiš disposed exception)
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
