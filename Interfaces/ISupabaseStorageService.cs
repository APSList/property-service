namespace property_service.Interfaces;
public interface ISupabaseStorageService
{
    Task<string> GetSignedUrlAsync(string storagePath);
    Task<string> UploadPropertyImageAsync(int propertyId, IFormFile file);
    Task DeleteImageAsync(string storagePath);
}
