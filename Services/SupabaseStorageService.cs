using Microsoft.Extensions.Options;
using property_service.Interfaces;
using property_service.Options;
using Supabase;

namespace property_service.Services;

public class SupabaseStorageService : ISupabaseStorageService
{
    private readonly SupabaseStorageOptions _options;
    private readonly Client _client;

    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SupabaseStorageService(IOptions<SupabaseStorageOptions> options)
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
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized) return;

            await _client.InitializeAsync();
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<string> GetSignedUrlAsync(string storagePath)
    {
        await EnsureInitializedAsync();

        return await _client.Storage
            .From(_options.StorageBucket)
            .CreateSignedUrl(storagePath, 3600); // 1h
    }

    public async Task<string> UploadPropertyImageAsync(int propertyId, IFormFile file)
    {
        await EnsureInitializedAsync();

        var extension = System.IO.Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = $"properties/{propertyId}/{fileName}";

        byte[] fileBytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            fileBytes = ms.ToArray();
        }

        await _client.Storage
            .From(_options.StorageBucket)
            .Upload(
                fileBytes,
                filePath,
                new Supabase.Storage.FileOptions
                {
                    ContentType = file.ContentType,
                    Upsert = false
                }
            );

        return filePath;
    }

    public async Task DeleteImageAsync(string storagePath)
    {
        await EnsureInitializedAsync();

        await _client.Storage
            .From(_options.StorageBucket)
            .Remove(new List<string> { storagePath });
    }
}
