namespace PracticalWork.Library.Abstractions.Services
{
    /// <summary>
    /// Сервис для версионирования кеша
    /// </summary>
    public interface ICacheVersionService
    {
        /// <summary>
        /// Получить текущую версию кеша
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Версия кеша</returns>
        Task<int> GetVersionAsync(string key, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Увеличить версию кеша
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task<int> IncrementVersionAsync(string key, CancellationToken cancellationToken = default);
    }
}