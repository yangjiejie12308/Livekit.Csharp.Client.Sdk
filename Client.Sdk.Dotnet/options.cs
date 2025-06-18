using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.e2ee;
using Client.Sdk.Dotnet.types;

namespace Client.Sdk.Dotnet
{
    /// <summary>
    /// 连接到服务器时使用的选项
    /// </summary>
    public class ConnectOptions
    {
        /// <summary>
        /// 成功连接到 Room 后，是否自动订阅现有和新的 RemoteTrackPublication
        /// 默认为 true
        /// </summary>
        public bool AutoSubscribe { get; }

        /// <summary>
        /// 要使用的默认 RTCConfiguration
        /// </summary>
        public RTCConfiguration RtcConfiguration { get; }

        /// <summary>
        /// 要使用的协议版本。通常不需要修改此项
        /// </summary>
        public ProtocolVersion ProtocolVersion { get; }

        /// <summary>
        /// 各种操作的超时设置
        /// </summary>
        public Timeouts Timeouts { get; }

        /// <summary>
        /// 创建连接选项实例
        /// </summary>
        /// <param name="autoSubscribe">是否自动订阅轨道，默认为 true</param>
        /// <param name="rtcConfiguration">WebRTC 配置</param>
        /// <param name="protocolVersion">协议版本</param>
        /// <param name="timeouts">超时设置</param>
        public ConnectOptions(
            bool autoSubscribe = true,
            RTCConfiguration? rtcConfiguration = null,
            ProtocolVersion protocolVersion = ProtocolVersion.V12,
            Timeouts? timeouts = null)
        {
            AutoSubscribe = autoSubscribe;
            RtcConfiguration = rtcConfiguration ?? new RTCConfiguration();
            ProtocolVersion = protocolVersion;
            Timeouts = timeouts ?? Timeouts.DefaultTimeouts;
        }
    }

    /// <summary>
    /// WebRTC 配置
    /// </summary>
    public class RTCConfiguration
    {
        // 此处省略 RTCConfiguration 的具体实现
        // 在实际应用中应包含与 WebRTC 相关的配置选项

        /// <summary>
        /// 创建默认的 WebRTC 配置
        /// </summary>
        public RTCConfiguration()
        {
            // 初始化默认配置
        }
    }

    /// <summary>
    /// 协议版本
    /// </summary>
    public enum ProtocolVersion
    {
        /// <summary>
        /// 协议版本 12
        /// </summary>
        V12 = 12,

        // 可以添加其他版本
    }

    /// <summary>
    /// 操作超时设置
    /// </summary>
    public class Timeouts
    {
        /// <summary>
        /// 默认的超时设置实例
        /// </summary>
        public static readonly Timeouts DefaultTimeouts = new Timeouts();

        // 此处可以添加各种超时设置属性
        // 例如:

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public int Connection { get; }

        /// <summary>
        /// 信令超时时间（毫秒）
        /// </summary>
        public int Signaling { get; }

        /// <summary>
        /// ICE 连接超时时间（毫秒）
        /// </summary>
        public int IceConnection { get; }

        /// <summary>
        /// 创建自定义超时设置
        /// </summary>
        public Timeouts(
            int connection = 30000,
            int signaling = 10000,
            int iceConnection = 15000)
        {
            Connection = connection;
            Signaling = signaling;
            IceConnection = iceConnection;
        }
    }
    /// <summary>
    /// Options used to modify the behavior of the Room.
    /// </summary>
    public class RoomOptions
    {

        /// <summary>
        /// Default options used when publishing a LocalVideoTrack.
        /// </summary>
        public VideoPublishOptions DefaultVideoPublishOptions { get; }

        /// <summary>
        /// Default options used when publishing a LocalAudioTrack.
        /// </summary>
        public AudioPublishOptions DefaultAudioPublishOptions { get; }


        /// <summary>
        /// AdaptiveStream lets LiveKit automatically manage quality of subscribed
        /// video tracks to optimize for bandwidth and CPU.
        /// When attached video elements are visible, it'll choose an appropriate
        /// resolution based on the size of largest video element it's attached to.
        ///
        /// When none of the video elements are visible, it'll temporarily pause
        /// the data flow until they are visible again.
        /// </summary>
        public bool AdaptiveStream { get; }

