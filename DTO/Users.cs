using Newtonsoft.Json;

namespace GrpcServiceTask2.DTO
{
    public class Users
    {
        [JsonProperty("Users")]
        public List<User> UsersList { get; set; }
    }
}