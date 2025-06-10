using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Serilog.Extensions.Logging;
using Serilog;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.FFmpeg;

namespace Client.Sdk.Dotnet.hardware
{
    /// <summary>
    /// 本地硬件枚举
    /// </summary>
    public class HardWare
    {
        private static Microsoft.Extensions.Logging.ILogger logger = HardWare.AddConsoleLogger();

        public HardWare()
        {
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, @"E:\TDG\FFmpeg.AutoGen\FFmpeg\bin\x64", logger);
        }

        public List<Camera>? GetAllCamera()
        {

            return FFmpegCameraManager.GetCameraDevices();
        }

        public List<SIPSorceryMedia.FFmpeg.Monitor>? GetAllScreen()
        {
            return FFmpegMonitorManager.GetMonitorDevices();
        }

        private static Microsoft.Extensions.Logging.ILogger AddConsoleLogger()
        {
            var seriLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();
            var factory = new SerilogLoggerFactory(seriLogger);
            SIPSorcery.LogFactory.Set(factory);
            return factory.CreateLogger<HardWare>();

        }
    }
}
