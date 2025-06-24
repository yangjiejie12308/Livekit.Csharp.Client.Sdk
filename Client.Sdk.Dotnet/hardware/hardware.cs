using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;

namespace Client.Sdk.Dotnet.hardware
{
    /// <summary>
    /// 本地硬件枚举
    /// </summary>
    public class HardWare
    {

        public static async Task<IReadOnlyList<VideoCaptureDevice>> GetVideoCaptureDevicesAsync()
        {
            try
            {
                var devices = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
                return devices;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"获取视频设备失败: {ex.Message}");
                return new List<VideoCaptureDevice>();
            }
        }

        public static async Task<DeviceAudioTrackSource?> GetLocalAudioTrackAsync()
        {
            try
            {
                // 创建本地音频轨道
                var audioTrack = await DeviceAudioTrackSource.CreateAsync();
                return audioTrack;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"创建本地音频轨道失败: {ex.Message}");
                return null;
            }
        }

        public static async Task<DeviceVideoTrackSource?> GetLocalVideoTrackAsync()
        {
            try
            {
                // 创建本地视频轨道
                var videoTrack = await DeviceVideoTrackSource.CreateAsync();
                return videoTrack;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"创建本地视频轨道失败: {ex.Message}");
                return null;
            }
        }

        public static LocalAudioTrack? GetLocalAudioTrack(DeviceAudioTrackSource source)
        {
            try
            {
                // 创建本地音频轨道
                return LocalAudioTrack.CreateFromSource(source, new LocalAudioTrackInitConfig());
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"创建本地音频轨道失败: {ex.Message}");
                return null;
            }
        }

        public static LocalVideoTrack? GetLocalVideoTrack(DeviceVideoTrackSource source)
        {
            try
            {
                // 创建本地视频轨道
                return LocalVideoTrack.CreateFromSource(source, new LocalVideoTrackInitConfig());
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"创建本地视频轨道失败: {ex.Message}");
                return null;
            }
        }
    }
}
