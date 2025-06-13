using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Client.Sdk.Dotnet.hardware;
using Client.Sdk.Dotnet.support.websocket;
using Google.Protobuf;
using LiveKit.Proto;
using Org.BouncyCastle.Asn1.X509;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using WebSocketSharp;

namespace Client.Sdk.Dotnet.Example
{
    public partial class Form1 : Form
    {
        private Bitmap? _webrtcBitmap;
        private readonly object _bitmapLock = new();
        public Form1()
        {
            InitializeComponent();
            connect();
            //HardWare hardWare = new HardWare();
            //var list = hardWare.GetAllScreen();
            //var list2 = hardWare.GetAllCamera();
        }


        private readonly Dictionary<string, PictureBox> _videoBoxes = new();

        private void AddOrUpdateVideoBox(string key)
        {
            if (!_videoBoxes.ContainsKey(key))
            {
                var pb = new PictureBox
                {
                    Width = 320,
                    Height = 180,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    Name = key
                };
                // �����������һ�� panelVideoContainer ���ڳ������� PictureBox
                if (panelVideoContainer.InvokeRequired)
                {
                    panelVideoContainer.Invoke(new Action(() =>
                    {
                        panelVideoContainer.Controls.Add(pb);
                        _videoBoxes[key] = pb;
                    }));
                }
                else
                {
                    panelVideoContainer.Controls.Add(pb);
                    _videoBoxes[key] = pb;
                }
            }
        }

        private void RenderFrameToBox(string key, Bitmap bmp)
        {

            if (!_videoBoxes.ContainsKey(key))
            {
                AddOrUpdateVideoBox(key);
            }

            if (_videoBoxes.TryGetValue(key, out var pb))
            {
                if (pb.InvokeRequired)
                {
                    pb.Invoke(new Action(() =>
                    {
                        pb.Image?.Dispose();
                        pb.Image = bmp;
                    }));
                }
                else
                {
                    pb.Image?.Dispose();
                    pb.Image = bmp;
                }
            }
        }


        //private void webrtcPanel_Paint(object? sender, PaintEventArgs e)
        //{
        //    lock (_bitmapLock)
        //    {
        //        if (_webrtcBitmap != null)
        //        {
        //            // ���ֱ������л���
        //            var destRect = GetFitRect(_webrtcBitmap.Width, _webrtcBitmap.Height, webrtcPanel.Width, webrtcPanel.Height);
        //            e.Graphics.DrawImage(_webrtcBitmap, destRect);
        //        }
        //    }
        //}

        //// ����ȱ����ź��Ŀ�����
        //private Rectangle GetFitRect(int srcW, int srcH, int destW, int destH)
        //{
        //    float ratio = Math.Min((float)destW / srcW, (float)destH / srcH);
        //    int w = (int)(srcW * ratio);
        //    int h = (int)(srcH * ratio);
        //    int x = (destW - w) / 2;
        //    int y = (destH - h) / 2;
        //    return new Rectangle(x, y, w, h);
        //}

        //public void RenderWebRTCFrame(byte[] frameData, int width, int height)
        //{
        //    var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        //    var bmpData = bmp.LockBits(
        //        new Rectangle(0, 0, width, height),
        //        System.Drawing.Imaging.ImageLockMode.WriteOnly,
        //        bmp.PixelFormat);

        //    System.Runtime.InteropServices.Marshal.Copy(frameData, 0, bmpData.Scan0, frameData.Length);
        //    bmp.UnlockBits(bmpData);

        //    // ���̰߳�ȫ���� PictureBox
        //    if (webrtcPanel.InvokeRequired)
        //    {
        //        webrtcPanel.Invoke(new Action(() => webrtcPanel.Image?.Dispose()));
        //        webrtcPanel.Invoke(new Action(() => webrtcPanel.Image = bmp));
        //    }
        //    else
        //    {
        //        webrtcPanel.Image?.Dispose();
        //        webrtcPanel.Image = bmp;
        //    }
        //}

        private LiveKitWebSocketIO WebSocketIO;

