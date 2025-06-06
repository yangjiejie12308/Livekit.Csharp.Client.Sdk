using System;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.managers;
using Client.Sdk.Dotnet.support.websocket;

namespace Client.Sdk.Dotnet.support
{
    /// <summary>
    /// WebSocket 相关的异常
    /// </summary>
    public class WebSocketException : Exception
    {
        public object Error { get; }

        public WebSocketException(string message)
            : base(message)
        {
        }

        public WebSocketException(string message, object error)
            : base(message)
        {
            Error = error;
        }
    }

    /// <summary>
    /// WebSocket 数据处理委托
    /// </summary>
    public delegate void WebSocketOnData(byte[] data);

    /// <summary>
    /// WebSocket 错误处理委托
    /// </summary>
    public delegate void WebSocketOnError(object error);

    /// <summary>
    /// WebSocket 销毁处理委托
    /// </summary>
    public delegate void WebSocketOnDispose();

    /// <summary>
    /// WebSocket 事件处理器
    /// </summary>
    public class WebSocketEventHandlers
    {
        public WebSocketOnData OnData { get; }
        public WebSocketOnError OnError { get; }
        public WebSocketOnDispose OnDispose { get; }

        public WebSocketEventHandlers(
            WebSocketOnData onData = null,
            WebSocketOnError onError = null,
            WebSocketOnDispose onDispose = null)
        {
            OnData = onData;
            OnError = onError;
            OnDispose = onDispose;
        }
    }

    /// <summary>
    /// WebSocket 连接器委托
    /// </summary>
    public delegate Task<LiveKitWebSocket> WebSocketConnector(Uri uri, WebSocketEventHandlers options = null);

    /// <summary>
    /// LiveKit WebSocket 抽象基类
    /// </summary>
    public abstract class LiveKitWebSocket : Disposable
    {
        /// <summary>
        /// 发送二进制数据
        /// </summary>
        /// <param name="data">要发送的数据</param>
        public abstract void Send(byte[] data);

        /// <summary>
        /// 创建并连接到 WebSocket
        /// </summary>
        /// <param name="uri">WebSocket URI</param>
        /// <param name="options">连接选项</param>
        /// <returns>已连接的 WebSocket 实例</returns>
        public static Task<LiveKitWebSocketIO> Connect(Uri uri, WebSocketEventHandlers options = null)
        {
            return IoWebSocketProvider.lkWebSocketConnect(uri, options);
        }
    }
}
