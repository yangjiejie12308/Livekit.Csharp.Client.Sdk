using Client.Sdk.Dotnet.core;
using LiveKit.Proto;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client.Sdk.Dotnet.Example
{
    public partial class Form2 : Form
    {
        private Engine engine;
        public Form2()
        {
            InitializeComponent();
            engine = new Engine("ws://127.0.0.1:7880", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTA2MDM2OTAsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIxMSIsIm5iZiI6MTc1MDI1ODA5MCwic3ViIjoidGVzdF91c2VyMTEiLCJ2aWRlbyI6eyJyb29tIjoidGVzdF9yb29tIiwicm9vbUpvaW4iOnRydWV9fQ.u-iQ_L5-f9APZl6MkC8U54xtR5IqzGsZDraIx8zOQ6s");
            InitAsync().ConfigureAwait(false);
        }

        private async Task InitAsync()
        {
            engine.RemoteParticipantUpdated += Engine_RemoteParticipantUpdated;
            engine.onStreamUpdated += Engine_onStreamUpdated;
            await engine.ConnectAsync();
        }

        private void Engine_onStreamUpdated(object? sender, StreamStateInfo e)
        {
            if (e.State == StreamState.Active)
            {
                RenderFrameToBox(engine.GetTrackStream(e.TrackSid), e);
            }
        }

        private void Engine_RemoteParticipantUpdated(object? sender, ParticipantInfo e)
        {
            Debug.WriteLine($"Remote participant updated: {e.ToString()}");

        }



        private readonly Dictionary<string, PictureBox> _videoBoxes = new();

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

        private void RenderFrameToBox(VideoEncoderEndPoint videoEncoder, StreamStateInfo streamStateInfo)
        {
            Debug.WriteLine($"streamStateInfo: {streamStateInfo.ToString()}");

            if (!_videoBoxes.ContainsKey(streamStateInfo.TrackSid))
            {
                AddOrUpdateVideoBox(streamStateInfo.TrackSid);
            }

            videoEncoder.OnVideoSinkDecodedSample += (byte[] bmp, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat) =>
            {
                Debug.WriteLine($"TrackSid:{streamStateInfo.TrackSid} bytes width:{width} height:{height}");
                // 假设 bmp 是 byte[]，你需要转 Bitmap
                using var bitmap = new Bitmap((int)width, (int)height, PixelFormat.Format24bppRgb);


                var bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, (int)width, (int)height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    bitmap.PixelFormat);

                System.Runtime.InteropServices.Marshal.Copy(bmp, 0, bmpData.Scan0, bmp.Length);
                bitmap.UnlockBits(bmpData);

                var bitMAP = (Bitmap)bitmap.Clone();

                if (_videoBoxes.TryGetValue(streamStateInfo.TrackSid, out var pb))
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
    }
}
