using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.types
{
    public sealed class VideoDimensions : IEquatable<VideoDimensions>
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 高度
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 创建视频尺寸实例
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public VideoDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{GetType().Name}({Width}x{Height})";

        /// <summary>
        /// 创建一个具有更新属性的新实例
        /// </summary>
        /// <param name="width">新宽度，为null则使用当前值</param>
        /// <param name="height">新高度，为null则使用当前值</param>
        /// <returns>新的VideoDimensions实例</returns>
        public VideoDimensions CopyWith(int? width = null, int? height = null) =>
            new VideoDimensions(
                width ?? Width,
                height ?? Height
            );

        // ----------------------------------------------------------------------
        // 相等性比较

        /// <inheritdoc/>
        public bool Equals(VideoDimensions? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Width == other.Width && Height == other.Height;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is VideoDimensions dimensions && Equals(dimensions);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Width, Height);

        /// <summary>
        /// 相等运算符
        /// </summary>
        public static bool operator ==(VideoDimensions? left, VideoDimensions? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        /// <summary>
        /// 不等运算符
        /// </summary>
        public static bool operator !=(VideoDimensions? left, VideoDimensions? right) =>
            !(left == right);

        /// <summary>
        /// 计算宽高比
        /// </summary>
        public double Aspect() => Width > Height ? (double)Width / Height : (double)Height / Width;

        /// <summary>
        /// 返回较大的值（宽度或高度）
        /// </summary>
        public int Max() => Math.Max(Width, Height);

        /// <summary>
        /// 返回较小的值（宽度或高度）
        /// </summary>
        public int Min() => Math.Min(Width, Height);

        /// <summary>
        /// 计算面积
        /// </summary>
        public int Area() => Width * Height;
    }

    public static class VideoDimensionsHelpers
    {
        // 纵横比常量
        public const double Aspect169 = 16.0 / 9.0;
        public const double Aspect43 = 4.0 / 3.0;
    }

    /// <summary>
    /// 预设的视频尺寸集合
    /// </summary>
    public static class VideoDimensionsPresets
    {
        // 16:9 纵横比预设
        public static readonly VideoDimensions H90_169 = new VideoDimensions(160, 90);
        public static readonly VideoDimensions H180_169 = new VideoDimensions(320, 180);
        public static readonly VideoDimensions H216_169 = new VideoDimensions(384, 216);
        public static readonly VideoDimensions H360_169 = new VideoDimensions(640, 360);
        public static readonly VideoDimensions H540_169 = new VideoDimensions(960, 540);
        public static readonly VideoDimensions H720_169 = new VideoDimensions(1280, 720);
        public static readonly VideoDimensions H1080_169 = new VideoDimensions(1920, 1080);
        public static readonly VideoDimensions H1440_169 = new VideoDimensions(2560, 1440);
        public static readonly VideoDimensions H2160_169 = new VideoDimensions(3840, 2160);

        // 4:3 纵横比预设
        public static readonly VideoDimensions H120_43 = new VideoDimensions(160, 120);
        public static readonly VideoDimensions H180_43 = new VideoDimensions(240, 180);
        public static readonly VideoDimensions H240_43 = new VideoDimensions(320, 240);
        public static readonly VideoDimensions H360_43 = new VideoDimensions(480, 360);
        public static readonly VideoDimensions H480_43 = new VideoDimensions(640, 480);
        public static readonly VideoDimensions H540_43 = new VideoDimensions(720, 540);
        public static readonly VideoDimensions H720_43 = new VideoDimensions(960, 720);
        public static readonly VideoDimensions H1080_43 = new VideoDimensions(1440, 1080);
        public static readonly VideoDimensions H1440_43 = new VideoDimensions(1920, 1440);
    }
}
