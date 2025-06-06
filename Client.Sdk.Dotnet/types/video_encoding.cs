using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.types
{
    /// <summary>
    /// 表示视频编码信息的类型
    /// </summary>
    public sealed class VideoEncoding : IComparable<VideoEncoding>, IEquatable<VideoEncoding>
    {
        /// <summary>
        /// 最大帧率
        /// </summary>
        public int MaxFramerate { get; }

        /// <summary>
        /// 最大比特率
        /// </summary>
        public int MaxBitrate { get; }

        /// <summary>
        /// 创建视频编码实例
        /// </summary>
        /// <param name="maxFramerate">最大帧率</param>
        /// <param name="maxBitrate">最大比特率</param>
        public VideoEncoding(int maxFramerate, int maxBitrate)
        {
            MaxFramerate = maxFramerate;
            MaxBitrate = maxBitrate;
        }

        /// <summary>
        /// 创建具有更新属性的新实例
        /// </summary>
        /// <param name="maxFramerate">新的最大帧率，为null则使用当前值</param>
        /// <param name="maxBitrate">新的最大比特率，为null则使用当前值</param>
        /// <returns>新的VideoEncoding实例</returns>
        public VideoEncoding CopyWith(int? maxFramerate = null, int? maxBitrate = null) =>
            new VideoEncoding(
                maxFramerate ?? MaxFramerate,
                maxBitrate ?? MaxBitrate
            );

        /// <inheritdoc/>
        public override string ToString() =>
            $"{GetType().Name}(maxFramerate: {MaxFramerate}, maxBitrate: {MaxBitrate})";

        // ----------------------------------------------------------------------
        // 相等性比较

        /// <inheritdoc/>
        public bool Equals(VideoEncoding? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return MaxFramerate == other.MaxFramerate && MaxBitrate == other.MaxBitrate;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is VideoEncoding encoding && Equals(encoding);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(MaxFramerate, MaxBitrate);

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(VideoEncoding? left, VideoEncoding? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(VideoEncoding? left, VideoEncoding? right) =>
            !(left == right);

        // ----------------------------------------------------------------------
        // Comparable 接口实现

        /// <inheritdoc/>
        public int CompareTo(VideoEncoding? other)
        {
            if (other is null) return 1;

            // 比较比特率
            var result = MaxBitrate.CompareTo(other.MaxBitrate);
            // 如果比特率相同，则比较帧率
            if (result == 0)
            {
                return MaxFramerate.CompareTo(other.MaxFramerate);
            }

            return result;
        }
    }

    /// <summary>
    /// VideoEncoding 的扩展方法
    /// </summary>
    public static class VideoEncodingExtensions
    {
        /// <summary>
        /// 转换为 RTCRtpEncoding 对象
        /// </summary>
        /// <param name="encoding">视频编码实例</param>
        /// <param name="rid">编码 ID</param>
        /// <param name="scaleResolutionDownBy">分辨率缩放比例</param>
        /// <param name="numTemporalLayers">时域层数</param>
        /// <returns>RTCRtpEncoding 实例</returns>
        public static RTCRtpEncoding ToRTCRtpEncoding(
            this VideoEncoding encoding,
            string? rid = null,
            double? scaleResolutionDownBy = 1.0,
            int? numTemporalLayers = null)
        {
            return new RTCRtpEncoding
            {
                Rid = rid,
                ScaleResolutionDownBy = scaleResolutionDownBy,
                MaxFramerate = encoding.MaxFramerate,
                MaxBitrate = encoding.MaxBitrate,
                NumTemporalLayers = numTemporalLayers
            };
        }
    }

    /// <summary>
    /// RTP 编码配置类（模拟 flutter_webrtc 的 RTCRtpEncoding）
    /// </summary>
    public class RTCRtpEncoding
    {
        /// <summary>
        /// 编码 ID
        /// </summary>
        public string? Rid { get; set; }

        /// <summary>
        /// 分辨率缩放比例
        /// </summary>
        public double? ScaleResolutionDownBy { get; set; }

        /// <summary>
        /// 最大帧率
        /// </summary>
        public int MaxFramerate { get; set; }

        /// <summary>
        /// 最大比特率
        /// </summary>
        public int MaxBitrate { get; set; }

        /// <summary>
        /// 时域层数
        /// </summary>
        public int? NumTemporalLayers { get; set; }
    }
}
