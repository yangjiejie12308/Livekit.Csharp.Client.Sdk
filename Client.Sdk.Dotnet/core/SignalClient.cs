//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Client.Sdk.Dotnet.managers;
//using Client.Sdk.Dotnet.support;
//using Client.Sdk.Dotnet.types;
//using LiveKit.Proto;

//namespace Client.Sdk.Dotnet.core
//{
//    public class SignalClient : EventsEmittableBase<EngineEvent>, IDisposable
//    {
//        // 最简洁的现代 C# 写法
//        public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;

//        public WebSocketConnector? _wsConnector;

//        public LiveKitWebSocket? _ws;

//        public Queue<SignalRequest> _queue = new Queue<SignalRequest>();

//        // 心跳超时时长
//        private TimeSpan? _pingTimeoutDuration;
//        private System.Threading.Timer? _pingTimeoutTimer;

//        // 心跳间隔时长
//        private TimeSpan? _pingIntervalDuration;
//        private System.Threading.Timer? _pingIntervalTimer;

//        // Ping计数，外部只读属性
//        public int PingCount => _pingCount;

//        // 内部ping计数
//        private int _pingCount = 0;
//        // 参与者会话ID
//        public string? ParticipantSid { get; private set; }

//        private Connectivity connectivity = new Connectivity();

//        //List<ConnectivityResult> _connectivityResult = [];
//        //StreamSubscription<List<ConnectivityResult>>? _connectivitySubscription;

//        //Future<bool> networkIsAvailable() async {
//        //  // Skip check for web or flutter test
//        //  if (kIsWeb || lkPlatformIsTest()) {
//        //    return true;
//        //  }
//        //  _connectivityResult = await Connectivity().checkConnectivity();
//        //  return _connectivityResult.isNotEmpty &&
//        //      !_connectivityResult.contains(ConnectivityResult.none);
//        //}

//        public SignalClient(WebSocketConnector wsConnector)
//        {
//            _wsConnector = wsConnector;
//            _ws = null; // 初始化时WebSocket连接为null
//            Events.Listen(async (events) =>
//            {
//                Debug.WriteLine($"SignalClient received event: {events}");
//                await Task.CompletedTask; // 确保所有代码路径都返回一个 Task  
//            });
//        }


//        public void Dispose()
//        {
//            throw new NotImplementedException();
//        }

//        /// <summary>
//        /// 连接信令服务器
//        /// </summary>
//        public async Task ConnectAsync(
//            string uriString,
//            string token,
//            ConnectOptions connectOptions,
//            RoomOptions roomOptions,
//            bool reconnect = false)
//        {
//            // 检查网络连接状态（非 Web 平台）

//                var results = await connectivity.CheckConnectivity();

//            connectivity.ConnectivityChanged +=  async (sender, result) =>
//            {
//                var _connectivityResult = await connectivity.CheckConnectivity();
//                if (_connectivityResult.Count()>0)
//                {
//                    if (_connectivityResult.Contains(ConnectivityResult.None))
//                    {
//                        Debug.Assert(
//                            _connectivityResult.Count() == 1,
//                            "ConnectivityResult should only contain ConnectivityResult.None");
//                    }
//                    else
//                    {
//                        Debug.Assert(
//                            _connectivityResult.Count() == 1,
//                            "ConnectivityResult should only contain ConnectivityResult.Wifi or ConnectivityResult.Mobile");

//                    }

//                    Events.Emit(new SignalConnectivityChangedEvent(
//                        OldState: _connectivityResult,
//                        State: result));

//                    _connectivityResult = result;
//                }

//            };

//                // 如果没有网络连接，抛出异常
//                if (_connectivityResult.Contains(ConnectivityResult.None))
//                {
//                    _logger.LogWarning("no internet connection");
//                    throw new ConnectException(
//                        "no internet connection",
//                        reason: ConnectionErrorReason.InternalError,
//                        statusCode: 503);
//                }


//            // 构建 WebSocket URI
//            var rtcUri = await Utils.BuildUriAsync(
//                uriString,
//                token: token,
//                connectOptions: connectOptions,
//                roomOptions: roomOptions,
//                reconnect: reconnect,
//                sid: reconnect ? ParticipantSid : null);

//            Debug.Assert(rtcUri != null, "SignalClient connecting with url: {Url}", rtcUri);

//            try
//            {
//                // 根据是否是重连设置连接状态
//                if (reconnect)
//                {
//                    ConnectionState = ConnectionState.Reconnecting;
//                    Events.Emit(new SignalReconnectingEvent());
//                }
//                else
//                {
//                    ConnectionState = ConnectionState.Connecting;
//                    Events.Emit(new SignalConnectingEvent());
//                }

//                // 清理现有的 WebSocket 连接
//                await CleanUpAsync();

//                // 尝试连接
//                var connectTask = _wsConnector(
//                    rtcUri,
//                    new WebSocketEventHandlers(
//                        onData: _OnSocketData,
//                        onDispose: _OnSocketDispose,
//                        onError: _OnSocketError));

//                // 设置连接超时
//                var timeoutTask = Task.Delay(connectOptions.Timeouts.Connection);
//                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

//                if (completedTask == timeoutTask)
//                {
//                    throw new TimeoutException("WebSocket connection timed out");
//                }

//                _ws = await connectTask;

//                // 连接成功
//                _connectionState = ConnectionState.Connected;
//                Events.Emit(new SignalConnectedEvent());
//            }
//            catch (Exception socketError)
//            {
//                // 如果是重连模式，直接抛出异常
//                if (reconnect)
//                {
//                    throw;
//                }

//                // 尝试验证
//                var finalError = socketError;
//                try
//                {
//                    // 重建用于验证模式的相同 URI
//                    var validateUri = await Utils.BuildUriAsync(
//                        uriString,
//                        token: token,
//                        connectOptions: connectOptions,
//                        roomOptions: roomOptions,
//                        validate: true,
//                        forceSecure: rtcUri.Scheme == "wss" || rtcUri.Scheme == "https");

//                    using var httpClient = new HttpClient();
//                    var validateResponse = await httpClient.GetAsync(validateUri);

//                    if (validateResponse.StatusCode != System.Net.HttpStatusCode.OK)
//                    {
//                        var responseBody = await validateResponse.Content.ReadAsStringAsync();
//                        finalError = new ConnectException(
//                            responseBody,
//                            reason: (int)validateResponse.StatusCode >= 400
//                                ? ConnectionErrorReason.NotAllowed
//                                : ConnectionErrorReason.InternalError,
//                            statusCode: (int)validateResponse.StatusCode);
//                    }
//                }
//                catch (Exception error)
//                {
//                    if (socketError.GetType() != error.GetType())
//                    {
//                        finalError = error;
//                    }
//                }
//                finally
//                {
//                    Events.Emit(new SignalDisconnectedEvent(
//                        Reason: DisconnectReason.SignalingConnectionFailure));

//                    throw finalError;
//                }
//            }
//        }
//    }
//}
