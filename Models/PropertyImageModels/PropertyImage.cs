using property_service.Models.PropertyModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace property_service.Models.PropertyImageModels;

[Table("property_image", Schema = "public")]
public class PropertyImage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("property_id")]
    public int PropertyId { get; set; }

    [Column("storage_path")]
    public string StoragePath { get; set; } = null!;

    public Property Property { get; set; } = null!;
}
