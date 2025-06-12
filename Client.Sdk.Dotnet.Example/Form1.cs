using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using Client.Sdk.Dotnet.hardware;
using Client.Sdk.Dotnet.support.websocket;
using Google.Protobuf;
using LiveKit.Proto;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using WebSocketSharp;

namespace Client.Sdk.Dotnet.Example
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            connect();
            //HardWare hardWare = new HardWare();
            //var list = hardWare.GetAllScreen();
            //var list2 = hardWare.GetAllCamera();
        }
        private LiveKitWebSocketIO WebSocketIO;

        private RTCPeerConnection subscriberPeerConnection;

        private RTCPeerConnection publisherPeerConnection;

        private string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NDk4MDIwNjAsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIxIiwibmJmIjoxNzQ5NzE1NjYwLCJzdWIiOiJ0ZXN0X3VzZXIxIiwidmlkZW8iOnsicm9vbSI6InRlc3Rfcm9vbSIsInJvb21Kb2luIjp0cnVlfX0.QLCcOqxLVDvkhFv2Cs87_bundrojWeG6D5EBv3niI9w";
        private async Task connect()
        {
            Uri uri = await buildUri("ws://127.0.0.1:7880", token);
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

            var useSecure = false;
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
                ["auto_subscribe"] = "1",
                ["adaptive_stream"] = "1",
                ["protocol"] = "12",
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

                //if (!string.IsNullOrEmpty(clientInfo.OsVersion))
                //    queryParams["os_version"] = clientInfo.OsVersion;

                //if (!string.IsNullOrEmpty(clientInfo.DeviceModel))
                //    queryParams["device_model"] = clientInfo.DeviceModel;

                //if (!string.IsNullOrEmpty(clientInfo.Browser))
                //    queryParams["browser"] = clientInfo.Browser;

                //if (!string.IsNullOrEmpty(clientInfo.BrowserVersion))
                //    queryParams["browser_version"] = clientInfo.BrowserVersion;
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
                    urls = JsonSerializer.Serialize(iceServer.Urls),
                    username = iceServer.Username.IsNullOrEmpty() ? null : iceServer.Username,
                    credential = iceServer.Credential.IsNullOrEmpty() ? null : iceServer.Credential
                }).ToList(),
            };

            subscriberPeerConnection = new RTCPeerConnection(configuration);



            //testPatternSource.OnVideoSourceRawSample += videoEncoderEndPoint.ExternalVideoSourceRawSample;
            //videoEncoderEndPoint.OnVideoSourceEncodedSample += subscriberPeerConnection.SendVideo;
            //audioSource.OnAudioSourceEncodedSample += subscriberPeerConnection.SendAudio;

            //subscriberPeerConnection.OnVideoFormatsNegotiated += (formats) => videoEncoderEndPoint.SetVideoSourceFormat(formats.First());
            //subscriberPeerConnection.OnAudioFormatsNegotiated += (formats) => audioSource.SetAudioSourceFormat(formats.First());

            // 创建发布者和订阅者的 PeerConnection
            publisherPeerConnection = new RTCPeerConnection(configuration);

            var testPatternSource = new VideoTestPatternSource();
            var videoEncoderEndPoint = new VideoEncoderEndPoint();
            //var audioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });

            MediaStreamTrack videoTrack = new MediaStreamTrack(videoEncoderEndPoint.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
            publisherPeerConnection.addTrack(videoTrack);
            //MediaStreamTrack audioTrack = new MediaStreamTrack(audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);
            //publisherPeerConnection.addTrack(audioTrack);




            subscriberPeerConnection.onicecandidate += async (candidate) =>
            {

                var trickleCandidate = new IceCandidate
                {
                    candidate = "candidate:" + candidate.candidate,
                    sdpMid = candidate.sdpMid ?? "0",
                    sdpMLineIndex = candidate.sdpMLineIndex,
                };
                SignalRequest signalRequest = new SignalRequest
                {
                    Trickle = new TrickleRequest
                    {
                        Target = SignalTarget.Subscriber,
                        CandidateInit = JsonSerializer.Serialize(trickleCandidate),
                    },
                };

                WebSocketIO.Send(signalRequest.ToByteArray());
            };

            subscriberPeerConnection.oniceconnectionstatechange += async (state) =>
            {
                if (state == RTCIceConnectionState.connected)
                {
                    SignalRequest signalRequest = new SignalRequest();
                    AddTrackRequest addTrackRequest = new AddTrackRequest();
                    addTrackRequest.Name = "默认音频轨道名字";
                    addTrackRequest.Type = TrackType.Video;
                    addTrackRequest.Source = TrackSource.Camera;
                    addTrackRequest.Sid = joinResponse.Participant.Sid;
                    addTrackRequest.Stream = "";
                    addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
                    signalRequest.AddTrack = addTrackRequest;
                    WebSocketIO.Send(signalRequest.ToByteArray());
                    Debug.WriteLine($"SubPeer Send AddTrackRequest: {addTrackRequest.ToString()}");
                    var result2 = publisherPeerConnection.createOffer(null);
                    SignalRequest signalRequest3 = new SignalRequest();
                    signalRequest3.Offer = new SessionDescription
                    {
                        Sdp = result2.sdp,
                        Type = "offer"
                    };
                    Debug.WriteLine($"PublishPeer Send Offer SDP: {signalRequest3.Offer.Sdp}");
                    WebSocketIO.Send(signalRequest3.ToByteArray());
                    await publisherPeerConnection.setLocalDescription(result2);
                    publisherPeerConnection.restartIce();
                }
                else if (state == RTCIceConnectionState.failed)
                {
                    subscriberPeerConnection.Close("ice disconnection");
                }
                else if (state == RTCIceConnectionState.closed)
                {
                    // 连接关闭
                }
            };

            publisherPeerConnection.onicecandidate += (candidate) =>
            {
                Debug.WriteLine(publisherPeerConnection.signalingState);
                if (candidate == null || publisherPeerConnection.signalingState == RTCSignalingState.closed)
                {
                    return;
                }
                var trickleCandidate = new IceCandidate
                {
                    candidate = "candidate:" + candidate.candidate,
                    sdpMid = candidate.sdpMid ?? "0",
                    sdpMLineIndex = candidate.sdpMLineIndex,
                };
                SignalRequest signalRequest = new SignalRequest
                {
                    Trickle = new TrickleRequest
                    {
                        Target = SignalTarget.Publisher,
                        CandidateInit = JsonSerializer.Serialize(trickleCandidate)
                    },
                };
                Debug.WriteLine($"PulishbPeer Send ICE candidate: {signalRequest}");

                WebSocketIO.Send(signalRequest.ToByteArray());
            };
            publisherPeerConnection.oniceconnectionstatechange += (state) =>
            {
                Debug.WriteLine($"PublishPeer ICE connection state change to {state}.");
                if (state == RTCIceConnectionState.connected)
                {
                    // 连接成功

                }
                else if (state == RTCIceConnectionState.failed)
                {
                    publisherPeerConnection.Close("ice disconnection");
                }
                else if (state == RTCIceConnectionState.closed)
                {
                    // 连接关闭
                }
            };
        }

        private async void onData(byte[] data)
        {
            SignalResponse? signalResponse = SignalResponse.Parser.ParseFrom(data);
            Debug.WriteLine($"Received signal : {signalResponse?.MessageCase}");
            switch (signalResponse.MessageCase)
            {
                case SignalResponse.MessageOneofCase.Offer:
                    if (subscriberPeerConnection == null)
                    {
                        createPeerConnection();
                    }
                    RTCSessionDescriptionInit rTCSessionDescriptionInit = new RTCSessionDescriptionInit();
                    rTCSessionDescriptionInit.sdp = signalResponse.Offer.Sdp;
                    rTCSessionDescriptionInit.type = RTCSdpType.offer;
                    var result = subscriberPeerConnection!.setRemoteDescription(rTCSessionDescriptionInit);

                    if (result == SetDescriptionResultEnum.OK)
                    {
                        var answer = subscriberPeerConnection.createAnswer();
                        SignalRequest signalRequest = new SignalRequest();
                        signalRequest.Answer = new SessionDescription
                        {
                            Sdp = answer.sdp,
                            Type = "answer",
                        };
                        WebSocketIO.Send(signalRequest.ToByteArray());
                        await subscriberPeerConnection.setLocalDescription(answer);
                    }
                    else
                    {
                        Debug.WriteLine("Failed to set remote description for subscriber.");
                    }
                    break;
                case SignalResponse.MessageOneofCase.Answer:
                    RTCSessionDescriptionInit rTCSessionDescriptionInit2 = new RTCSessionDescriptionInit();
                    rTCSessionDescriptionInit2.sdp = signalResponse.Answer.Sdp;
                    rTCSessionDescriptionInit2.type = RTCSdpType.answer;
                    var answerResult = publisherPeerConnection!.setRemoteDescription(rTCSessionDescriptionInit2);
                    if (answerResult != SetDescriptionResultEnum.OK)
                    {
                        Debug.WriteLine("Failed to set remote description for publisher.");
                    }
                    break;
                case SignalResponse.MessageOneofCase.Join:
                    joinResponse = signalResponse.Join;
                    Debug.WriteLine(joinResponse.ToString());
                    if (joinResponse != null)
                    {
                        // 创建 PeerConnection
                        createPeerConnection();
                        // 处理加入房间的响应
                        _pingInterval = TimeSpan.FromSeconds(signalResponse.Join.PingInterval);
                        _pingTimer = new System.Timers.Timer(_pingInterval); // 30秒发送一次Ping
                        _pingTimer.Elapsed += async (sender, e) =>
                        {
                            SignalRequest pingRequest = new SignalRequest
                            {
                                Ping = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            };
                            WebSocketIO.Send(pingRequest.ToByteArray());
                        };
                        _pingTimer.Start();
                    }
                    // 处理加入房间的响应
                    break;
                case SignalResponse.MessageOneofCase.Pong:
                    _pingTimer.Start();
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
                    if (
                        //publisherPeerConnection == null ||
                        subscriberPeerConnection == null)
                    {
                        Debug.WriteLine("Peer connections are not initialized.");
                        return;
                    }
                    else
                    {
                        if (signalResponse.Trickle.Target == SignalTarget.Subscriber)
                        {
                            var jsObj = JsonSerializer.Deserialize<IceCandidate>(signalResponse.Trickle.CandidateInit);
                            var trickleCandidate = new RTCIceCandidateInit
                            {
                                candidate = jsObj.candidate.Replace("candidate:", ""),
                                sdpMid = jsObj.sdpMid,
                                sdpMLineIndex = jsObj.sdpMLineIndex
                            };
                            subscriberPeerConnection.addIceCandidate(trickleCandidate);

                            if (signalResponse.Trickle.Final)
                            {

                            }

                        }
                        else
                        {
                            Debug.WriteLine($"{signalResponse.ToString()}");
                            var jsObj = JsonSerializer.Deserialize<IceCandidate>(signalResponse.Trickle.CandidateInit);
                            var trickleCandidate = new RTCIceCandidateInit
                            {
                                candidate = jsObj.candidate.Replace("candidate:", ""),
                                sdpMid = jsObj.sdpMid ?? "0",
                                sdpMLineIndex = jsObj.sdpMLineIndex
                            };
                            publisherPeerConnection.addIceCandidate(trickleCandidate);
                        }
                    }
                    break;
                case SignalResponse.MessageOneofCase.TrackPublished:
                    HandleTrackPublishedEvent(signalResponse.TrackPublished.Cid, signalResponse.TrackPublished.Track);
                    break;
                case SignalResponse.MessageOneofCase.RefreshToken:
                    // 处理刷新令牌的响应
                    token = signalResponse.RefreshToken;
                    break;
                case SignalResponse.MessageOneofCase.RoomUpdate:
                    roomUpdate(signalResponse.RoomUpdate.Room);
                    break;
                case SignalResponse.MessageOneofCase.Update:
                    // 处理房间更新的响应

                    break;
                case SignalResponse.MessageOneofCase.TrackUnpublished:
                    HandleTrackUnPublishedEvent(signalResponse.TrackUnpublished.TrackSid);
                    break;
                case SignalResponse.MessageOneofCase.TrackSubscribed:
                    HandleTrackSubscribed(signalResponse.TrackSubscribed.TrackSid);
                    break;
                case SignalResponse.MessageOneofCase.SpeakersChanged:
                    // 处理扬声器变化的响应
                    HandleSpeakerChanged(signalResponse.SpeakersChanged.Speakers.ToList());
                    break;
                case SignalResponse.MessageOneofCase.ConnectionQuality:
                    connectionQuality(signalResponse.ConnectionQuality.Updates.ToList());
                    break;
                case SignalResponse.MessageOneofCase.Mute:
                    mute(signalResponse.Mute.Sid, signalResponse.Mute.Muted);
                    break;
                case SignalResponse.MessageOneofCase.StreamStateUpdate:
                    // 处理流状态更新的响应
                    streamStateUpdate(signalResponse.StreamStateUpdate.StreamStates.ToList());
                    break;
                case SignalResponse.MessageOneofCase.SubscribedQualityUpdate:
                    subscribedQualityUpdate(
                        signalResponse.SubscribedQualityUpdate.TrackSid,
                        signalResponse.SubscribedQualityUpdate.SubscribedQualities.ToList(),
                        signalResponse.SubscribedQualityUpdate.SubscribedCodecs.ToList());

                    // 处理订阅质量更新的响应
                    break;
                case SignalResponse.MessageOneofCase.SubscriptionPermissionUpdate:
                    subscriptionPermissionUpdate(
                        signalResponse.SubscriptionPermissionUpdate.ParticipantSid,
                        signalResponse.SubscriptionPermissionUpdate.TrackSid,
                        signalResponse.SubscriptionPermissionUpdate.Allowed);
                    break;
                case SignalResponse.MessageOneofCase.Reconnect:
                    // 处理重连请求
                    break;
            }
        }

        private System.Timers.Timer? _pingTimer;
        private TimeSpan _pingInterval;


        private void HandleTrackPublishedEvent(string cid, TrackInfo info) { }

        private void HandleTrackUnPublishedEvent(string cid) { }

        private void HandleTrackSubscribed(string trackSid) { }

        private void HandleSpeakerChanged(List<SpeakerInfo> speakers) { }

        private void roomUpdate(Room room) { }

        private void connectionQuality(List<ConnectionQualityInfo> connectionQualities) { }

        private void leave(LeaveRequest leave) { }

        private void mute(string sid, bool mute) { }

        private void streamStateUpdate(List<StreamStateInfo> streamStateInfos) { }

        private void subscribedQualityUpdate(string sid, List<SubscribedQuality> subscribedQualities, List<SubscribedCodec> subscribedCodecs) { }

        private void subscriptionPermissionUpdate(string participantSid, string trackSid, bool allowed) { }
        public class IceCandidate
        {
            public string candidate { get; set; }
            public string sdpMid { get; set; }
            public ushort sdpMLineIndex { get; set; }
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
                    return "wifi";
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
