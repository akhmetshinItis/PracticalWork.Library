namespace PracticalWork.Library.Options
{
    /// <summary>
    /// Настройки кеша для книг
    /// </summary>
    public class BooksCacheOptions
    {
        /// <summary>
        /// Настройки кеша для списка книг
        /// </summary>
        public CacheOptionsBase BooksListCacheOptions { get; set; }
        
        /// <summary>
        /// Настройки кеша для деталей книг
        /// </summary>
        public CacheOptionsBase BookDetailsCacheOptions { get; set; }
    }
}