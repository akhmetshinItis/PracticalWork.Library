namespace PracticalWork.Library.Abstractions.Services;

/// <summary>
/// Сервис взаимодействия с Minio
/// </summary>
public interface IMinioService
{
    /// <summary>
    /// Загрузить файл
    /// </summary>
    /// <param name="bucket">Имя бакета</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="fileStream">Поток</param>
    /// <param name="contentType">Тип контента</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>-</returns>
    Task UploadFileAsync(string bucket, string fileName, Stream fileStream, string contentType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Получить URL для доступа к файлу
    /// </summary>
    /// <param name="bucket">Имя бакета</param>
    /// <param name="fileName">Имя файла</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>URL файла</returns>
    Task<string> GetFileUrlAsync(string bucket, string fileName, CancellationToken cancellationToken = default);
}