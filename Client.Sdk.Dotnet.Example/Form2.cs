using Client.Sdk.Dotnet.core;
using Client.Sdk.Dotnet.hardware;
using LiveKit.Proto;
using Microsoft.MixedReality.WebRTC;
using SIPSorceryMedia.Abstractions;
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

        //private HardWare hardWare = new HardWare();
        public Form2()
        {
            InitializeComponent();
            //hardWare.GetAllScreen();
            engine = new Engine("ws://127.0.0.1:7880", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NTA5NDk1NDgsImlzcyI6ImRldmtleSIsIm5hbWUiOiJ0ZXN0X3VzZXIxMTQiLCJuYmYiOjE3NTA2MDM5NDgsInN1YiI6InRlc3RfdXNlcjExNCIsInZpZGVvIjp7InJvb20iOiJ0ZXN0X3Jvb20iLCJyb29tSm9pbiI6dHJ1ZX19.yavYAZK6qKuAVjMPPtqVwX2GB3N0oimk9ktcB-YFPNA");
            InitAsync().ConfigureAwait(false);
        }

        private async Task InitAsync()
        {
            //engine.RemoteParticipantUpdated += Engine_RemoteParticipantUpdated;
            //engine.onVideoTrackAdded += Engine_onVideoTrackAdded;
            //engine.onVideoTrackRemoved += Engine_onVideoTrackRemoved;
            await engine.ConnectAsync();
        }

        private void Engine_onVideoTrackRemoved(object? sender, (string, string) e)
        {
          var control =   this.panelVideoContainer.Controls.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
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
  }
}
