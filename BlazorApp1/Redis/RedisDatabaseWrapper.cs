using StackExchange.Redis;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Wrapper
    /// </summary>
    public class RedisDatabaseWrapper
    {
        /// <summary>
        /// 데이터베이스 인스턴스
        /// </summary>
        public IDatabase? Database { get; set; } = null;

        /// <summary>
        /// 트렌젝션
        /// </summary>
        public ITransaction? Transaction { get; set; } = null;

        /// <summary>
        /// 락이름
        /// </summary>
        public string? LockName { get; set; } = string.Empty;

        /// <summary>
        /// 락토큰
        /// </summary>
        public string? LockToken { get; set; } = string.Empty;
    }
}
