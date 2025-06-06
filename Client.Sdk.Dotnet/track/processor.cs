using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Sdk.Dotnet.track
{
    /// <summary>
    /// 处理器选项基类
    /// </summary>
    /// <typeparam name="T">轨道类型</typeparam>
    public class ProcessorOptions<T> where T : TrackType
    {
        /// <summary>
        /// 轨道类型
        /// </summary>
        public T Kind { get; }

        /// <summary>
        /// 媒体轨道
        /// </summary>
        public MediaStreamTrack Track { get; }

        /// <summary>
        /// 创建处理器选项
        /// </summary>
        /// <param name="kind">轨道类型</param>
        /// <param name="track">媒体轨道</param>
        public ProcessorOptions(T kind, MediaStreamTrack track)
        {
            Kind = kind;
            Track = track;
        }
    }

    /// <summary>
    /// 轨道处理器抽象基类
    /// </summary>
    /// <typeparam name="T">处理器选项类型</typeparam>
    public interface TrackProcessor<T, TTrackType>
    where T : ProcessorOptions<TTrackType>
    where TTrackType : TrackType
    {
        /// <summary>
        /// 处理器名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 初始化处理器
        /// </summary>
        /// <param name="options">处理器选项</param>
        public abstract Task InitAsync(T options);

        /// <summary>
        /// 重新启动处理器
        /// </summary>
        /// <param name="options">处理器选项</param>
        public abstract Task RestartAsync(T options);

        /// <summary>
        /// 销毁处理器
        /// </summary>
        public abstract Task DestroyAsync();

        /// <summary>
        /// 发布轨道时调用
        /// </summary>
        /// <param name="room">房间实例</param>
        public abstract Task OnPublishAsync(Room room);

        /// <summary>
        /// 取消发布轨道时调用
        /// </summary>
        public abstract Task OnUnpublishAsync();

        /// <summary>
        /// 处理后的媒体轨道
        /// </summary>
        public abstract MediaStreamTrack? ProcessedTrack { get; }
    }

    /// <summary>
    /// 媒体轨道抽象类（对应 flutter_webrtc 的 MediaStreamTrack）
    /// </summary>
    public abstract class MediaStreamTrack
    {
        // 此处添加 MediaStreamTrack 必要的属性和方法
        // 由于没有完整的上下文，这里提供一个简单的模型

        /// <summary>
        /// 轨道ID
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// 轨道类型
        /// </summary>
        public abstract string Kind { get; }

        /// <summary>
        /// 轨道是否启用
        /// </summary>
        public abstract bool Enabled { get; set; }

        /// <summary>
        /// 轨道是否结束
        /// </summary>
        public abstract bool Ended { get; }
    }

    /// <summary>
    /// 轨道类型基类
    /// </summary>
    public abstract class TrackType
    {
        // 简化的基本实现，实际内容根据 ../types/other.dart 中定义
    }

    /// <summary>
    /// 音频轨道类型
    /// </summary>
    public class AudioTrackType : TrackType
    {
        // 音频轨道特有的属性/方法
    }

    /// <summary>
    /// 视频轨道类型
    /// </summary>
    public class VideoTrackType : TrackType
    {
        // 视频轨道特有的属性/方法
    }

    /// <summary>
    /// 房间类（对应 Room）
    /// </summary>
    public class Room
    {
        // 简化的房间类，实际内容应根据 ../core/room.dart 中的定义进行扩展
    }


    /// <summary>
    /// 视频处理器选项
    /// </summary> 
    public class VideoProcessorOptions : ProcessorOptions<VideoTrackType>
    {
        public VideoProcessorOptions(VideoTrackType kind, MediaStreamTrack track)
            : base(kind, track)
        {
        }
    }

    /// <summary>
    /// 音频处理器选项
    /// </summary>
    public class AudioProcessorOptions : ProcessorOptions<AudioTrackType>
    {
        public AudioProcessorOptions(AudioTrackType kind, MediaStreamTrack track)
            : base(kind, track)
        {
        }
    }
}
