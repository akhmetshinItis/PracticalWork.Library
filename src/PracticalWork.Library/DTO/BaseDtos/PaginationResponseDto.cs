namespace PracticalWork.Library.DTO.BaseDtos
{
    /// <summary>
    /// Базовый ответ с пагинацией
    /// </summary>
    /// <typeparam name="T">Тип сущностей</typeparam>
    public class PaginationResponseDto<T>
    {
        /// <summary>
        /// Список сущностей
        /// </summary>
        public IReadOnlyList<T> Entities { get; set; }
        
        /// <summary>
        /// Общее количество записей
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Количество страниц
        /// </summary>
        public int PageCount
            => PageSize <= 0
                ? 0
                : (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        /// <summary>
        /// Номер страницы
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// Размер страницы
        /// </summary>
        public int PageSize { get; set; }
    }
}