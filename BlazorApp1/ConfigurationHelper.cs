using AppBase.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AppBase
{
    /// <summary>
    /// App 전역 
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// 전역변수 컬렉션
        /// </summary>
        public class ValueCollection
        {
            readonly ConcurrentDictionary<string, object> _values = new ConcurrentDictionary<string, object>();

            /// <summary>
            /// indexer
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public object? this[string key]
            {
                get
                {
                    if (_values.TryGetValue(key, out var value)) return value;
                    else return null;
                }
                set
                {
                    if (value == null) return;
                    _values[key] = value;
                }
            }
        }
        /// <summary>
        /// 서비스 프로바이더
        /// </summary>
        public static IServiceProvider? Provider { get; set; }

        /// <summary>
        /// 개발모드?
        /// </summary>
        public static bool IsDevelopment { get; set; }
#pragma warning restore CS8618 // 생성자를 종료할 때 null을 허용하지 않는 필드에 null이 아닌 값을 포함해야 합니다. null 허용으로 선언해 보세요.
        /// <summary>
        /// App 생성 시간
        /// </summary>
        public static DateTime CreateAt { get; private set; }
        /// <summary>
        /// App 아이디
        /// </summary>
        public static string? ServerId { get; private set; }
        /// <summary>
        /// AppSetting(appsettings.json)
        /// </summary>
        public static IConfiguration? Configuration { get; private set; }
        /// <summary>
        /// 전역변수
        /// </summary>
        public static ValueCollection Collection { get; private set; } = new ValueCollection();
        /// <summary>
        /// 서버간 통신키
        /// </summary>
        public static string ServerAuthorizationToken { get; private set; } = string.Empty;
        /// <summary>
        /// AWSAccessKey
        /// </summary>
        public static string? AWSAccessKey { get; private set; }
        /// <summary>
        /// AWSSecretKey
        /// </summary>
        public static string? AWSSecretKey { get; private set; }


        /// <summary>
        /// 신규유저 유지기간
        /// </summary>
        public static int NewbieDurationDay { get; } = 14;
        /// <summary>
        /// 복귀유저 유지기간
        /// </summary>
        public static int ReturnDurationDay { get; } = 7;
        /// <summary>
        /// 비활성화된 날짜기준
        /// </summary>
        public static int DaysInactive { get; } = 30;
        /// <summary>
        /// 이벤트 출석부 종료후 유예기간
        /// </summary>
        public static int EventBoardDelayDays { get; } = 7;


        /// <summary>
        /// app id를 생성하거나 가져온다
        /// </summary>
        /// <returns></returns>
        public static async Task<string> AppID()
        {
            var serverid = Guid.NewGuid().ToString();
            if (Configuration != null)
            {
                switch (Configuration["OS"])
                {
                    case "LOCAL": break;
                    case "EC2": break;
                    case "ECS-EC2": break;
                    case "ECS-FARGATE":
                        {
                            var url = Environment.GetEnvironmentVariable("ECS_CONTAINER_METADATA_URI_V4");
                            if (url != null)
                            {
                                var httpClientTransient = Provider!.GetService<HttpClientTransient>();
                                var text = await httpClientTransient!.GetAsync(url, null, 2);
                                var metadata = Newtonsoft.Json.Linq.JObject.Parse(text);
                                if (metadata["DockerId"] != null)
                                {
                                    serverid = metadata["DockerId"]!.ToString();
                                }
                            }
                        }
                        break;

                    default: break;
                }
            }

            return serverid;
        }

        /// <summary>
        /// 로거를 생성한다.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ILogger? GetLogger(string name)
        {
            var loggerFactory = (ILoggerFactory?)Provider?.GetService(typeof(ILoggerFactory));
            return loggerFactory?.CreateLogger(name);
        }

        /// <summary>
        /// 로거를 생성한다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ILogger<T>? GetLogger<T>()
        {
            var loggerFactory = (ILoggerFactory?)Provider?.GetService(typeof(ILoggerFactory));
            return loggerFactory?.CreateLogger<T>();
        }
    }
}
