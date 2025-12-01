
namespace PracticalWork.Library.Exceptions;

    public class EntityNotFoundException<TEntity> : NotFoundException
    {
        /// <summary>
        /// Исключение о том, что сущность не найдена
        /// </summary>
        /// <param name="id">Идентификатор сущности</param>
        public EntityNotFoundException(Guid id)
            : base($"Не найдены {typeof(TEntity).FullName} с идентификатором: {id}.")
        {
        }
    }