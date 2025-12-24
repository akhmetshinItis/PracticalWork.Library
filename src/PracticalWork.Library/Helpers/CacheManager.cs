using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.DTO.BaseDtos;
using PracticalWork.Library.Models.BookModels;
using PracticalWork.Library.Options;

namespace PracticalWork.Library.Helpers;

public static class CacheManager
{
    /// <summary>
    /// Инвалидация кеша связанных данных при изменении книги.
    /// Инкрементирует версию кеша по префиксу, что делает все старые ключи невалидными.
    /// Ключи генерируются через CacheKeyHasher в формате: {prefix}:v{version}:{hash}
    /// </summary>
    public static async Task InvalidateBookCacheAsync(ICacheVersionService cacheVersionService, BooksCacheOptions cacheOptions)
    {
        if (cacheOptions?.BooksListCacheOptions?.Prefix != null)
        {
            await cacheVersionService.IncrementVersionAsync(cacheOptions.BooksListCacheOptions.Prefix);
        }

        if (cacheOptions?.LibraryBooksCacheOptions?.Prefix != null)
        {
            await cacheVersionService.IncrementVersionAsync(cacheOptions.LibraryBooksCacheOptions.Prefix);
        }

        if (cacheOptions?.BookDetailsCacheOptions?.Prefix != null)
        {
            await cacheVersionService.IncrementVersionAsync(cacheOptions.BookDetailsCacheOptions.Prefix);
        }
    }
      
    public static async Task InvalidateReportsCacheAsync(ICacheVersionService cacheVersionService, BooksCacheOptions cacheOptions)
    {
        if (cacheOptions?.ReportsCacheOptions?.Prefix != null)
        {
            await cacheVersionService.IncrementVersionAsync(cacheOptions.ReportsCacheOptions.Prefix);
        }
    }

    /// <summary>
    /// Проверяет кэш на наличие содержимого по типу
    /// </summary>
    /// <param name="cacheVersionService">Сервис версий кэша</param>
    /// <param name="cacheService">Сервис кэша</param>
    /// <param name="prefix">Префикс кэша</param>
    /// <param name="parameter">Параметр клюсча кэша</param>
    /// <param name="map">Делегат маппинга</param>
    /// <typeparam name="TCache">Тип объекта кэша</typeparam>
    /// <typeparam name="TModel">Тип объекта доменной модели</typeparam>
    /// <returns>Список книг</returns>
    public static async Task<List<TModel>> CheckCacheAsync<TModel,TCache>(ICacheVersionService cacheVersionService, ICacheService cacheService,
        string prefix, object parameter, Func<TCache, TModel> map)
    {
        if (prefix == null) return [];
        var version = await cacheVersionService.GetVersionAsync(prefix);
        var cacheKey = CacheKeyHasher.GenerateCacheKey(
            prefix,
            version,
            new
            {
                parameter
            });

        var cachedBooks = await cacheService.GetAsync<List<TCache>>(cacheKey);
        if (cachedBooks == null || cachedBooks.Count == 0) return [];
        var books = cachedBooks
            .Select(map)
            .ToList();

        return books;
    }

    /// <summary>
    /// записивает в кэш сериализованные объекты
    /// </summary>
    /// <param name="cacheVersionService">Сервис версий кэша</param>
    /// <param name="cacheService">Сервис кэша</param>
    /// <param name="option">Опция кэша</param>
    /// <param name="parameter">Параметр ключа кэша</param>
    /// <param name="modelList">Список объектов для маппинга</param>
    /// <param name="map">Делегат маппинга</param>
    /// <typeparam name="TCache">Тип объекта кэша</typeparam>
    /// <typeparam name="TModel">Тип объекта доменной модели</typeparam>
    public static async Task WriteToCacheAsync<TModel, TCache>(ICacheVersionService cacheVersionService, ICacheService cacheService,
        CacheOptions option, object parameter, IReadOnlyList<TModel> modelList, Func<TModel, TCache> map)
    {
        if (option.Prefix != null)
        {
            var version = await cacheVersionService.GetVersionAsync(option.Prefix);
            var cacheKey = CacheKeyHasher.GenerateCacheKey(
                option.Prefix,
                version,
                new
                {
                    parameter
                });

            var dto = modelList
                .Select(map)
                .ToList();

            var ttl = option.TtlMinutes;
            await cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(ttl));
        }
    }
}