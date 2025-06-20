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
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.FFmpeg;
using static DirectShowLib.MediaSubType;

namespace Client.Sdk.Dotnet.core
{
    public class Engine : IDisposable
    {
        private PeerConnection? subscriberPeerConnection;

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

        public event EventHandler<string> ParticipantConnectionQualityUpdated;
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
            if (ParticipantConnectionQualityUpdated != null)
                ParticipantConnectionQualityUpdated?.Invoke(this, connectionQualityInfo.ParticipantSid);
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
        public event EventHandler<LiveKit.Proto.Room> LiveKitConnectionQualityUpdated;
        private void UpdateRoom(LiveKit.Proto.Room room)
        {
            this.room = room;
            if (LiveKitConnectionQualityUpdated != null)
                LiveKitConnectionQualityUpdated?.Invoke(this, room);
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

        AudioTrackSource microphoneSource = null;
        VideoTrackSource webcamSource = null;

        LocalAudioTrack localAudioTrack = null;
        LocalVideoTrack localVideoTrack = null;


        public async Task createPublisherPeerConnection()
        {
            //var testPatternSource = new FFmpegFileSource(@"E:\TDG\sipsorcery\examples\WebRTCExamples\WebRTCDaemon\media\max_intro.mp4", true, new AudioEncoder());
            //testPatternSource.RestrictFormats(format => format.Codec == VideoCodecsEnum.VP8);
            // 创建发布者和订阅者的 PeerConnection
            publisherPeerConnection = new PeerConnection();
            await publisherPeerConnection.InitializeAsync(configuration);
            Transceiver audioTransceiver = null;
            Transceiver videoTransceiver = null;
            webcamSource = await DeviceVideoTrackSource.CreateAsync();
            var videoTrackConfig = new LocalVideoTrackInitConfig
            {
                trackName = "webcam_track"
            };
            localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);
            microphoneSource = await DeviceAudioTrackSource.CreateAsync();
            var audioTrackConfig = new LocalAudioTrackInitConfig
            {
                trackName = "microphone_track"
            };
            localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);
            videoTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Video);
            videoTransceiver.LocalVideoTrack = localVideoTrack;
            videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
            audioTransceiver = publisherPeerConnection.AddTransceiver(MediaKind.Audio);
            audioTransceiver.LocalAudioTrack = localAudioTrack;
            audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;


            //MediaStreamTrack videoTrack = new MediaStreamTrack(testPatternSource.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
            //MediaStreamTrack audoTrack = new MediaStreamTrack(testPatternSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);
            //publisherPeerConnection.addTrack(videoTrack);
            //publisherPeerConnection.addTrack(audoTrack);

            //testPatternSource.OnVideoSourceEncodedSample += publisherPeerConnection.SendVideo;
            //testPatternSource.OnAudioSourceEncodedSample += publisherPeerConnection.SendAudio;

            //publisherPeerConnection.OnVideoFormatsNegotiated += (formats) => testPatternSource.SetVideoSourceFormat(formats.First());
            //publisherPeerConnection.OnAudioFormatsNegotiated += (formats) => testPatternSource.SetAudioSourceFormat(formats.First());

            //publisherPeerConnection.onicecandidate += (candidate) =>
            //{
            //    Debug.WriteLine(publisherPeerConnection.signalingState);
            //    if (candidate == null || publisherPeerConnection.signalingState == RTCSignalingState.closed)
            //    {
            //        return;
            //    }
            //    var trickleCandidate = new IceCandidate
            //    {
            //        candidate = "candidate:" + candidate.candidate,
            //        sdpMid = candidate.sdpMid ?? "0",
            //        sdpMLineIndex = candidate.sdpMLineIndex,
            //    };
            //    SignalRequest signalRequest = new SignalRequest
            //    {
            //        Trickle = new TrickleRequest
            //        {
            //            Target = SignalTarget.Publisher,
            //            CandidateInit = JsonSerializer.Serialize(trickleCandidate)
            //        },
            //    };
            //    Debug.WriteLine($"PulishbPeer Send ICE candidate: {signalRequest}");

            //    WebSocketIO.Send(signalRequest.ToByteArray());
            //};
            //publisherPeerConnection.oniceconnectionstatechange += async (state) =>
            //{
            //    if (state == RTCIceConnectionState.connected)
            //    {
            //        // 连接成功
            //        Debug.WriteLine($"publisherPeerConnection: connected");
            //        await testPatternSource.StartVideo();
            //        await testPatternSource.StartAudio();
            //    }
            //    else if (state == RTCIceConnectionState.failed)
            //    {
            //        publisherPeerConnection.Close("ice disconnection");
            //    }
            //    else if (state == RTCIceConnectionState.closed)
            //    {
            //        // 连接关闭
            //    }
            //};

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


