using System.Text.Json.Serialization;

namespace Home.Data
{
    public class Answer<T>
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "ok";

        [JsonPropertyName("error")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public T Result { get; set; }

        [JsonIgnore]
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
