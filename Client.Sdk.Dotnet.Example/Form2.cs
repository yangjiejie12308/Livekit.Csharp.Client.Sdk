using Client.Sdk.Dotnet.core;
using Client.Sdk.Dotnet.hardware;
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
    public partial class Form2 : Form
    {
        private Engine engine;

        private HardWare hardWare = new HardWare();
        public Form2()
        {
            InitializeComponent();
            engine = new Engine("ws://127.0.0.1:7880", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTA5ODg1MDgsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIzIiwibmJmIjoxNzUwNjQyOTA4LCJzdWIiOiJ0ZXN0X3VzZXIzIiwidmlkZW8iOnsicm9vbSI6InRlc3Rfcm9vbSIsInJvb21Kb2luIjp0cnVlfX0.TbYGQf6fPCdaesiJ-zco1B2_NGFphO1tecWwWvPj_No");
            InitAsync().ConfigureAwait(false);
        }


        private async Task InitAsync()
        {
            engine.RemoteParticipantUpdated += Engine_RemoteParticipantUpdated;
            engine.onVideoTrackAdded += Engine_onVideoTrackAdded;
            engine.onVideoTrackRemoved += Engine_onVideoTrackRemoved;
            engine.onAudioTrackAdded += Engine_onAudioTrackAdded;
            engine.onAudioTrackRemoved += Engine_onAudioTrackRemoved;
            engine.onAudioTrackUnMuted += Engine_onAudioTrackUnMuted;
            engine.onAudioTrackMuted += Engine_onAudioTrackMuted;
            engine.onVideoTrackMuted += Engine_onVideoTrackMuted;
            engine.onVideoTrackUnMuted += Engine_onVideoTrackUnMuted;
            engine.onParticipantConnectionQualityUpdated += Engine_onParticipantConnectionQualityUpdated;
            engine.onSpeakersChangedEvent += Engine_onSpeakersChangedEvent;
            engine.LocalParticipantUpdated += Engine_LocalParticipantUpdated;

            await engine.ConnectAsync();
        }

        private void Engine_LocalParticipantUpdated(object? sender, ParticipantInfo e)
        {
            if (Identity.InvokeRequired)
            {
                Identity.Invoke((Action)(() => Identity.Text = e.Identity));
            }
            else
            {
                Identity.Text = e.Identity;
            }
        }

        private void Engine_onSpeakersChangedEvent(object? sender, List<SpeakerInfo> e)
        {
            foreach (var item in e)
            {
                var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == item.Sid);
                if (control != null)
                {
                    control.QualityUpdated();
                }

                if (item.Sid == engine.LocalParticipant.Sid)
                {
                    if (speaker.InvokeRequired)
                    {
                        speaker.Invoke((Action)(() => Speaking()));
                    }
                    else
                    {
                        Speaking();
                    }
                }
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

            if (this.speaker.InvokeRequired)
            {
                this.speaker.Invoke((Action)(() => Speak()));
            }
            else
            {
                Speak();
            }
        }

        private void Engine_onParticipantConnectionQualityUpdated(object? sender, string e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e);
            if (control != null)
            {
                control.QualityUpdated();
            }
        }

        private void Engine_onVideoTrackUnMuted(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.UnMuteVideo(e.Item2);
            }
        }

        private void Engine_onVideoTrackMuted(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.MuteVideo(e.Item2);
            }
        }

        private void Engine_onAudioTrackMuted(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.MuteAudio(e.Item2);
            }
        }

        private void Engine_onAudioTrackUnMuted(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.UnMuteAudio(e.Item2);
            }
        }

        private void Engine_onAudioTrackRemoved(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.RemoveAudio(e.Item2);
            }
        }

        private void Engine_onAudioTrackAdded(object? sender, (string, string) e)
        {
            var c = this.panelVideoContainer.Controls.OfType<lkUserControl>().ToList();
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.AddAudio(e.Item2);
            }
        }

        private void Engine_onVideoTrackRemoved(object? sender, (string, string) e)
        {
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.RemoveVideo(e.Item2);
            }
        }

        private void Engine_onVideoTrackAdded(object? sender, (string, string) e)
        {
            var c = this.panelVideoContainer.Controls.OfType<lkUserControl>().ToList();
            var control = this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
            if (control != null)
            {
                control.AddVideo(e.Item2);
            }
        }


        private void Engine_RemoteParticipantUpdated(object? sender, ParticipantInfo e)
        {
            Debug.WriteLine($"Remote participant updated: {e.ToString()}");

            void AddControl()
            {
                var control = this.panelVideoContainer.Controls
                    .OfType<lkUserControl>()
                    .FirstOrDefault(v => v.participantInfo.Identity == e.Identity);
                if (control == null)
                {
                    lkUserControl lkUserControl = new(participant: e, engine: engine)
                    {
                        Dock = DockStyle.Fill,
                        Name = e.Identity
                    };
                    this.panelVideoContainer.Controls.Add(lkUserControl);
                }
            }

            if (this.panelVideoContainer.InvokeRequired)
            {
                this.panelVideoContainer.Invoke((Action)(AddControl));
            }
            else
            {
                AddControl();
            }
        }

        private void openVideo_Click(object sender, EventArgs e)
        {
            try
            {
                engine.OpenVideo().ContinueWith((t) =>
                {
                    engine.webcamSource.I420AVideoFrameReady += (frame) =>
                    {
                        var bitMAP = lkUserControl.ConvertI420AToBitmap(frame);

                        if (webcamera.InvokeRequired)
                        {
                            webcamera.Invoke(new Action(() =>
                            {
                                webcamera.Image?.Dispose();
                                webcamera.Image = bitMAP;
                            }));
                        }
                        else
                        {
                            webcamera.Image?.Dispose();
                            webcamera.Image = bitMAP;
                        }
                    };
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("未能成功开启摄像头");
            }

        }

        private void openAudio_Click(object sender, EventArgs e)
        {
            try
            {
                engine.OpenAudio().ConfigureAwait(false);

                if (microphone.InvokeRequired)
                {
                    microphone.Invoke((Action)(() => microphone.Text = "已打开麦克风"));
                }
                else
                {
                    microphone.Text = "已打开麦克风";
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("未能成功开启麦克风");
            }
        }
    }
}
