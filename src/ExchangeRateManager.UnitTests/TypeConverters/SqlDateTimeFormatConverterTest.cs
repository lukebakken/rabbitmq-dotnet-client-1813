using ExchangeRateManager.Common.Serialization;
using FluentAssertions;
using System.Text;
using System.Text.Json;

namespace ExchangeRateManager.Tests.UnitTests.TypeConverters
{
    /// <summary>
    /// Tests the SQL DateTime json Type converter 
    /// </summary>
    public class SqlDateTimeFormatConverterTests
    {
        [Fact]
        public void FromStringToDateTime_CorrectFormat_Desserializes()
        {
            // Arrange
            var date = "\"2019-07-26 12:34:56\"";
            Utf8JsonReader reader = new(Encoding.UTF8.GetBytes(date));

            // Act
            reader.Read();
            var converter = new SqlDateTimeFormatJsonConverter();
            var result = converter.Read(ref reader, default!, default!);

            // Assert
            result.Should().Be(new DateTime(2019, 07, 26, 12, 34, 56));
        }

        [Fact]
        public void FromDateTimeToString_Serializes()
        {
            // Arrange
            var date = new DateTime(2019, 07, 26, 12, 34, 56);
            MemoryStream stream = new();
            Utf8JsonWriter writer = new(stream);

            // Act
            var converter = new SqlDateTimeFormatJsonConverter();
            converter.Write(writer, date, default!);
            writer.Flush();

            // Assert
            stream.Seek(0, SeekOrigin.Begin);
            Utf8JsonReader reader = new(stream.ToArray());
            reader.Read();

            reader.GetString().Should().Be("2019-07-26 12:34:56");
        }
    }
}
