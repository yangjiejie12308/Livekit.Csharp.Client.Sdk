using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.Internal;
using Client.Sdk.Dotnet.managers;
using Client.Sdk.Dotnet;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;
using Client.Sdk.Dotnet.types;
using System.Diagnostics;
using DirectShowLib.BDA;


namespace Client.Sdk.Dotnet.track
{
    /// <summary>
    /// MediaStreamTrack 的包装器，带有附加元数据。
    /// 是 AudioTrack 和 VideoTrack 的基类，不能直接实例化。
    /// </summary>
    public abstract class Track : EventsEmittableBase<TrackEvent>, IDisposable
    {
        private static readonly Guid _uuid = Guid.NewGuid();
        private MediaStream _mediaStream;
        private SIPSorcery.Net.MediaStreamTrack _mediaStreamTrack;
        private SIPSorcery.Net.MediaStreamTrack? _originalTrack;
        private string? _cid;
        private bool _active = false;
        private bool _muted = false;
        private Timer? _monitorTimer;

        /// <summary>
        /// 轨道类型
        /// </summary>
        public types.TrackType Kind { get; }

        /// <summary>
        /// 轨道来源
        /// </summary>
        public TrackSource Source { get; }

        /// <summary>
        /// 只读媒体流
        /// </summary>
        public MediaStream MediaStream => _mediaStream;

        /// <summary>
        /// 只读媒体轨道
        /// </summary>
        public SIPSorcery.Net.MediaStreamTrack MediaStreamTrack => _mediaStreamTrack;

        /// <summary>
        /// 服务器分配的轨道ID
        /// </summary>
        public string? Sid { get; set; }

        /// <summary>
        /// WebRTC 收发器
        /// </summary>
        public RTCPReceiverReport? Transceiver { get; set; }

        /// <summary>
        /// 轨道是否处于活动状态
        /// </summary>
        public bool IsActive => _active;

        /// <summary>
        /// 轨道是否已静音
        /// </summary>
        public bool Muted => _muted;

        /// <summary>
        /// WebRTC 发送器
        /// </summary>
        //public RTCPSenderReport? Sender => Transceiver?.;

        /// <summary>
        /// WebRTC 接收器
        /// </summary>
        public RTCPReceiverReport? Receiver { get; set; }

        /// <summary>
        /// 事件管理器
        /// </summary>
        public EventsEmitter<TrackEvent> Events { get; }

        /// <summary>
        /// 创建轨道实例
        /// </summary>
        /// <param name="kind">轨道类型</param>
        /// <param name="source">轨道来源</param>
        /// <param name="mediaStream">媒体流</param>
        /// <param name="mediaStreamTrack">媒体轨道</param>
        /// <param name="receiver">接收器，可选</param>
        /// <param name="logger">日志记录器，可选</param>
        protected Track(
            types.TrackType kind,
            TrackSource source,
            MediaStream mediaStream,
            SIPSorcery.Net.MediaStreamTrack mediaStreamTrack,
            RTCPReceiverReport? receiver = null,
            ILogger<Track>? logger = null)
        {
            Kind = kind;
            Source = source;
            _mediaStream = mediaStream;
            _mediaStreamTrack = mediaStreamTrack;
            Receiver = receiver;
            Events = new EventsEmitter<TrackEvent>();

            Events.Listen(async (events) =>
            {
                Debug.WriteLine($"[{GetHashCode()}] Track event received: {events}");
                await Task.CompletedTask; // 确保所有代码路径都返回一个 Task
            });
        }

        /// <summary>
        /// 获取媒体类型
        /// </summary>
        public SDPMediaTypesEnum MediaType
        {
            get
            {
                return Kind switch
                {
                    types.TrackType.Audio => SDPMediaTypesEnum.audio,
                    types.TrackType.Video => SDPMediaTypesEnum.video,
                    types.TrackType.Data => SDPMediaTypesEnum.data,
                    _ => SDPMediaTypesEnum.audio
                };
            }
        }

        /// <summary>
        /// 获取客户端ID
        /// </summary>
        /// <returns>客户端ID</returns>
        public string GetCid()
        {
            var cid = _cid ?? MediaStreamTrack.SeqNum.ToString();

            if (string.IsNullOrEmpty(cid))
            {
                cid = Guid.NewGuid().ToString();
                _cid = cid;
            }
            return cid;
        }

