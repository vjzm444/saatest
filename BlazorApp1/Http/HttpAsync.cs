using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AppBase.Http
{
    /// <summary>
    /// HttpClient pool
    /// </summary>
    public class HttpClientPool : IDisposable
    {
        private readonly ConcurrentBag<HttpClient> _pool;

        /// <summary>
        /// 생성자, 기본값만큼 HttpClient 을 만들어둔다.
        /// </summary>
        /// <param name="poolSize"></param>
        public HttpClientPool(int poolSize)
        {
            _pool = new ConcurrentBag<HttpClient>();
            for (int i = 0; i < poolSize; i++)
            {
                _pool.Add(CreateHttpClient());
            }
        }

        /// <summary>
        /// 기존에 만들어둔 HttpClient을 가져오거나 새로 생성
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            if (_pool.TryTake(out HttpClient? client))
            {
                if (client != null)
                {
                    client.DefaultRequestHeaders.Clear();
                    return client;
                }
                else
                {
                    return CreateHttpClient();
                }
            }

            return CreateHttpClient();
        }

        /// <summary>
        /// 사용이 끝난 HttpClient 을 다음 사용을 위해 반환
        /// </summary>
        /// <param name="client"></param>
        public void ReleaseHttpClient(HttpClient client)
        {
            _pool.Add(client);
        }

        /// <summary>
        /// 모든 커넥션을 종료시킨다.
        /// </summary>
        public void Dispose()
        {
            foreach (var client in _pool)
            {
                client.Dispose();
            }
        }

        /// <summary>
        /// 새로운 HttpClient 을 만든다.
        /// </summary>
        /// <returns></returns>
        private HttpClient CreateHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler
            {
                MaxConnectionsPerServer = int.MaxValue,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

            };
            return new HttpClient(handler);
        }
    }

    /// <summary>
    /// Http 통신 helper
    /// </summary>
    [Obsolete("HttpClientTransient 사용", true)]
    public static class HttpAsync
    {
        private const int MaxTry = 3;
        private static HttpClientPool _httpClientPool = new HttpClientPool(50);

        /// <summary>
        /// Get 호출(오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
            => await GetAsync(url, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Get 호출
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> GetAsync(string url, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
            => await GetAsync(url, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Get 호출을 한다.
        /// 응답 받은 Body를 리턴
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<string> GetAsync(string url, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.GetAsync");

            HttpRequestMessage NewHttpRequestMessage()
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(contentType))
                    httpRequestMessage.Headers.Add("Accept", contentType);

                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpRequestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpRequestMessage;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var requestMessage = NewHttpRequestMessage();
                    try
                    {
                        using var result = await client.SendAsync(requestMessage, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: GET {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: GET {url}, Exception: {ex.Message}");
                    }
                    finally
                    {
                        requestMessage.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return contentBody;
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: GET {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Get 호출(오버라이드)
        ///     -return : T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
           => await GetAsync<T>(url, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Get 호출(오버라이드)
        ///     -return : T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
           => await GetAsync<T>(url, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Get 호출을 한다.
        /// 응답받은 body는 T 형태로 인스턴스화 하여 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.GetAsync<T>");

            HttpRequestMessage NewHttpRequestMessage()
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrEmpty(contentType))
                    httpRequestMessage.Headers.Add("Accept", contentType);

                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpRequestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpRequestMessage;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;
            try
            {

                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var requestMessage = NewHttpRequestMessage();
                    try
                    {
                        using var result = await client.SendAsync(requestMessage, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: GET {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: GET {url}, Exception: {ex.Message}");
                    }
                    finally
                    {
                        requestMessage.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return JsonSerializer.Deserialize<T>(contentBody);
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: GET {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
           => await PostAsync(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
           => await PostAsync(url, payload, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
            => await PostAsync(url, payload, header, contentType, 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다.
        /// 응답 받은 Body를 리턴
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync");

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payload, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            long checkpoint = 0;
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;
            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    var httpContent = NewHttpContent();
                    try
                    {
                        checkpoint = stopwatch.ElapsedMilliseconds;
                        using (var result = await client.PostAsync(url, httpContent, linkedCts.Token))
                        {
                            statusCode = result.StatusCode;
                            using (var responseStream = await result.Content.ReadAsStreamAsync())
                            {
                                using (StreamReader reader = new StreamReader(responseStream))
                                {
                                    contentBody = reader.ReadToEnd();
                                }
                            }

                            if (statusCode == HttpStatusCode.OK) break;
                            else logger?.LogWarning($"[Try: {tring}] URL: POST {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: POST {url} In '{stopwatch.ElapsedMilliseconds}' - '{checkpoint}' = '{stopwatch.ElapsedMilliseconds - checkpoint}ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        httpContent.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return contentBody;
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T?> PostAsync<T>(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
           => await PostAsync<T>(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다.
        /// 응답받은 body는 T 형태로 인스턴스화 하여 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<T?> PostAsync<T>(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync<T>");

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payload, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            long checkpoint = 0;
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var httpContent = NewHttpContent();
                    try
                    {
                        checkpoint = stopwatch.ElapsedMilliseconds;
                        using var result = await client.PostAsync(url, httpContent, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: POST {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: POST {url} In '{stopwatch.ElapsedMilliseconds}' - '{checkpoint}' = '{stopwatch.ElapsedMilliseconds - checkpoint}ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        httpContent.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return JsonSerializer.Deserialize<T>(contentBody);
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
            => await PostAsync<T, U>(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
            => await PostAsync<T, U>(url, payload, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
            => await PostAsync<T, U>(url, payload, header, contentType, 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다.
        /// T 인스턴스를 직렬화 하여 Post 전송
        /// 응답받은 body는 U 형태로 인스턴스화 하여 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestNullException"></exception>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync<T, U>");

            var payloadStr = JsonSerializer.Serialize(payload);
            if (payloadStr == null)
                throw new HttpRequestNullException();

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payloadStr, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }


            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            HttpClient client = _httpClientPool.GetHttpClient();


            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                while (tring < MaxTry)
                {
                    using CancellationTokenSource tokenSource = new CancellationTokenSource();
                    tokenSource.CancelAfter(TimeSpan.FromSeconds(10));

                    //  타임아웃과 호출자 캔슬토큰을 하나로
                    CancellationToken token = CancellationToken.None;
                    if (cancellationToken != null) token = cancellationToken.Value;
                    using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

                    using var httpContent = NewHttpContent();
                    try
                    {
                        using var result = await client.PostAsync(url, httpContent, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: POST {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");

                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning($"[Try: {tring}] URL: POST {url} In '{stopwatch.ElapsedMilliseconds}'ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return JsonSerializer.Deserialize<U>(contentBody);
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    payloadStr: {payloadStr}
                    timeout: {timeout}
                    HttpStatusCode: {statusCode}
                    contentBody: {contentBody}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }



        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
           => await PutAsync(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
           => await PutAsync(url, payload, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
            => await PutAsync(url, payload, header, contentType, 30, cancellationToken);

        /// <summary>
        /// Put 호출을 한다.
        /// 응답 받은 Body를 리턴
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync");

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payload, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            long checkpoint = 0;
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;
            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var httpContent = NewHttpContent();
                    try
                    {
                        checkpoint = stopwatch.ElapsedMilliseconds;
                        using var result = await client.PutAsync(url, httpContent, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: POST {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: POST {url} In '{stopwatch.ElapsedMilliseconds}' - '{checkpoint}' = '{stopwatch.ElapsedMilliseconds - checkpoint}ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        httpContent.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return contentBody;
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T?> PutAsync<T>(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
           => await PutAsync<T>(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Put 호출을 한다.
        /// 응답받은 body는 T 형태로 인스턴스화 하여 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<T?> PutAsync<T>(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync<T>");

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payload, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            long checkpoint = 0;
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
            var client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var httpContent = NewHttpContent();
                    try
                    {
                        checkpoint = stopwatch.ElapsedMilliseconds;
                        using var result = await client.PutAsync(url, httpContent, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: PUT {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: PUT {url} In '{stopwatch.ElapsedMilliseconds}' - '{checkpoint}' = '{stopwatch.ElapsedMilliseconds - checkpoint}ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        httpContent.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return JsonSerializer.Deserialize<T>(contentBody);
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
            => await PutAsync<T, U>(url, payload, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
            => await PutAsync<T, U>(url, payload, header, "Application/json", timeout, cancellationToken);

        /// <summary>
        /// Put 호출을 한다. (오버라이드)
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
            => await PutAsync<T, U>(url, payload, header, contentType, 30, cancellationToken);

        /// <summary>
        /// Put 호출을 한다.
        /// T 인스턴스를 직렬화 하여 Put 전송
        /// 응답받은 body는 U 형태로 인스턴스화 하여 리턴
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="contentType"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestNullException"></exception>
        /// <exception cref="HttpResponseNullException"></exception>
        /// <exception cref="HttpStatusCodeException"></exception>
        public static async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            ILogger? logger = ConfigurationHelper.GetLogger("HttpAsync.PostAsync<T, U>");

            var payloadStr = JsonSerializer.Serialize(payload);
            if (payloadStr == null)
                throw new HttpRequestNullException();

            HttpContent NewHttpContent()
            {
                HttpContent httpContent = new StringContent(payloadStr, Encoding.UTF8, contentType);
                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpContent.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                return httpContent;
            }


            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            long checkpoint = 0;
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            HttpClient client = _httpClientPool.GetHttpClient();
            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  타임아웃과 호출자 캔슬토큰을 하나로
            CancellationToken token = CancellationToken.None;
            if (cancellationToken != null) token = cancellationToken.Value;
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, token);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                while (tring < MaxTry && !linkedCts.IsCancellationRequested)
                {
                    using var httpContent = NewHttpContent();
                    try
                    {
                        checkpoint = stopwatch.ElapsedMilliseconds;
                        using var result = await client.PutAsync(url, httpContent, linkedCts.Token);
                        statusCode = result.StatusCode;
                        using var responseStream = await result.Content.ReadAsStreamAsync();
                        using StreamReader reader = new StreamReader(responseStream);
                        contentBody = reader.ReadToEnd();

                        if (statusCode == HttpStatusCode.OK) break;
                        else logger?.LogWarning($"[Try: {tring}] URL: PUT {url} [{result.StatusCode}]{Environment.NewLine}{contentBody}");

                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, $"[Try: {tring}] URL: PUT {url} In '{stopwatch.ElapsedMilliseconds}' - '{checkpoint}' = '{stopwatch.ElapsedMilliseconds - checkpoint}ms', Exception: {ex.Message}");
                    }
                    finally
                    {
                        httpContent.Dispose();
                        tring++;
                    }
                }

                if (statusCode == HttpStatusCode.OK) return JsonSerializer.Deserialize<U>(contentBody);
                else throw new Exception();
            }
            catch (Exception ex)
            {
                logger?.LogWarning($@"
                    url: {url}
                    timeout: {timeout}
                    StartTime: {dateTimeOffset}
                    NowTime: {DateTimeOffset.UtcNow}
                    ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}
                    cancellationToken: {cancellationToken}");
                throw new HttpStatusCodeException($"URL: POST {url} [{stopwatch.ElapsedMilliseconds}ms]", ex);
            }
            finally
            {
                _httpClientPool.ReleaseHttpClient(client);
            }
        }
    }
}
