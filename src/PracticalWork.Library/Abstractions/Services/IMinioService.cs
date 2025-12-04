namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис взаимодействия с Minio
/// </summary>
public interface IMinioService
{
    Task UploadFileAsync(string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default);
    Task<string> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default);
}