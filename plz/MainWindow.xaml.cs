using System;
using System.Windows;
using System.IO;
using System.Windows.Threading;
using System.Text;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Controls;

namespace media_player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        private bool IsUserDraggingSlider = false;

        private readonly DispatcherTimer Timer = new() { Interval = TimeSpan.FromSeconds(0.1) };
        private readonly OpenFileDialog MediaOpenDialog = new()
        {
            Title = "Open a media file",
            Filter = "Media Files (*.mp3,*.mp4)|*.mp3;*.mp4"
        };

        public MainWindow()
        {
            InitializeComponent();
            Player.MediaOpened += Player_MediaOpened;


            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Player.Source != null && Player.NaturalDuration.HasTimeSpan && !IsUserDraggingSlider)
            {
                ProgressSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
                ProgressSlider.Value = Player.Position.TotalSeconds;
            }
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MediaOpenDialog.ShowDialog() == true)
            {
                Player.Source = new Uri(MediaOpenDialog.FileName);
                TitleLbl.Content = Path.GetFileName(MediaOpenDialog.FileName);

                Player.Play();
                IsPlaying = true;
                PlayPauseBtn.IsEnabled = true;                                   
                (PlayPauseIcon.Content as TextBlock)!.Text = "⏸️";
            }
        }

        #region Media Controls


        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source != null)
            {
                Player.Stop();        
                IsPlaying = false;
                (PlayPauseIcon.Content as TextBlock)!.Text = "▶️";
            }
        }

        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
                return;

            if (IsPlaying)
            {
                Player.Pause();
                IsPlaying = false;

                (PlayPauseIcon.Content as TextBlock)!.Text = "▶️";
            }
            else
            {
                Player.Play();
                IsPlaying = true;

                (PlayPauseIcon.Content as TextBlock)!.Text = "⏸️";
            }
        }


        private void ProgressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            IsUserDraggingSlider = true;
        }

        private void ProgressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            IsUserDraggingSlider = false;
            Player.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            StatusLbl.Text = TimeSpan.FromSeconds(ProgressSlider.Value).ToString(@"hh\:mm\:ss");
        }

        #endregion
        #region Properties

        private void PropertiesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MediaOpenDialog.FileName != "")
            {
                var tfile = TagLib.File.Create(MediaOpenDialog.FileName);
                StringBuilder sb = new();

                sb.AppendLine("Duration: " + tfile.Properties.Duration.ToString(@"hh\:mm\:ss"));

                if (tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Audio))
                {
                    sb.AppendLine("Audio bitrate: " + tfile.Properties.AudioBitrate);
                    sb.AppendLine("Audio sample rate: " + tfile.Properties.AudioSampleRate);
                    sb.AppendLine("Audio channels: " + (tfile.Properties.AudioChannels == 1 ? "Mono" : "Stereo"));
                }

                if (tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video))
                {
                    sb.AppendLine($"Video resolution: {tfile.Properties.VideoWidth} x {tfile.Properties.VideoHeight}");
                }

                MessageBox.Show(sb.ToString(), "Properties");
            }
        }

        private void Player_MediaOpened(object? sender, EventArgs e)
        {
            if (Player.NaturalVideoWidth > 0 && Player.NaturalVideoHeight > 0)
            {
                double videoWidth = Player.NaturalVideoWidth;
                double videoHeight = Player.NaturalVideoHeight;

                // Resize the Player to fit video
                Player.Width = videoWidth;
                Player.Height = videoHeight;

                // Resize the Window too (with some margin maybe)
                this.Width = videoWidth + 20;  // small padding
                this.Height = videoHeight + 40; // small padding
            }
        }


        #endregion
    }
}
