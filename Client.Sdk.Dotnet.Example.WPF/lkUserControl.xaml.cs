using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.Sdk.Dotnet.core;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;

namespace Client.Sdk.Dotnet.Example.WPF
{
    /// <summary>
    /// lkUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class lkUserControl : UserControl
    {
        public ParticipantInfo? participantInfo;
        public Engine engine;
        private ConnectionQualityInfo connectionQualityInfo = new ConnectionQualityInfo();
        public lkUserControl(ParticipantInfo participant, Engine engine)
        {
            InitializeComponent();
            this.participantInfo = participant;
            this.engine = engine;
            this.Identity.Text = participant.Identity;

        }

        public void AddVideo(string trackId)
        {

            RemoteVideoTrack? videoTrack = this.engine.GetTrackStream(trackId);

            if (videoTrack == null) return;

            if (this.videoPanels.Children.OfType<System.Windows.Controls.Image>().Any(v => v.Name == trackId)) return;

            void AddControl()
            {
                System.Windows.Controls.Image newVideoBox = new System.Windows.Controls.Image
                {
                    Name = trackId,
                    Stretch = Stretch.Fill,
                };

                videoTrack.I420AVideoFrameReady += (frame) =>
                {

                    var bitMAP = ConvertI420AToBitmap(frame);

                    Dispatcher.Invoke(() =>
                    {
                        newVideoBox.Source = BitmapToBitmapSource(bitMAP);
                    });
                };

                AddContextMenu(newVideoBox, trackId);
                this.videoPanels.Children.Add(newVideoBox);
            }


            Dispatcher.Invoke(AddControl);
        }


        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public void RemoveVideo(string videoTrackName)
        {
            System.Windows.Controls.Image? pictureBox = this.videoPanels.Children
                  .OfType<System.Windows.Controls.Image>()
                  .FirstOrDefault(pb => pb.Name == videoTrackName);

            void RemoveControl(System.Windows.Controls.Image pictureBox)
            {
                this.videoPanels.Children.Remove(pictureBox);
            }

            if (pictureBox != null)
            {

                Dispatcher.Invoke(() => RemoveControl(pictureBox));
            }
        }

        public void AddAudio(string trackId)
        {
            RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

            if (audioTrack == null) return;

            // 保证在UI线程执行
            Dispatcher.Invoke(() => ChangeControl("已发布"));
        }
        void ChangeControl(string text)
        {
            this.microphone.Text = text;
        }
        public void RemoveAudio(string trackId)
        {
            RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);
            if (audioTrack == null) return;

            Dispatcher.Invoke(() => ChangeControl("未发布"));
        }

        public void MuteVideo(string trackId)
        {
            System.Windows.Controls.Image? pictureBox = this.videoPanels.Children
                  .OfType<System.Windows.Controls.Image>()
                  .FirstOrDefault(pb => pb.Name == trackId);

            void RemoveControl(System.Windows.Controls.Image pictureBox)
            {
                this.videoPanels.Children.Remove(pictureBox);
            }

            if (pictureBox != null)
            {
                Dispatcher.Invoke(() => RemoveControl(pictureBox));
            }
        }

        public void UnMuteVideo(string trackId)
        {
            RemoteVideoTrack? videoTrack = this.engine.GetTrackStream(trackId);

            if (videoTrack == null) return;

            if (this.videoPanels.Children.OfType<System.Windows.Controls.Image>().Any(v => v.Name == trackId)) return;


            void AddControl()
            {
                System.Windows.Controls.Image newVideoBox = new System.Windows.Controls.Image
                {
                    Name = trackId,
                    Stretch = Stretch.Fill,

                };

                AddContextMenu(newVideoBox, trackId);
                this.videoPanels.Children.Add(newVideoBox);
                videoTrack.I420AVideoFrameReady += (frame) =>
                {
                    var bitMAP = ConvertI420AToBitmap(frame);
                    Dispatcher.Invoke(() => newVideoBox.Source = BitmapToBitmapSource(bitMAP));
                };
            }
            Dispatcher.Invoke(() => AddControl());
        }

        public void MuteAudio(string trackId)
        {
            RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

            if (audioTrack == null) return;

            Dispatcher.Invoke(() => ChangeControl("micro:已静音"));
        }

        public void UnMuteAudio(string trackId)
        {
            RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

            if (audioTrack == null) return;

            Dispatcher.Invoke(() => ChangeControl("micro:已开麦"));

        }

        public void QualityUpdated()
        {
            connectionQualityInfo = engine.GetParticipantConnectionQuality(participantInfo?.Identity ?? string.Empty);

            void ChangeScore(ConnectionQualityInfo connectionQualityInfo)
            {
                this.Quality.Text = $"Quality：{connectionQualityInfo.Quality} Score: {connectionQualityInfo.Score}";
            }

            Dispatcher.Invoke(() => ChangeScore(connectionQualityInfo));
        }


        public void Speaking()
        {
            async void Speak()
            {
                speaker.Text = "speaking:true";
                await Task.Delay(2000);
                speaker.Text = "speaking:false";
            }
            Dispatcher.Invoke(() => Speak());
        }

        public static Bitmap ConvertI420AToBitmap(I420AVideoFrame frame)
        {
            // 创建目标 Bitmap (ARGB格式)
            Bitmap bitmap = new Bitmap((int)frame.width, (int)frame.height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // 锁定 Bitmap 的位图数据
            BitmapData bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
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

        void AddContextMenu(System.Windows.Controls.Image pictureBox, string trackId)
        {
            ContextMenu contextMenu = new ContextMenu();

            var track = this.engine.RemoteParticipants.Where(v => v.Identity == this.Name).Select(v => v.Tracks.Where(s => s.Sid == trackId).FirstOrDefault()).FirstOrDefault();

            if (track == null) return;

            foreach (var item in track.Layers)
            {
                //contextMenu.Items.Add()
                // 添加菜单项
                MenuItem menuItem = new MenuItem();
                menuItem.Header = $"{item.Width}x{item.Height}";
                menuItem.Click += (sender, e) => engine.Subscribe(trackId, item.Quality);
                contextMenu.Items.Add(menuItem);
                contextMenu.Items.Add(new Separator());
            }
            pictureBox.ContextMenu = contextMenu;
        }

    }
}