        /// <summary>
        /// Enable Dynacast, off by default. With Dynacast dynamically pauses
        /// video layers that are not being consumed by any subscribers, significantly
        /// reducing publishing CPU and bandwidth usage.
        /// Dynacast will be enabled if SVC codecs (VP9/AV1) are used. Multi-codec
        /// simulcast requires dynacast
        /// </summary>
        public bool Dynacast { get; }

        /// <summary>
        /// Set this to false in case you would like to stop the track yourself.
        /// If you set this to false, make sure you call Track.Stop.
        /// Defaults to true.
        /// </summary>
        public bool StopLocalTrackOnUnpublish { get; }

        /// <summary>
        /// Options for end-to-end encryption.
        /// </summary>
        public E2EEOptions? E2EEOptions { get; }

        /// <summary>
        /// Fast track publication
        /// </summary>
        public bool FastPublish { get; }

        /// <summary>
        /// Deprecated, use CreateVisualizer instead
        /// </summary>
        [Obsolete("Use CreateVisualizer instead")]
        public bool? EnableVisualizer { get; }

        /// <summary>
        /// Creates a new instance of RoomOptions with default values.
        /// </summary>
       
    }

    /// <summary>
    /// 视频发布选项，用于发布视频轨道
    /// </summary>
    public class VideoPublishOptions : PublishOptions
    {
        /// <summary>
        /// 默认相机轨道名称
        /// </summary>
        public static readonly string DefaultCameraName = "camera";

        /// <summary>
        /// 默认屏幕共享轨道名称
        /// </summary>
        public static readonly string DefaultScreenShareName = "screenshare";

        /// <summary>
        /// 默认备用视频编码配置
        /// </summary>
        public static readonly BackupVideoCodec DefaultBackupVideoCodec = new BackupVideoCodec(
            enabled: true,
            codec: Constants.DefaultVideoCodec,
            simulcast: true
        );

        /// <summary>
        /// 要使用的视频编解码器
        /// </summary>
        public string VideoCodec { get; }

        /// <summary>
        /// 如果提供，将用于替代SDK推荐的编码设置
        /// 通常不需要提供此项
        /// 默认为null
        /// </summary>
        public VideoEncoding? VideoEncoding { get; }

        /// <summary>
        /// 屏幕共享编码设置
        /// </summary>
        public VideoEncoding? ScreenShareEncoding { get; }

        /// <summary>
        /// 是否启用simulcast（多层编码）
        /// https://blog.livekit.io/an-introduction-to-webrtc-simulcast-6c5f1f6402eb
        /// 默认为true
        /// </summary>
        public bool Simulcast { get; }

        /// <summary>
        /// 视频降级首选项
        /// </summary>
        public DegradationPreference? DegradationPreference { get; }

        /// <summary>
        /// 视频simulcast层配置
        /// </summary>
        public IReadOnlyList<VideoParameters> VideoSimulcastLayers { get; }

        /// <summary>
        /// 屏幕共享simulcast层配置
        /// </summary>
        public IReadOnlyList<VideoParameters> ScreenShareSimulcastLayers { get; }

        /// <summary>
        /// 可伸缩性模式
        /// </summary>
        public string? ScalabilityMode { get; }

        /// <summary>
        /// 备用视频编解码器配置
        /// </summary>
        public BackupVideoCodec BackupVideoCodec { get; }

