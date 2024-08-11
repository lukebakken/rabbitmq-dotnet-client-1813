using System.Text;
using System.Text.Json;

namespace ExchangeRateManager.Common.Extensions
{
    /// <summary>
    /// General object extensions, mostly for serialization and desserialization.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Serializes an object into a json string
        /// </summary>
        public static string ToJson<T>(this T source)
            => JsonSerializer.Serialize(source, typeof(T));

        /// <summary>
        /// Desserializes a json string into an object
        /// </summary>
        public static T? FromJson<T>(this string json)
            => JsonSerializer.Deserialize<T>(json);

        /// <summary>
        /// Serializes an object into an UTF8 json byte array.
        /// </summary>
        public static byte[] ToUTF8JsonByteArray<T>(this T source)
            => Encoding.UTF8.GetBytes(source.ToJson());
    }
}