        /// <summary>
        /// 启动轨道（如果尚未启动）
        /// </summary>
        /// <returns>如果已启动则返回true，如果已经处于启动状态则返回false</returns>
        public virtual async Task<bool> Start()
        {
            if (_active)
            {
                // 已经启动
                return false;
            }

            Debug.Assert(MediaStreamTrack != null, "MediaStreamTrack should not be null when starting the track.");

            StartMonitor();

            await OnStarted();

            _active = true;
            return true;
        }

        /// <summary>
        /// 停止轨道（如果尚未停止）
        /// </summary>
        /// <returns>如果已停止则返回true，如果已经处于停止状态则返回false</returns>
        public virtual async Task<bool> Stop()
        {
            if (!_active)
            {
                // 已经停止
                return false;
            }

            StopMonitor();

            await OnStopped();

            Debug.Assert(MediaStreamTrack != null, "MediaStreamTrack should not be null when stopping the track.");

            //// Web平台特殊处理
            //if (!PlatformInfo.IsWeb)
            //{
            //    await MediaStreamTrack.di
            //}

            //if (_originalTrack != null)
            //{
            //    await _originalTrack.Stop();
            //    _originalTrack = null;
            //}

            _active = false;
            return true;
        }

        /// <summary>
        /// 启用轨道
        /// </summary>
        public async Task Enable()
        {
            Debug.WriteLine($"[{GetHashCode()}] Enable() enabling {MediaStreamTrack.GetHashCode()}...");
            try
            {
                if (_active)
                {
                    //MediaStreamTrack.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetHashCode()}] Set MediaStreamTrack.Enabled threw exception: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 禁用轨道
        /// </summary>
        public async Task Disable()
        {
            Debug.WriteLine($"[{GetHashCode()}] Disable() disabling {MediaStreamTrack.GetHashCode()}...");
            try
            {
                if (_active)
                {
                    //MediaStreamTrack.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{GetHashCode()}] Set MediaStreamTrack.Enabled threw exception: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 监控统计信息
        /// </summary>
        /// <returns>如果应继续监控则为true，否则为false</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal abstract Task<bool> MonitorStats();

        /// <summary>
        /// 轨道启动时的钩子方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal virtual Task OnStarted() => Task.CompletedTask;

        /// <summary>
        /// 轨道停止时的钩子方法
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal virtual Task OnStopped() => Task.CompletedTask;

        /// <summary>
        /// 启动监控
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal void StartMonitor()
        {
            const int monitorFrequency = 2000; // 假设常量值，实际应从配置中获取
            _monitorTimer ??= new Timer(async _ =>
            {
                if (!await MonitorStats())
                {
                    StopMonitor();
                }
            }, null, 0, monitorFrequency);
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal void StopMonitor()
        {
            _monitorTimer?.Dispose();
            _monitorTimer = null;
        }

        /// <summary>
        /// 更新静音状态
        /// </summary>
        /// <param name="muted">是否静音</param>
        /// <param name="shouldNotify">是否应通知</param>
        /// <param name="shouldSendSignal">是否应发送信号</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal void UpdateMuted(
            bool muted,
            bool shouldNotify = true,
            bool shouldSendSignal = false)
        {
            if (_muted == muted) return;
            _muted = muted;
            if (shouldNotify)
            {
                Events.Emit(new InternalTrackMuteUpdatedEvent(
                    this,
                    muted,
                    shouldSendSignal));
            }
        }

        /// <summary>
        /// 更新媒体流和轨道
        /// </summary>
        /// <param name="stream">新的媒体流</param>
        /// <param name="track">新的媒体轨道</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal void UpdateMediaStreamAndTrack(
            MediaStream stream,
            SIPSorcery.Net.MediaStreamTrack track)
        {
            _mediaStream = stream;
            _mediaStreamTrack = track;
            Events.Emit(new TrackStreamUpdatedEvent(
                this,
                stream));
        }

        /// <summary>
        /// 设置处理后的轨道
        /// </summary>
        /// <param name="track">处理后的媒体轨道</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal void SetProcessedTrack(SIPSorcery.Net.MediaStreamTrack track)
        {
            _originalTrack = _mediaStreamTrack;
            _mediaStreamTrack = track;
        }

        public async void Dispose()
        {
            Debug.Assert(_mediaStreamTrack != null, "MediaStreamTrack should not be null when disposing the track.");
            await Stop();
            // 释放事件
            await Events.DisposeAsync();
        }
    }


}