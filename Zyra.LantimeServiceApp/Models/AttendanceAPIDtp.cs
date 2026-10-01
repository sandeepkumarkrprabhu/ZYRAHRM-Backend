using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zyra.LantimeServiceApp.Models
{
    public class AttendanceAPIDto
    {
        [JsonPropertyName("employee_code")]
        public string employee_code { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("date_time")]
        [JsonConverter(typeof(AttendanceApiDateTimeConverter))]
        public DateTime date_time { get; set; }
    }

    public sealed class AttendanceApiDateTimeConverter : JsonConverter<DateTime>
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return DateTime.Parse(
                reader.GetString()!,
                System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(
                DateTimeFormat,
                System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
