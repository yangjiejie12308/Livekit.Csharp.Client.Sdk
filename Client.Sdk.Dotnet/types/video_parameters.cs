using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.types
{
    /// <summary>
    /// 视频参数配置
    /// </summary>
    public sealed class VideoParameters : IComparable<VideoParameters>, IEquatable<VideoParameters>
    {
        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// 视频尺寸
        /// </summary>
        public VideoDimensions Dimensions { get; }

        /// <summary>
        /// 视频编码参数
        /// </summary>
        public VideoEncoding? Encoding { get; }

        /// <summary>
        /// 创建视频参数实例
        /// </summary>
        /// <param name="dimensions">视频尺寸（必需）</param>
        /// <param name="description">可选描述</param>
        /// <param name="encoding">可选编码参数</param>
        public VideoParameters(
            VideoDimensions dimensions,
            string? description = null,
            VideoEncoding? encoding = null)
        {
            Dimensions = dimensions;
            Description = description;
            Encoding = encoding;
        }

        // ----------------------------------------------------------------------
        // 相等性比较

        /// <inheritdoc/>
        public bool Equals(VideoParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return Description == other.Description &&
                   Dimensions.Equals(other.Dimensions) &&
                   ((Encoding == null && other.Encoding == null) ||
                    (Encoding != null && Encoding.Equals(other.Encoding)));
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is VideoParameters parameters && Equals(parameters);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Description, Dimensions, Encoding);
        }

        /// <summary>
        /// 相等性运算符
        /// </summary>
        public static bool operator ==(VideoParameters? left, VideoParameters? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(VideoParameters? left, VideoParameters? right)
        {
            return !(left == right);
        }

        // ----------------------------------------------------------------------
        // IComparable 实现

        /// <inheritdoc/>
        public int CompareTo(VideoParameters? other)
        {
            if (other is null) return 1;

            // 通过面积比较维度
            var result = Dimensions.Area().CompareTo(other.Dimensions.Area());

            // 如果维度有相同的区域，则通过编码比较
            if (result == 0 && Encoding != null && other.Encoding != null)
            {
                return Encoding.CompareTo(other.Encoding);
            }

            return result;
        }

        /// <summary>
        /// 转换为媒体约束映射
        /// </summary>
        /// <returns>包含约束的字典</returns>
        public Dictionary<string, object> ToMediaConstraintsMap()
        {
            // TODO: 返回适用于所有平台(Web和移动)的约束
            // https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia
            return new Dictionary<string, object>
            {
                ["width"] = Dimensions.Width,
                ["height"] = Dimensions.Height,
                ["frameRate"] = Encoding?.MaxFramerate ?? 30
            };
        }
    }

    /// <summary>
    /// 视频参数预设
    /// </summary>
    public static class VideoParametersPresets
    {
        // 16:9 默认预设
        public static readonly List<VideoParameters> DefaultSimulcast169 = new List<VideoParameters>
        {
            H180_169,
            H360_169,
        };

        // 4:3 默认预设
        public static readonly List<VideoParameters> DefaultSimulcast43 = new List<VideoParameters>
        {
            H180_43,
            H360_43,
        };

        // 所有 16:9 预设
        public static readonly List<VideoParameters> All169 = new List<VideoParameters>
        {
            H90_169,
            H180_169,
            H216_169,
            H360_169,
            H540_169,
            H720_169,
            H1080_169,
            H1440_169,
            H2160_169,
        };

        // 所有 4:3 预设
        public static readonly List<VideoParameters> All43 = new List<VideoParameters>
        {
            H120_43,
            H180_43,
            H240_43,
            H360_43,
            H480_43,
            H540_43,
            H720_43,
            H1080_43,
            H1440_43,
        };

        // 所有屏幕共享预设
        public static readonly List<VideoParameters> AllScreenShare = new List<VideoParameters>
        {
            ScreenShareH360FPS3,
            ScreenShareH720FPS5,
            ScreenShareH720FPS15,
            ScreenShareH1080FPS15,
            ScreenShareH1080FPS30,
        };

        // 16:9 预设
        public static readonly VideoParameters H90_169 = new VideoParameters(
            VideoDimensionsPresets.H90_169,
            encoding: new VideoEncoding(
                maxBitrate: 90 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H180_169 = new VideoParameters(
            VideoDimensionsPresets.H180_169,
            encoding: new VideoEncoding(
                maxBitrate: 160 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H216_169 = new VideoParameters(
            VideoDimensionsPresets.H216_169,
            encoding: new VideoEncoding(
                maxBitrate: 180 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H360_169 = new VideoParameters(
            VideoDimensionsPresets.H360_169,
            encoding: new VideoEncoding(
                maxBitrate: 450 * 1000,
                maxFramerate: 20
            )
        );

        public static readonly VideoParameters H540_169 = new VideoParameters(
            VideoDimensionsPresets.H540_169,
            encoding: new VideoEncoding(
                maxBitrate: 800 * 1000,
                maxFramerate: 25
            )
        );

        public static readonly VideoParameters H720_169 = new VideoParameters(
            VideoDimensionsPresets.H720_169,
            encoding: new VideoEncoding(
                maxBitrate: 1700 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters H1080_169 = new VideoParameters(
            VideoDimensionsPresets.H1080_169,
            encoding: new VideoEncoding(
                maxBitrate: 3000 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters H1440_169 = new VideoParameters(
            VideoDimensionsPresets.H1440_169,
            encoding: new VideoEncoding(
                maxBitrate: 5000 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters H2160_169 = new VideoParameters(
            VideoDimensionsPresets.H2160_169,
            encoding: new VideoEncoding(
                maxBitrate: 8000 * 1000,
                maxFramerate: 30
            )
        );

        // 4:3 预设
        public static readonly VideoParameters H120_43 = new VideoParameters(
            VideoDimensionsPresets.H120_43,
            encoding: new VideoEncoding(
                maxBitrate: 70 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H180_43 = new VideoParameters(
            VideoDimensionsPresets.H180_43,
            encoding: new VideoEncoding(
                maxBitrate: 125 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H240_43 = new VideoParameters(
            VideoDimensionsPresets.H240_43,
            encoding: new VideoEncoding(
                maxBitrate: 140 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters H360_43 = new VideoParameters(
            VideoDimensionsPresets.H360_43,
            encoding: new VideoEncoding(
                maxBitrate: 330 * 1000,
                maxFramerate: 20
            )
        );

        public static readonly VideoParameters H480_43 = new VideoParameters(
            VideoDimensionsPresets.H480_43,
            encoding: new VideoEncoding(
                maxBitrate: 500 * 1000,
                maxFramerate: 20
            )
        );

        public static readonly VideoParameters H540_43 = new VideoParameters(
            VideoDimensionsPresets.H540_43,
            encoding: new VideoEncoding(
                maxBitrate: 600 * 1000,
                maxFramerate: 25
            )
        );

        public static readonly VideoParameters H720_43 = new VideoParameters(
            VideoDimensionsPresets.H720_43,
            encoding: new VideoEncoding(
                maxBitrate: 1300 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters H1080_43 = new VideoParameters(
            VideoDimensionsPresets.H1080_43,
            encoding: new VideoEncoding(
                maxBitrate: 2300 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters H1440_43 = new VideoParameters(
            VideoDimensionsPresets.H1440_43,
            encoding: new VideoEncoding(
                maxBitrate: 3800 * 1000,
                maxFramerate: 30
            )
        );

        // 屏幕共享预设
        public static readonly VideoParameters ScreenShareH360FPS3 = new VideoParameters(
            VideoDimensionsPresets.H360_169,
            encoding: new VideoEncoding(
                maxBitrate: 200 * 1000,
                maxFramerate: 3
            )
        );

        public static readonly VideoParameters ScreenShareH720FPS5 = new VideoParameters(
            VideoDimensionsPresets.H720_169,
            encoding: new VideoEncoding(
                maxBitrate: 400 * 1000,
                maxFramerate: 5
            )
        );

        public static readonly VideoParameters ScreenShareH720FPS15 = new VideoParameters(
            VideoDimensionsPresets.H720_169,
            encoding: new VideoEncoding(
                maxBitrate: 1500 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters ScreenShareH1080FPS15 = new VideoParameters(
            VideoDimensionsPresets.H1080_169,
            encoding: new VideoEncoding(
                maxBitrate: 2500 * 1000,
                maxFramerate: 15
            )
        );

        public static readonly VideoParameters ScreenShareH1080FPS30 = new VideoParameters(
            VideoDimensionsPresets.H1080_169,
            encoding: new VideoEncoding(
                maxBitrate: 4000 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters ScreenShareH1440FPS30 = new VideoParameters(
            VideoDimensionsPresets.H1440_169,
            encoding: new VideoEncoding(
                maxBitrate: 6000 * 1000,
                maxFramerate: 30
            )
        );

        public static readonly VideoParameters ScreenShareH2160FPS30 = new VideoParameters(
            VideoDimensionsPresets.H2160_169,
            encoding: new VideoEncoding(
                maxBitrate: 8000 * 1000,
                maxFramerate: 30
            )
        );
    }
}