        private RTCPeerConnection subscriberPeerConnection;

        private RTCPeerConnection publisherPeerConnection;

        private string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTAxMjcwMDQsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXI3NyIsIm5iZiI6MTc0OTc4MTQwNCwic3ViIjoidGVzdF91c2VyNzciLCJ2aWRlbyI6eyJyb29tIjoidGVzdF9yb29tIiwicm9vbUpvaW4iOnRydWV9fQ.-JQnEioq00oyBZ47ITZrjc044lNpycDURoP94hmeRtg";
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

            // ����·�����б�
            var pathSegments = uri.Segments
                .Where(segment => !string.IsNullOrEmpty(segment) && segment != "/")
                .Select(segment => segment.TrimEnd('/'))
                .ToList();

            // �Ƴ���·����
            pathSegments.RemoveAll(string.IsNullOrEmpty);

            // �������·����
            pathSegments.AddRange(lastSegments);

            var clientInfo = await GetClientInfo();
            var networkType = await GetNetworkType();

            // ������ѯ�����ֵ�
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

            // ������ز���
            if (reconnect)
            {
                queryParams["reconnect"] = "1";
                if (sid != null)
                {
                    queryParams["sid"] = sid;
                }
            }

            // ��ӿͻ�����Ϣ
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

            // �������� URI
            var uriBuilder = new UriBuilder
            {
                Scheme = validate ? httpScheme : wsScheme,
                Host = uri.Host,
                Port = uri.Port != -1 ? uri.Port : (useSecure ? (validate ? 443 : 443) : (validate ? 80 : 80))
            };

            // ����·��
            uriBuilder.Path = string.Join("/", pathSegments);

