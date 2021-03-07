#if !LEGACY
using System.Text.Json.Serialization;
#endif

using Newtonsoft.Json;

namespace Home.Data
{
    public class Answer<T>
    {
        [JsonProperty("status")]
#if !LEGACY
        [JsonPropertyName("status")]
#endif
        public string Status { get; set; } = "ok";

#if !LEGACY
        [JsonPropertyName("error")]
#endif
        [JsonProperty("error")]
        public string ErrorMessage { get; set; } = string.Empty;

#if !LEGACY
        [JsonPropertyName("result")]
#endif
        [JsonProperty("result")]
        public T Result { get; set; }

#if !LEGACY
        [System.Text.Json.Serialization.JsonIgnore]
#endif
        [Newtonsoft.Json.JsonIgnore]
        public bool Success => Status == "ok";

        public Answer()
        { }

        public Answer(string status, T value)
        {
            this.Status = status;
            Result = value;
        }
    }

    public static class AnswerExtensions
    {
        public static Answer<object> Fail(string reason)
        {
            return new Answer<object>() { Status = "fail", ErrorMessage = reason };
        }

        public static Answer<T> Fail<T>(string reason)
        {
            return new Answer<T>() { Status = "fail", ErrorMessage = reason };
        }

        public static Answer<T> Success<T>(T value)
        {
            return new Answer<T>("ok", value);
        }
    }

}
