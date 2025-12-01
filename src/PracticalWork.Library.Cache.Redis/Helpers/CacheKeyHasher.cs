using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PracticalWork.Library.Cache.Redis.Helpers
{
    /// <summary>
    /// Вспомогательный класс для формирования ключей
    /// </summary>
    public static class CacheKeyHasher
    {
        /// <summary>
        /// Сгенерировать ключ
        /// </summary>
        /// <param name="prefix">Префикс</param>
        /// <param name="version">Версия</param>
        /// <param name="parameters">Параметры</param>
        /// <returns>Ключ</returns>
        public static string GenerateCacheKey(string prefix, int version, object parameters)
        {
            var json = JsonSerializer.Serialize(parameters);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
                .Substring(0, 16)
                .ToLower();

            return $"{prefix}:v{version}:{hash}";
        }
    }
}