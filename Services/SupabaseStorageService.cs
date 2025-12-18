using Microsoft.Extensions.Options;
using property_service.Interfaces;
using property_service.Options;
using Supabase;

namespace property_service.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly SupabaseStorageOptions _options;
    private readonly Client _client;

    public SupabaseStorageService(IOptions<SupabaseStorageOptions> options, IConfiguration configuration)
    {
        _options = options.Value;

        _client = new Client(
            _options.Url,
            _options.ServiceRoleKey,
            new SupabaseOptions
            {
                AutoRefreshToken = false
            }
        );

        _client.InitializeAsync().GetAwaiter().GetResult();
    }

    public async Task<string> GetSignedUrlAsync(string storagePath)
    {
        var result = await _client.Storage
            .From(_options.StorageBucket)
            .CreateSignedUrl(storagePath, 3600); // 1h

        return result;
    }


    public async Task<string> UploadPropertyImageAsync(int propertyId, IFormFile file)
    {
        var extension = System.IO.Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = $"properties/{propertyId}/{fileName}";

        byte[] fileBytes;

        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        await _client.Storage
            .From(_options.StorageBucket)
            .Upload(fileBytes, filePath, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = false
            });

        return filePath;
    }


    public async Task DeleteImageAsync(string storagePath)
    {
        await _client.Storage
            .From(_options.StorageBucket)
            .Remove(new List<string> { storagePath });
    }
}
