using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.support;
using Client.Sdk.Dotnet.types;

namespace Client.Sdk.Dotnet.track
{
    public abstract class LocalTrackOptions
    {
        public abstract Dictionary<string, object> ToMediaConstraintsMap();
        protected LocalTrackOptions()
        {

        }
    }

    /// <summary>
    /// 用于创建捕获相机的本地视频轨道的选项
    /// </summary>
    public class CameraCaptureOptions : VideoCaptureOptions
    {
        /// <summary>
        /// 相机位置
        /// </summary>
        public CameraPosition CameraPosition { get; }

        /// <summary>
        /// 设置为 false 则仅切换启用状态而非停止/替换轨道来实现静音
        /// </summary>
        public bool StopCameraCaptureOnMute { get; }

        /// <summary>
        /// 相机使用的聚焦模式
        /// </summary>
        public CameraFocusMode FocusMode { get; }

        /// <summary>
        /// 相机使用的曝光模式
        /// </summary>
        public CameraExposureMode ExposureMode { get; }

        /// <summary>
        /// 创建相机捕获选项实例
        /// </summary>
        /// <param name="cameraPosition">相机位置，默认为前置</param>
        /// <param name="focusMode">聚焦模式，默认为自动</param>
        /// <param name="exposureMode">曝光模式，默认为自动</param>
        /// <param name="deviceId">设备ID</param>
        /// <param name="maxFrameRate">最大帧率</param>
        /// <param name="params">视频参数，默认为720p 16:9</param>
        /// <param name="stopCameraCaptureOnMute">静音时是否停止相机捕获，默认为true</param>
        /// <param name="processor">视频处理器</param>
        public CameraCaptureOptions(
            CameraPosition cameraPosition = CameraPosition.Front,
            CameraFocusMode focusMode = CameraFocusMode.Auto,
            CameraExposureMode exposureMode = CameraExposureMode.Auto,
            string? deviceId = null,
            double? maxFrameRate = null,
            VideoParameters? @params = null,
            bool stopCameraCaptureOnMute = true,
            TrackProcessor<VideoProcessorOptions, VideoTrackType>? processor = null)
            : base(
                @params ?? VideoParametersPresets.H720_169,
                deviceId,
                maxFrameRate,
                processor)
        {
            CameraPosition = cameraPosition;
            FocusMode = focusMode;
            ExposureMode = exposureMode;
            StopCameraCaptureOnMute = stopCameraCaptureOnMute;
        }

        /// <summary>
        /// 从视频捕获选项创建相机捕获选项
        /// </summary>
        /// <param name="captureOptions">基础视频捕获选项</param>
        public CameraCaptureOptions(VideoCaptureOptions captureOptions)
            : base(
                captureOptions.Parameters,
                captureOptions.DeviceId,
                captureOptions.MaxFrameRate)
        {
            CameraPosition = CameraPosition.Front;
            FocusMode = CameraFocusMode.Auto;
            ExposureMode = CameraExposureMode.Auto;
            StopCameraCaptureOnMute = true;
        }

        /// <inheritdoc/>
        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = new Dictionary<string, object>(base.ToMediaConstraintsMap());

            // 添加相机朝向设置
            if (DeviceId == null)
            {
                constraints["facingMode"] = CameraPosition == CameraPosition.Front ? "user" : "environment";
            }

            // 添加设备ID
            if (!string.IsNullOrEmpty(DeviceId))
            {

                constraints["optional"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        ["sourceId"] = DeviceId!
                    }
                };
            }

            // 添加帧率设置
            if (MaxFrameRate.HasValue)
            {
                constraints["frameRate"] = new Dictionary<string, object>
                {
                    ["max"] = MaxFrameRate.Value
                };
            }

