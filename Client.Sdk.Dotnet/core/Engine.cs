using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.Internal;
using Client.Sdk.Dotnet.managers;
using Client.Sdk.Dotnet.support;
using Client.Sdk.Dotnet.support.websocket;
using Google.Protobuf;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;

namespace Client.Sdk.Dotnet.core
{
    public class Engine : IDisposable
    {
        public PeerConnection? subscriberPeerConnection;

        private PeerConnection? publisherPeerConnection;

        protected JoinResponse? joinResponse;

        private System.Timers.Timer? _pingTimer;

        private TimeSpan _pingInterval;

        public Dictionary<string, List<SubscribedCodec>> TrackSubscribedCodecs = new Dictionary<string, List<SubscribedCodec>>();

        public event EventHandler<string> TrackSubscribedCodecsUpdated;

        private void UpdateTrackSubscribedCodecs(string trackSid, List<SubscribedCodec> subscribedCodecs)
        {
            if (TrackSubscribedCodecs.ContainsKey(trackSid))
            {
                TrackSubscribedCodecs[trackSid] = subscribedCodecs;
            }
            else
            {
                TrackSubscribedCodecs.Add(trackSid, subscribedCodecs);
            }

            if (TrackSubscribedCodecsUpdated != null)
            {
                TrackSubscribedCodecsUpdated?.Invoke(this, trackSid);
            }
        }

        public List<SubscribedCodec> GetTrackSubscribedCodecs(string trackSid)
        {
            if (TrackSubscribedCodecs.TryGetValue(trackSid, out var subscribedCodecs))
            {
                return subscribedCodecs;
            }
            return new List<SubscribedCodec> { };
        }

        public event EventHandler<string> TrackSubscribedQualitysUpdated;

        public Dictionary<string, List<SubscribedQuality>> TrackSubscribedQualitys = new Dictionary<string, List<SubscribedQuality>>();

        private void UpdateTrackSubscribedQualitys(string trackSid, List<SubscribedQuality> subscribedQualities)
        {
            if (TrackSubscribedQualitys.ContainsKey(trackSid))
            {
                TrackSubscribedQualitys[trackSid] = subscribedQualities;
            }
            else
            {
                TrackSubscribedQualitys.Add(trackSid, subscribedQualities);
            }
            if (TrackSubscribedQualitysUpdated != null)
                TrackSubscribedQualitysUpdated?.Invoke(this, trackSid);
        }

        public List<SubscribedQuality> GetTrackSubscribedQualitys(string trackSid)
        {
            if (TrackSubscribedQualitys.TryGetValue(trackSid, out var subscribedQualities))
            {
                return subscribedQualities;
            }
            return new List<SubscribedQuality> { };
        }


        public Dictionary<string, ConnectionQualityInfo> ParticipantConnectionQuality = new Dictionary<string, ConnectionQualityInfo>();

        public event EventHandler<string> onParticipantConnectionQualityUpdated;
        private void UpdateParticipantConnectionQuality(ConnectionQualityInfo connectionQualityInfo)
        {
            if (ParticipantConnectionQuality.ContainsKey(connectionQualityInfo.ParticipantSid))
            {
                ParticipantConnectionQuality[connectionQualityInfo.ParticipantSid] = connectionQualityInfo;
            }
            else
            {
                ParticipantConnectionQuality.Add(connectionQualityInfo.ParticipantSid, connectionQualityInfo);
            }
            if (onParticipantConnectionQualityUpdated != null)
                onParticipantConnectionQualityUpdated?.Invoke(this, connectionQualityInfo.ParticipantSid);
        }

        public ConnectionQualityInfo GetParticipantConnectionQuality(string sid)
        {
            if (ParticipantConnectionQuality.TryGetValue(sid, out var connectionQualityInfo))
            {
                return connectionQualityInfo;
            }
            return new ConnectionQualityInfo { ParticipantSid = sid, Quality = ConnectionQuality.Good };

        }

        public LiveKit.Proto.Room room { get; set; }
        public event EventHandler<LiveKit.Proto.Room> LiveKitRoomUpdated;
        private void UpdateRoom(LiveKit.Proto.Room room)
        {
            this.room = room;
            if (LiveKitRoomUpdated != null)
                LiveKitRoomUpdated?.Invoke(this, room);
        }

