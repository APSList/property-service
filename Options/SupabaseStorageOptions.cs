using System.ComponentModel.DataAnnotations;

namespace property_service.Options;

public class SupabaseStorageOptions
{
    public const string SectionName = "SupabaseStorage";

    [Required]
    public string Url { get; set; } = null!;

    [Required]
    public string ServiceRoleKey { get; set; } = null!;

    [Required]
    public string StorageBucket { get; set; } = "property-images";

}
