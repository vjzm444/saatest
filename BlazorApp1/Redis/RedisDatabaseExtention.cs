using StackExchange.Redis;
using System.Text.Json;

namespace AppBase.Redis
{
    /// <summary>
    /// Redis Command
    /// </summary>
    public static partial class RedisDatabaseExtention
    {
        /// <summary>
        /// Lock 설정
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <param name="lockname"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> TakeLockAsync(this RedisDatabaseWrapper databaseEx, string lockname, TimeSpan? timeout = null)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            RedisKey lockKey = $"{lockname}_lock";
            string lockToken = Guid.NewGuid().ToString();

            if (timeout == null)
                timeout = TimeSpan.FromSeconds(10);

            try
            {
                int retry = 0;
                while (!await database.LockTakeAsync(lockKey, lockToken, timeout.Value))
                {
                    await Task.Delay(10);

                    if (retry++ > 20)
                        throw new Exception($"Cannot Take Lock: Retry {retry}");
                }

                databaseEx.LockName = lockKey;
                databaseEx.LockToken = lockToken;

                return true;
            }
            catch (Exception e)
            {
                throw new Exception($"Can't Take Lock: {lockname}", e);
            }
        }

        /// <summary>
        /// Lock 해제
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<bool> ReleaseLockAsync(this RedisDatabaseWrapper databaseEx)
        {
            var database = databaseEx.Database;
            var lockName = databaseEx.LockName;
            var lockToken = databaseEx.LockToken;

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (string.IsNullOrEmpty(lockName))
                throw new ArgumentNullException(nameof(lockName));

            if (string.IsNullOrEmpty(lockToken))
                throw new ArgumentNullException(nameof(lockToken));

            try
            {
                var result = await database.LockReleaseAsync(lockName, lockToken);
                if (result)
                {
                    databaseEx.LockName = String.Empty;
                    databaseEx.LockToken = String.Empty;
                }

                return result;
            }
            catch (Exception e)
            {
                throw new Exception($"Can't Release Lock: {lockName} With {lockToken}", e);
            }
        }

        /// <summary>
        /// 트렌젝션 설정
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static void CreateTransaction(this RedisDatabaseWrapper databaseEx)
        {
            var database = databaseEx.Database;
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            try
            {
                databaseEx.Transaction = database.CreateTransaction();
            }
            catch (Exception e)
            {
                throw new Exception($"Cannot Create Transaction: {e.ToString()}");
            }
        }

        /// <summary>
        /// 트렌젝션 실행
        /// </summary>
        /// <param name="databaseEx"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task ExecuteTransactionAsync(this RedisDatabaseWrapper databaseEx)
        {
            var database = databaseEx.Database;
            var transaction = databaseEx.Transaction;

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            try
            {
                bool result = await transaction.ExecuteAsync();
                databaseEx.Transaction = null;
            }
            catch (Exception e)
            {
                throw new Exception($"Cannot Execute Transaction: {e.ToString()}");
            }
        }

        /// <summary>
        /// RedisValue 를 T 인스턴스화
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public static T? ConvertModel<T>(this RedisValue redisValue)
        {
            if (redisValue.IsNull)
                return default(T);

            return JsonSerializer.Deserialize<T>(redisValue.ToString());
        }

        /// <summary>
        /// 
        /// RedisValue[] 를 T[] 인스턴스화
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisValue"></param>
        /// <returns></returns>
        public static T?[] ConvertModel<T>(this RedisValue[] redisValue)
        {
            T?[] stringValues = new T?[redisValue.Length];
            for (int i = 0; i < redisValue.Length; i++)
            {
                if (redisValue[i].IsNull)
                    stringValues[i] = default;

                else
                    stringValues[i] = JsonSerializer.Deserialize<T>(redisValue[i].ToString());
            }

            return stringValues;
        }

        /// <summary>
        /// HashEntry[] 를 KeyValuePair로 변환
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static KeyValuePair<string, T?>[] ConvertModel<T>(this HashEntry[] entry)
        {
            KeyValuePair<string, T?>[] values = new KeyValuePair<string, T?>[entry.Length];
            for (int i = 0; i < entry.Length; i++)
            {
                KeyValuePair<string, T?> keyValue;
                try
                {
                    keyValue = new KeyValuePair<string, T?>(entry[i].Name.ToString(), JsonSerializer.Deserialize<T>(entry[i].Value.ToString()));
                }
                catch
                {
                    keyValue = new KeyValuePair<string, T?>(entry[i].Name.ToString(), default);
                }
                values[i] = keyValue;
            }

            return values;
        }
    }
}
