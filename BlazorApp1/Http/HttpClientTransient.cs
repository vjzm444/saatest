using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AppBase.Http
{
    /// <summary>
    /// Http 통신 helper
    /// </summary>
    public class HttpClientTransient
    {
        /// <summary>
        /// Http default name
        /// </summary>
        public const string HttpDefaultClientName = "DefaultHttpClient";

        private readonly ILogger<HttpClientTransient> logger;
        private readonly IHttpClientFactory httpClientFactory;
        //private readonly HttpClient httpClient;
        private readonly int OneWorkTime = 10;

        /// <summary>
        /// 통신실패시 최대 재시도 횟수
        /// </summary>
        private readonly int MaxTry = 3;
        
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="_httpClientFactory"></param>
        public HttpClientTransient(ILogger<HttpClientTransient> logger, IHttpClientFactory _httpClientFactory)
        {
            this.logger = logger;
            this.httpClientFactory = _httpClientFactory;
        }

        /// <summary>
        /// Get 호출(오버라이드)
        ///     -return : string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetAsync(string url, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
            => await GetAsync(url, header, "Application/json", 30, cancellationToken);

        /// <summary>
        /// Get 호출
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> GetAsync(string url, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<string> GetAsync(string url, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var requestMessage = NewHttpRequestMessage())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.SendAsync(requestMessage, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return contentBody;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: GET {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null) 
                    throw exception;
                if (statusCode != HttpStatusCode.OK) 
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return contentBody;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Get) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Head 호출(오버라이드)
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, IEnumerable<string>>> HeadAsync(string url, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
            => await HeadAsync(url, header, 30, cancellationToken);

        /// <summary>
        /// Head 호출
        /// </summary>
        /// <param name="url"></param>
        /// <param name="header"></param>
        /// <param name="timeout"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpStatusCodeException"></exception>
        public async Task<Dictionary<string, IEnumerable<string>>> HeadAsync(string url, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
        {
            HttpRequestMessage NewHttpRequestMessage()
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, url);

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

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);
                Dictionary<string, IEnumerable<string>> responseHeaders = new Dictionary<string, IEnumerable<string>>();

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var requestMessage = NewHttpRequestMessage())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                if (result.IsSuccessStatusCode)
                                {
                                    var headers = result.Headers; 
                                    foreach (var h in headers)
                                    {
                                        responseHeaders.Add(h.Key, h.Value);
                                    }

                                    return responseHeaders;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: HEAD {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return responseHeaders;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(HEAD) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<T?> GetAsync<T>(string url, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var requestMessage = NewHttpRequestMessage())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.SendAsync(requestMessage, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) 
                                    return JsonSerializer.Deserialize<T>(contentBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: GET {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<T>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Get) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
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
        public async Task<string> PostAsync(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PostAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return contentBody;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: POST {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return contentBody;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Post) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Post 호출을 한다.
        ///     확인사항: 보낼 Contents 셋팅
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
        public async Task<T?> PostAsync<T>(string url, Dictionary<string, string> payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
            HttpRequestMessage NewHttpRequestMessage()
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                
                if (!string.IsNullOrEmpty(contentType))
                    httpRequestMessage.Headers.Add("Accept", contentType);

                if (header != null)
                {
                    foreach (var keyValuePair in header)
                        httpRequestMessage.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }

                //body값에 필요한값 셋팅
                if (payload.Any())
                    httpRequestMessage.Content = new FormUrlEncodedContent(payload);

                return httpRequestMessage;
            }

            //  HttpClient 을 새로 생성하지 않기 기존에 만들어둔것을 재사용
            Stopwatch stopwatch = Stopwatch.StartNew();
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var requestMessage = NewHttpRequestMessage())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.SendAsync(requestMessage, oneTimelinkedCts.Token)) 
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return JsonSerializer.Deserialize<T>(contentBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: POST {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<T>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Post) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<T?> PostAsync<T>(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<T?> PostAsync<T>(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PostAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return JsonSerializer.Deserialize<T>(contentBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: POST {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<T>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Post) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
            => await PostAsync<T, U>(url, payload, header, contentType, 30, cancellationToken);

        /// <summary>
        /// Post 호출을 한다. (오버라이드)
        ///     확인사항 :  contents 값을 셋팅한다.
        ///     -return : U
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="header"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<T?> PostAsync<T>(string url, Dictionary<string, string> payload, Dictionary<string, string> header, int timeout, CancellationToken? cancellationToken = null)
            => await PostAsync<T>(url, payload, header, "Application/json", timeout, cancellationToken);

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
        public async Task<U?> PostAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PostAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return JsonSerializer.Deserialize<U>(contentBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: POST {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<U>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Post) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, payloadStr : {payloadStr}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
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
        public async Task<string> PutAsync(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PutAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return contentBody;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: PUT {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return contentBody;
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Put) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<T?> PutAsync<T>(string url, string payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<T?> PutAsync<T>(string url, string payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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
            DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;


            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PutAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return JsonSerializer.Deserialize<T>(contentBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: PUT {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<T>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Put) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
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
        public async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, CancellationToken? cancellationToken = null)
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
        public async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, int timeout, CancellationToken? cancellationToken = null)
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
        public async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string contentType, CancellationToken? cancellationToken = null)
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
        public async Task<U?> PutAsync<T, U>(string url, T payload, Dictionary<string, string>? header, string? contentType, int timeout, CancellationToken? cancellationToken = null)
        {
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

            using CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(timeout));

            //  캔슬 토큰이 null일경우 처리
            CancellationToken CancelToken = CancellationToken.None;
            if (cancellationToken != null) CancelToken = cancellationToken.Value;

            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(tokenSource.Token, CancelToken);

            int tring = 0;
            HttpStatusCode? statusCode = null;
            string contentBody = string.Empty;

            try
            {
                Exception? exception = null;
                var httpClient = httpClientFactory.CreateClient(HttpDefaultClientName);

                while (!linkedCts.IsCancellationRequested && tring < MaxTry)
                {
                    using (var httpContent = NewHttpContent())
                    {
                        CancellationTokenSource oneTimetokenSource = new CancellationTokenSource();
                        oneTimetokenSource.CancelAfter(TimeSpan.FromSeconds(OneWorkTime));
                        CancellationTokenSource oneTimelinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, oneTimetokenSource.Token);

                        try
                        {
                            using (var result = await httpClient.PutAsync(url, httpContent, oneTimelinkedCts.Token))
                            {
                                statusCode = result.StatusCode;
                                using (var responseStream = await result.Content.ReadAsStreamAsync(oneTimelinkedCts.Token))
                                {
                                    using (StreamReader reader = new StreamReader(responseStream))
                                    {
                                        contentBody = reader.ReadToEnd();
                                    }
                                }

                                if (result.IsSuccessStatusCode) return JsonSerializer.Deserialize<U>(contentBody);
                            }

                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, $"[Try: {tring} in {stopwatch.ElapsedMilliseconds}ms] URL: PUT {url} {Environment.NewLine}{ex.Message}");
                            exception = ex;
                        }
                        finally
                        {
                            tring++;
                            oneTimetokenSource.Dispose();
                            oneTimelinkedCts.Dispose();
                        }
                    }
                }

                if (exception != null)
                    throw exception;
                if (statusCode != HttpStatusCode.OK)
                    throw new HttpStatusCodeException($"Http Status Code :: {statusCode}");

                return JsonSerializer.Deserialize<U>(contentBody);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"(Put) Url: {url},StatusCode: {statusCode}, StartTime: {dateTimeOffset}, NowTime: {DateTimeOffset.UtcNow}, ElapsedMilliseconds: {stopwatch.ElapsedMilliseconds}, CancellationToken: {cancellationToken?.IsCancellationRequested}, body: {contentBody}, exMessage :: {ex.Message}");
                throw;
            }
        }
    }
}
