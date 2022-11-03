using Grpc.Core;

using GrpcServiceTask2.DTO;

using Newtonsoft.Json;

using System.Diagnostics;

namespace Billing.Services
{
    public class BillingService : Billing.BillingBase
    {
        private readonly ILogger<BillingService> _logger;
        private readonly string _pathToJSON = Environment.CurrentDirectory + @"\JSONs\Users.json";

        public BillingService(ILogger<BillingService> logger)
        {
            _logger = logger;
        }

        public override Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            Response response = new() { Comment = "", Status = Response.Types.Status.Unspecified };
            Users users;

            try
            {
                if (request.Amount <= 0)
                {
                    throw new ArgumentOutOfRangeException($"{nameof(request.Amount)} was zero or less");
                }

                using (var file = File.OpenText(_pathToJSON))
                {
                    users = JsonConvert.DeserializeObject<Users>(file.ReadToEnd());
                    if (users == null)
                    {
                        throw new NullReferenceException($"{nameof(users)} was null");
                    }
                }

                foreach (var user in users.UsersList)
                {
                    for (long i = 0; i <= user.Rating && request.Amount > 0; i++)
                    {
                        user.Coins.Add(new GrpcServiceTask2.DTO.Coin(user.Name));
                        request.Amount--;

                        if (user.Rating == 0)
                        {
                            break;
                        }
                    }
                }

                var serializedUsers = JsonConvert.SerializeObject(users);
                File.WriteAllText(_pathToJSON, serializedUsers);

                response.Status = Response.Types.Status.Ok;
            }
            catch (Exception ex)
            {
                response.Status = Response.Types.Status.Failed;
                response.Comment = ex.Message;
            }

            return Task.FromResult(response);
        }

        public override async Task ListUsers(None request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {
            try
            {
                string json;

                using (var stream = File.OpenText(_pathToJSON))
                {
                    json = stream.ReadToEnd();
                }

                Users users = JsonConvert.DeserializeObject<Users>(json);

                if (users == null)
                {
                    throw new NullReferenceException($"{nameof(users)} was null");
                }

                foreach (var user in users.UsersList)
                {
                    await responseStream.WriteAsync(new UserProfile() { Name = user.Name, Amount = user.Rating });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public override async Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            GrpcServiceTask2.DTO.Coin coinWithLongestHistory = default;

            try
            {
                string json = File.OpenText(_pathToJSON).ReadToEnd();

                Users users = JsonConvert.DeserializeObject<Users>(json);

                if (users == null)
                {
                    throw new NullReferenceException($"{nameof(users)} was null");
                }

                List<GrpcServiceTask2.DTO.Coin> coins = new();

                foreach (var user in users.UsersList)
                {
                    coins.AddRange(user.Coins);
                }

                coinWithLongestHistory = coins.First(x => x.OwnersList.Count == coins.Max(a => a.OwnersList.Count));
            }
            catch (Exception ex)
            {
                return await Task.FromException<Coin>(ex);
            }

            return await Task.FromResult(new Coin() { Id = coinWithLongestHistory.Id, History = coinWithLongestHistory.OwnersList.Count.ToString() });
        }

        public override Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            Response response = new() { Comment = "", Status = Response.Types.Status.Unspecified };
            Users users;
            try
            {
                users = GetAllUsersFromJsonFile();
            }
            catch (Exception ex)
            {
                throw new RpcException(Status.DefaultCancelled, ex.Message);
            }

            User transmitFrom;
            try
            {
                transmitFrom = GetUserIfExists(users, request.SrcUser);
            }
            catch (Exception ex)
            {
                throw new RpcException(Status.DefaultCancelled, ex.Message);
            }

            if (transmitFrom.Coins.Count >= request.Amount)
            {
                try
                {
                    User transmitTo = GetUserIfExists(users, request.DstUser);

                    UpdateCoinsOwnerAndTransmitThem(request, transmitFrom, transmitTo);
                }
                catch (Exception ex)
                {
                    throw new RpcException(Status.DefaultCancelled, ex.Message);
                }

                string serializedUsers = JsonConvert.SerializeObject(users);
                File.WriteAllText(_pathToJSON, serializedUsers);
                response.Status = Response.Types.Status.Ok;
            }
            else
            {
                response.Status = Response.Types.Status.Failed;
                response.Comment = "Not enought money";
            }

            return Task.FromResult(response);
        }

        private static void UpdateCoinsOwnerAndTransmitThem(MoveCoinsTransaction request, User transmitFrom, User transmitTo)
        {
            //HACK: possible accuracy loss cause of unboxing
            var coinsForTransmitting = transmitFrom.Coins.Take((int)request.Amount);

            foreach (var coin in coinsForTransmitting)
            {
                coin.OwnersList.Add(transmitTo.Name);
            }

            transmitTo.Coins.AddRange(coinsForTransmitting);
            transmitFrom.Coins.RemoveRange(0, (int)request.Amount);
        }

        private Users GetAllUsersFromJsonFile()
        {
            Users users = new();
            try
            {
                string json = File.ReadAllText(_pathToJSON);
                users = JsonConvert.DeserializeObject<Users>(json) ?? throw new NullReferenceException(nameof(JsonConvert.DeserializeObject) +
                    "returned null");
            }
            catch (Exception)
            {
                throw;
            }

            return users;
        }

        private User GetUserIfExists(Users users, string name)
        {
            User foundedUser = new();
            try
            {
                foundedUser = users.UsersList.First(user => user.Name == name);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"{ex.Message} User {name} not found");
            }
            catch (Exception)
            {
                throw;
            }

            return foundedUser;
        }
    }
}