        public ObservableCollection<ParticipantInfo> RemoteParticipants { get; set; } = new ObservableCollection<ParticipantInfo>();

        public event EventHandler<ParticipantInfo> RemoteParticipantUpdated;
        private void UpdateRemoteParticipants(List<ParticipantInfo> participantInfos)
        {
            foreach (var item in participantInfos)
            {
                if (item.Sid != LocalParticipant.Sid)
                {
                    if (RemoteParticipants.Any(v => v.Identity == item.Identity))
                    {
                        int index = RemoteParticipants.IndexOf(RemoteParticipants.FirstOrDefault(v => v.Identity == item.Identity));

                        foreach (var item1 in RemoteParticipants[index].Tracks)
                        {
                            if (item.Tracks.Any(v => v.Sid == item1.Sid))
                            {
                                var track = item.Tracks.FirstOrDefault(v => v.Sid == item1.Sid);
                                if (track.Muted && !item1.Muted)
                                {
                                    Debug.WriteLine($"Track {track.Sid} muted state changed from {item1.Muted} to {track.Muted} for participant {RemoteParticipants[index].Sid}");
                                    // mute
                                    switch (track.Type)
                                    {
                                        case TrackType.Audio:
                                            if (onAudioTrackMuted != null)
                                                onAudioTrackMuted.Invoke(this, (RemoteParticipants[index].Sid, track.Sid));
                                            break;
                                        case TrackType.Video:
                                            if (onVideoTrackMuted != null)
                                                onVideoTrackMuted.Invoke(this, (RemoteParticipants[index].Sid, track.Sid));
                                            break;
                                    }
                                }
                                else if (!track.Muted && item1.Muted)
                                {
                                    Debug.WriteLine($"Track {track.Sid} muted state changed from {item1.Muted} to {track.Muted} for participant {RemoteParticipants[index].Sid}");
                                    // unmute
                                    switch (track.Type)
                                    {
                                        case TrackType.Audio:
                                            if (onAudioTrackUnMuted != null)
                                                onAudioTrackUnMuted.Invoke(this, (RemoteParticipants[index].Sid, track.Sid));
                                            break;
                                        case TrackType.Video:
                                            if (onVideoTrackUnMuted != null)
                                                onVideoTrackUnMuted.Invoke(this, (RemoteParticipants[index].Sid, track.Sid));
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                // 取消了流发布
                                switch (item1.Type)
                                {
                                    case TrackType.Video:
                                        Debug.WriteLine($"Video track removed: {item1.Sid} from participant {RemoteParticipants[index].Sid}");
                                        if (onVideoTrackRemoved != null) onVideoTrackRemoved?.Invoke(this, (RemoteParticipants[index].Sid, item1.Sid));
                                        break;
                                    case TrackType.Audio:
                                        Debug.WriteLine($"Audio track removed: {item1.Sid} from participant {RemoteParticipants[index].Sid}");
                                        if (onAudioTrackRemoved != null) onAudioTrackRemoved?.Invoke(this, (RemoteParticipants[index].Sid, item1.Sid));
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        RemoteParticipants[index] = item;

                    }
                    else
                    {
                        RemoteParticipants.Add(item);
                    }
                    if (RemoteParticipantUpdated != null)
                    {
                        RemoteParticipantUpdated?.Invoke(this, item);
                    }
                }
                else
                {
                    UpdateLocalParticipant(item);
                }
            }
        }

        public ParticipantInfo LocalParticipant { get; set; }

        public event EventHandler<ParticipantInfo> LocalParticipantUpdated;

        private void UpdateLocalParticipant(ParticipantInfo participantInfo)
        {
            if (LocalParticipant == null || LocalParticipant.Identity != participantInfo.Identity)
            {
                LocalParticipant = participantInfo;
            }
            else
            {
                LocalParticipant = participantInfo;
            }
            if (LocalParticipantUpdated != null)
            {
                LocalParticipantUpdated?.Invoke(this, LocalParticipant);
            }
        }


        private LiveKitWebSocketIO WebSocketIO;

        private PeerConnectionConfiguration configuration;

        public string url;

        public string token;

        public Engine(string url, string token)
        {
            this.url = url;
            this.token = token;
        }

        public void Dispose()
        {
        }

        public async Task ConnectAsync()
        {
            Uri uri = await GetBuildUri(url, token);
            WebSocketIO = await LiveKitWebSocketIO.Connect(uri, new WebSocketEventHandlers(
                onData: onData,
                onError: onError,
                onDispose: onDispose
                ));
        }

        protected void createIceServer()
        {
            configuration = new PeerConnectionConfiguration
            {
                IceServers = joinResponse.IceServers.Select(iceServer => new IceServer
                {
                    Urls = iceServer.Urls.ToList(),
                    TurnUserName = iceServer.Username.IsNullOrEmpty() ? null : iceServer.Username,
                    TurnPassword = iceServer.Credential.IsNullOrEmpty() ? null : iceServer.Credential
                }).ToList(),
            };

            _pingInterval = TimeSpan.FromSeconds(joinResponse.PingInterval);
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

        public AudioTrackSource microphoneSource = null;
        public VideoTrackSource webcamSource = null;
        public Transceiver audioTransceiver = null;
        public Transceiver videoTransceiver = null;
        public LocalAudioTrack localAudioTrack = null;
        public LocalVideoTrack localVideoTrack = null;

        public void Subscribe(string trackId, VideoQuality Quality)
        {
            SignalRequest signalRequest = new SignalRequest
            {
                TrackSetting = new UpdateTrackSettings
                {
                    TrackSids = { trackId },
                    Quality = Quality,
                    Disabled = false
                }
            };
            WebSocketIO.Send(signalRequest.ToByteArray());
        }

        public async Task createPublisherPeerConnection()
        {

            publisherPeerConnection = new PeerConnection();
            publisherPeerConnection.Connected += async () =>
            {
                Debug.WriteLine($"publisherPeerConnection: connected");
            };
            publisherPeerConnection.IceCandidateReadytoSend += (candidate) =>
            {

                SignalRequest signalRequest = new SignalRequest
                {
                    Trickle = new TrickleRequest
                    {
                        Target = SignalTarget.Publisher,
                        CandidateInit = JsonSerializer.Serialize(candidate)
                    },
                };
                Debug.WriteLine($"PulishbPeer Send ICE candidate: {signalRequest}");

                WebSocketIO.Send(signalRequest.ToByteArray());
            };
            publisherPeerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Console.WriteLine($"ICE state: {newState}");
            };
            publisherPeerConnection.LocalSdpReadytoSend += (peer) =>
            {
                SignalRequest signalRequest3 = new SignalRequest();
                signalRequest3.Offer = new SessionDescription
                {
                    Sdp = peer.Content,
                    Type = "offer"
                };
                WebSocketIO.Send(signalRequest3.ToByteArray());
                Debug.WriteLine($"subscriberPeerConnection: connected");
            };
            await publisherPeerConnection.InitializeAsync(configuration);
        }

        public async Task OpenAudio()
        {
            if (publisherPeerConnection == null)
            {
                await createPublisherPeerConnection();
            }

            microphoneSource = await DeviceAudioTrackSource.CreateAsync();
            var audioTrackConfig = new LocalAudioTrackInitConfig
            {
            };
            localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);
            audioTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Audio);
            audioTransceiver.LocalAudioTrack = localAudioTrack;
            audioTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            localAudioTrack.Name = Guid.NewGuid().ToString();

            SignalRequest signalRequest = new SignalRequest();
            AddTrackRequest addTrackRequest = new AddTrackRequest();
            addTrackRequest.Cid = localAudioTrack.Name ?? "default-video-cid";
            addTrackRequest.Name = microphoneSource.Name ?? "microphone";
            addTrackRequest.Type = TrackType.Audio;
            addTrackRequest.Source = TrackSource.Microphone;
            addTrackRequest.DisableRed = false;
            addTrackRequest.Stream = $"{localAudioTrack.Name}_screenshare_audio";
            addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
            signalRequest.AddTrack = addTrackRequest;
            WebSocketIO.Send(signalRequest.ToByteArray());

            var result2 = publisherPeerConnection.CreateOffer();
        }

        public async Task OpenVideo()
        {
            if (publisherPeerConnection == null)
            {
                await createPublisherPeerConnection();
            }

            webcamSource = await DeviceVideoTrackSource.CreateAsync();
            var videoTrackConfig = new LocalVideoTrackInitConfig
            {
            };
            localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);
            videoTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Video);
            videoTransceiver.LocalVideoTrack = localVideoTrack;
            videoTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            localVideoTrack.Name = Guid.NewGuid().ToString();

            SignalRequest signalRequest = new SignalRequest();
            AddTrackRequest addTrackRequest = new AddTrackRequest();
            addTrackRequest.Cid = localVideoTrack.Name ?? "default-video-cid";
            addTrackRequest.Name = webcamSource.Name ?? "camera";
            addTrackRequest.Type = TrackType.Video;
            addTrackRequest.Source = TrackSource.Camera;
            addTrackRequest.DisableRed = false;
            addTrackRequest.Stream = $"{localVideoTrack.Name}_screenshare_video";
            addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
            signalRequest.AddTrack = addTrackRequest;
            WebSocketIO.Send(signalRequest.ToByteArray());

            var result2 = publisherPeerConnection.CreateOffer();
        }

        public async Task OpenScreenShare()
        {
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, @"E:\TDG\client-sdk-dotnet\Client.Sdk.Dotnet\dll");
            //if (publisherPeerConnection == null)
            //{
            //    await createPublisherPeerConnection();
            //}

            //webcamSource = await DeviceVideoTrackSource.CreateAsync();
            //var videoTrackConfig = new LocalVideoTrackInitConfig
            //{
            //};
            //localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);
            //videoTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Video);
            //videoTransceiver.LocalVideoTrack = localVideoTrack;
            //videoTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            //localVideoTrack.Name = Guid.NewGuid().ToString();

            //List<SIPSorceryMedia.FFmpeg.Monitor>? monitors = FFmpegMonitorManager.GetMonitorDevices();
            //FFmpegScreenSource videoSource = new FFmpegScreenSource(monitors[0].Path, monitors[0].Rect, 10);

            //VideoTrackSource videoTrackSource = new VideoTrackSource(videoSource);

            //localVideoTrack = LocalVideoTrack.CreateFromSource(videoTrackSource, new LocalVideoTrackInitConfig());
            //videoTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Video);
            //videoTransceiver.DesiredDirection = Transceiver.Direction.SendOnly;
            //localVideoTrack.Name = Guid.NewGuid().ToString();


            //SignalRequest signalRequest = new SignalRequest();
            //AddTrackRequest addTrackRequest = new AddTrackRequest();
            //addTrackRequest.Cid = localVideoTrack.Name ?? "default-video-cid";
            //addTrackRequest.Name = webcamSource.Name ?? "camera";
            //addTrackRequest.Type = TrackType.Video;
            //addTrackRequest.Source = TrackSource.Camera;
            //addTrackRequest.DisableRed = false;
            //addTrackRequest.Stream = $"{localAudioTrack.Name}_screenshare_video";
            //addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
            //signalRequest.AddTrack = addTrackRequest;
            //WebSocketIO.Send(signalRequest.ToByteArray());

            //var result2 = publisherPeerConnection.CreateOffer();
        }

