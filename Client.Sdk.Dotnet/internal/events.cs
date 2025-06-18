using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Sdk.Dotnet.support;
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
