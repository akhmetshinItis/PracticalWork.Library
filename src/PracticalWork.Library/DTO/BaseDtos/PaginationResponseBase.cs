namespace PracticalWork.Library.DTO.BaseDtos
{
    /// <summary>
    /// Базовый ответ с пагинацией
    /// </summary>
    /// <typeparam name="T">Тип сущностей</typeparam>
    public class PaginationResponseBase<T>
    {
        /// <summary>
        /// Список сущностей
        /// </summary>
        public IReadOnlyList<T> Entities { get; set; }
    }
}