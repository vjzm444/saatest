using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis
    /// </summary>
    public abstract class RedisConnector
    {
        /// <summary>
        /// Redis Database Number
        /// </summary>
        public enum RedisDatabaseType
        {
            /// <summary>
            /// default
            /// </summary>
            Default = 0,
            /// <summary>
            /// 라이브러리에서 시스템 관리
            /// </summary>
            SystemMaintenance = 1,
            /// <summary>
            /// 서버 관리
            /// </summary>
            ServerMaintenance = 2,
            /// <summary>
            /// 랭킹 스테이지 임시데이터
            /// </summary>
            RankStage = 3,
            /// <summary>
            /// 퀘스트
            /// </summary>
            Quest = 4,
            /// <summary>
            /// 손님액션
            /// </summary>
            CustomActionInfo = 5,
            /// <summary>
            /// 소켓세션
            /// </summary>
            SocketSession = 6,
            /// <summary>
            /// 스테이지 게임룸 생성 히스토리
            /// </summary>
            StageHistory = 7,
            /// <summary>
            /// 지갑 토큰 수량
            /// </summary>
            WebWalletBalance =11,
        }

        /// <summary>
        /// ConnectionMultiplexer
        /// </summary>
        protected ConnectionMultiplexer RedisObject { get; set; }

        /// <summary>
        /// ConnectString
        /// </summary>
        public string ConnectString { get; private set; }

        /// <summary>
        /// Redis 연결
        /// </summary>
        /// <param name="configuration"></param>
        public RedisConnector(IConfiguration configuration)
        {
            ConnectString = configuration["ConnectionStrings:Redis"];
            RedisObject = ConnectionMultiplexer.Connect(ConnectString);
        }
    }
}
