namespace PracticalWork.Library.Options;

/// <summary>
/// Настройки для Minio
/// </summary>
public class MinioOptions
{
    public string Endpoint { get; set; }
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string ReportsBucketName { get; set; }
    public string CoversBucketName { get; set; }
    public int ExpInSec { get; set; }
}