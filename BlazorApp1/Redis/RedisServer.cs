using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Server Methods
    /// </summary>
    public class RedisServer : RedisConnector
    {
        private IServer ServerObject { get; set; }
        private readonly ILogger<RedisServer> logger;

        /// <summary>
        /// Dependency Injection(DI)
        /// 객체를 생성하지 말자.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public RedisServer(ILogger<RedisServer> logger, IConfiguration configuration) : base(configuration)
        {
            this.logger = logger;
            var endPoint = RedisObject.GetEndPoints().First();
            ServerObject = RedisObject.GetServer(endPoint);
        }

        /// <summary>
        /// Redis 서버에 등록되어 있는 key SCAN
        /// 시간 복잡도:  O(N)
        /// </summary>
        /// <param name="database"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public async Task<List<string>> ScanKeysAsync(int database, string pattern)
        {
            List<string> result = new();

            try
            {
                var keys = ServerObject.KeysAsync(database: database, pattern: pattern);
                await foreach (RedisKey key in keys)
                {
                    result.Add(key.ToString());
                }

            }catch (Exception ex)
            {
                logger.LogWarning($"Redis ScanKeysAsync Error :: {ex}");
            }

            return result;
        }
    }
}
