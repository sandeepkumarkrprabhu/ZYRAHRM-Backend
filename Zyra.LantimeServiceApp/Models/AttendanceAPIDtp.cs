using System.Text.Json.Serialization;

namespace Zyra.LantimeServiceApp.Models
{
    public class AttendanceAPIDto
    {
        [JsonPropertyName("employee_code")]
        public string employee_code {get; set;}

        [JsonPropertyName("type")]
        public string type {get; set;}

        [JsonPropertyName("date_time")]
        public DateTime date_time {get; set;}
    }
}
