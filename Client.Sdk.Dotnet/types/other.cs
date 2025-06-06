using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.types
{
    // C# 等价于 Dart 的 CancelListenFunc
    public delegate Task CancelListenFunc();

    /// <summary>
    /// Protocol version to use when connecting to server.
    /// Usually it's not recommended to change this.
    /// </summary>
    public enum ProtocolVersion
    {
        v2,
        v3, // Subscriber as primary
        v4,
        v5,
        v6, // Session migration
        v7, // Remote unpublish
        v8,
        v9,
        v10,
        v11,
        v12
    }

    /// <summary>
    /// Connection state type used throughout the SDK.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Reconnecting,
        Connected
    }

    /// <summary>
    /// The type of participant.
    /// </summary>
    public enum ParticipantKind
    {
        Standard,
        Ingress,
        Egress,
        Sip,
        Agent
    }

    /// <summary>
    /// The type of track.
    /// </summary>
    public enum TrackType
    {
        Audio,
        Video,
        Data
    }

    /// <summary>
    /// Video quality used for publishing video tracks.
    /// </summary>
    public enum VideoQuality
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Connection quality between the Participant and server.
    /// </summary>
    public enum ConnectionQuality
    {
        Unknown,
        Lost,
        Poor,
        Good,
        Excellent
    }

    /// <summary>
    /// Reliability used for publishing data through data channel.
    /// </summary>
    public enum Reliability
    {
        Reliable,
        Lossy
    }

    /// <summary>
    /// Track sources
    /// </summary>
    public enum TrackSource
    {
        Unknown,
        Camera,
        Microphone,
        ScreenShareVideo,
        ScreenShareAudio
    }

    /// <summary>
    /// Track subscription states
    /// </summary>
    public enum TrackSubscriptionState
    {
        Unsubscribed,
        Subscribed,
        NotAllowed
    }

    /// <summary>
    /// The state of track data stream.
    /// This is controlled by server to optimize bandwidth.
    /// </summary>
    public enum StreamState
    {
        Paused,
        Active
    }

    /// <summary>
    /// Reasons for disconnection
    /// </summary>
    public enum DisconnectReason
    {
        Unknown,
        ClientInitiated,
        DuplicateIdentity,
        ServerShutdown,
        ParticipantRemoved,
        RoomDeleted,
        StateMismatch,
        JoinFailure,
        Disconnected,
        SignalingConnectionFailure,
        ReconnectAttemptsExceeded
    }

    /// <summary>
    /// The reason why a track failed to publish.
    /// </summary>
    public enum TrackSubscribeFailReason
    {
        InvalidServerResponse,
        NotTrackMetadataFound,
        UnsupportedTrackType,
        NoParticipantFound
        // ...
    }

    /// <summary>
    /// The iceTransportPolicy used for RTCConfiguration.
    /// See https://developer.mozilla.org/en-US/docs/Web/API/RTCPeerConnection/RTCPeerConnection
    /// </summary>
    public enum RTCIceTransportPolicy
    {
        All,
        Relay
    }

    /// <summary>
    /// WebRTC configuration options
    /// </summary>
    public sealed class RTCConfiguration
    {
        public int? IceCandidatePoolSize { get; }
        public IReadOnlyList<RTCIceServer>? IceServers { get; }
        public RTCIceTransportPolicy? IceTransportPolicy { get; }
        public bool? EncodedInsertableStreams { get; }

        public RTCConfiguration(
            int? iceCandidatePoolSize = null,
            IReadOnlyList<RTCIceServer>? iceServers = null,
            RTCIceTransportPolicy? iceTransportPolicy = null,
            bool? encodedInsertableStreams = null)
        {
            IceCandidatePoolSize = iceCandidatePoolSize;
            IceServers = iceServers;
            IceTransportPolicy = iceTransportPolicy;
            EncodedInsertableStreams = encodedInsertableStreams;
        }

        public Dictionary<string, object> ToMap()
        {
            var result = new Dictionary<string, object>
            {
                // only supports unified plan
                { "sdpSemantics", "unified-plan" }
            };

            if (EncodedInsertableStreams.HasValue)
                result["encodedInsertableStreams"] = EncodedInsertableStreams.Value;

            if (IceServers != null && IceServers.Count > 0)
            {
                var iceServersMap = IceServers.Select(server => server.ToMap()).ToList();
                result["iceServers"] = iceServersMap;
            }

            if (IceCandidatePoolSize.HasValue)
                result["iceCandidatePoolSize"] = IceCandidatePoolSize.Value;

            if (IceTransportPolicy.HasValue)
                result["iceTransportPolicy"] = IceTransportPolicy.Value.ToString().ToLowerInvariant();

            return result;
        }

        // Returns new options with updated properties
        public RTCConfiguration CopyWith(
            int? iceCandidatePoolSize = null,
            IReadOnlyList<RTCIceServer>? iceServers = null,
            RTCIceTransportPolicy? iceTransportPolicy = null,
            bool? encodedInsertableStreams = null)
        {
            return new RTCConfiguration(
                iceCandidatePoolSize ?? IceCandidatePoolSize,
                iceServers ?? IceServers,
                iceTransportPolicy ?? IceTransportPolicy,
                encodedInsertableStreams ?? EncodedInsertableStreams
            );
        }
    }

    /// <summary>
    /// ICE server configuration
    /// </summary>
    public sealed class RTCIceServer
    {
        public IReadOnlyList<string>? Urls { get; }
        public string? Username { get; }
        public string? Credential { get; }

        public RTCIceServer(
            IReadOnlyList<string>? urls = null,
            string? username = null,
            string? credential = null)
        {
            Urls = urls;
            Username = username;
            Credential = credential;
        }

        public Dictionary<string, object> ToMap()
        {
            var result = new Dictionary<string, object>();

            if (Urls != null && Urls.Count > 0)
                result["urls"] = Urls;

            if (!string.IsNullOrEmpty(Username))
                result["username"] = Username!;

            if (!string.IsNullOrEmpty(Credential))
                result["credential"] = Credential!;

            return result;
        }
    }

    /// <summary>
    /// Participant track permission configuration
    /// </summary>
    public sealed class ParticipantTrackPermission
    {
        /// <summary>
        /// The participant identity this permission applies to.
        /// </summary>
        public string ParticipantIdentity { get; }

        /// <summary>
        /// If set to true, the target participant can subscribe to all tracks from the local participant.
        /// Takes precedence over AllowedTrackSids.
        /// </summary>
        public bool AllTracksAllowed { get; }

        /// <summary>
        /// The list of track ids that the target participant can subscribe to.
        /// </summary>
        public IReadOnlyList<string>? AllowedTrackSids { get; }

        public ParticipantTrackPermission(
            string participantIdentity,
            bool allTracksAllowed,
            IReadOnlyList<string>? allowedTrackSids = null)
        {
            ParticipantIdentity = participantIdentity;
            AllTracksAllowed = allTracksAllowed;
            AllowedTrackSids = allowedTrackSids;
        }
    }
}
