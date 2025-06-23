using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Client.Sdk.Dotnet.core;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using DirectShowLib;

namespace Client.Sdk.Dotnet.Example.Avalonia;

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

        if (this.videoPanels.Children.OfType<Image>().Any(v => v.Name == trackId)) return;

        void AddControl()
        {
            Image newVideoBox = new Image
            {
                Name = trackId,
                Stretch = Stretch.Fill,
            };

            videoTrack.I420AVideoFrameReady += (frame) =>
            {

                var bitMAP = ConvertI420AToBitmap(frame);

                Dispatcher.UIThread.Post(() =>
                {
                    newVideoBox.Source = bitMAP;
                });
            };

            AddContextMenu(newVideoBox, trackId);
            this.videoPanels.Children.Add(newVideoBox);
        }


        // ��֤��UI�߳�ִ��
        if (Dispatcher.UIThread.CheckAccess())
        {
            AddControl();
        }
        else
        {
            Dispatcher.UIThread.Post(AddControl);
        }
    }

    public void RemoveVideo(string videoTrackName)
    {
        Image? pictureBox = this.videoPanels.Children
              .OfType<Image>()
              .FirstOrDefault(pb => pb.Name == videoTrackName);

        void RemoveControl(Image pictureBox)
        {
            this.videoPanels.Children.Remove(pictureBox);
        }

        if (pictureBox != null)
        {

            // ��֤��UI�߳�ִ��
            if (Dispatcher.UIThread.CheckAccess())
            {
                RemoveControl(pictureBox);
            }
            else
            {
                Dispatcher.UIThread.Post(() => RemoveControl(pictureBox));
            }
        }
    }

    public void AddAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;

        // ��֤��UI�߳�ִ��
        if (Dispatcher.UIThread.CheckAccess())
        {
            ChangeControl("�ѷ���");
        }
        else
        {
            Dispatcher.UIThread.Post(() => ChangeControl("�ѷ���"));
        }
    }
    void ChangeControl(string text)
    {
        this.microphone.Text = text;
    }
    public void RemoveAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);
        if (audioTrack == null) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ChangeControl("δ����");
        }
        else
        {
            Dispatcher.UIThread.Post(() => ChangeControl("δ����"));
        }
    }

    public void MuteVideo(string trackId)
    {
        Image? pictureBox = this.videoPanels.Children
              .OfType<Image>()
              .FirstOrDefault(pb => pb.Name == trackId);

        void RemoveControl(Image pictureBox)
        {
            this.videoPanels.Children.Remove(pictureBox);
        }

        if (pictureBox != null)
        {

            if (Dispatcher.UIThread.CheckAccess())
            {
                RemoveControl(pictureBox);
            }
            else
            {
                Dispatcher.UIThread.Post(() => RemoveControl(pictureBox));
            }
        }
    }

    public void UnMuteVideo(string trackId)
    {
        RemoteVideoTrack? videoTrack = this.engine.GetTrackStream(trackId);

        if (videoTrack == null) return;

        if (this.videoPanels.Children.OfType<Image>().Any(v => v.Name == trackId)) return;


        void AddControl()
        {
            Image newVideoBox = new Image
            {
                Name = trackId,
                Stretch = Stretch.Fill,

            };

            AddContextMenu(newVideoBox, trackId);
            this.videoPanels.Children.Add(newVideoBox);
            videoTrack.I420AVideoFrameReady += (frame) =>
            {
                var bitMAP = ConvertI420AToBitmap(frame);
                Dispatcher.UIThread.Post(() =>
                {
                    newVideoBox.Source = bitMAP;
                });
            };
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            AddControl();
        }
        else
        {
            Dispatcher.UIThread.Post(() => AddControl());
        }


    }

    public void MuteAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ChangeControl("micro:�Ѿ���");
        }
        else
        {
            Dispatcher.UIThread.Post(() => ChangeControl("micro:�Ѿ���"));
        }
    }

    public void UnMuteAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            ChangeControl("micro:�ѿ���");
        }
        else
        {
            Dispatcher.UIThread.Post(() => ChangeControl("micro:�ѿ���"));
        }

    }

    public void QualityUpdated()
    {
        connectionQualityInfo = engine.GetParticipantConnectionQuality(participantInfo?.Identity ?? string.Empty);

        void ChangeScore(ConnectionQualityInfo connectionQualityInfo)
        {
            this.Quality.Text = $"Quality��{connectionQualityInfo.Quality} Score: {connectionQualityInfo.Score}";
        }


        if (Dispatcher.UIThread.CheckAccess())
        {
            ChangeScore(connectionQualityInfo);
        }
        else
        {
            Dispatcher.UIThread.Post(() => ChangeScore(connectionQualityInfo));
        }
    }


    public void Speaking()
    {
        async void Speak()
        {
            speaker.Text = "speaking:true";
            await Task.Delay(2000);
            speaker.Text = "speaking:false";
        }
        if (Dispatcher.UIThread.CheckAccess())
        {
            Speak();
        }
        else
        {
            Dispatcher.UIThread.Post(() => Speak());
        }
    }

    public static Bitmap ConvertI420AToBitmap(I420AVideoFrame frame)
    {
        // ���� I420A �����ܴ�С (Y + U + V + A)
        int ySize = (int)(frame.width * frame.height);
        int uvSize = ySize / 4; // ɫ��ƽ�������ȵ� 1/4
        int totalSize = ySize + uvSize * 2;
        if (frame.dataA != IntPtr.Zero)
            totalSize += ySize; // Alpha ƽ���С�� Y ƽ����ͬ

        // ������ʱ�������洢 YUV(A) ����
        byte[] yuvData = new byte[totalSize];
        frame.CopyTo(yuvData);

        // ���� ARGB ���������
        byte[] argbData = new byte[frame.width * frame.height * 4];

        // ת�� YUV(A) �� ARGB (ʹ�����з���)
        ConvertYUVAToARGB(yuvData, argbData, (int)frame.width, (int)frame.height, frame.dataA != IntPtr.Zero);

        // ���� Avalonia �� Bitmap
        var bitmap = new WriteableBitmap(
            new PixelSize((int)frame.width, (int)frame.height),
            new Vector(96, 96), // DPI (��׼Ϊ96)
            PixelFormat.Bgra8888, // Avalonia ʹ�� BGRA ������ ARGB
            AlphaFormat.Premul);

        // ����������д�� Avalonia �� Bitmap
        using (var frameBuffer = bitmap.Lock())
        {
            // ע�⣺������Ҫ��������˳����ΪAvaloniaʹ��BGRA�������ǵ����ݿ�����ARGB
            SwapRedAndBlueChannels(argbData); // ��ARGBת��ΪBGRA

            // �������ݵ�Bitmap
            Marshal.Copy(argbData, 0, frameBuffer.Address, argbData.Length);
        }

        return bitmap;
    }
    // ������������������ͨ������ARGBת��ΪBGRA
    private static void SwapRedAndBlueChannels(byte[] pixelData)
    {
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            // ���� R (index+0) �� B (index+2)
            byte temp = pixelData[i];
            pixelData[i] = pixelData[i + 2];
            pixelData[i + 2] = temp;
        }
    }

    private static void ConvertYUVAToARGB(byte[] yuvData, byte[] argbData, int width, int height, bool hasAlpha)
    {
        int ySize = width * height;
        int uvSize = ySize / 4;

        // ȷ�� Y��U��V ���ݲ��ᳬ������߽�
        if (yuvData.Length < ySize + uvSize * 2)
        {
            Debug.WriteLine("YUV ���ݻ�������С����");
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int yIndex = y * width + x;

                // ���� UV �������㣬ȷ������Խ��
                int uvY = y >> 1; // y / 2
                int uvX = x >> 1; // x / 2
                int uvWidth = width >> 1; // width / 2
                int uvIndex = uvY * uvWidth + uvX;

                // ȷ����������Ч��Χ��
                if (yIndex >= ySize || ySize + uvIndex >= ySize + uvSize ||
                    ySize + uvSize + uvIndex >= ySize + uvSize * 2)
                {
                    continue; // ������Ч����
                }

                // ��ȡ YUV ֵ
                byte Y = yuvData[yIndex];
                byte U = yuvData[ySize + uvIndex];
                byte V = yuvData[ySize + uvSize + uvIndex];

                // YUV �� RGB ת��
                int C = Y - 16;
                int D = U - 128;
                int E = V - 128;

                // YUV �� RGB ת����ʽ
                //int R = (298 * C + 409 * E + 128) >> 8;
                //int G = (298 * C - 100 * D - 208 * E + 128) >> 8;
                //int B = (298 * C + 516 * D + 128) >> 8;

                // �ü�ֵ�� 0-255 ��Χ
                byte R = (byte)Math.Max(0, Math.Min(255, (298 * C + 409 * E + 128) >> 8));
                byte G = (byte)Math.Max(0, Math.Min(255, (298 * C - 100 * D - 208 * E + 128) >> 8));
                byte B = (byte)Math.Max(0, Math.Min(255, (298 * C + 516 * D + 128) >> 8));

                // ���� Alpha ֵ
                byte A = hasAlpha ? yuvData[ySize + uvSize * 2 + yIndex] : (byte)255;

                // ��� ARGB ���� (BGRA ˳����Ϊ Windows Bitmap �� BGRA)
                int destIndex = (y * width + x) * 4;
                argbData[destIndex] = B;     // Blue
                argbData[destIndex + 1] = G; // Green
                argbData[destIndex + 2] = R; // Red
                argbData[destIndex + 3] = A; // Alpha
            }
        }
    }

    void AddContextMenu(Image pictureBox, string trackId)
    {
        ContextMenu contextMenu = new ContextMenu();

        var track = this.engine.RemoteParticipants.Where(v => v.Identity == this.Name).Select(v => v.Tracks.Where(s => s.Sid == trackId).FirstOrDefault()).FirstOrDefault();

        if (track == null) return;

        foreach (var item in track.Layers)
        {
            //contextMenu.Items.Add()
            // ��Ӳ˵���
            MenuItem menuItem = new MenuItem();
            menuItem.Header = $"{item.Width}x{item.Height}";
            menuItem.Click += (sender, e) => engine.Subscribe(trackId, item.Quality);
            contextMenu.Items.Add(menuItem);
            contextMenu.Items.Add(new Separator());
        }
        pictureBox.ContextMenu = contextMenu;
    }

}