            // ������ѯ�ַ���
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
        #region Join after
        //        {
        //	"room": {
        //		"sid": "RM_N2Xcgnvs4kz9",
        //		"name": "test_room",
        //		"emptyTimeout": 300,
        //		"creationTime": "1749735434",
        //		"turnPassword": "YJLmOAHGHY89C8n3YnVkA3iaXlIehNsmCI3vnRi5kaH",
        //		"enabledCodecs": [{
        //			"mime": "audio/opus"
        //		}, {
        //			"mime": "audio/red"
        //		}, {
        //			"mime": "video/VP8"
        //		}, {
        //			"mime": "video/H264"
        //		}, {
        //			"mime": "video/VP9"
        //		}, {
        //			"mime": "video/AV1"
        //		}, {
        //			"mime": "video/rtx"
        //		}],
        //		"numParticipants": 2,
        //		"numPublishers": 2,
        //		"departureTimeout": 20,
        //		"creationTimeMs": "1749735434617"
        //	},
        //	"participant": {
        //		"sid": "PA_pqdAZ5A7PkLs",
        //		"identity": "test_user1",
        //		"joinedAt": "1749735527",
        //		"name": "test_user1",
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"joinedAtMs": "1749735527143"
        //	},
        //	"otherParticipants": [{
        //		"sid": "PA_d8Z3swcgQpcd",
        //		"identity": "test_user3",
        //		"state": "ACTIVE",
        //		"tracks": [{
        //			"sid": "TR_VSoERp55Wn4MMk",
        //			"type": "VIDEO",
        //			"name": "screenshare",
        //			"width": 1920,
        //			"height": 1080,
        //			"simulcast": true,
        //			"source": "SCREEN_SHARE",
        //			"layers": [{
        //				"width": 960,
        //				"height": 540,
        //				"bitrate": 150000,
        //				"ssrc": 1047453374
        //			}, {
        //				"quality": "MEDIUM",
        //				"width": 1920,
        //				"height": 1080,
        //				"bitrate": 3000000,
        //				"ssrc": 1750359185
        //			}],
        //			"mimeType": "video/VP8",
        //			"mid": "2",
        //			"codecs": [{
        //				"mimeType": "video/VP8",
        //				"mid": "2",
        //				"cid": "92F02745-89F4-4439-8737-0A227D93D668",
        //				"layers": [{
        //					"width": 960,
        //					"height": 540,
        //					"bitrate": 150000,
        //					"ssrc": 1047453374
        //				}, {
        //					"quality": "MEDIUM",
        //					"width": 1920,
        //					"height": 1080,
        //					"bitrate": 3000000,
        //					"ssrc": 1750359185
        //				}]
        //			}],
        //			"stream": "screen",
        //			"version": {
        //				"unixMicro": "1749735497814411"
        //			}
        //		}],
        //		"joinedAt": "1749735434",
        //		"name": "test_user3",
        //		"version": 8,
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"isPublisher": true,
        //		"joinedAtMs": "1749735434628"
        //	}, {
        //		"sid": "PA_r2bpQ8JJ9Ctj",
        //		"identity": "bot_user",
        //		"state": "ACTIVE",
        //		"tracks": [{
        //			"sid": "TR_VCUQhdDZspVoXB",
        //			"type": "VIDEO",
        //			"name": "demo",
        //			"width": 1280,
        //			"height": 720,
        //			"simulcast": true,
        //			"source": "CAMERA",
        //			"layers": [{
        //				"width": 320,
        //				"height": 180,
        //				"bitrate": 120000,
        //				"ssrc": 3495242482
        //			}, {
        //				"quality": "MEDIUM",
        //				"width": 640,
        //				"height": 360,
        //				"bitrate": 400000,
        //				"ssrc": 2770722753
        //			}, {
        //				"quality": "HIGH",
        //				"width": 1280,
        //				"height": 720,
        //				"bitrate": 1500000,
        //				"ssrc": 1554602843
        //			}],
        //			"mimeType": "video/H264",
        //			"mid": "1",
        //			"codecs": [{
        //				"mimeType": "video/H264",
        //				"mid": "1",
        //				"cid": "demo-video",
        //				"layers": [{
        //					"width": 320,
        //					"height": 180,
        //					"bitrate": 120000,
        //					"ssrc": 3495242482
        //				}, {
        //					"quality": "MEDIUM",
        //					"width": 640,
        //					"height": 360,
        //					"bitrate": 400000,
        //					"ssrc": 2770722753
        //				}, {
        //					"quality": "HIGH",
        //					"width": 1280,
        //					"height": 720,
        //					"bitrate": 1500000,
        //					"ssrc": 1554602843
        //				}]
        //			}],
        //			"stream": "camera",
        //			"version": {
        //				"unixMicro": "1749735451895869"
        //			}
        //		}],
        //		"joinedAt": "1749735450",
        //		"version": 6,
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"isPublisher": true,
        //		"joinedAtMs": "1749735450658"
        //	}],
        //	"serverVersion": "1.9.0",
        //	"iceServers": [{
        //		"urls": ["stun:global.stun.twilio.com:3478", "stun:stun.l.google.com:19302", "stun:stun1.l.google.com:19302"]
        //	}],
        //	"subscriberPrimary": true,
        //	"pingTimeout": 15,
        //	"pingInterval": 5,
        //	"serverInfo": {
        //		"version": "1.9.0",
        //		"protocol": 16,
        //		"nodeId": "ND_kWn94PxEn6PD",
        //		"agentProtocol": 1
        //	},
        //	"sifTrailer": "bndKc1BxQjNPazFRNXQ0N0l0N1N0VlV5WHpUMUFyaDJKTllXMkZsT1ByTA==",
        //	"enabledPublishCodecs": [{
        //		"mime": "video/VP8"
        //	}, {
        //		"mime": "video/H264"
        //	}, {
        //		"mime": "video/VP9"
        //	}, {
        //		"mime": "video/AV1"
        //	}, {
        //		"mime": "audio/opus"
        //	}, {
        //		"mime": "audio/red"
        //	}],
        //	"fastPublish": true
        //}{
        //	"room": {
        //		"sid": "RM_N2Xcgnvs4kz9",
        //		"name": "test_room",
        //		"emptyTimeout": 300,
        //		"creationTime": "1749735434",
        //		"turnPassword": "YJLmOAHGHY89C8n3YnVkA3iaXlIehNsmCI3vnRi5kaH",
        //		"enabledCodecs": [{
        //			"mime": "audio/opus"
        //		}, {
        //			"mime": "audio/red"
        //		}, {
        //			"mime": "video/VP8"
        //		}, {
        //			"mime": "video/H264"
        //		}, {
        //			"mime": "video/VP9"
        //		}, {
        //			"mime": "video/AV1"
        //		}, {
        //			"mime": "video/rtx"
        //		}],
        //		"numParticipants": 2,
        //		"numPublishers": 2,
        //		"departureTimeout": 20,
        //		"creationTimeMs": "1749735434617"
        //	},
        //	"participant": {
        //		"sid": "PA_pqdAZ5A7PkLs",
        //		"identity": "test_user1",
        //		"joinedAt": "1749735527",
        //		"name": "test_user1",
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"joinedAtMs": "1749735527143"
        //	},
        //	"otherParticipants": [{
        //		"sid": "PA_d8Z3swcgQpcd",
        //		"identity": "test_user3",
        //		"state": "ACTIVE",
        //		"tracks": [{
        //			"sid": "TR_VSoERp55Wn4MMk",
        //			"type": "VIDEO",
        //			"name": "screenshare",
        //			"width": 1920,
        //			"height": 1080,
        //			"simulcast": true,
        //			"source": "SCREEN_SHARE",
        //			"layers": [{
        //				"width": 960,
        //				"height": 540,
        //				"bitrate": 150000,
        //				"ssrc": 1047453374
        //			}, {
        //				"quality": "MEDIUM",
        //				"width": 1920,
        //				"height": 1080,
        //				"bitrate": 3000000,
        //				"ssrc": 1750359185
        //			}],
        //			"mimeType": "video/VP8",
        //			"mid": "2",
        //			"codecs": [{
        //				"mimeType": "video/VP8",
        //				"mid": "2",
        //				"cid": "92F02745-89F4-4439-8737-0A227D93D668",
        //				"layers": [{
        //					"width": 960,
        //					"height": 540,
        //					"bitrate": 150000,
        //					"ssrc": 1047453374
        //				}, {
        //					"quality": "MEDIUM",
        //					"width": 1920,
        //					"height": 1080,
        //					"bitrate": 3000000,
        //					"ssrc": 1750359185
        //				}]
        //			}],
        //			"stream": "screen",
        //			"version": {
        //				"unixMicro": "1749735497814411"
        //			}
        //		}],
        //		"joinedAt": "1749735434",
        //		"name": "test_user3",
        //		"version": 8,
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"isPublisher": true,
        //		"joinedAtMs": "1749735434628"
        //	}, {
        //		"sid": "PA_r2bpQ8JJ9Ctj",
        //		"identity": "bot_user",
        //		"state": "ACTIVE",
        //		"tracks": [{
        //			"sid": "TR_VCUQhdDZspVoXB",
        //			"type": "VIDEO",
        //			"name": "demo",
        //			"width": 1280,
        //			"height": 720,
        //			"simulcast": true,
        //			"source": "CAMERA",
        //			"layers": [{
        //				"width": 320,
        //				"height": 180,
        //				"bitrate": 120000,
        //				"ssrc": 3495242482
        //			}, {
        //				"quality": "MEDIUM",
        //				"width": 640,
        //				"height": 360,
        //				"bitrate": 400000,
        //				"ssrc": 2770722753
        //			}, {
        //				"quality": "HIGH",
        //				"width": 1280,
        //				"height": 720,
        //				"bitrate": 1500000,
        //				"ssrc": 1554602843
        //			}],
        //			"mimeType": "video/H264",
        //			"mid": "1",
        //			"codecs": [{
        //				"mimeType": "video/H264",
        //				"mid": "1",
        //				"cid": "demo-video",
        //				"layers": [{
        //					"width": 320,
        //					"height": 180,
        //					"bitrate": 120000,
        //					"ssrc": 3495242482
        //				}, {
        //					"quality": "MEDIUM",
        //					"width": 640,
        //					"height": 360,
        //					"bitrate": 400000,
        //					"ssrc": 2770722753
        //				}, {
        //					"quality": "HIGH",
        //					"width": 1280,
        //					"height": 720,
        //					"bitrate": 1500000,
        //					"ssrc": 1554602843
        //				}]
        //			}],
        //			"stream": "camera",
        //			"version": {
        //				"unixMicro": "1749735451895869"
        //			}
        //		}],
        //		"joinedAt": "1749735450",
        //		"version": 6,
        //		"permission": {
        //			"canSubscribe": true,
        //			"canPublish": true,
        //			"canPublishData": true
        //		},
        //		"isPublisher": true,
        //		"joinedAtMs": "1749735450658"
        //	}],
        //	"serverVersion": "1.9.0",
        //	"iceServers": [{
        //		"urls": ["stun:global.stun.twilio.com:3478", "stun:stun.l.google.com:19302", "stun:stun1.l.google.com:19302"]
        //	}],
        //	"subscriberPrimary": true,
        //	"pingTimeout": 15,
        //	"pingInterval": 5,
        //	"serverInfo": {
        //		"version": "1.9.0",
        //		"protocol": 16,
        //		"nodeId": "ND_kWn94PxEn6PD",
        //		"agentProtocol": 1
        //	},
        //	"sifTrailer": "bndKc1BxQjNPazFRNXQ0N0l0N1N0VlV5WHpUMUFyaDJKTllXMkZsT1ByTA==",
        //	"enabledPublishCodecs": [{
        //		"mime": "video/VP8"
        //	}, {
        //		"mime": "video/H264"
        //	}, {
        //		"mime": "video/VP9"
        //	}, {
        //		"mime": "video/AV1"
        //	}, {
        //		"mime": "audio/opus"
        //	}, {
        //		"mime": "audio/red"
        //	}],
        //	"fastPublish": true
        //}
        #endregion
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

