using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Client.Sdk.Dotnet.support.websocket;
using Google.Protobuf;
using LiveKit.Proto;
using SIPSorcery.Net;

namespace Client.Sdk.Dotnet.Example
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            connect();
        }
        private LiveKitWebSocketIO WebSocketIO;

        private RTCPeerConnection subscriberPeerConnection;

        private RTCPeerConnection publisherPeerConnection;

        private string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NDkyOTI2NTQsImlzcyI6IkFQSUtGZ3lVVmFHOWQ2ZiIsIm5hbWUiOiJ1c2VyMSIsIm5iZiI6MTc0OTIwNjI1NCwic3ViIjoidXNlcjEiLCJ2aWRlbyI6eyJyb29tIjoibXktZmlyc3Qtcm9vbSIsInJvb21Kb2luIjp0cnVlfX0.f4CjVKTxaGeLcwTTO4wGaf5xPBGQVRp46GxvVOnHgaA";
        private async Task connect()
        {
            Uri uri = await buildUri("wss://livekit.zhiyuanchangda.xyz", token);
            WebSocketIO = await LiveKitWebSocketIO.Connect(uri, new support.WebSocketEventHandlers(
                onData: onData,
                onError: onError,
                onDispose: onDispose
                ));

        }

        private async Task<Uri> buildUri(string uriString, string token, bool reconnect = false,
            bool validate = false,
            bool forceSecure = false,
            string? sid = null)
        {
            var uri = new Uri(uriString);

            var useSecure = true;
            var httpScheme = useSecure ? "https" : "http";
            var wsScheme = useSecure ? "wss" : "ws";
            var lastSegments = validate ? new[] { "rtc", "validate" } : new[] { "rtc" };

            // 创建路径段列表
            var pathSegments = uri.Segments
                .Where(segment => !string.IsNullOrEmpty(segment) && segment != "/")
                .Select(segment => segment.TrimEnd('/'))
                .ToList();

            // 移除空路径段
            pathSegments.RemoveAll(string.IsNullOrEmpty);

            // 添加最终路径段
            pathSegments.AddRange(lastSegments);

            var clientInfo = await GetClientInfo();
            var networkType = await GetNetworkType();

            // 构建查询参数字典
            var queryParams = new Dictionary<string, string>
            {
                ["access_token"] = token,
                ["auto_subscribe"] = "0",
                ["adaptive_stream"] = "1",
                ["protocol"] = "9",
                ["sdk"] = "flutter",
                ["version"] = "2.4.7",
                ["network"] = networkType
            };

            // 重连相关参数
            if (reconnect)
            {
                queryParams["reconnect"] = "1";
                if (sid != null)
                {
                    queryParams["sid"] = sid;
                }
            }

            // 添加客户端信息
            if (clientInfo != null)
            {
                if (!string.IsNullOrEmpty(clientInfo.Os))
                    queryParams["os"] = clientInfo.Os;

                if (!string.IsNullOrEmpty(clientInfo.OsVersion))
                    queryParams["os_version"] = clientInfo.OsVersion;

                if (!string.IsNullOrEmpty(clientInfo.DeviceModel))
                    queryParams["device_model"] = clientInfo.DeviceModel;

                if (!string.IsNullOrEmpty(clientInfo.Browser))
                    queryParams["browser"] = clientInfo.Browser;

                if (!string.IsNullOrEmpty(clientInfo.BrowserVersion))
                    queryParams["browser_version"] = clientInfo.BrowserVersion;
            }

            // 构建最终 URI
            var uriBuilder = new UriBuilder
            {
                Scheme = validate ? httpScheme : wsScheme,
                Host = uri.Host,
                Port = uri.Port != -1 ? uri.Port : (useSecure ? (validate ? 443 : 443) : (validate ? 80 : 80))
            };

            // 设置路径
            uriBuilder.Path = string.Join("/", pathSegments);

            // 构建查询字符串
            uriBuilder.Query = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return uriBuilder.Uri;
        }

        private void onDispose()
        {
        }

        private void onError(object error)
        {
            throw new NotImplementedException();
        }

        private JoinResponse joinResponse;

        private void createPeerConnection()
        {
            var configuration = new SIPSorcery.Net.RTCConfiguration
            {
                iceServers = joinResponse.IceServers.Select(iceServer => new SIPSorcery.Net.RTCIceServer
                {
                    urls = iceServer.Urls.ToString(),
                }).ToList()
            };
            // 创建发布者和订阅者的 PeerConnection
            publisherPeerConnection = new RTCPeerConnection(configuration);
            subscriberPeerConnection = new RTCPeerConnection(configuration);
            // 这里可以添加更多的事件处理逻辑，例如 ICE 连接状态变化等

            var result = publisherPeerConnection.createOffer();
            publisherPeerConnection.setLocalDescription(new RTCSessionDescriptionInit
            {
                type = SIPSorcery.Net.RTCSdpType.offer,
                sdp = result.sdp
            });
            SignalRequest signalRequest = new SignalRequest();
            signalRequest.Offer = new SessionDescription
            {
                Sdp = result.sdp,
                Type = "offer"
            };
            WebSocketIO.Send(signalRequest.ToByteArray());
        }

        private async void onData(byte[] data)
        {
            SignalResponse? signalResponse = SignalResponse.Parser.ParseFrom(data);
            Debug.WriteLine($"Received signal : {signalResponse?.MessageCase}");
            switch (signalResponse.MessageCase)
            {
                case SignalResponse.MessageOneofCase.Join:
                    joinResponse = signalResponse.Join;
                    if (joinResponse != null)
                    {
                        // 创建 PeerConnection
                        createPeerConnection();
                        // 处理加入房间的响应
                    }
                    // 处理加入房间的响应
                    break;
                case SignalResponse.MessageOneofCase.Offer:
                    if (subscriberPeerConnection == null)
                    {
                        createPeerConnection();
                    }
                    var sdp = SDP.ParseSDPDescription(signalResponse.Offer.Sdp);
                    var result = subscriberPeerConnection!.SetRemoteDescription(SIPSorcery.SIP.App.SdpType.offer, sdp);
                    // 处理提供的媒体流
                    if (result != SetDescriptionResultEnum.OK)
                    {
                        var answer = subscriberPeerConnection!.createAnswer();
                        await subscriberPeerConnection.setLocalDescription(new RTCSessionDescriptionInit
                        {
                            type = SIPSorcery.Net.RTCSdpType.answer,
                            sdp = answer.sdp
                        });
                        SignalRequest signalRequest = new SignalRequest();
                        signalRequest.Answer = new SessionDescription
                        {
                            Sdp = answer.sdp,
                            Type = "answer"
                        };
                        WebSocketIO.Send(signalRequest.ToByteArray());
                    }
                    break;
                case SignalResponse.MessageOneofCase.Answer:
                    if (publisherPeerConnection == null)
                    {
                        createPeerConnection();
                    }
                    var answerSdp = SDP.ParseSDPDescription(signalResponse.Answer.Sdp);
                    var answerResult = publisherPeerConnection!.SetRemoteDescription(SIPSorcery.SIP.App.SdpType.answer, answerSdp);
                    if (answerResult != SetDescriptionResultEnum.OK)
                    {
                        Debug.WriteLine("Failed to set remote description for publisher.");
                    }
                    break;
                case SignalResponse.MessageOneofCase.RefreshToken:
                    // 处理刷新令牌的响应
                    token = signalResponse.RefreshToken;
                    break;
                case SignalResponse.MessageOneofCase.RoomUpdate:

                    break;
                case SignalResponse.MessageOneofCase.Leave:
                    Debug.WriteLine($"Received leave signal, disposing WebSocketIO.{signalResponse.Leave.ToString()}");
                    switch (signalResponse.Leave.Action)
                    {
                        case LeaveRequest.Types.Action.Disconnect:
                            Debug.WriteLine("Disconnecting from the room.");
                            await WebSocketIO.DisposeAsync();
                            break;
                        case LeaveRequest.Types.Action.Reconnect:
                            Debug.WriteLine("Reconnecting to the room.");
                            await connect();
                            break;
                        case LeaveRequest.Types.Action.Resume:
                            Debug.WriteLine("Resuming the room connection.");
                            // 这里可以添加恢复连接的逻辑
                            break;
                    }
                    await WebSocketIO.DisposeAsync();
                    break;
                case SignalResponse.MessageOneofCase.Trickle:
                    if (publisherPeerConnection == null || subscriberPeerConnection == null)
                    {
                        Debug.WriteLine("Peer connections are not initialized.");
                        return;
                    }
                    else
                    {
                        if (signalResponse.Trickle.Target == SignalTarget.Subscriber)
                        {
                            var trickleCandidate = new RTCIceCandidateInit
                            {
                                candidate = signalResponse.Trickle.CandidateInit,
                            };
                            subscriberPeerConnection.addIceCandidate(trickleCandidate);
                        }
                        else if (signalResponse.Trickle.Target == SignalTarget.Publisher)
                        {
                            var trickleCandidate = new RTCIceCandidateInit
                            {
                                candidate = signalResponse.Trickle.CandidateInit,
                            };
                            publisherPeerConnection.addIceCandidate(trickleCandidate);
                        }
                    }
                    break;
                case SignalResponse.MessageOneofCase.Pong:
                    // 处理自定义数据消息
                    break;
                case SignalResponse.MessageOneofCase.Reconnect:
                    // 处理重连请求
                    Debug.WriteLine("Received reconnect signal, attempting to reconnect...");
                    break;
            }
        }

        /// <summary>
        /// 获取设备和操作系统信息
        /// </summary>
        private static async Task<ClientInfo?> GetClientInfo()
        {
            var clientInfo = new ClientInfo { Os = RuntimeInformation.OSDescription };

            try
            {
                // 根据运行平台填充不同的信息
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    clientInfo.Os = "windows";
                    clientInfo.OsVersion = Environment.OSVersion.Version.ToString();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    clientInfo.Os = "macOS";

                    // 在实际实现中应通过平台特定API获取更详细的信息
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sw_vers",
                        Arguments = "-productVersion",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    if (process != null)
                    {
                        clientInfo.OsVersion = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    clientInfo.Os = "linux";

                    // 可以进一步检测特定Linux发行版
                    if (File.Exists("/etc/os-release"))
                    {
                        var osReleaseContent = await File.ReadAllTextAsync("/etc/os-release");
                        var versionIdMatch = Regex.Match(osReleaseContent, @"VERSION_ID=""?([^""\n]+)");
                        if (versionIdMatch.Success)
                        {
                            clientInfo.OsVersion = versionIdMatch.Groups[1].Value;
                        }
                    }
                }

                // 设备型号信息
                clientInfo.DeviceModel = RuntimeInformation.ProcessArchitecture.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取客户端信息时出错: {ex.Message}");
            }

            return clientInfo;
        }

        /// <summary>
        /// 获取当前网络连接类型
        /// </summary>
        private static async Task<string> GetNetworkType()
        {
            try
            {
                // 实际实现中应使用平台特定API检测网络状态
                // 这里是一个简化的实现

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 假设默认是有线连接，实际应通过NetworkInterface API检测
                    return "wired";
                }

                return "empty";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取网络类型时出错: {ex.Message}");
                return "empty";
            }
        }

        /// <summary>
        /// 客户端信息类
        /// </summary>
        internal class ClientInfo
        {
            public string Os { get; set; } = string.Empty;
            public string OsVersion { get; set; } = string.Empty;
            public string DeviceModel { get; set; } = string.Empty;
            public string Browser { get; set; } = string.Empty;
            public string BrowserVersion { get; set; } = string.Empty;
        }
    }
}
