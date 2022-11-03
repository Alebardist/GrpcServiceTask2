using Newtonsoft.Json;

namespace GrpcServiceTask2.DTO
{
    public class Coin
    {
        public Coin(string newOwnerName)
        {
            Id = new Random().NextInt64();
            OwnersList.Add("Emissioned");
            OwnersList.Add(newOwnerName);
        }

        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("OwnersList")]
        public List<string> OwnersList { get; set; } = new List<string>();
    }
}