            // ���������ߺͶ����ߵ� PeerConnection
            publisherPeerConnection = new RTCPeerConnection(configuration);

            var vp8VideoSink2 = new VideoEncoderEndPoint();

            MediaStreamTrack audioTrack = new MediaStreamTrack(vp8VideoSink2.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
            publisherPeerConnection.addTrack(audioTrack);


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
                    addTrackRequest.Name = "Ĭ����Ƶ�������";
                    addTrackRequest.Type = TrackType.Video;
                    addTrackRequest.Source = TrackSource.Camera;
                    addTrackRequest.Sid = joinResponse.Participant.Sid;
                    addTrackRequest.Stream = "";
                    addTrackRequest.BackupCodecPolicy = BackupCodecPolicy.Simulcast;
                    signalRequest.AddTrack = addTrackRequest;
                    WebSocketIO.Send(signalRequest.ToByteArray());
                    var result2 = publisherPeerConnection.createOffer(null);
                    SignalRequest signalRequest3 = new SignalRequest();
                    signalRequest3.Offer = new SessionDescription
                    {
                        Sdp = result2.sdp,
                        Type = "offer"
                    };
                    WebSocketIO.Send(signalRequest3.ToByteArray());
                    await publisherPeerConnection.setLocalDescription(result2);
                    publisherPeerConnection.restartIce();
                    Debug.WriteLine($"subscriberPeerConnection: connected");
                }
                else if (state == RTCIceConnectionState.failed)
                {
                    subscriberPeerConnection.Close("ice disconnection");
                }
                else if (state == RTCIceConnectionState.closed)
                {
                    // ���ӹر�
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
                if (state == RTCIceConnectionState.connected)
                {
                    // ���ӳɹ�
                    Debug.WriteLine($"publisherPeerConnection: connected");

                }
                else if (state == RTCIceConnectionState.failed)
                {
                    publisherPeerConnection.Close("ice disconnection");
                }
                else if (state == RTCIceConnectionState.closed)
                {
                    // ���ӹر�
                }
            };
        }