        private async Task createSubPeerConnection()
        {

            subscriberPeerConnection = new PeerConnection();
            subscriberPeerConnection.Connected += () =>
            {
                // 连接成功
                Debug.WriteLine($"subscriberPeerConnection: connected");
            };
            subscriberPeerConnection.IceCandidateReadytoSend += (candidate) =>
            {
                if (candidate == null) return;

                Internal.IceCandidate iceCandidate = new Internal.IceCandidate();
                iceCandidate.candidate = candidate.Content;
                iceCandidate.sdpMid = candidate.SdpMid ?? "0";
                iceCandidate.sdpMLineIndex = candidate.SdpMlineIndex;

                SignalRequest signalRequest = new SignalRequest
                {
                    Trickle = new TrickleRequest
                    {
                        Target = SignalTarget.Subscriber,
                        CandidateInit = JsonSerializer.Serialize(iceCandidate),
                    },
                };

                WebSocketIO.Send(signalRequest.ToByteArray());
            };
            subscriberPeerConnection.LocalSdpReadytoSend += (sdps) =>
            {
                Debug.WriteLine($"local_sdp:{sdps.Type}");
                SignalRequest signalRequest = new SignalRequest();
                signalRequest.Answer = new SessionDescription
                {
                    Sdp = sdps.Content,
                    Type = sdps.Type == SdpMessageType.Answer ? "answer" : "offer",
                };
                WebSocketIO.Send(signalRequest.ToByteArray());
                //subscriberPeerConnection.res();
            };
            subscriberPeerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Console.WriteLine($"ICE state: {newState}");
            };
            subscriberPeerConnection.VideoTrackAdded += (track) =>
            {
                VideoTrackAdded(track.Name, track);
            };
            subscriberPeerConnection.VideoTrackRemoved += (t, track) =>
            {
                VideoTrackRemoved(track.Name, track);
            };
            subscriberPeerConnection.AudioTrackAdded += (
                track) =>
            {
                Debug.WriteLine($"Audio track added: {track.Name}");
                if (usedTrackName.Contains(track.Name))
                {
                    track.SetName("readyToSet");
                }
            };

