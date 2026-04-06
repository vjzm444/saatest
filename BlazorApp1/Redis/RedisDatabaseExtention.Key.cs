using StackExchange.Redis;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Command
    /// </summary>
    public static partial class RedisDatabaseExtention
    {
        /// <summary>
        /// DEL
        /// 시간 복잡도 : O (N) 여기서 N은 제거 할 키 수입니다. 
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> KeyDeleteAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                return await database.KeyDeleteAsync(key, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// EXISTS
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> KeyExistsAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                return await database.KeyExistsAsync(key, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// EXPIRE
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task KeyExpireAsync(this RedisDatabaseWrapper databaseEx, string key, TimeSpan timeSpan)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            try
            {
                if (transaction == null)
                    await database.KeyExpireAsync(key, timeSpan, CommandFlags.None);
                else
                    await transaction.KeyExpireAsync(key, timeSpan, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// EXPIRE
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task KeyExpireAsync(this RedisDatabaseWrapper databaseEx, string key, DateTime dateTime)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            try
            {
                if (transaction == null)
                    await database.KeyExpireAsync(key, dateTime, CommandFlags.None);
                else
                    await transaction.KeyExpireAsync(key, dateTime, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// EXPIRE
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="days"></param>
        /// <param name="hour"></param>
        /// <param name="min"></param>
        /// <param name="sec"></param>
        /// <returns></returns>
        public static async Task KeyExpireAsync(this RedisDatabaseWrapper databaseEx, string key, int days = 0, int hour = 0, int min = 0, int sec = 0)
        {
            await databaseEx.KeyExpireAsync(key, new TimeSpan(days, hour, min, sec));
        }

        /// <summary>
        /// idletime 
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<TimeSpan?> KeyIdleTimeAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                TimeSpan? ts = await database.KeyIdleTimeAsync(key, CommandFlags.None);
                return ts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// TTL
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<TimeSpan?> KeyTimeToLiveAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                TimeSpan? ts = await database.KeyTimeToLiveAsync(key, CommandFlags.None);
                return ts;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// RANDOMKEY
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string> KeyRandomAsync(this RedisDatabaseWrapper databaseEx)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                return (await database.KeyRandomAsync(CommandFlags.None)).ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// TYPE
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string> KeyTypeAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                return (await database.KeyTypeAsync(key, CommandFlags.None)).ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }
    }
}
