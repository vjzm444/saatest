using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Key-Value Dictionary
    /// </summary>
    public partial class RedisDatabase : RedisConnector
    {
        private object AsyncState { get; set; }
        private ILogger<RedisDatabase> logger;

        /// <summary>
        /// Dependency Injection(DI)
        /// 객체를 생성하지 말자.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public RedisDatabase(ILogger<RedisDatabase> logger, IConfiguration configuration) : base(configuration)
        {
            this.logger = logger;
            AsyncState = new object();
        }

        /// <summary>
        /// 인스턴스 랩퍼 생성
        /// </summary>
        /// <param name="databaseType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public RedisDatabaseWrapper GetDatabase(RedisDatabaseType databaseType = RedisDatabaseType.Default)
        {
            try
            {
                return new RedisDatabaseWrapper
                {
                    Database = RedisObject.GetDatabase((int)databaseType, AsyncState)
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Can't Get Database: {databaseType.ToString()}");
                throw new Exception($"Can't Get Database: {databaseType.ToString()}", ex);
            }
        }

        /// <summary>
        /// 랭킹 스테이지 키
        /// </summary>
        /// <param name="roundId"></param>
        /// <param name="userId"></param>
        /// <param name="areaKey"></param>
        /// <returns></returns>
        public static string RankingUserStageKey(string roundId, ulong userId, string areaKey)
            => $"RankingUserStageKey:{roundId}:{userId}:{areaKey}";

        /// <summary>
        /// 최고점수 관리 키
        /// </summary>
        /// <param name="roundId"></param>
        /// <param name="userId"></param>
        /// <param name="areaKey"></param>
        /// <returns></returns>
        public static string RankingUserStageHighScoreKey(string roundId, ulong userId, string areaKey)
            => $"RankingUserStageHighScoreKey:{roundId}:{userId}:{areaKey}";

        /// <summary>
        /// 퀘스트 키
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string QuestUserKey(ulong userId)
            => $"QuestUserKey:{userId}";

        /// <summary>
        /// 유저 월렛 키
        /// </summary>
        /// <param name="walletAddress"></param>
        /// <returns></returns>
        public static string WalletUserKey(string walletAddress)
            => $"WalletUserKey:{walletAddress.ToLower()}";

        /// <summary>
        /// 손님액션 키
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="mode"></param>
        /// <param name="gameRoomId"></param>
        /// <returns></returns>
        public static string CustomActionUserKey(ulong userId, int mode, string gameRoomId)
            => $"CustomAction:{userId}:{mode}:{gameRoomId}";
        /// <summary>
        /// 소켓세션의 키
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static string SocketSessionKey(string token)
            => $"SocketSession:{token}";

        /// <summary>
        /// 게임룸 생성 기록
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string StageHistoryKey(ulong userId)
            => $"StageHistoryKey:{userId}";

        /// <summary>
        /// 게임룸 현재점수 기록
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string StageScoreKey(ulong userId)
            => $"StageScoreKey:{userId}";

        /// <summary>
        /// 클라이언트 버전 관리
        /// </summary>
        public static string ServiceClientVersionKey => "ServiceClientVersionKey";
        /// <summary>
        /// 게임 데이터 버전 관리
        /// </summary>
        public static string ServiceGameDataVersionKey => "ServiceGameDataVersionKey";
        /// <summary>
        /// 테스트 디바이스 관리
        /// </summary>
        [Obsolete("ServiceMaintenanceModeKey", true)]
        public static string ServiceTesterGuIdKey => "ServiceTesterGuIdKey";
        /// <summary>
        /// 테스트 유저 관리
        /// </summary>
        [Obsolete("ServiceMaintenanceModeKey", true)]
        public static string ServiceTesterUserIdKey => "ServiceTesterUserIdKey";
        /// <summary>
        /// 서버 점검 모드
        /// </summary>
        public static string ServiceMaintenanceModeKey => "ServiceMaintenanceModeKey";
    }
}
