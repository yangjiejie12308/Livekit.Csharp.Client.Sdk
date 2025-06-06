using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.support.websocket
{
    public static class IoWebSocketProvider
    {
        public static Task<LiveKitWebSocketIO> lkWebSocketConnect(
            Uri uri,
            WebSocketEventHandlers options = null)
        {
            return LiveKitWebSocketIO.Connect(uri, options);
        }
    }

    public class LiveKitWebSocketIO : LiveKitWebSocket
    {
        private readonly ClientWebSocket _ws;
        private readonly WebSocketEventHandlers _options;
        private readonly CancellationTokenSource _receiveCts = new CancellationTokenSource();
        private Task _receiveTask;
        private bool _isReceiving = false;

        public LiveKitWebSocketIO(
            ClientWebSocket ws,
            WebSocketEventHandlers options
           )
        {
            _ws = ws;
            _options = options;

            // 开始接收消息
            StartReceiving();

            // 当对象被释放时关闭WebSocket
            OnDispose(async () =>
            {
                if (_ws.State != WebSocketState.Closed)
                {
                    try
                    {
                        await _ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Disposing",
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error closing WebSocket during dispose: {ex.Message}");
                    }
                }

                _receiveCts.Cancel();
                _receiveCts.Dispose();
            });
        }

        private void StartReceiving()
        {
            if (_isReceiving)
                return;

            _isReceiving = true;
            _receiveTask = Task.Run(ReceiveLoop);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (_ws.State == WebSocketState.Open && !_receiveCts.Token.IsCancellationRequested)
                {
                    var receiveResult = await _ws.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _receiveCts.Token);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await _ws.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Connection closed by the server",
                            CancellationToken.None);

                        _options?.OnDispose?.Invoke();
                        break;
                    }

                    if (IsDisposed)
                    {
                        Debug.WriteLine("WebSocket already disposed, ignoring received data.");
                        break;
                    }

                    // 根据消息类型处理数据
                    if (receiveResult.MessageType == WebSocketMessageType.Binary)
                    {
                        var data = new byte[receiveResult.Count];
                        Array.Copy(buffer, data, receiveResult.Count);
                        _options?.OnData?.Invoke(data);
                    }
                    else if (receiveResult.MessageType == WebSocketMessageType.Text)
                    {
                        _options?.OnData?.Invoke(buffer);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不需处理
            }
            catch (Exception ex)
            {
                _options?.OnError?.Invoke(ex);
            }
            finally
            {
                _isReceiving = false;
                _options?.OnDispose?.Invoke();
            }
        }

        public override void Send(byte[] data)
        {
            if (_ws.State != WebSocketState.Open)
            {
                Debug.WriteLine($"Socket not open (state: {_ws.State})");
                return;
            }

            try
            {
                _ws.SendAsync(
                    new ArraySegment<byte>(data),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None).Wait();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Send did throw {ex.Message}");
            }
        }

        public static async Task<LiveKitWebSocketIO> Connect(
            Uri uri,
            WebSocketEventHandlers options = null)
        {

            Debug.Assert(uri != null, "WebSocket URI cannot be null");
            try
            {
                var ws = new ClientWebSocket();
                await ws.ConnectAsync(uri, CancellationToken.None);
                Debug.Assert(ws.State == WebSocketState.Open, "WebSocket should be open after connection");
                return new LiveKitWebSocketIO(ws, options);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocket connection failed: {ex.Message}");
                throw new WebSocketException("Failed to connect", ex);
            }
        }


    }
}
