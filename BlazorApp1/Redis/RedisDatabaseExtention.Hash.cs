using StackExchange.Redis;
using System.Text.Json;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Command
    /// </summary>
    public static partial class RedisDatabaseExtention
    {
        #region Delete
        /// <summary>
        /// HDEL
        /// 시간 복잡도 : O (N) 여기서 N은 제거 할 필드 수입니다.
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task HashDeleteAsync(this RedisDatabaseWrapper databaseEx, string key, string subkey)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            try
            {
                if (transaction == null)
                    await database.HashDeleteAsync(key, subkey, CommandFlags.None);

                else
                    await transaction.HashDeleteAsync(key, subkey, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HDEL
        /// 시간 복잡도 : O (N) 여기서 N은 제거 할 필드 수입니다.
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task HashDeleteAsync(this RedisDatabaseWrapper databaseEx, string key, string[] subkey)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            RedisValue[] entry = new RedisValue[subkey.Length];
            for (var i = 0; i < subkey.Length; i++)
            {
                entry[i] = new RedisValue(subkey[i]);
            }

            try
            {
                if (transaction == null)
                    await database.HashDeleteAsync(key, entry, CommandFlags.None);

                else
                    await transaction.HashDeleteAsync(key, entry, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }
        #endregion

        #region Get
        /// <summary>
        /// HEXISTS
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> HashExistsAsync(this RedisDatabaseWrapper databaseEx, string key, string subkey)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                bool exist = await database.HashExistsAsync(key, subkey, CommandFlags.None);
                return exist;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HGETALL
        /// 시간 복잡도 : O (N) 여기서 N은 해시 크기입니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<KeyValuePair<string, T?>[]> HashGetAllAsync<T>(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                HashEntry[] exist = await database.HashGetAllAsync(key, CommandFlags.None);
                return exist.ConvertModel<T>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HGET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?> HashGetAsync<T>(this RedisDatabaseWrapper databaseEx, string key, string subkey)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisValue redisValue = await database.HashGetAsync(key, new RedisValue(subkey), CommandFlags.None);
                return redisValue.ConvertModel<T>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HGET
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?[]> HashGetAsync<T>(this RedisDatabaseWrapper databaseEx, string key, string[] subkey)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            RedisValue[] entry = new RedisValue[subkey.Length];
            for (var i = 0; i < subkey.Length; i++)
            {
                entry[i] = new RedisValue(subkey[i]);
            }

            try
            {
                RedisValue[] redisValue = await database.HashGetAsync(key, entry, CommandFlags.None);
                return redisValue.ConvertModel<T>();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HKEYS
        /// [WARNIG] 시간 복잡도 : O (N) 여기서 N은 해시 크기입니다.
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<string[]> HashKeysAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                RedisValue[] redisValues = await database.HashKeysAsync(key, CommandFlags.None);

                string[] result = new string[redisValues.Length];
                for (var i = 0; i < redisValues.Length; i++)
                {
                    result[i] = redisValues[i].ToString();
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// 테스트 안됨.
        /// HSCAN
        /// 시간 복잡도 : 모든 통화에 대한 O (1). 커서가 0으로 되돌아 갈 수 있도록 충분한 명령 호출을 포함하여 완전한 반복을위한 O (N). N은 콜렉션 내부의 요소 수입니다
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<List<string>> HashScanAsync(this RedisDatabaseWrapper databaseEx, string key, string pattern)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                var redisValues = database.HashScanAsync(key, pattern);

                List<string> result = new();
                await foreach (var item in redisValues)
                {
                    result.Add(item.ToString());
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }
        /// <summary>
        /// HLEN
        /// 시간 복잡도 : O (1)
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<long> HashLengthAsync(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                long count = await database.HashLengthAsync(key, CommandFlags.None);
                return count;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }
        #endregion

        #region Set
        /// <summary>
        /// HSET
        /// 시간 복잡도 : 추가 된 각 필드 / 값 쌍에 대해 O (1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="subkey"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task HashSetAsync<T>(this RedisDatabaseWrapper databaseEx, string key, string subkey, T model)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            RedisValue name = subkey;
            RedisValue value = JsonSerializer.Serialize(model);

            try
            {
                if (transaction == null)
                    await database.HashSetAsync(key, name, value, When.Always, CommandFlags.None);

                else
                    await transaction.HashSetAsync(key, name, value, When.Always, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        /// <summary>
        /// HSET
        /// 시간 복잡도 : N 필드 / 값 쌍을 추가하려면 O (N).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="pairs"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task HashSetAsync<T>(this RedisDatabaseWrapper databaseEx, string key, KeyValuePair<string, T>[] pairs)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;

            HashEntry[] entry = new HashEntry[pairs.Length];
            for (var i = 0; i < pairs.Length; i++)
            {
                entry[i] = new HashEntry(pairs[i].Key, JsonSerializer.Serialize(pairs[i].Value));
            }

            try
            {
                if (transaction == null)
                    await database.HashSetAsync(key, entry, CommandFlags.None);

                else
                    await transaction.HashSetAsync(key, entry, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }
        #endregion
    }
}
