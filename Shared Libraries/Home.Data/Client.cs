using System;

namespace Home.Data
{
    public class Client
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        public bool IsRealClient { get; set; } = false;

        public override string ToString()
        {
            return $"c{ID}";
        }
    }
}
