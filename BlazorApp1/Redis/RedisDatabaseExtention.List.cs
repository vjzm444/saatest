using StackExchange.Redis;
using System.Text.Json;

namespace AppBase.Redis.DataObjects
{
    /// <summary>
    /// Redis Command
    /// </summary>
    public static partial class RedisDatabaseExtention
    {
        /// <summary>
        /// 인덱스 순서의 값을 반환
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?> ListGetByIndexAsync<T>(this RedisDatabaseWrapper databaseEx, string key, long index)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            RedisValue redisValue = default;
            try
            {
                if (transaction == null)
                    redisValue = await database.ListGetByIndexAsync(key, index, CommandFlags.None);

                else
                    redisValue = await transaction.ListGetByIndexAsync(key, index, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return redisValue.ConvertModel<T>();
        }

        /// <summary>
        /// ListLeftPopAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?[]> ListLeftPopAsync<T>(this RedisDatabaseWrapper databaseEx, string key, long count)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            RedisValue[]? redisValue = default;

            try
            {
                if (transaction == null)
                    redisValue = await database.ListLeftPopAsync(key, count, CommandFlags.None);

                else
                    redisValue = await transaction.ListLeftPopAsync(key, count, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return redisValue.ConvertModel<T>();
        }

        /// <summary>
        /// ListLeftPushAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<long> ListLeftPushAsync<T>(this RedisDatabaseWrapper databaseEx, string key, T model)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            long length = 0;

            try
            {
                RedisKey redisKey = key;
                RedisValue value = JsonSerializer.Serialize(model);

                if (transaction == null)
                    length = await database.ListLeftPushAsync(redisKey, value, When.Always, CommandFlags.None);

                else
                    length = await transaction.ListLeftPushAsync(redisKey, value, When.Always, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return length;
        }

        /// <summary>
        /// ListLengthAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<long> ListLengthAsync<T>(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            long length = 0;

            try
            {
                if (transaction == null)
                    length = await database.ListLengthAsync(key, CommandFlags.None);

                else
                    length = await transaction.ListLengthAsync(key, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return length;
        }

        /// <summary>
        /// ListRangeAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?[]> ListRangeAsync<T>(this RedisDatabaseWrapper databaseEx, string key, long start = 0, long stop = -1)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            RedisValue[]? redisValues = default;
            try
            {
                if (transaction == null)
                    redisValues = await database.ListRangeAsync(key, start, stop, CommandFlags.None);
                
                else
                    redisValues = await transaction.ListRangeAsync(key, start, stop, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return redisValues.ConvertModel<T>();
        }

        /// <summary>
        /// ListRemoveAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="model"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<long> ListRemoveAsync<T>(this RedisDatabaseWrapper databaseEx, string key, T model, long count = 0)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            long result = 0;
            try
            {
                RedisValue value = JsonSerializer.Serialize(model);

                if (transaction == null)
                    result = await database.ListRemoveAsync(key, value, count, CommandFlags.None);

                else
                    result = await transaction.ListRemoveAsync(key, value, count, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return result;
        }

        /// <summary>
        /// ListRightPopAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<T?> ListRightPopAsync<T>(this RedisDatabaseWrapper databaseEx, string key)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            RedisValue redisValue = default(RedisValue);
            try
            {
                if (transaction == null)
                    redisValue = await database.ListRightPopAsync(key, CommandFlags.None);

                else
                    redisValue = await transaction.ListRightPopAsync(key, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return redisValue.ConvertModel<T>();
        }

        /// <summary>
        /// ListRightPushAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="databaseEx"></param>
        /// <param name="key"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<long> ListRightPushAsync<T>(this RedisDatabaseWrapper databaseEx, string key, T model)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            var transaction = databaseEx.Transaction;
            long length = 0;
            try
            {
                RedisValue value = JsonSerializer.Serialize(model);
                if (transaction == null)
                    length = await database.ListRightPushAsync(key, value, When.Always, CommandFlags.None);

                else
                    length = await transaction.ListRightPushAsync(key, value, When.Always, CommandFlags.None);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }

            return length;
        }
    }
}
