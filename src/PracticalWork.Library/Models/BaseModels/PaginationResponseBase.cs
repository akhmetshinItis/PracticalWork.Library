namespace PracticalWork.Library.Models.BaseModels
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
        public List<T> Entities { get; set; }
    }
}