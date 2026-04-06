using StackExchange.Redis;
using System.Text.Json;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Command
    /// </summary>
    public static partial class RedisDatabaseExtention
    {
        #region Get
        /// <summary>
        /// GET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string> StringGetAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisValue redisValue = await database.StringGetAsync(new RedisKey(key), CommandFlags.None);
                return redisValue.ToString();
            }
            catch (RedisServerException ex)
            {
                throw new Exception("Redis ServerException", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Redis Database Exception: {ex}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// GET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string[]> StringGetAsync(this RedisDatabaseWrapper databaseEx, string[] keys)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisKey[] redisKeys = new RedisKey[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                    redisKeys[i] = new RedisKey(keys[i]);

                RedisValue[] redisValue = await database.StringGetAsync(redisKeys, CommandFlags.None);

                string[] stringValues = new string[redisValue.Length];
                for (int i = 0; i < redisValue.Length; i++)
                {
                    if (redisValue[i].IsNull)
                        stringValues[i] = string.Empty;

                    else
                        stringValues[i] = redisValue[i].ToString();
                }

                return stringValues;
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// GET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?> StringGetAsync<T>(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisValue redisValue = await database.StringGetAsync(new RedisKey(key), CommandFlags.None);
                return redisValue.ConvertModel<T>();
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// GET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="keys"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?[]> StringGetAsync<T>(this RedisDatabaseWrapper databaseEx, string[] keys)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisKey[] redisKeys = new RedisKey[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                    redisKeys[i] = new RedisKey(keys[i]);

                RedisValue[] redisValue = await database.StringGetAsync(redisKeys, CommandFlags.None);
                return redisValue.ConvertModel<T>();
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }
        #endregion

        #region Set
        /// <summary>
        /// SET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> StringSetAsync(this RedisDatabaseWrapper databaseEx, string key, string value, TimeSpan expire)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            try
            {
                bool result;
                if (transaction == null)
                    result = await database.StringSetAsync(key, value, expire, When.Always, CommandFlags.None);

                else
                    result = await transaction.StringSetAsync(key, value, expire, When.Always, CommandFlags.None);

                return result;
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// SET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task StringSetAsync(this RedisDatabaseWrapper databaseEx, KeyValuePair<string, string>[] pairs)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            KeyValuePair<RedisKey, RedisValue>[] entry = new KeyValuePair<RedisKey, RedisValue>[pairs.Length];
            for (var i = 0; i < pairs.Length; i++)
            {
                entry[i] = new KeyValuePair<RedisKey, RedisValue>(pairs[i].Key, pairs[i].Value);
            }
            try
            {
                if (transaction == null)
                    await database.StringSetAsync(entry, When.Always, CommandFlags.None);

                else
                    await transaction.StringSetAsync(entry, When.Always, CommandFlags.None);
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// SET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> StringSetAsync<T>(this RedisDatabaseWrapper databaseEx, string key, T value, TimeSpan? expire = null)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            try
            {
                bool result;
                var val = JsonSerializer.Serialize(value);
                if (transaction == null)
                    result = await database.StringSetAsync(key, val, expire, When.Always, CommandFlags.None);

                else
                    result = await transaction.StringSetAsync(key, val, expire, When.Always, CommandFlags.None);

                return result;
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        /// <summary>
        /// SET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task StringSetAsync<T>(this RedisDatabaseWrapper databaseEx, KeyValuePair<string, T>[] pairs)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            KeyValuePair<RedisKey, RedisValue>[] entry = new KeyValuePair<RedisKey, RedisValue>[pairs.Length];
            for (var i = 0; i < pairs.Length; i++)
            {
                entry[i] = new KeyValuePair<RedisKey, RedisValue>(pairs[i].Key, JsonSerializer.Serialize(pairs[i].Value));
            }
            try
            {
                if (transaction == null)
                    await database.StringSetAsync(entry, When.Always, CommandFlags.None);

                else
                    await transaction.StringSetAsync(entry, When.Always, CommandFlags.None);
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }

        #region Set
        /// <summary>
        /// SET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> StringIncrementAsync(this RedisDatabaseWrapper databaseEx, string key, long value)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            //await db.StringIncrementAsync(redisKey, 1); // 1씩 증가
            try
            {
                await database.StringIncrementAsync(key, value);
                await database.KeyExpireAsync(key, DateTime.UtcNow.AddDays(300));
                
                return true;
            }
            catch (RedisServerException ex)
            {
                throw new Exception("", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RedisDatabaseExtentionException: {ex.Message}");
                throw new Exception("", ex);
            }
        }
        #endregion
    }
}
#endregion