            subscriberPeerConnection.AudioTrackRemoved += (t, track) =>
            {
                Debug.WriteLine($"Audio track removed: {track.Name}");
                usedTrackName.Add(track.Name);

            };

            await subscriberPeerConnection.InitializeAsync(configuration);
            Transceiver videoTransceiver = subscriberPeerConnection.AddTransceiver(MediaKind.Video);
            //videoTransceiver.LocalVideoTrack = localVideoTrack;
            videoTransceiver.DesiredDirection = Transceiver.Direction.ReceiveOnly;
            Transceiver audioTransceiver = subscriberPeerConnection.AddTransceiver(MediaKind.Audio);
            //audioTransceiver.LocalAudioTrack = localAudioTrack;
            audioTransceiver.DesiredDirection = Transceiver.Direction.ReceiveOnly;

        }

        private async Task<Uri> GetBuildUri(string uriString, string token, bool reconnect = false,
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

            var clientInfo = await Device.GetClientInfo();
            var networkType = await Device.GetNetworkType();

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


        private async void onData(byte[] data)
        {
            SignalResponse? signalResponse = SignalResponse.Parser.ParseFrom(data);
            switch (signalResponse.MessageCase)
            {
                case SignalResponse.MessageOneofCase.Join:
                    onJoinResponse(signalResponse);
                    // 处理加入房间的响应
                    break;
                case SignalResponse.MessageOneofCase.Offer:
                    onAcceptOffer(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.Answer:
                    onReceiveAnswer(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.Pong:
                    _pingTimer?.Start();
                    break;
                case SignalResponse.MessageOneofCase.Leave:
                    await onLeave(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.Trickle:
                    onTrickle(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.TrackPublished:
                    onTrackPublished(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.RefreshToken:
                    // 处理刷新令牌的响应
                    token = signalResponse.RefreshToken;
                    break;
                case SignalResponse.MessageOneofCase.RoomUpdate:
                    onRoomUpdate(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.Update:
                    onUpdate(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.TrackUnpublished:
                    onTrackUnpublished(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.TrackSubscribed:
                    onTrackSubscribed(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.SpeakersChanged:
                    // 处理扬声器变化的响应
                    onSpeakersChanged(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.ConnectionQuality:
                    onConnectionQuality(signalResponse);
                    break;
                case SignalResponse.MessageOneofCase.Mute:
                    onMute(signalResponse.Mute.Sid, signalResponse.Mute.Muted);
                    break;
                case SignalResponse.MessageOneofCase.StreamStateUpdate:
                    // 处理流状态更新的响应
                    onStreamStateUpdate(signalResponse.StreamStateUpdate.StreamStates.ToList());
                    break;
                case SignalResponse.MessageOneofCase.SubscribedQualityUpdate:
                    onSubscribedQualityUpdate(
                        signalResponse.SubscribedQualityUpdate.TrackSid,
                        signalResponse.SubscribedQualityUpdate.SubscribedQualities.ToList(),
                        signalResponse.SubscribedQualityUpdate.SubscribedCodecs.ToList());
                    // 处理订阅质量更新的响应
                    break;
                case SignalResponse.MessageOneofCase.SubscriptionPermissionUpdate:
                    onSubscriptionPermissionUpdate(
                        signalResponse.SubscriptionPermissionUpdate.ParticipantSid,
                        signalResponse.SubscriptionPermissionUpdate.TrackSid,
                        signalResponse.SubscriptionPermissionUpdate.Allowed);
                    break;
                case SignalResponse.MessageOneofCase.Reconnect:
                    // 处理重连请求
                    onReconnect(signalResponse);
                    break;
            }
        }

        private void onReconnect(SignalResponse signalResponse)
        {
        }

        public event EventHandler<(string, string, string)> onSubscriptionPermissionUpdated;
        private void onSubscriptionPermissionUpdate(string participantSid, string trackSid, bool allowed)
        {
            //Debug.WriteLine($"Subscription permission update for participant {participantSid} on track {trackSid} is now {(allowed ? "allowed" : "denied")}.");
            if (onSubscriptionPermissionUpdated != null)
            {
                onSubscriptionPermissionUpdated?.Invoke(this, (participantSid, trackSid, allowed ? "allowed" : "denied"));
            }
        }

        private void onSubscribedQualityUpdate(string trackSid, List<SubscribedQuality> subscribedQualities, List<SubscribedCodec> subscribedCodecs)
        {
            //Debug.WriteLine($"Subscribed quality update for track {sid} with qualities: {string.Join(", ", subscribedQualities)} and codecs: {string.Join(", ", subscribedCodecs)}");
            UpdateTrackSubscribedQualitys(trackSid, subscribedQualities);
            UpdateTrackSubscribedCodecs(trackSid, subscribedCodecs);
        }

        public event EventHandler<(string, string)> onVideoTrackAdded;
        public event EventHandler<(string, string)> onVideoTrackRemoved;
        public event EventHandler<(string, string)> onAudioTrackAdded;
        public event EventHandler<(string, string)> onAudioTrackRemoved;
        public event EventHandler<(string, string)> onVideoTrackMuted;
        public event EventHandler<(string, string)> onAudioTrackMuted;
        public event EventHandler<(string, string)> onVideoTrackUnMuted;
        public event EventHandler<(string, string)> onAudioTrackUnMuted;

        private List<string> usedTrackName = new List<string>();

        private void VideoTrackAdded(string trackId, RemoteVideoTrack videoTrack)
        {
            Debug.WriteLine($"Video track added: {trackId}");
            if (usedTrackName.Contains(trackId))
            {
                videoTrack.SetName("readyToSet");
            }
        }

        private void VideoTrackRemoved(string trackId, RemoteVideoTrack videoTrack)
        {
            Debug.WriteLine($"Video track removed: {trackId}");
            usedTrackName.Add(trackId);

        }

        public RemoteVideoTrack? GetTrackStream(string trackId)
        {
            return subscriberPeerConnection.RemoteVideoTracks.FirstOrDefault(v => v.Name == trackId);
        }

        public RemoteAudioTrack? GetAudioTrackStream(string trackId)
        {
            return subscriberPeerConnection.RemoteAudioTracks.FirstOrDefault(v => v.Name == trackId);
        }
        private void onStreamStateUpdate(List<StreamStateInfo> streamStateInfos)
        {
            foreach (var streamState in streamStateInfos)
            {
                Debug.WriteLine($"Stream state updated for participant {streamState.ToString()}");

                if (streamState.State == StreamState.Active)
                {
                    if (RemoteParticipants.FirstOrDefault(v => v.Sid == streamState.ParticipantSid).Tracks.FirstOrDefault(v => v.Sid == streamState.TrackSid).Type == TrackType.Video)
                    {
                        if (subscriberPeerConnection.RemoteVideoTracks.Count(v => v.Name == streamState.TrackSid) == 0)
                        {
                            subscriberPeerConnection.RemoteVideoTracks.FirstOrDefault(v => v.Name == "readyToSet")!.Name = streamState.TrackSid;
                        }

                        if (onVideoTrackAdded != null)
                        {
                            onVideoTrackAdded.Invoke(this, (streamState.ParticipantSid, streamState.TrackSid));
                        }
                    }
                    else if (RemoteParticipants.FirstOrDefault(v => v.Sid == streamState.ParticipantSid).Tracks.FirstOrDefault(v => v.Sid == streamState.TrackSid).Type == TrackType.Audio)
                    {
                        if (subscriberPeerConnection.RemoteAudioTracks.Count(v => v.Name == streamState.TrackSid) == 0)
                        {
                            subscriberPeerConnection.RemoteAudioTracks.FirstOrDefault(v => v.Name == "readyToSet")!.Name = streamState.TrackSid;
                        }

                        if (onAudioTrackAdded != null)
                        {
                            onAudioTrackAdded.Invoke(this, (streamState.ParticipantSid, streamState.TrackSid));
                        }
                    }
                }
                else
                {
                    if (RemoteParticipants.FirstOrDefault(v => v.Sid == streamState.ParticipantSid).Tracks.FirstOrDefault(v => v.Sid == streamState.TrackSid).Type == TrackType.Video)
                    {
                        if (onVideoTrackRemoved != null)
                        {
                            onVideoTrackRemoved.Invoke(this, (streamState.ParticipantSid, streamState.TrackSid));
                        }
                    }
                    else if (RemoteParticipants.FirstOrDefault(v => v.Sid == streamState.ParticipantSid).Tracks.FirstOrDefault(v => v.Sid == streamState.TrackSid).Type == TrackType.Audio)
                    {
                        if (onAudioTrackRemoved != null)
                        {
                            onAudioTrackRemoved.Invoke(this, (streamState.ParticipantSid, streamState.TrackSid));
                        }
                    }
                }
            }
        }


        private void onMute(string sid, bool muted)
        {
            Debug.WriteLine($"{sid}:::::::::::::::::::::::{muted}");
            //if (onMuted != null)
            //{
            //    onMuted?.Invoke(this, (sid, muted));
            //}
        }

        private void onConnectionQuality(SignalResponse signalResponse)
        {
            foreach (var item in signalResponse.ConnectionQuality.Updates.ToList())
            {
                UpdateParticipantConnectionQuality(item);
            }
        }

        public event EventHandler<List<SpeakerInfo>> onSpeakersChangedEvent;

        private void onSpeakersChanged(SignalResponse signalResponse)
        {
            List<SpeakerInfo> speakers = signalResponse.SpeakersChanged.Speakers.ToList();
            if (onSpeakersChangedEvent != null)
                onSpeakersChangedEvent?.Invoke(this, speakers);
        }


        public event EventHandler<TrackSubscribed> onTrackSubscribedEvent;
        /// <summary>
        /// 本轨道被其他人订阅
        /// </summary>
        /// <param name="signalResponse"></param>
        private void onTrackSubscribed(SignalResponse signalResponse)
        {
            if (onTrackSubscribedEvent != null)
                onTrackSubscribedEvent.Invoke(this, signalResponse.TrackSubscribed);
        }

        private async void onAcceptOffer(SignalResponse signalResponse)
        {

            if (subscriberPeerConnection == null)
            {
                await createSubPeerConnection();
            }

            SdpMessage sdpMessage = new SdpMessage();
            sdpMessage.Content = signalResponse.Offer.Sdp;
            sdpMessage.Type = SdpMessageType.Offer;

            await subscriberPeerConnection!.SetRemoteDescriptionAsync(sdpMessage).ContinueWith((t) =>
            {
                bool result = subscriberPeerConnection.CreateAnswer();
                if (!result)
                {
                    Debug.WriteLine("Failed to create peer connection answer, closing peer connection.");
                    subscriberPeerConnection.Close();
                }
            });
        }

        private async Task onReceiveAnswer(SignalResponse signalResponse)
        {
            //RTCSessionDescriptionInit rTCSessionDescriptionInit2 = new RTCSessionDescriptionInit();
            //rTCSessionDescriptionInit2.sdp = signalResponse.Answer.Sdp;
            //rTCSessionDescriptionInit2.type = RTCSdpType.answer;
            SdpMessage sdpMessage = new SdpMessage();
            sdpMessage.Content = signalResponse.Answer.Sdp;
            sdpMessage.Type = SdpMessageType.Answer;

            await publisherPeerConnection!.SetRemoteDescriptionAsync(sdpMessage);
            //if (answerResult != SetDescriptionResultEnum.OK)
            //{
            //    Debug.WriteLine("Failed to set remote description for publisher.");
            //}
        }

        private void onJoinResponse(SignalResponse signalResponse)
        {
            joinResponse = signalResponse.Join;
            UpdateRoom(signalResponse.Join.Room);
            UpdateLocalParticipant(joinResponse.Participant);
            UpdateRemoteParticipants(signalResponse.Join.OtherParticipants.ToList());
            createIceServer();
        }

        private async Task onLeave(SignalResponse signalResponse)
        {
            Debug.WriteLine($"Received leave signal, disposing WebSocketIO.{signalResponse.Leave.ToString()}");
            await WebSocketIO.DisposeAsync();
            switch (signalResponse.Leave.Action)
            {
                case LeaveRequest.Types.Action.Disconnect:
                    Debug.WriteLine("Disconnecting from the room.");
                    await WebSocketIO.DisposeAsync();
                    break;
                case LeaveRequest.Types.Action.Reconnect:
                    Debug.WriteLine("Reconnecting to the room.");
                    await ConnectAsync();
                    break;
                case LeaveRequest.Types.Action.Resume:
                    Debug.WriteLine("Resuming the room connection.");
                    // 这里可以添加恢复连接的逻辑
                    break;
            }
        }

        private void onTrickle(SignalResponse signalResponse)
        {

            if (signalResponse.Trickle.Target == SignalTarget.Subscriber)
            {
                var jsObj = JsonSerializer.Deserialize<Internal.IceCandidate>(signalResponse.Trickle.CandidateInit);
                var trickleCandidate = new Microsoft.MixedReality.WebRTC.IceCandidate
                {
                    Content = jsObj.candidate,
                    SdpMid = jsObj.sdpMid,
                    SdpMlineIndex = jsObj.sdpMLineIndex
                };
                subscriberPeerConnection.AddIceCandidate(trickleCandidate);

                if (signalResponse.Trickle.Final)
                {

                }

            }
            else
            {
                var jsObj = JsonSerializer.Deserialize<Internal.IceCandidate>(signalResponse.Trickle.CandidateInit);
                var trickleCandidate = new Microsoft.MixedReality.WebRTC.IceCandidate
                {
                    Content = jsObj.candidate,
                    SdpMid = jsObj.sdpMid ?? "0",
                    SdpMlineIndex = jsObj.sdpMLineIndex
                };
                publisherPeerConnection.AddIceCandidate(trickleCandidate);
            }
        }

        /// <summary>
        ///自己发布的 TrackPublished 事件
        /// </summary>
        /// <param name="signalResponse"></param>
        private void onTrackPublished(SignalResponse signalResponse)
        {
            //HandleTrackPublishedEvent(signalResponse.TrackPublished.Cid, signalResponse.TrackPublished.Track);
        }

        private void onRoomUpdate(SignalResponse signalResponse)
        {
            UpdateRoom(signalResponse.RoomUpdate.Room);
        }

        private void onUpdate(SignalResponse signalResponse)
        {
            UpdateRemoteParticipants(signalResponse.Update.Participants.ToList());
        }

        private void onTrackUnpublished(SignalResponse signalResponse)
        {
            //signalResponse.TrackUnpublished.TrackSid
        }

        private void onError(object error)
        {
            throw new NotImplementedException();
        }
        private void onDispose()
        {
        }
    }
}
