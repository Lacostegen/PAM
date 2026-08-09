using PragmaticAnalyzer.Abstractions;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PragmaticAnalyzer.Services
{
    public class FileService : IFileService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task<T?> LoadFileToPathAsync<T>(
            string? path,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return default;
            }

            await using var fileStream = File.OpenRead(path);

            return await JsonSerializer.DeserializeAsync<T>(
                fileStream,
                JsonOptions,
                ct);
        }

        public async Task<T?> LoadJsonAsync<T>(
            string? json,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            await using var memoryStream = new MemoryStream(
                System.Text.Encoding.UTF8.GetBytes(json));

            return await JsonSerializer.DeserializeAsync<T>(
                memoryStream,
                JsonOptions,
                ct);
        }

        public async Task<bool> SaveFileAsync(
            object value,
            string path,
            CancellationToken ct = default)
        {
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = File.Create(path);

            await JsonSerializer.SerializeAsync(
                fileStream,
                value,
                value.GetType(),
                JsonOptions,
                ct);

            return true;
        }
    }
}
