using Newtonsoft.Json;

namespace GrpcServiceTask2.DTO
{
    public class User
    {
        public User()
        {
            Coins = new();
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rating")]
        public long Rating { get; set; }

        [JsonProperty("Coins")]
        public List<Coin> Coins { get; set; }
    }
}