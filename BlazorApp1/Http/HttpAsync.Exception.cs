using System.Runtime.Serialization;

namespace AppBase.Http
{
    /// <summary>
    /// HTTP STATUS 코드가 200 OK 가 아닐때 발생
    /// </summary>
    [Serializable]
    public class HttpStatusCodeException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpStatusCodeException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public HttpStatusCodeException(string? message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public HttpStatusCodeException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected HttpStatusCodeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// HTTP 요청 payload 가 null 일때 발생
    /// </summary>
    [Serializable]
    public class HttpRequestNullException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpRequestNullException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public HttpRequestNullException(string? message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public HttpRequestNullException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected HttpRequestNullException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// HTTP 응답이 null 일떄 발생
    /// </summary>
    [Serializable]
    public class HttpResponseNullException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public HttpResponseNullException() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public HttpResponseNullException(string? message) : base(message) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public HttpResponseNullException(string? message, Exception? innerException) : base(message, innerException) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected HttpResponseNullException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}