            return constraints;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        /// <param name="params">更新的视频参数</param>
        /// <param name="cameraPosition">更新的相机位置</param>
        /// <param name="deviceId">更新的设备ID</param>
        /// <param name="maxFrameRate">更新的最大帧率</param>
        /// <param name="stopCameraCaptureOnMute">更新的静音时是否停止捕获</param>
        /// <returns>新的相机捕获选项实例</returns>
        public CameraCaptureOptions CopyWith(
            VideoParameters? @params = null,
            CameraPosition? cameraPosition = null,
            string? deviceId = null,
            double? maxFrameRate = null,
            bool? stopCameraCaptureOnMute = null)
        {
            return new CameraCaptureOptions(
                cameraPosition: cameraPosition ?? CameraPosition,
                focusMode: FocusMode,
                exposureMode: ExposureMode,
                deviceId: deviceId ?? DeviceId,
                maxFrameRate: maxFrameRate ?? MaxFrameRate,
                @params: @params ?? Parameters,
                stopCameraCaptureOnMute: stopCameraCaptureOnMute ?? StopCameraCaptureOnMute,
                processor: Processor
            );
        }
    }


    /// <summary>
    /// 相机位置类型
    /// </summary>
    public enum CameraPosition
    {
        /// <summary>
        /// 前置相机
        /// </summary>
        Front,

        /// <summary>
        /// 后置相机
        /// </summary>
        Back
    }

    /// <summary>
    /// 相机聚焦模式
    /// </summary>
    public enum CameraFocusMode
    {
        /// <summary>
        /// 自动聚焦
        /// </summary>
        Auto,

        /// <summary>
        /// 锁定聚焦
        /// </summary>
        Locked
    }

    /// <summary>
    /// 相机曝光模式
    /// </summary>
    public enum CameraExposureMode
    {
        /// <summary>
        /// 自动曝光
        /// </summary>
        Auto,

        /// <summary>
        /// 锁定曝光
        /// </summary>
        Locked
    }

    /// <summary>
    /// 相机位置扩展方法
    /// </summary>
    public static class CameraPositionExtensions
    {
        /// <summary>
        /// 切换前置/后置相机
        /// </summary>
        /// <param name="position">当前相机位置</param>
        /// <returns>切换后的位置</returns>
        public static CameraPosition Switched(this CameraPosition position)
        {
            return position == CameraPosition.Front
                ? CameraPosition.Back
                : CameraPosition.Front;
        }
    }
    /// <summary>
    /// Base class for options when creating a LocalVideoTrack.
    /// </summary>
    public abstract class VideoCaptureOptions : LocalTrackOptions
    {
        /// <summary>
        /// Video parameters for this capture.
        /// </summary>
        public VideoParameters Parameters { get; }

        /// <summary>
        /// The deviceId of the capture device to use.
        /// Available deviceIds can be obtained through platform-specific methods.
        /// </summary>
        /// <remarks>
        /// For example:
        /// <code>
        /// var devices = await MediaDevices.EnumerateDevices();
        /// // or
        /// var desktopSources = await DesktopCapturer.GetSources(new[] { SourceType.Screen, SourceType.Window });
        /// </code>
        /// </remarks>
        public string? DeviceId { get; }

        /// <summary>
        /// Limit the maximum frameRate of the capture device.
        /// </summary>
        public double? MaxFrameRate { get; }

        /// <summary>
        /// A processor to apply to the video track.
        /// </summary>
        public TrackProcessor<VideoProcessorOptions, VideoTrackType>? Processor { get; }

        /// <summary>
        /// Creates a new instance of VideoCaptureOptions.
        /// </summary>
        /// <param name="parameters">Video parameters to use. Defaults to h540_169.</param>
        /// <param name="deviceId">Optional device ID to capture from.</param>
        /// <param name="maxFrameRate">Optional maximum frame rate limit.</param>
        /// <param name="processor">Optional video processor to apply.</param>
        protected VideoCaptureOptions(
            VideoParameters? parameters = null,
            string? deviceId = null,
            double? maxFrameRate = null,
            TrackProcessor<VideoProcessorOptions, VideoTrackType>? processor = null)
        {
            Parameters = parameters ?? VideoParametersPresets.H540_169;
            DeviceId = deviceId;
            MaxFrameRate = maxFrameRate;
            Processor = processor;
        }

        /// <summary>
        /// Converts the options to media constraints map.
        /// </summary>
        /// <returns>A dictionary containing the media constraints.</returns>
        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            return Parameters.ToMediaConstraintsMap();
        }
    }

    public class ScreenShareCaptureOptions : VideoCaptureOptions
    {
        /// <summary>
        /// iOS 专用标志：使用广播扩展进行屏幕共享捕获。
        /// 有关如何设置广播扩展的说明，请参阅：
        /// https://github.com/flutter-webrtc/flutter-webrtc/wiki/iOS-Screen-Sharing#broadcast-extension-quick-setup
        /// </summary>
        public bool UseIOSBroadcastExtension { get; }

        /// <summary>
        /// 仅用于浏览器：如果为 true，将捕获屏幕音频。
        /// </summary>
        public bool CaptureScreenAudio { get; }

        /// <summary>
        /// 仅用于浏览器：如果为 true，将捕获当前标签页。
        /// </summary>
        public bool PreferCurrentTab { get; }

        /// <summary>
        /// 仅用于浏览器：包含或排除自身浏览器表面。
        /// </summary>
        public string? SelfBrowserSurface { get; }

        /// <summary>
        /// 创建屏幕共享捕获选项实例
        /// </summary>
        public ScreenShareCaptureOptions(
            bool useIOSBroadcastExtension = false,
            bool captureScreenAudio = false,
            bool preferCurrentTab = false,
            string? selfBrowserSurface = null,
            string? sourceId = null,
            double? maxFrameRate = null,
            VideoParameters? @params = null)
            : base(
                @params ?? VideoParametersPresets.ScreenShareH1080FPS15,
                sourceId,
                maxFrameRate)
        {
            UseIOSBroadcastExtension = useIOSBroadcastExtension;
            CaptureScreenAudio = captureScreenAudio;
            PreferCurrentTab = preferCurrentTab;
            SelfBrowserSurface = selfBrowserSurface;
        }

        /// <summary>
        /// 从基本视频捕获选项创建屏幕共享捕获选项
        /// </summary>
        public ScreenShareCaptureOptions(
            VideoCaptureOptions captureOptions,
            bool useIOSBroadcastExtension = false,
            bool captureScreenAudio = false,
            bool preferCurrentTab = false,
            string? selfBrowserSurface = null)
            : base(captureOptions.Parameters, captureOptions.DeviceId, captureOptions.MaxFrameRate)
        {
            UseIOSBroadcastExtension = useIOSBroadcastExtension;
            CaptureScreenAudio = captureScreenAudio;
            PreferCurrentTab = preferCurrentTab;
            SelfBrowserSurface = selfBrowserSurface;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        public ScreenShareCaptureOptions CopyWith(
            bool? useIOSBroadcastExtension = null,
            bool? captureScreenAudio = null,
            VideoParameters? @params = null,
            string? sourceId = null,
            double? maxFrameRate = null,
            bool? preferCurrentTab = null,
            string? selfBrowserSurface = null)
        {
            return new ScreenShareCaptureOptions(
                useIOSBroadcastExtension: useIOSBroadcastExtension ?? UseIOSBroadcastExtension,
                captureScreenAudio: captureScreenAudio ?? CaptureScreenAudio,
                @params: @params ?? Parameters,
                sourceId: sourceId ?? DeviceId,
                maxFrameRate: maxFrameRate ?? MaxFrameRate,
                preferCurrentTab: preferCurrentTab ?? PreferCurrentTab,
                selfBrowserSurface: selfBrowserSurface ?? SelfBrowserSurface
            );
        }

        /// <inheritdoc/>
        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = base.ToMediaConstraintsMap();


            if (DeviceId != null)
            {
                constraints["deviceId"] = new Dictionary<string, object> { ["exact"] = DeviceId };
            }

            if (MaxFrameRate.HasValue && MaxFrameRate.Value != 0.0)
            {
                constraints["mandatory"] = new Dictionary<string, object> { ["frameRate"] = MaxFrameRate.Value };
            }

            return constraints;
        }
    }
    /// <summary>
    /// 音频捕获选项，用于创建本地音频轨道
    /// </summary>
    public class AudioCaptureOptions : LocalTrackOptions
    {
        /// <summary>
        /// 要使用的捕获设备的设备ID。
        /// 可通过平台特定的媒体设备API获取可用设备列表。
        /// </summary>
        public string? DeviceId { get; }

        /// <summary>
        /// 尝试使用噪声抑制选项（如果平台支持）。
        /// 参见 https://developer.mozilla.org/en-US/docs/Web/API/MediaTrackSettings/noiseSuppression
        /// 默认为 true。
        /// </summary>
        public bool NoiseSuppression { get; }

        /// <summary>
        /// 尝试使用回声消除选项（如果平台支持）。
        /// 参见 https://developer.mozilla.org/en-US/docs/Web/API/MediaTrackSettings/echoCancellation
        /// 默认为 true。
        /// </summary>
        public bool EchoCancellation { get; }

        /// <summary>
        /// 尝试使用自动增益控制选项（如果平台支持）。
        /// 参见 https://developer.mozilla.org/en-US/docs/Web/API/MediaTrackConstraints/autoGainControl
        /// 默认为 true。
        /// </summary>
        public bool AutoGainControl { get; }

        /// <summary>
        /// 尝试使用高通滤波器选项（如果平台支持）。
        /// 默认为 false。
        /// </summary>
        public bool HighPassFilter { get; }

        /// <summary>
        /// 尝试使用打字噪声检测选项（如果平台支持）。
        /// 默认为 true。
        /// </summary>
        public bool TypingNoiseDetection { get; }

        /// <summary>
        /// 尝试使用语音隔离选项（如果平台支持）。
        /// 默认为 true。
        /// </summary>
        public bool VoiceIsolation { get; }

        /// <summary>
        /// 当静音时是否停止音频捕获。设置为false仅切换启用状态而不是停止或替换轨道。
        /// 默认为 true。
        /// </summary>
        public bool StopAudioCaptureOnMute { get; }

        /// <summary>
        /// 应用于音频轨道的处理器。
        /// </summary>
        public TrackProcessor<AudioProcessorOptions, AudioTrackType>? Processor { get; }

        /// <summary>
        /// 创建音频捕获选项实例
        /// </summary>
        public AudioCaptureOptions(
            string? deviceId = null,
            bool noiseSuppression = true,
            bool echoCancellation = true,
            bool autoGainControl = true,
            bool highPassFilter = false,
            bool voiceIsolation = true,
            bool typingNoiseDetection = true,
            bool stopAudioCaptureOnMute = true,
            TrackProcessor<AudioProcessorOptions, AudioTrackType>? processor = null)
        {
            DeviceId = deviceId;
            NoiseSuppression = noiseSuppression;
            EchoCancellation = echoCancellation;
            AutoGainControl = autoGainControl;
            HighPassFilter = highPassFilter;
            VoiceIsolation = voiceIsolation;
            TypingNoiseDetection = typingNoiseDetection;
            StopAudioCaptureOnMute = stopAudioCaptureOnMute;
            Processor = processor;
        }

        /// <inheritdoc/>
        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = new Dictionary<string, object>();

            if (Native.BypassVoiceProcessing)
            {
                constraints["optional"] = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["googEchoCancellation"] = false },
                new Dictionary<string, object> { ["googEchoCancellation2"] = false },
                new Dictionary<string, object> { ["googNoiseSuppression"] = false },
                new Dictionary<string, object> { ["googNoiseSuppression2"] = false },
                new Dictionary<string, object> { ["googAutoGainControl"] = false },
                new Dictionary<string, object> { ["googHighpassFilter"] = false },
                new Dictionary<string, object> { ["googTypingNoiseDetection"] = false },
                new Dictionary<string, object> { ["noiseSuppression"] = false },
                new Dictionary<string, object> { ["echoCancellation"] = false },
                new Dictionary<string, object> { ["autoGainControl"] = false },
                new Dictionary<string, object> { ["voiceIsolation"] = false },
                new Dictionary<string, object> { ["googDAEchoCancellation"] = false }
            };
            }
            else
            {
                //// 在Web平台上，无法同时提供可选和必选参数
                //// deviceId是必选参数
                //if (!PlatformHelper.IsWeb || (PlatformHelper.IsWeb && DeviceId == null))
                //{
                constraints["optional"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { ["echoCancellation"] = EchoCancellation },
                    new Dictionary<string, object> { ["noiseSuppression"] = NoiseSuppression },
                    new Dictionary<string, object> { ["autoGainControl"] = AutoGainControl },
                    new Dictionary<string, object> { ["voiceIsolation"] = NoiseSuppression },
                    new Dictionary<string, object> { ["googDAEchoCancellation"] = EchoCancellation },
                    new Dictionary<string, object> { ["googEchoCancellation"] = EchoCancellation },
                    new Dictionary<string, object> { ["googEchoCancellation2"] = EchoCancellation },
                    new Dictionary<string, object> { ["googNoiseSuppression"] = NoiseSuppression },
                    new Dictionary<string, object> { ["googNoiseSuppression2"] = NoiseSuppression },
                    new Dictionary<string, object> { ["googAutoGainControl"] = AutoGainControl },
                    new Dictionary<string, object> { ["googHighpassFilter"] = HighPassFilter },
                    new Dictionary<string, object> { ["googTypingNoiseDetection"] = TypingNoiseDetection }
                };
                //}
            }

            if (!string.IsNullOrEmpty(DeviceId))
            {
                //if (PlatformHelper.IsWeb)
                //{
                //    if (PlatformHelper.IsChrome129OrLater())
                //    {
                //        constraints["deviceId"] = new Dictionary<string, object> { ["exact"] = DeviceId };
                //    }
                //    else
                //    {
                //        constraints["deviceId"] = new Dictionary<string, object> { ["ideal"] = DeviceId };
                //    }
                //}
                //else
                //{
                var optionalList = (List<Dictionary<string, object>>)constraints["optional"];
                optionalList.Add(new Dictionary<string, object> { ["sourceId"] = DeviceId });
                //}
            }

            return constraints;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        public AudioCaptureOptions CopyWith(
            string? deviceId = null,
            bool? noiseSuppression = null,
            bool? echoCancellation = null,
            bool? autoGainControl = null,
            bool? highPassFilter = null,
            bool? typingNoiseDetection = null,
            bool? voiceIsolation = null,
            bool? stopAudioCaptureOnMute = null,
            TrackProcessor<AudioProcessorOptions, AudioTrackType>? processor = null)
        {
            return new AudioCaptureOptions(
                deviceId: deviceId ?? DeviceId,
                noiseSuppression: noiseSuppression ?? NoiseSuppression,
                echoCancellation: echoCancellation ?? EchoCancellation,
                autoGainControl: autoGainControl ?? AutoGainControl,
                highPassFilter: highPassFilter ?? HighPassFilter,
                typingNoiseDetection: typingNoiseDetection ?? TypingNoiseDetection,
                voiceIsolation: voiceIsolation ?? VoiceIsolation,
                stopAudioCaptureOnMute: stopAudioCaptureOnMute ?? StopAudioCaptureOnMute,
                processor: processor ?? Processor
            );
        }
    }

    /// <summary>
    /// 音频输出设备选项
    /// </summary>
    public class AudioOutputOptions
    {
        /// <summary>
        /// 要使用的输出设备的设备ID
        /// </summary>
        public string? DeviceId { get; }

        /// <summary>
        /// 如果为true，音频将通过扬声器播放
        /// 仅适用于移动平台
        /// </summary>
        public bool? SpeakerOn { get; }

        /// <summary>
        /// 创建音频输出选项实例
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="speakerOn">是否使用扬声器</param>
        public AudioOutputOptions(string? deviceId = null, bool? speakerOn = null)
        {
            DeviceId = deviceId;
            SpeakerOn = speakerOn;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        /// <param name="deviceId">新的设备ID，为null则使用当前值</param>
        /// <param name="speakerOn">新的扬声器状态，为null则使用当前值</param>
        /// <returns>新的音频输出选项实例</returns>
        public AudioOutputOptions CopyWith(string? deviceId = null, bool? speakerOn = null)
        {
            return new AudioOutputOptions(
                deviceId: deviceId ?? DeviceId,
                speakerOn: speakerOn ?? SpeakerOn
            );
        }
    }
}
