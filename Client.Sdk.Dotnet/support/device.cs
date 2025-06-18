using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiveKit.Proto;

namespace Client.Sdk.Dotnet.support
{
    internal class Device
    {
        /// <summary>
        /// 获取设备和操作系统信息
        /// </summary>
        public static async Task<ClientInfo?> GetClientInfo()
        {
            var clientInfo = new ClientInfo { Os = RuntimeInformation.OSDescription };

            try
            {
                // 根据运行平台填充不同的信息
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    clientInfo.Os = "windows";
                    clientInfo.OsVersion = Environment.OSVersion.Version.ToString();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    clientInfo.Os = "macOS";

                    // 在实际实现中应通过平台特定API获取更详细的信息
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sw_vers",
                        Arguments = "-productVersion",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    if (process != null)
                    {
                        clientInfo.OsVersion = await process.StandardOutput.ReadToEndAsync();
                        await process.WaitForExitAsync();
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    clientInfo.Os = "linux";

                    // 可以进一步检测特定Linux发行版
                    if (File.Exists("/etc/os-release"))
                    {
                        var osReleaseContent = await File.ReadAllTextAsync("/etc/os-release");
                        var versionIdMatch = Regex.Match(osReleaseContent, @"VERSION_ID=""?([^""\n]+)");
                        if (versionIdMatch.Success)
                        {
                            clientInfo.OsVersion = versionIdMatch.Groups[1].Value;
                        }
                    }
                }

                // 设备型号信息
                clientInfo.DeviceModel = RuntimeInformation.ProcessArchitecture.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取客户端信息时出错: {ex.Message}");
            }

            return clientInfo;
        }

        /// <summary>
        /// 获取当前网络连接类型
        /// </summary>
        public static async Task<string> GetNetworkType()
        {
            try
            {
                // 实际实现中应使用平台特定API检测网络状态
                // 这里是一个简化的实现

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // 假设默认是有线连接，实际应通过NetworkInterface API检测
                    return "wifi";
                }

                return "empty";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取网络类型时出错: {ex.Message}");
                return "empty";
            }
        }

    }
}
