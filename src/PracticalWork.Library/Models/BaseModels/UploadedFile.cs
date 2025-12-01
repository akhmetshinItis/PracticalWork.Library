namespace PracticalWork.Library.Models.BaseModels
{
    public class UploadedFile
    {
        public string FileName { get; }
        public string ContentType { get; }
        public Stream Stream { get; }

        public bool HasFile => Stream != null;

        public UploadedFile(string fileName, string contentType, Stream stream)
        {
            FileName = fileName;
            ContentType = contentType;
            Stream = stream;
        }
    }
}