
using PracticalWork.Library.Exceptions;

namespace ProFSB.Application.Exceptions;

    public class EntityNotFoundException<TEntity> : NotFoundException
    {
        /// <summary>
        /// Исключение о том, что сущность не найдена
        /// </summary>
        /// <param name="customMessage">Кастомное сообщение</param>
        public EntityNotFoundException(string customMessage)
            : base(customMessage)
        {
        }
    }