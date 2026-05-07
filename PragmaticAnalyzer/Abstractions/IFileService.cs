using PragmaticAnalyzer.Enums;

namespace PragmaticAnalyzer.Abstractions
{
    /// <summary>
    /// Интерфейс для сервиса, который позволяет сохранять/загружать различные объекты.
    /// </summary>
    public interface IFileService
    {
        Task<T?> LoadDTOAsync<T>(string? path, DataType type, CancellationToken ct = default);
        Task<T?> LoadFileToPathAsync<T>(string? path, CancellationToken ct = default);
        Task<T?> LoadJsonAsync<T>(string? json, CancellationToken ct = default);
        Task<bool> SaveDTOAsync<T>(T value, DataType type, string path, CancellationToken ct = default);
        Task<bool> SaveFileAsync(object value, string path, CancellationToken ct = default);
    }
}