namespace PracticalWork.Library.Data.Minio;

/// <summary>
/// Настройки для Minio
/// </summary>
public class MinioOptions
{
    public string Endpoint { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public int ExpInSec { get; set; }
}