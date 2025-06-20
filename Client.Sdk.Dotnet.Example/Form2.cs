using Client.Sdk.Dotnet.core;
using Client.Sdk.Dotnet.hardware;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.FFmpeg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client.Sdk.Dotnet.Example
{
    public partial class Form2 : Form
    {
        private Engine engine;

        private HardWare hardWare = new HardWare();
        public Form2()
        {
            InitializeComponent();
            hardWare.GetAllScreen();
            engine = new Engine("ws://127.0.0.1:7880", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTA2MDM2OTAsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIxMSIsIm5iZiI6MTc1MDI1ODA5MCwic3ViIjoidGVzdF91c2VyMTEiLCJ2aWRlbyI6eyJyb29tIjoidGVzdF9yb29tIiwicm9vbUpvaW4iOnRydWV9fQ.u-iQ_L5-f9APZl6MkC8U54xtR5IqzGsZDraIx8zOQ6s");
            InitAsync().ConfigureAwait(false);
        }

        private async Task InitAsync()
        {
            engine.RemoteParticipantUpdated += Engine_RemoteParticipantUpdated;
            engine.onStreamUpdated += Engine_onStreamUpdated;
            await engine.ConnectAsync();
        }

        private void Engine_onStreamUpdated(object? sender, RemoteVideoTrack e)
        {
            RenderFrameToBox(e, e.Name);
        }

        private void Engine_RemoteParticipantUpdated(object? sender, ParticipantInfo e)
        {
            Debug.WriteLine($"Remote participant updated: {e.ToString()}");

        }



        private Dictionary<string, PictureBox> _videoBoxes = new();


        private void AddOrUpdateVideoBox(string key)
        {
            if (!_videoBoxes.ContainsKey(key))
            {
                var pb = new PictureBox
                {
                    Width = 320,
                    Height = 180,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BorderStyle = BorderStyle.FixedSingle,
                    Name = key,
                    Dock = DockStyle.Fill,
                };
                // 这里假设你有一个 panelVideoContainer 用于承载所有 PictureBox
                if (panelVideoContainer.InvokeRequired)
                {
                    panelVideoContainer.Invoke(new Action(() =>
                    {
                        panelVideoContainer.Controls.Add(pb);
                        _videoBoxes[key] = pb;
                    }));
                }
                else
                {
                    panelVideoContainer.Controls.Add(pb);
                    _videoBoxes[key] = pb;
                }
            }
        }


        private void RenderFrameToBox(RemoteVideoTrack videoTrack, string streamStateInfo)
        {
            Debug.WriteLine($"streamStateInfo: {streamStateInfo.ToString()}");

            if (!_videoBoxes.ContainsKey(streamStateInfo))
            {
                AddOrUpdateVideoBox(streamStateInfo);
            }
            else
            {
                var pb = _videoBoxes[streamStateInfo];
                if (panelVideoContainer.InvokeRequired)
                {
                    panelVideoContainer.Invoke(new Action(() =>
                    {
                        panelVideoContainer.Controls.Remove(pb);
                    }));
                }
                else
                {
                    panelVideoContainer.Controls.Remove(pb);
                }

                _videoBoxes.Remove(streamStateInfo);
            }

            //var videoEP = new VideoEncoderEndPoint();

            //videoEP.OnVideoSinkDecodedSampleFaster += (RawImage rawImage) =>
            //{
            //    Debug.WriteLine($"RawImage：");


            //    if (_videoBoxes.TryGetValue(streamStateInfo.TrackSid, out var pb))
            //    {
            //        if (rawImage.PixelFormat == VideoPixelFormatsEnum.Rgb)
            //        {
            //            Bitmap bmpImage = new Bitmap(rawImage.Width, rawImage.Height, rawImage.Stride, PixelFormat.Format24bppRgb, rawImage.Sample);
            //            pb.Image = bmpImage;
            //        }
            //    }
            //};

            //videoEP.OnVideoSinkDecodedSample += (byte[] bmp, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat) =>
            //{
            //    Debug.WriteLine($"TrackSid:{streamStateInfo} bytes width:{width} height:{height}");
            //    // 假设 bmp 是 byte[]，你需要转 Bitmap
            //    using var bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);


            //    var bmpData = bitmap.LockBits(
            //        new Rectangle(0, 0, (int)width, (int)height),
            //        System.Drawing.Imaging.ImageLockMode.WriteOnly,
            //        bitmap.PixelFormat);

            //    System.Runtime.InteropServices.Marshal.Copy(bmp, 0, bmpData.Scan0, bmp.Length);
            //    bitmap.UnlockBits(bmpData);

            //    var bitMAP = (Bitmap)bitmap.Clone();

            //    if (_videoBoxes.TryGetValue(streamStateInfo, out var pb))
            //    {
            //        if (pb.InvokeRequired)
            //        {
            //            pb.Invoke(new Action(() =>
            //            {
            //                pb.Image?.Dispose();
            //                pb.Image = bitMAP;
            //            }));
            //        }
            //        else
            //        {
            //            pb.Image?.Dispose();
            //            pb.Image = bitMAP;
            //        }
            //    }
            //};

            videoTrack.I420AVideoFrameReady += (frame) =>
            {
                //Debug.WriteLine($"TrackSid:{streamStateInfo} bytes width:{frame.width} height:{frame.height}");
                //// 假设 bmp 是 byte[]，你需要转 Bitmap
                //using var bitmap = new Bitmap((int)frame.width, (int)frame.height, PixelFormat.Format24bppRgb);


                //var bmpData = bitmap.LockBits(
                //    new Rectangle(0, 0, (int)frame.width, (int)frame.height),
                //    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                //    bitmap.PixelFormat);

                //byte[] bmp = new byte[frame.width * frame.height * 3]; // RGB format requires 3 bytes per pixel
                //frame.CopyTo(bmp);

                //System.Runtime.InteropServices.Marshal.Copy(bmp, 0, bmpData.Scan0, bmp.Length);
                //bitmap.UnlockBits(bmpData);

                //var bitMAP = (Bitmap)bitmap.Clone();

                // 1. 准备存储RGB数据的数组
                byte[] rgbData = new byte[frame.width * frame.height * 3]; // RGB数据

                // 2. 执行YUV到RGB的转换
                ConvertI420AToRgb(frame, rgbData);

                // 3. 创建并填充Bitmap
                using var bitmap = new Bitmap((int)frame.width, (int)frame.height, PixelFormat.Format24bppRgb);
                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, (int)frame.width, (int)frame.height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                // 4. 复制RGB数据到Bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbData, 0, bmpData.Scan0, rgbData.Length);
                bitmap.UnlockBits(bmpData);

                var bitMAP = (Bitmap)bitmap.Clone();

                if (_videoBoxes.TryGetValue(streamStateInfo, out var pb))
                {
                    if (pb.InvokeRequired)
                    {
                        pb.Invoke(new Action(() =>
                        {
                            pb.Image?.Dispose();
                            pb.Image = bitMAP;
                        }));
                    }
                    else
                    {
                        pb.Image?.Dispose();
                        pb.Image = bitMAP;
                    }
                }

            };
        }

        // YUV到RGB转换函数
        private void ConvertI420AToRgb(I420AVideoFrame frame, byte[] rgbData)
        {
            // 获取YUV各平面的数据
            byte[] yPlane = new byte[frame.strideY * frame.height];
            byte[] uPlane = new byte[frame.strideU * (frame.height / 2)];
            byte[] vPlane = new byte[frame.strideV * (frame.height / 2)];

            // 从frame中获取YUV数据 - 这里需要根据您的I420AVideoFrame类的具体接口调整
            unsafe
            {
                // 假设frame有一些方法或属性可以获取各个平面的指针或数据
                Marshal.Copy(frame.dataY, yPlane, 0, yPlane.Length);
                Marshal.Copy(frame.dataU, uPlane, 0, uPlane.Length);
                Marshal.Copy(frame.dataV, vPlane, 0, vPlane.Length);
            }

            // 执行YUV到RGB的转换
            int rgbIndex = 0;
            for (int y = 0; y < frame.height; y++)
            {
                for (int x = 0; x < frame.width; x++)
                {
                    int yIndex = y * frame.strideY + x;
                    int uIndex = (y / 2) * frame.strideU + (x / 2);
                    int vIndex = (y / 2) * frame.strideV + (x / 2);

                    int Y = yPlane[yIndex] & 0xff;
                    int U = uPlane[uIndex] & 0xff - 128;
                    int V = vPlane[vIndex] & 0xff - 128;

                    // YUV到RGB转换公式
                    int R = Y + (int)(1.402f * V);
                    int G = Y - (int)(0.344f * U + 0.714f * V);
                    int B = Y + (int)(1.772f * U);

                    // 裁剪值到0-255范围
                    R = Math.Max(0, Math.Min(255, R));
                    G = Math.Max(0, Math.Min(255, G));
                    B = Math.Max(0, Math.Min(255, B));

                    // 填充RGB数据
                    rgbData[rgbIndex++] = (byte)B; // Windows中Bitmap存储顺序是BGR
                    rgbData[rgbIndex++] = (byte)G;
                    rgbData[rgbIndex++] = (byte)R;
                }
            }
        }
    }
}
