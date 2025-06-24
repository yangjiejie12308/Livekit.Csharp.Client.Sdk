using Godot;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public partial class LkUserControl : Control
{
    public ParticipantInfo? participantInfo;
    public Client.Sdk.Dotnet.core.Engine engine;
    private ConnectionQualityInfo connectionQualityInfo = new ConnectionQualityInfo();
    private GridContainer grid;
    public void Init(ParticipantInfo participant, Client.Sdk.Dotnet.core.Engine engine)
    {
        this.participantInfo = participant;
        this.engine = engine;
        var control = this.GetNode<Label>("HBoxContainer/Identity");
        control.Text = participant.Identity;
        grid = this.GetNode<GridContainer>("GridContainer");
    }


    public void AddVideo(string trackId)
    {

        RemoteVideoTrack videoTrack = this.engine.GetTrackStream(trackId);

        if (videoTrack == null) return;

        if (grid.GetNodeOrNull<TextureRect>($"{trackId}") != null) return;

        TextureRect newVideoBox = new TextureRect
        {
            Name = trackId,
            StretchMode = TextureRect.StretchModeEnum.Scale, // 保持比例填充并裁剪超出部分
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, // 可选，通常默认即可
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Godot.Vector2(200, 150) // 设置按钮最小尺寸
        };

        videoTrack.I420AVideoFrameReady += (frame) =>
        {

            var bitMAP = ConvertI420AToBitmap(frame);
            var texture = ImageTexture.CreateFromImage(bitMAP);
            newVideoBox.CallDeferred("set_texture", texture);

        };

        CallDeferred(nameof(AddControl), newVideoBox);
    }

    void AddControl(TextureRect newVideoBox)
    {

        grid.AddChild(newVideoBox);
    }

    void RemoveControl(Godot.TextureRect pictureBox)
    {
        grid.RemoveChild(pictureBox);
    }

    public void RemoveVideo(string videoTrackName)
    {
        var control = grid.GetNodeOrNull<TextureRect>($"{videoTrackName}");

        if (control != null)
        {
            CallDeferred(nameof(RemoveControl), control);
        }
    }

    public void AddAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;

        CallDeferred(nameof(ChangeControl), "已发布");
    }
    void ChangeControl(string text)
    {
        var control = this.GetNode<Label>("HBoxContainer/microphone");
        control.Text = text;
    }
    public void RemoveAudio(string trackId)
    {
        RemoteAudioTrack audioTrack = this.engine.GetAudioTrackStream(trackId);
        if (audioTrack == null) return;

        CallDeferred(nameof(ChangeControl), "未发布");
    }

    public void MuteVideo(string trackId)
    {
        var control = grid.GetNodeOrNull<TextureRect>($"{trackId}");
        if (control != null)
        {

            CallDeferred(nameof(RemoveControl), control);
        }
    }

    public void UnMuteVideo(string trackId)
    {
        RemoteVideoTrack? videoTrack = this.engine.GetTrackStream(trackId);

        if (videoTrack == null) return;

        if (grid.GetNodeOrNull<TextureRect>($"{trackId}") != null) return;

        TextureRect newVideoBox = new TextureRect
        {
            Name = trackId,
            StretchMode = TextureRect.StretchModeEnum.Scale, // 保持比例填充并裁剪超出部分
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, // 可选，通常默认即可
            CustomMinimumSize = new Godot.Vector2(200, 150) // 设置按钮最小尺寸
        };

        videoTrack.I420AVideoFrameReady += (frame) =>
        {

            var bitMAP = ConvertI420AToBitmap(frame);
            var texture = ImageTexture.CreateFromImage(bitMAP);
            newVideoBox.CallDeferred("set_texture", texture);
        };

        CallDeferred(nameof(AddControl), newVideoBox);

    }


    public void MuteAudio(string trackId)
    {
        RemoteAudioTrack audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;

        CallDeferred(nameof(ChangeControl), "micro:已静音");
    }

    public void UnMuteAudio(string trackId)
    {
        RemoteAudioTrack? audioTrack = this.engine.GetAudioTrackStream(trackId);

        if (audioTrack == null) return;
        CallDeferred(nameof(ChangeControl), "micro:已开麦");
    }

    public void QualityUpdated()
    {
        connectionQualityInfo = engine.GetParticipantConnectionQuality(participantInfo?.Identity ?? string.Empty);

        CallDeferred(nameof(ChangeScore), $"Quality：{connectionQualityInfo.Quality} Score: {connectionQualityInfo.Score}");
    }

    void ChangeScore(string str)
    {
        var Quality = this.GetNode<Label>("HBoxContainer/Quality");
        Quality.Text = str;
    }


    public void Speaking()
    {

        CallDeferred(nameof(Speak));
    }

    async void Speak()
    {
        var speaker = this.GetNode<Label>("HBoxContainer/speaker");
        speaker.Text = "speaking:true";
        await Task.Delay(2000);
        speaker.Text = "speaking:false";
    }

    public static Godot.Image ConvertI420AToBitmap(I420AVideoFrame frame)
    {
        int width = (int)frame.width;
        int height = (int)frame.height;
        int ySize = width * height;
        int uvSize = ySize / 4;
        int totalSize = ySize + uvSize * 2;
        bool hasAlpha = frame.dataA != IntPtr.Zero;
        if (hasAlpha)
            totalSize += ySize;

        byte[] yuvData = new byte[totalSize];
        frame.CopyTo(yuvData);

        byte[] bgraData = new byte[width * height * 4];
        ConvertYUVAToARGB(yuvData, bgraData, width, height, hasAlpha);

        // 转换为 RGBA 顺序
        byte[] rgbaData = new byte[bgraData.Length];
        BgraToRgba(bgraData, rgbaData);

        var image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, rgbaData);
        return image;

    }

    private static void BgraToRgba(byte[] bgra, byte[] rgba)
    {
        for (int i = 0; i < bgra.Length; i += 4)
        {
            rgba[i] = bgra[i + 2]; // R
            rgba[i + 1] = bgra[i + 1]; // G
            rgba[i + 2] = bgra[i];     // B
            rgba[i + 3] = bgra[i + 3]; // A
        }
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
