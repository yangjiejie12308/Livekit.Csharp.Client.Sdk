using Client.Sdk.Dotnet.core;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
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
    public partial class lkUserControl : UserControl
    {
        public  ParticipantInfo? participantInfo;
        public  Engine engine;
        public lkUserControl(ParticipantInfo participant,Engine engine)
        {
            InitializeComponent();
            this.participantInfo = participant;
            this.engine = engine;
            this.Identity.Text = participant.Identity;
     
        }

        public void AddVideo(string trackId)
        {

            RemoteVideoTrack? videoTrack = this.engine.GetTrackStream(trackId) ;

            if (videoTrack == null) return;

            if (this.videoPanels.Controls.OfType<PictureBox>().Any(v => v.Name == trackId)) return;

            PictureBox newVideoBox = new PictureBox
            {
                Name = trackId,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Fill,
            };
            void AddControl()
            {
                this.videoPanels.Controls.Add(newVideoBox);
            }

            if (this.videoPanels.InvokeRequired)
            {
                this.videoPanels.Invoke((Action)(AddControl));
            }
            else
            {
                AddControl();
            }


            videoTrack.I420AVideoFrameReady += (frame) =>
            {

                var bitMAP = ConvertI420AToBitmap(frame);

                    if (newVideoBox.InvokeRequired)
                    {
                    newVideoBox.Invoke(new Action(() =>
                        {
                            newVideoBox.Image?.Dispose();
                            newVideoBox.Image = bitMAP;
                        }));
                    }
                    else
                    {
                    newVideoBox.Image?.Dispose();
                    newVideoBox.Image = bitMAP;
                    }
            };

        }

        public void RemoveVideo(string videoTrackName)
        {
          PictureBox? pictureBox =   this.videoPanels.Controls
                .OfType<PictureBox>()
                .FirstOrDefault(pb => pb.Name == videoTrackName);

            if (pictureBox != null)
            {
                pictureBox.Image?.Dispose();
                this.videoPanels.Controls.Remove(pictureBox);
                pictureBox.Dispose();
            }
        }

        public static Bitmap ConvertI420AToBitmap(I420AVideoFrame frame)
        {
            // 创建目标 Bitmap (ARGB格式)
            Bitmap bitmap = new Bitmap((int)frame.width, (int)frame.height, PixelFormat.Format32bppArgb);

            // 锁定 Bitmap 的位图数据
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            // 计算 I420A 数据总大小 (Y + U + V + A)
            int ySize = (int)(frame.width * frame.height);
            int uvSize = ySize / 4; // 色度平面是亮度的 1/4
            int totalSize = ySize + uvSize * 2;
            if (frame.dataA != IntPtr.Zero)
                totalSize += ySize; // Alpha 平面大小与 Y 平面相同

            // 创建临时缓冲区存储 YUV(A) 数据
            byte[] yuvData = new byte[totalSize];
            frame.CopyTo(yuvData);

            // 分配 ARGB 输出缓冲区
            byte[] argbData = new byte[frame.width * frame.height * 4];

            // 转换 YUV(A) 到 ARGB
            ConvertYUVAToARGB(yuvData, argbData, (int)frame.width, (int)frame.height, frame.dataA != IntPtr.Zero);

            // 复制数据到 Bitmap
            Marshal.Copy(argbData, 0, bitmapData.Scan0, argbData.Length);

            // 解锁位图
            bitmap.UnlockBits(bitmapData);

            return bitmap;
        }

        private static void ConvertYUVAToARGB(byte[] yuvData, byte[] argbData, int width, int height, bool hasAlpha)
        {
            int ySize = width * height;
            int uvSize = ySize / 4;

            // 确保 Y、U、V 数据不会超出数组边界
            if (yuvData.Length < ySize + uvSize * 2)
            {
                Debug.WriteLine("YUV 数据缓冲区大小不足");
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int yIndex = y * width + x;

                    // 修正 UV 索引计算，确保不会越界
                    int uvY = y >> 1; // y / 2
                    int uvX = x >> 1; // x / 2
                    int uvWidth = width >> 1; // width / 2
                    int uvIndex = uvY * uvWidth + uvX;

                    // 确保索引在有效范围内
                    if (yIndex >= ySize || ySize + uvIndex >= ySize + uvSize ||
                        ySize + uvSize + uvIndex >= ySize + uvSize * 2)
                    {
                        continue; // 跳过无效像素
                    }

                    // 获取 YUV 值
                    byte Y = yuvData[yIndex];
                    byte U = yuvData[ySize + uvIndex];
                    byte V = yuvData[ySize + uvSize + uvIndex];

                    // YUV 到 RGB 转换
                    int C = Y - 16;
                    int D = U - 128;
                    int E = V - 128;

                    // YUV 到 RGB 转换公式
                    //int R = (298 * C + 409 * E + 128) >> 8;
                    //int G = (298 * C - 100 * D - 208 * E + 128) >> 8;
                    //int B = (298 * C + 516 * D + 128) >> 8;

                    // 裁剪值到 0-255 范围
                    byte R = (byte)Math.Max(0, Math.Min(255, (298 * C + 409 * E + 128) >> 8));
                    byte G = (byte)Math.Max(0, Math.Min(255, (298 * C - 100 * D - 208 * E + 128) >> 8));
                    byte B = (byte)Math.Max(0, Math.Min(255, (298 * C + 516 * D + 128) >> 8));

                    // 设置 Alpha 值
                    byte A = hasAlpha ? yuvData[ySize + uvSize * 2 + yIndex] : (byte)255;

                    // 填充 ARGB 数据 (BGRA 顺序，因为 Windows Bitmap 是 BGRA)
                    int destIndex = (y * width + x) * 4;
                    argbData[destIndex] = B;     // Blue
                    argbData[destIndex + 1] = G; // Green
                    argbData[destIndex + 2] = R; // Red
                    argbData[destIndex + 3] = A; // Alpha
                }
            }
        }

    }
}
