using property_service.Enums;

namespace property_service.Models.PropertyModels;

public class PropertyCreateRequestDTO
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }
    public string? Adress { get; set; }
    public string? Country { get; set; }

    public PropertyTypeEnum PropertyTypeEnum { get; set; }
    public int? MaxGuests { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }

    public decimal? PricePerPersonDay { get; set; }
    public decimal? DennyFee { get; set; }
    public PropertyStatusEnum Status { get; set; }

    public List<IFormFile>? Images { get; set; }
    public List<AmenityTypeEnum>? Amenities { get; set; }

}