            SignalRequest signalRequest = new SignalRequest();
            AddTrackRequest addTrackRequest = new AddTrackRequest();
            addTrackRequest.Cid = webcamSource.Name ?? "default-video-cid";
            addTrackRequest.Name = "microphone";
            addTrackRequest.Type = TrackType.Video;
            addTrackRequest.Source = TrackSource.Camera;
            addTrackRequest.DisableRed = false;
            addTrackRequest.Stream = "screenshare_video";
            addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
            signalRequest.AddTrack = addTrackRequest;
            WebSocketIO.Send(signalRequest.ToByteArray());
            publisherPeerConnection.LocalSdpReadytoSend += (peer) =>
            { //await publisherPeerConnection.(result2);
                SignalRequest signalRequest3 = new SignalRequest();
                signalRequest3.Offer = new SessionDescription
                {
                    Sdp = peer.Content,
                    Type = "offer"
                };
                WebSocketIO.Send(signalRequest3.ToByteArray());
                //publisherPeerConnection.restartIce();
                Debug.WriteLine($"subscriberPeerConnection: connected");
            };
            var result2 = publisherPeerConnection.CreateOffer();


        }

        VideoEncoderEndPoint videoEP = new VideoEncoderEndPoint();

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
                //Debug.WriteLine($"local_sdp:{sdps.Content}");
                SignalRequest signalRequest = new SignalRequest();
                signalRequest.Answer = new SessionDescription
                {
                    Sdp = sdps.Content,
                    Type = "answer",
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
                UpdateParticipantStream(track.Name, track);
            };
            subscriberPeerConnection.VideoTrackRemoved += (t, track) =>
            {
                Debug.WriteLine($"Audio track removed: {track.Name}");
                UpdateParticipantStream(track.Name, track);
            };
            await subscriberPeerConnection.InitializeAsync(configuration);
            Transceiver videoTransceiver = subscriberPeerConnection.AddTransceiver(MediaKind.Video);
            //videoTransceiver.LocalVideoTrack = localVideoTrack;
            videoTransceiver.DesiredDirection = Transceiver.Direction.ReceiveOnly;
            Transceiver audioTransceiver = subscriberPeerConnection.AddTransceiver(MediaKind.Audio);
            //audioTransceiver.LocalAudioTrack = localAudioTrack;
            audioTransceiver.DesiredDirection = Transceiver.Direction.ReceiveOnly;





            //MediaStreamTrack videoTrack = new MediaStreamTrack(videoEP.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly);
            //subscriberPeerConnection = new RTCPeerConnection(configuration);
            //subscriberPeerConnection.addTrack(videoTrack);
            //subscriberPeerConnection.OnVideoFormatsNegotiated += (formats) =>
            //  videoEP.SetVideoSinkFormat(formats.First());
            //subscriberPeerConnection.onicecandidate += async (candidate) =>
            //{

            //    var trickleCandidate = new IceCandidate
            //    {
            //        candidate = "candidate:" + candidate.candidate,
            //        sdpMid = candidate.sdpMid ?? "0",
            //        sdpMLineIndex = candidate.sdpMLineIndex,
            //    };
            //    SignalRequest signalRequest = new SignalRequest
            //    {
            //        Trickle = new TrickleRequest
            //        {
            //            Target = SignalTarget.Subscriber,
            //            CandidateInit = JsonSerializer.Serialize(trickleCandidate),
            //        },
            //    };

            //    WebSocketIO.Send(signalRequest.ToByteArray());
            //};

            //subscriberPeerConnection.oniceconnectionstatechange += async (state) =>
            //{
            //    if (state == RTCIceConnectionState.connected)
            //    {
            //        Debug.WriteLine("订阅轨道连接成功!");
            //    }
            //    else if (state == RTCIceConnectionState.failed)
            //    {
            //        subscriberPeerConnection.Close("ice disconnection");
            //    }
            //    else if (state == RTCIceConnectionState.closed)
            //    {
            //        // 连接关闭
            //    }
            //};

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
            Debug.WriteLine($"Received signal : {signalResponse?.MessageCase}");
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
                    // 处理房间更新的响应
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

        public Dictionary<string, RemoteVideoTrack> TrackStreams = new Dictionary<string, RemoteVideoTrack>();

        public event EventHandler<RemoteVideoTrack> onStreamUpdated;
        private void UpdateParticipantStream(string trackId, RemoteVideoTrack videoTrack)
        {
            if (TrackStreams.ContainsKey(trackId))
            {
                TrackStreams.Remove(trackId);
            }
            else
            {
                TrackStreams.Add(trackId, videoTrack);
            }
            if (onStreamUpdated != null)
            {
                onStreamUpdated?.Invoke(this, videoTrack);
            }
        }

        public RemoteVideoTrack? GetTrackStream(string trackId)
        {
            if (TrackStreams.TryGetValue(trackId, out var videoEncoderEndPoints))
            {
                return videoEncoderEndPoints;
            }
            return null;
        }


        private void onStreamStateUpdate(List<StreamStateInfo> streamStateInfos)
        {
            foreach (var streamState in streamStateInfos)
            {

                //Debug.WriteLine($"Stream state updated for participant {streamState.ToString()}");

                //Stream state updated for participant { "participantSid": "PA_d8Z3swcgQpcd", "trackSid": "TR_VSoERp55Wn4MMk" }

                //VideoEncoderEndPoint vp8VideoSink = new VideoEncoderEndPoint();

                //vp8VideoSink.OnVideoSinkDecodedSample += (byte[] bmp, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat) =>
                //{

                //    // 假设 bmp 是 byte[]，你需要转 Bitmap
                //    using var bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);


                //    var bmpData = bitmap.LockBits(
                //        new Rectangle(0, 0, (int)width, (int)height),
                //        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                //        bitmap.PixelFormat);

                //    System.Runtime.InteropServices.Marshal.Copy(bmp, 0, bmpData.Scan0, bmp.Length);
                //    bitmap.UnlockBits(bmpData);

                //    RenderFrameToBox(streamState.TrackSid, (Bitmap)bitmap.Clone());
                //};


                //VideoStream videoStream = subscriberPeerConnection.VideoStreamList.Where(v => v.RemoteTrack.SdpSsrc.Values.Any(s => s.Cname.Contains(streamState.ParticipantSid) && s.Cname.Contains(streamState.TrackSid))).FirstOrDefault();

                //if (videoStream == null)
                //{
                //    Debug.WriteLine("未找到对应的 VideoStream，可能 SDP 协商后 track/ssrc 变了。");
                //    continue;
                //}


                ////videoStream.OnVideoFrameReceivedByIndex += (q, e, c, bmp, f) =>
                ////{
                ////    vp8VideoSink.GotVideoFrame(e, c, bmp, f);
                ////};

                //videoStream.OnIsClosedStateChanged += (isClosed) =>
                //{
                //    Debug.WriteLine($"VideoStream {streamState.TrackSid} is now {(isClosed ? "closed" : "open")}.");
                //    var track = videoStream.RemoteTrack;
                //    if (track != null)
                //    {
                //        videoStream.RemoteTrack = null;
                //        MediaStreamTrack videoTrack = new MediaStreamTrack(new VideoEncoderEndPoint().GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly);
                //        videoStream.RemoteTrack = videoTrack;
                //    }
                //};

                //videoStream.OnTimeoutByIndex += (q, b) =>
                //{
                //    Debug.WriteLine($"VideoStream {streamState.TrackSid} timeout.");
                //};

                ////videoStream.OnRtpPacketReceivedByIndex += (a, b, c, d) =>
                ////{
                ////    Debug.WriteLine($"VideoStream {streamState.TrackSid} received RTP packet: {a}, {b}, {c}, {d}");
                ////};

                ////videoStream.OnRtpHeaderReceivedByIndex += (a, b, c, d, e) =>
                ////{
                ////    Debug.WriteLine($"VideoStream {streamState.TrackSid} received RTP header: {a}, {b}, {c}, {d}");
                ////};

                //UpdateParticipantStream(streamState.TrackSid, videoStream, streamState);

            }
        }

        public event EventHandler<(string, bool)> onMuted;
        private void onMute(string sid, bool muted)
        {
            //Debug.WriteLine($"Track {sid} is now {(mute ? "muted" : "unmuted")}.");
            if (onMuted != null)
            {
                onMuted?.Invoke(this, (sid, muted));
            }
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
            //if (subscriberPeerConnection.VideoRemoteTrack != null)
            //{
            //    subscriberPeerConnection.removeTrack(subscriberPeerConnection.VideoRemoteTrack);
            //}

            if (subscriberPeerConnection == null)
            {
                await createSubPeerConnection();
            }


            //VideoEncoderEndPoint vp8videosink = new VideoEncoderEndPoint();
            //MediaStreamTrack videotrack = new MediaStreamTrack(vp8videosink.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
            //subscriberPeerConnection!.addTrack(videotrack);


            //RTCSessionDescriptionInit rTCSessionDescriptionInit = new RTCSessionDescriptionInit();
            //rTCSessionDescriptionInit.sdp = signalResponse.Offer.Sdp;
            //rTCSessionDescriptionInit.type = RTCSdpType.offer;

            SdpMessage sdpMessage = new SdpMessage();
            sdpMessage.Content = signalResponse.Offer.Sdp;
            sdpMessage.Type = SdpMessageType.Offer;
            //subscriberPeerConnection.LocalSdpReadytoSend += (sdps) =>
            //{
            //    SignalRequest signalRequest = new SignalRequest();
            //    signalRequest.Answer = new SessionDescription
            //    {
            //        Sdp = sdps.Content,
            //        Type = "answer",
            //    };
            //    WebSocketIO.Send(signalRequest.ToByteArray());
            //    //subscriberPeerConnection.res();
            //};
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