        /// <summary>
        /// 创建视频发布选项
        /// </summary>
        public VideoPublishOptions(
            string? name = null,
            string? stream = null,
            string? videoCodec = null,
            VideoEncoding? videoEncoding = null,
            VideoEncoding? screenShareEncoding = null,
            bool simulcast = true,
            IEnumerable<VideoParameters>? videoSimulcastLayers = null,
            IEnumerable<VideoParameters>? screenShareSimulcastLayers = null,
            BackupVideoCodec? backupVideoCodec = null,
            string? scalabilityMode = null,
            DegradationPreference? degradationPreference = null)
            : base(name, stream)
        {
            VideoCodec = videoCodec ?? Constants.DefaultVideoCodec;
            VideoEncoding = videoEncoding;
            ScreenShareEncoding = screenShareEncoding;
            Simulcast = simulcast;
            VideoSimulcastLayers = videoSimulcastLayers?.ToList() ?? new List<VideoParameters>();
            ScreenShareSimulcastLayers = screenShareSimulcastLayers?.ToList() ?? new List<VideoParameters>();
            BackupVideoCodec = backupVideoCodec ?? DefaultBackupVideoCodec;
            ScalabilityMode = scalabilityMode;
            DegradationPreference = degradationPreference;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        public VideoPublishOptions CopyWith(
            VideoEncoding? videoEncoding = null,
            VideoEncoding? screenShareEncoding = null,
            bool? simulcast = null,
            IEnumerable<VideoParameters>? videoSimulcastLayers = null,
            IEnumerable<VideoParameters>? screenShareSimulcastLayers = null,
            string? videoCodec = null,
            BackupVideoCodec? backupVideoCodec = null,
            DegradationPreference? degradationPreference = null,
            string? scalabilityMode = null,
            string? name = null,
            string? stream = null)
        {
            return new VideoPublishOptions(
                videoCodec: videoCodec ?? VideoCodec,
                videoEncoding: videoEncoding ?? VideoEncoding,
                screenShareEncoding: screenShareEncoding ?? ScreenShareEncoding,
                simulcast: simulcast ?? Simulcast,
                videoSimulcastLayers: videoSimulcastLayers ?? VideoSimulcastLayers,
                screenShareSimulcastLayers: screenShareSimulcastLayers ?? ScreenShareSimulcastLayers,
                backupVideoCodec: backupVideoCodec ?? BackupVideoCodec,
                degradationPreference: degradationPreference ?? DegradationPreference,
                scalabilityMode: scalabilityMode ?? ScalabilityMode,
                name: name ?? Name,
                stream: stream ?? Stream
            );
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"{GetType().Name}(videoEncoding: {VideoEncoding}, simulcast: {Simulcast})";
    }

    /// <summary>
    /// 视频降级首选项
    /// </summary>
    public enum DegradationPreference
    {
        /// <summary>
        /// 关闭
        /// </summary>
        Disabled,

        /// <summary>
        /// 保持帧率
        /// </summary>
        MaintainFramerate,

        /// <summary>
        /// 保持分辨率
        /// </summary>
        MaintainResolution,

        /// <summary>
        /// 平衡模式
        /// </summary>
        Balanced
    }

    /// <summary>
    /// 备用视频编解码器配置
    /// </summary>
    public class BackupVideoCodec
    {
        /// <summary>
        /// 是否启用备用编解码器
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// 编解码器名称
        /// </summary>
        public string Codec { get; }

        /// <summary>
        /// 编码配置，可选，未设置时将根据尺寸和编解码器计算
        /// </summary>
        public VideoEncoding? Encoding { get; }

        /// <summary>
        /// 是否启用simulcast
        /// </summary>
        public bool Simulcast { get; }

        /// <summary>
        /// 创建备用视频编解码器配置
        /// </summary>
        public BackupVideoCodec(
            bool enabled = true,
            string codec = Constants.DefaultVideoCodec,
            VideoEncoding? encoding = null,
            bool simulcast = true)
        {
            Enabled = enabled;
            Codec = codec;
            Encoding = encoding;
            Simulcast = simulcast;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        public BackupVideoCodec CopyWith(
            bool? enabled = null,
            string? codec = null,
            VideoEncoding? encoding = null,
            bool? simulcast = null)
        {
            return new BackupVideoCodec(
                enabled: enabled ?? Enabled,
                codec: codec ?? Codec,
                encoding: encoding ?? Encoding,
                simulcast: simulcast ?? Simulcast
            );
        }
    }

    /// <summary>
    /// 发布选项基类
    /// </summary>
    public class PublishOptions
    {
        /// <summary>
        /// 轨道名称
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// 设置轨道的流名称。具有相同流名称的音频和视频轨道将被放置在相同的MediaStream中，并提供更好的同步。
        /// 默认情况下，摄像头和麦克风将被放置在一个流中；屏幕共享和屏幕共享音频也是如此。
        /// </summary>
        public string? Stream { get; }

        /// <summary>
        /// 创建发布选项
        /// </summary>
        public PublishOptions(string? name = null, string? stream = null)
        {
            Name = name;
            Stream = stream;
        }
    }

    /// <summary>
    /// 常量定义
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 默认视频编解码器
        /// </summary>
        public const string DefaultVideoCodec = "vp8";
    }

    /// <summary>
    /// 音频发布选项，用于发布音频轨道
    /// </summary>
    public class AudioPublishOptions : PublishOptions
    {
        /// <summary>
        /// 默认麦克风名称
        /// </summary>
        public static readonly string DefaultMicrophoneName = "microphone";

        /// <summary>
        /// 是否启用 DTX（不连续传输）
        /// https://en.wikipedia.org/wiki/Discontinuous_transmission
        /// 默认为 true
        /// </summary>
        public bool Dtx { get; }

        /// <summary>
        /// RED（冗余音频数据）
        /// </summary>
        public bool? Red { get; }

        /// <summary>
        /// 最大音频比特率
        /// </summary>
        public int AudioBitrate { get; }

        /// <summary>
        /// 创建音频发布选项实例
        /// </summary>
        /// <param name="name">轨道名称</param>
        /// <param name="stream">流名称</param>
        /// <param name="dtx">是否启用DTX</param>
        /// <param name="red">是否启用RED</param>
        /// <param name="audioBitrate">音频比特率</param>
        public AudioPublishOptions(
            string? name = null,
            string? stream = null,
            bool dtx = true,
            bool? red = true,
            int audioBitrate = AudioPreset.Music)
            : base(name, stream)
        {
            Dtx = dtx;
            Red = red;
            AudioBitrate = audioBitrate;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        public AudioPublishOptions CopyWith(
            bool? dtx = null,
            int? audioBitrate = null,
            string? name = null,
            string? stream = null,
            bool? red = null)
        {
            return new AudioPublishOptions(
                dtx: dtx ?? Dtx,
                audioBitrate: audioBitrate ?? AudioBitrate,
                name: name ?? Name,
                stream: stream ?? Stream,
                red: red ?? Red
            );
        }

        /// <inheritdoc/>
        public override string ToString() =>
            $"{GetType().Name}(dtx: {Dtx}, audioBitrate: {AudioBitrate}, red: {Red})";
    }

    /// <summary>
    /// 音频预设比特率
    /// </summary>
    public static class AudioPreset
    {
        /// <summary>
        /// 电话质量 (12 kbps)
        /// </summary>
        public const int Telephone = 12000;

        /// <summary>
        /// 语音质量 (24 kbps)
        /// </summary>
        public const int Speech = 24000;

        /// <summary>
        /// 音乐质量 (48 kbps)
        /// </summary>
        public const int Music = 48000;

        /// <summary>
        /// 立体声音乐质量 (64 kbps)
        /// </summary>
        public const int MusicStereo = 64000;

        /// <summary>
        /// 高质量音乐 (96 kbps)
        /// </summary>
        public const int MusicHighQuality = 96000;

        /// <summary>
        /// 高质量立体声音乐 (128 kbps)
        /// </summary>
        public const int MusicHighQualityStereo = 128000;
    }

    /// <summary>
    /// 备用编解码器和视频编解码器的集合与工具
    /// </summary>
    public static class CodecUtils
    {
        /// <summary>
        /// 备用编解码器列表
        /// </summary>
        public static readonly IReadOnlyList<string> BackupCodecs = new List<string> { "vp8", "h264" }.AsReadOnly();

        /// <summary>
        /// 视频编解码器列表
        /// </summary>
        public static readonly IReadOnlyList<string> VideoCodecs = new List<string> { "vp8", "h264", "vp9", "av1" }.AsReadOnly();

        /// <summary>
        /// 检查指定编解码器是否为备用编解码器
        /// </summary>
        /// <param name="codec">要检查的编解码器</param>
        /// <returns>如果是备用编解码器则返回true</returns>
        public static bool IsBackupCodec(string codec)
        {
            return BackupCodecs.Contains(codec.ToLowerInvariant());
        }
    }
}
