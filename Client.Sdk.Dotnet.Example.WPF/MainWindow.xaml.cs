using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.Sdk.Dotnet.core;
using LiveKit.Proto;

namespace Client.Sdk.Dotnet.Example.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Engine engine;
        public MainWindow()
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
            await engine.ConnectAsync();
        }

        private void Engine_onSpeakersChangedEvent(object? sender, List<SpeakerInfo> e)
        {
            foreach (var item in e)
            {
                Dispatcher.Invoke(() =>
                {
                    var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == item.Sid);
                    control.Speaking();
                });
            }
        }

        private void Engine_onParticipantConnectionQualityUpdated(object? sender, string e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e);
                if (control != null)
                {
                    control.QualityUpdated();
                }
            });
        }

        private void Engine_onVideoTrackUnMuted(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.UnMuteVideo(e.Item2);
                }
            });
        }

        private void Engine_onVideoTrackMuted(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.MuteVideo(e.Item2);
                }
            });
        }

        private void Engine_onAudioTrackMuted(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.MuteAudio(e.Item2);
                }
            });
        }

        private void Engine_onAudioTrackUnMuted(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.UnMuteAudio(e.Item2);
                }
            });
        }

        private void Engine_onAudioTrackRemoved(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.RemoveAudio(e.Item2);
                }
            });
        }

        private void Engine_onAudioTrackAdded(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var c = this.panelVideoContainer.Children.OfType<lkUserControl>().ToList();
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.AddAudio(e.Item2);
                }
            });
        }

        private void Engine_onVideoTrackRemoved(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.RemoveVideo(e.Item2);
                }
            });
        }

        private void Engine_onVideoTrackAdded(object? sender, (string, string) e)
        {
            Dispatcher.Invoke(() =>
            {
                var c = this.panelVideoContainer.Children.OfType<lkUserControl>().ToList();
                var control = this.panelVideoContainer.Children.OfType<lkUserControl>().FirstOrDefault(v => v.participantInfo.Sid == e.Item1);
                if (control != null)
                {
                    control.AddVideo(e.Item2);
                }
            });
        }


        private void Engine_RemoteParticipantUpdated(object? sender, ParticipantInfo e)
        {
            Debug.WriteLine($"Remote participant updated: {e.ToString()}");

            void AddControl()
            {
                var control = this.panelVideoContainer.Children
                    .OfType<lkUserControl>()
                    .FirstOrDefault(v => v.participantInfo.Identity == e.Identity);
                if (control == null)
                {
                    lkUserControl lkUserControl = new(participant: e, engine: engine)
                    {
                        Name = e.Identity,

                    };
                    this.panelVideoContainer.Children.Add(lkUserControl);
                }
            }

            if (Dispatcher.CheckAccess())
            {
                AddControl();
            }
            else
            {
                Dispatcher.Invoke(AddControl);
            }
            // 保证在UI线程执行
        }
    }
}