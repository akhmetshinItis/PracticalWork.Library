namespace PracticalWork.Library.DTO.BaseDtos
{
    /// <summary>
    /// Модель загруженного файла
    /// </summary>
    public class UploadedFile
    {
        /// <summary>
        /// Имя файла
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// MIME-тип содержимого файла
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Поток с содержимым файла
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Признак наличия файла (true, если поток не равен null)
        /// </summary>
        public bool HasFile => Stream != null;

        /// <summary>
        /// Конструктор модели загруженного файла
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        /// <param name="contentType">MIME-тип файла</param>
        /// <param name="stream">Поток с содержимым файла</param>
        public UploadedFile(string fileName, string contentType, Stream stream)
        {
            FileName = fileName;
            ContentType = contentType;
            Stream = stream;
        }
    }
}