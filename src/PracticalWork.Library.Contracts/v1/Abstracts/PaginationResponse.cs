namespace PracticalWork.Library.Contracts.v1.Abstracts
{
    /// <summary>
    /// Базовый ответ с пагинацией
    /// </summary>
    /// <typeparam name="T">Тип сущностей</typeparam>
    public class PaginationResponse<T>
    {
        /// <summary>
        /// Список сущностей
        /// </summary>
        public List<T> Entities { get; set; }
    }
}