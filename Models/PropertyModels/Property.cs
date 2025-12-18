using property_service.Enums;
using property_service.Models.PropertyAmenityModels;
using property_service.Models.PropertyImageModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace property_service.Models.PropertyModels;

[Table("property", Schema = "public")]
public class Property
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("country")]
    public string? Country { get; set; }

    [Column("property_type")]
    public string? PropertyType { get; set; }

    [Column("max_guests")]
    public int? MaxGuests { get; set; }

    [Column("bedrooms")]
    public int? Bedrooms { get; set; }

    [Column("bathrooms")]
    public int? Bathrooms { get; set; }

    [Required]
    [Column("status")]
    public PropertyStatusEnum Status { get; set; }

    [Column("denny_fee", TypeName = "numeric(10,2)")]
    public decimal? DennyFee { get; set; }

    [Column("price_per_person_day", TypeName = "numeric(10,2)")]
    public decimal? PricePerPersonDay { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_by")]
    public string? UpdatedBy { get; set; }

    // NAVIGATION
    public List<PropertyImage> Images { get; set; } = [];
    public List<PropertyAmenity> PropertyAmenities { get; set; } = [];

}
