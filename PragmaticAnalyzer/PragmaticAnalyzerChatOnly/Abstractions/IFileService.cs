namespace PragmaticAnalyzer.Abstractions
{
    public interface IFileService
    {
        Task<T?> LoadFileToPathAsync<T>(string? path, CancellationToken ct = default);

        Task<T?> LoadJsonAsync<T>(string? json, CancellationToken ct = default);

        Task<bool> SaveFileAsync(object value, string path, CancellationToken ct = default);
    }
}
