using property_service.Enums;
using property_service.Models.PropertyModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace property_service.Models.PropertyAmenityModels;


[Table("property_amenity", Schema = "public")]
public class PropertyAmenity
{
    [Column("property_id")]
    public int PropertyId { get; set; }

    [Required]
    [Column("amenity_name")]
    public AmenityTypeEnum AmenityName { get; set; }

    public Property Property { get; set; } = null!;
}
