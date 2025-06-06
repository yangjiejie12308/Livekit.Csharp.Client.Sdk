using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.support;
using Client.Sdk.Dotnet.track;
using Client.Sdk.Dotnet.track.lcoal;
using LiveKit.Proto;
using SIPSorcery.Net;

namespace Client.Sdk.Dotnet.Internal
{
    /// <summary>
    /// LiveKit 内部事件基础接口
    /// </summary>
    internal interface IInternalEvent : LiveKitEvent { }

    /// <summary>
    /// 引擎对等连接状态更新事件的抽象基类
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal abstract class EnginePeerStateUpdatedEvent : EngineEvent, IInternalEvent
    {
        /// <summary>
        /// WebRTC 对等连接状态
        /// </summary>
        public RTCPeerConnectionState State { get; }

        /// <summary>
        /// 是否为主要连接
        /// </summary>
        public bool IsPrimary { get; }

        /// <summary>
        /// 创建引擎对等连接状态更新事件
        /// </summary>
        /// <param name="state">连接状态</param>
        /// <param name="isPrimary">是否为主要连接</param>
        protected EnginePeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
        {
            State = state;
            IsPrimary = isPrimary;
        }
    }

    /// <summary>
    /// 订阅者对等连接状态更新事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class EngineSubscriberPeerStateUpdatedEvent : EnginePeerStateUpdatedEvent
    {
        /// <summary>
        /// 创建订阅者对等连接状态更新事件
        /// </summary>
        /// <param name="state">连接状态</param>
        /// <param name="isPrimary">是否为主要连接</param>
        public EngineSubscriberPeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
            : base(state, isPrimary)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{GetType().Name}(state: {State}, isPrimary: {IsPrimary})";
        }
    }

    /// <summary>
    /// 发布者对等连接状态更新事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class EnginePublisherPeerStateUpdatedEvent : EnginePeerStateUpdatedEvent
    {
        /// <summary>
        /// 创建发布者对等连接状态更新事件
        /// </summary>
        /// <param name="state">连接状态</param>
        /// <param name="isPrimary">是否为主要连接</param>
        public EnginePublisherPeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
            : base(state, isPrimary)
        {
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{GetType().Name}(state: {State}, isPrimary: {IsPrimary})";
        }
    }

    /// <summary>
    /// 轨道流更新事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class TrackStreamUpdatedEvent : TrackEvent, IInternalEvent
    {
        /// <summary>
        /// 关联的轨道
        /// </summary>
        public Track Track { get; }

        /// <summary>
        /// 媒体流
        /// </summary>
        public MediaStream Stream { get; }

        /// <summary>
        /// 创建轨道流更新事件
        /// </summary>
        /// <param name="track">轨道</param>
        /// <param name="stream">媒体流</param>
        public TrackStreamUpdatedEvent(Track track, MediaStream stream)
        {
            Track = track;
            Stream = stream;
        }
    }

    /// <summary>
    /// 音频播放开始事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class AudioPlaybackStarted : TrackEvent, EngineEvent, IInternalEvent
    {
        /// <summary>
        /// 关联的轨道
        /// </summary>
        public Track Track { get; }

        /// <summary>
        /// 创建音频播放开始事件
        /// </summary>
        /// <param name="track">轨道</param>
        public AudioPlaybackStarted(Track track)
        {
            Track = track;
        }
    }

    /// <summary>
    /// 音频播放失败事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class AudioPlaybackFailed : TrackEvent, EngineEvent, IInternalEvent
    {
        /// <summary>
        /// 关联的轨道
        /// </summary>
        public Track Track { get; }

        /// <summary>
        /// 创建音频播放失败事件
        /// </summary>
        /// <param name="track">轨道</param>
        public AudioPlaybackFailed(Track track)
        {
            Track = track;
        }
    }

    /// <summary>
    /// 本地轨道选项更新事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class LocalTrackOptionsUpdatedEvent : TrackEvent, IInternalEvent
    {
        /// <summary>
        /// 关联的本地轨道
        /// </summary>
        public LocalTrack Track { get; }

        /// <summary>
        /// 更新的轨道选项
        /// </summary>
        public LocalTrackOptions Options { get; }

        /// <summary>
        /// 创建本地轨道选项更新事件
        /// </summary>
        /// <param name="track">本地轨道</param>
        /// <param name="options">轨道选项</param>
        public LocalTrackOptionsUpdatedEvent(LocalTrack track, LocalTrackOptions options)
        {
            Track = track;
            Options = options;
        }
    }

    /// <summary>
    /// 内部轨道静音状态更新事件，用于从轨道通知到轨道发布
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class InternalTrackMuteUpdatedEvent : TrackEvent, IInternalEvent
    {
        /// <summary>
        /// 关联的轨道
        /// </summary>
        public Track Track { get; }

        /// <summary>
        /// 静音状态
        /// </summary>
        public bool Muted { get; }

        /// <summary>
        /// 是否应发送信令
        /// </summary>
        public bool ShouldSendSignal { get; }

        /// <summary>
        /// 创建内部轨道静音状态更新事件
        /// </summary>
        /// <param name="track">轨道</param>
        /// <param name="muted">静音状态</param>
        /// <param name="shouldSendSignal">是否发送信令</param>
        public InternalTrackMuteUpdatedEvent(Track track, bool muted, bool shouldSendSignal)
        {
            Track = track;
            Muted = muted;
            ShouldSendSignal = shouldSendSignal;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"TrackMuteUpdatedEvent(track: {Track}, muted: {Muted})";
        }
    }

    //
    // 信令事件
    //

    /// <summary>
    /// 收到服务器加入响应的事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class SignalJoinResponseEvent : SignalEvent, IInternalEvent
    {
        /// <summary>
        /// 加入响应数据
        /// </summary>
        public JoinResponse Response { get; }

        /// <summary>
        /// 创建信令加入响应事件
        /// </summary>
        /// <param name="response">加入响应数据</param>
        public SignalJoinResponseEvent(JoinResponse response)
        {
            Response = response;
        }
    }

    /// <summary>
    /// 收到服务器重连响应的事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class SignalReconnectResponseEvent : SignalEvent, IInternalEvent
    {
        /// <summary>
        /// 重连响应数据
        /// </summary>
        public ReconnectResponse Response { get; }

        /// <summary>
        /// 创建信令重连响应事件
        /// </summary>
        /// <param name="response">重连响应数据</param>
        public SignalReconnectResponseEvent(ReconnectResponse response)
        {
            Response = response;
        }
    }

    /// <summary>
    /// 连接状态变更事件
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal sealed class SignalConnectivityChangedEvent : SignalEvent, IInternalEvent
    {
        /// <summary>
        /// 旧的连接状态
        /// </summary>
        public List<ConnectivityResult> OldState { get; }

        /// <summary>
        /// 新的连接状态
        /// </summary>
        public List<ConnectivityResult> State { get; }

        /// <summary>
        /// 创建连接状态变更事件
        /// </summary>
        /// <param name="oldState">旧的连接状态</param>
        /// <param name="state">新的连接状态</param>
        public SignalConnectivityChangedEvent(List<ConnectivityResult> oldState, List<ConnectivityResult> state)
        {
            OldState = oldState;
            State = state;
        }
    }
}
