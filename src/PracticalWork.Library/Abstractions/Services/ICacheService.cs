using JetBrains.Annotations;

namespace PracticalWork.Library.Abstractions.Services
{
    /// <summary>
    /// Сервис для взаимодействия с кешем
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Получить данные из кеша
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <typeparam name="T">Тип данных</typeparam>
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Записать данные в кеш
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="value">Значение</param>
        /// <param name="ttl">Время жизни</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <typeparam name="T">Тип данных</typeparam>
        Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Удалить данные из кеша
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="cancellationToken">Токен отмены</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}