        private async void onData(byte[] data)
        {
            SignalResponse? signalResponse = SignalResponse.Parser.ParseFrom(data);
            //Debug.WriteLine($"Received signal : {signalResponse?.MessageCase}");
            switch (signalResponse.MessageCase)
            {
                case SignalResponse.MessageOneofCase.Offer:
                    if (subscriberPeerConnection == null)
                    {
                        createPeerConnection();
                    }

                    if (subscriberPeerConnection.VideoRemoteTrack != null)
                    {
                        subscriberPeerConnection.removeTrack(subscriberPeerConnection.VideoRemoteTrack);
                    }

                    VideoEncoderEndPoint vp8videosink = new VideoEncoderEndPoint();
                    MediaStreamTrack videotrack = new MediaStreamTrack(vp8videosink.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
                    subscriberPeerConnection!.addTrack(videotrack);


                    RTCSessionDescriptionInit rTCSessionDescriptionInit = new RTCSessionDescriptionInit();
                    rTCSessionDescriptionInit.sdp = signalResponse.Offer.Sdp;
                    rTCSessionDescriptionInit.type = RTCSdpType.offer;
                    var result = subscriberPeerConnection!.setRemoteDescription(rTCSessionDescriptionInit);

                    if (result == SetDescriptionResultEnum.OK)
                    {
                        var answer = subscriberPeerConnection.createAnswer();
                        await subscriberPeerConnection.setLocalDescription(answer);
                        SignalRequest signalRequest = new SignalRequest();
                        signalRequest.Answer = new SessionDescription
                        {
                            Sdp = answer.sdp,
                            Type = "answer",
                        };
                        WebSocketIO.Send(signalRequest.ToByteArray());
                        subscriberPeerConnection.restartIce();
                    }
                    else
                    {
                        Debug.WriteLine("Failed to set remote description for subscriber.");
                    }
                    Debug.WriteLine($"Received signal : {signalResponse?.MessageCase}");
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
                        // ���� PeerConnection
                        createPeerConnection();
                        // ������뷿�����Ӧ
                        _pingInterval = TimeSpan.FromSeconds(signalResponse.Join.PingInterval);
                        _pingTimer = new System.Timers.Timer(_pingInterval); // 30�뷢��һ��Ping
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
                    // ������뷿�����Ӧ
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
                            // ���������ӻָ����ӵ��߼�
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
                    // ����ˢ�����Ƶ���Ӧ
                    token = signalResponse.RefreshToken;
                    break;
                case SignalResponse.MessageOneofCase.RoomUpdate:
                    roomUpdate(signalResponse.RoomUpdate.Room);
                    break;
                case SignalResponse.MessageOneofCase.Update:
                    // ��������µ���Ӧ
                    update(signalResponse.Update.Participants.ToList());
                    break;
                case SignalResponse.MessageOneofCase.TrackUnpublished:
                    HandleTrackUnPublishedEvent(signalResponse.TrackUnpublished.TrackSid);
                    break;
                case SignalResponse.MessageOneofCase.TrackSubscribed:
                    HandleTrackSubscribed(signalResponse.TrackSubscribed.TrackSid);
                    break;
                case SignalResponse.MessageOneofCase.SpeakersChanged:
                    // �����������仯����Ӧ
                    HandleSpeakerChanged(signalResponse.SpeakersChanged.Speakers.ToList());
                    break;
                case SignalResponse.MessageOneofCase.ConnectionQuality:
                    connectionQuality(signalResponse.ConnectionQuality.Updates.ToList());
                    break;
                case SignalResponse.MessageOneofCase.Mute:
                    mute(signalResponse.Mute.Sid, signalResponse.Mute.Muted);
                    break;
                case SignalResponse.MessageOneofCase.StreamStateUpdate:
                    // ������״̬���µ���Ӧ
                    streamStateUpdate(signalResponse.StreamStateUpdate.StreamStates.ToList());
                    break;
                case SignalResponse.MessageOneofCase.SubscribedQualityUpdate:
                    subscribedQualityUpdate(
                        signalResponse.SubscribedQualityUpdate.TrackSid,
                        signalResponse.SubscribedQualityUpdate.SubscribedQualities.ToList(),
                        signalResponse.SubscribedQualityUpdate.SubscribedCodecs.ToList());

                    // �������������µ���Ӧ
                    break;
                case SignalResponse.MessageOneofCase.SubscriptionPermissionUpdate:
                    subscriptionPermissionUpdate(
                        signalResponse.SubscriptionPermissionUpdate.ParticipantSid,
                        signalResponse.SubscriptionPermissionUpdate.TrackSid,
                        signalResponse.SubscriptionPermissionUpdate.Allowed);
                    break;
                case SignalResponse.MessageOneofCase.Reconnect:
                    // ������������
                    break;
            }
        }

        private System.Timers.Timer? _pingTimer;
        private TimeSpan _pingInterval;


        private void HandleTrackPublishedEvent(string cid, TrackInfo info)
        {
            Debug.WriteLine($"Track published with CID: {cid}, Track Info: {info.ToString()}");
        }

        private void HandleTrackUnPublishedEvent(string cid)
        {
            Debug.WriteLine($"Track unpublished with CID: {cid}.");

        }

        private void HandleTrackSubscribed(string trackSid)
        {
            Debug.WriteLine($"Track subscribed with SID: {trackSid}.");
        }

        private void HandleSpeakerChanged(List<SpeakerInfo> speakers)
        {
            //Debug.WriteLine($"Speakers changed: {speakers[0].ToString()}");
            foreach (SpeakerInfo speaker in speakers)
            {
                Debug.WriteLine($"Speaker changed: {speaker.ToString()}");
            }
        }

        private void update(List<ParticipantInfo> participants)
        {
            foreach (ParticipantInfo participant in participants)
            {
                Debug.WriteLine($"Participant updated: {participant.ToString()}");
            }
        }

        //        {
        //	"sid": "RM_N2Xcgnvs4kz9",
        //	"name": "test_room",
        //	"emptyTimeout": 300,
        //	"creationTime": "1749735434",
        //	"turnPassword": "YJLmOAHGHY89C8n3YnVkA3iaXlIehNsmCI3vnRi5kaH",
        //	"enabledCodecs": [{
        //		"mime": "audio/opus"

        //    }, {
        //		"mime": "audio/red"
        //	}, {
        //    "mime": "video/VP8"

        //    }, {
        //    "mime": "video/H264"

        //    }, {
        //    "mime": "video/VP9"

        //    }, {
        //    "mime": "video/AV1"

        //    }, {
        //    "mime": "video/rtx"

        //    }],
        //	"numParticipants": 3,
        //	"numPublishers": 2,
        //	"departureTimeout": 20,
        //	"creationTimeMs": "1749735434617"
        //}
        private void roomUpdate(Room room)
        {
            Debug.WriteLine($"Room updated: {room.ToString()}");

        }

        private void connectionQuality(List<ConnectionQualityInfo> connectionQualities)
        {
            foreach (var quality in connectionQualities)
            {
                Debug.WriteLine($"Connection quality for participant {quality.ParticipantSid}: {quality.Quality}");
                //Connection quality for participant PA_d8Z3swcgQpcd: Excellent
            }
        }

        private void leave(LeaveRequest leave) { }

        private void mute(string sid, bool mute)
        {
            Debug.WriteLine($"Track {sid} is now {(mute ? "muted" : "unmuted")}.");
        }

        private void streamStateUpdate(List<StreamStateInfo> streamStateInfos)
        {
            foreach (var streamState in streamStateInfos)
            {
                Debug.WriteLine($"Stream state updated for participant {streamState.ToString()}");

                //Stream state updated for participant { "participantSid": "PA_d8Z3swcgQpcd", "trackSid": "TR_VSoERp55Wn4MMk" }

                VideoEncoderEndPoint vp8VideoSink = new VideoEncoderEndPoint();

                vp8VideoSink.OnVideoSinkDecodedSample += (byte[] bmp, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat) =>
                {
                    //Debug.WriteLine($"Received video frame: {width}x{height}, stride: {stride}, pixelFormat: {pixelFormat}");
                    //unsafe
                    //{
                    //    fixed (byte* s = bmp)
                    //    {
                    //        var bmpImage = new Bitmap((int)width, (int)height, stride, PixelFormat.Format24bppRgb, (IntPtr)s);
                    //        webrtcPanel.Image = bmpImage;
                    //    }
                    //}
                    //RenderWebRTCFrame(bmp, (int)width, (int)height);

                    // ���� bmp �� byte[]������Ҫת Bitmap
                    using var bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);


                    var bmpData = bitmap.LockBits(
                        new Rectangle(0, 0, (int)width, (int)height),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        bitmap.PixelFormat);

                    System.Runtime.InteropServices.Marshal.Copy(bmp, 0, bmpData.Scan0, bmp.Length);
                    bitmap.UnlockBits(bmpData);

                    RenderFrameToBox(streamState.TrackSid, (Bitmap)bitmap.Clone());
                };

                VideoStream videoStream = subscriberPeerConnection.VideoStreamList.Where(v => v.RemoteTrack.SdpSsrc.Values.Any(s => s.Cname.Contains(streamState.ParticipantSid) && s.Cname.Contains(streamState.TrackSid))).FirstOrDefault();

                if (videoStream == null)
                {
                    Debug.WriteLine("δ�ҵ���Ӧ�� VideoStream������ SDP Э�̺� track/ssrc ���ˡ�");
                    continue;
                }

                videoStream.OnVideoFrameReceivedByIndex += (q, e, c, bmp, f) =>
                {
                    vp8VideoSink.GotVideoFrame(e, c, bmp, f);
                };
            }
        }


        private void subscribedQualityUpdate(string sid, List<SubscribedQuality> subscribedQualities, List<SubscribedCodec> subscribedCodecs)
        {
            Debug.WriteLine($"Subscribed quality update for track {sid} with qualities: {string.Join(", ", subscribedQualities)} and codecs: {string.Join(", ", subscribedCodecs)}");

        }

        private void subscriptionPermissionUpdate(string participantSid, string trackSid, bool allowed)
        {
            Debug.WriteLine($"Subscription permission for participant {participantSid} on track {trackSid} is now {(allowed ? "allowed" : "denied")}.");
            //Subscription permission for participant PA_d8Z3swcgQpcd on track TR_VSoERp55Wn4MMk is now allowed.

        }
        public class IceCandidate
        {
            public string candidate { get; set; }
            public string sdpMid { get; set; }
            public ushort sdpMLineIndex { get; set; }
        }



        /// <summary>
        /// ��ȡ�豸�Ͳ���ϵͳ��Ϣ
        /// </summary>
        private static async Task<ClientInfo?> GetClientInfo()
        {
            var clientInfo = new ClientInfo { Os = RuntimeInformation.OSDescription };

            try
            {
                // ��������ƽ̨��䲻ͬ����Ϣ
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    clientInfo.Os = "windows";
                    clientInfo.OsVersion = Environment.OSVersion.Version.ToString();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    clientInfo.Os = "macOS";

                    // ��ʵ��ʵ����Ӧͨ��ƽ̨�ض�API��ȡ����ϸ����Ϣ
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

                    // ���Խ�һ������ض�Linux���а�
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

                // �豸�ͺ���Ϣ
                clientInfo.DeviceModel = RuntimeInformation.ProcessArchitecture.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"��ȡ�ͻ�����Ϣʱ����: {ex.Message}");
            }

            return clientInfo;
        }

        /// <summary>
        /// ��ȡ��ǰ������������
        /// </summary>
        private static async Task<string> GetNetworkType()
        {
            try
            {
                // ʵ��ʵ����Ӧʹ��ƽ̨�ض�API�������״̬
                // ������һ���򻯵�ʵ��

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // ����Ĭ�����������ӣ�ʵ��Ӧͨ��NetworkInterface API���
                    return "wifi";
                }

                return "empty";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"��ȡ��������ʱ����: {ex.Message}");
                return "empty";
            }
        }

        /// <summary>
        /// �ͻ�����Ϣ��
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
