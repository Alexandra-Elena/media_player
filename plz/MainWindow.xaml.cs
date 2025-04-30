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

    public partial class MainWindow : Window
    {
        private bool IsPlaying = false;
        private bool IsUserDraggingSlider = false;
        private readonly List<string> Playlist = new();


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

        private void UpdateThumbnail(string filePath)
        {
            try
            {
                var tfile = TagLib.File.Create(filePath);

                bool isAudio = tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Audio)
                               && !tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video);

                if (isAudio)
                {
                    ThumbnailImage.Visibility = Visibility.Visible;


                    if (tfile.Tag.Pictures.Length > 0)
                    {
                        var bin = (byte[]?)tfile.Tag.Pictures[0].Data.Data;
                        if (bin != null)
                        {
                            var image = new System.Windows.Media.Imaging.BitmapImage();
                            using var memStream = new MemoryStream(bin);

                            image.BeginInit();
                            image.StreamSource = memStream;
                            image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                            image.EndInit();
                            image.Freeze();

                            ThumbnailImage.Source = image;
                            return;
                        }
                    }

                    ThumbnailImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/media_player;component/thumbnail/music.png"));
                }
                else
                {
                    // Let MediaOpened handle thumbnail hiding
                }
            }
            catch
            {
                ThumbnailImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/media_player;component/thumbnail/music.png"));
                ThumbnailImage.Visibility = Visibility.Visible;
            }
        }


        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MediaOpenDialog.ShowDialog() == true)
            {
                Player.Source = new Uri(MediaOpenDialog.FileName);
                TitleLbl.Content = Path.GetFileName(MediaOpenDialog.FileName);

                UpdateThumbnail(MediaOpenDialog.FileName); // New line

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
            if (string.IsNullOrEmpty(MediaOpenDialog.FileName) || Player.Source == null)
                return;

            try
            {
                var tfile = TagLib.File.Create(MediaOpenDialog.FileName);
                StringBuilder sb = new();

                // Use MediaElement duration if available and valid
                TimeSpan mediaDuration = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;
                sb.AppendLine("Duration: " + mediaDuration.ToString(@"hh\:mm\:ss"));

                if (tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Audio))
                {
                    sb.AppendLine("Audio bitrate: " + tfile.Properties.AudioBitrate + " kbps");
                    sb.AppendLine("Audio sample rate: " + tfile.Properties.AudioSampleRate + " Hz");
                    sb.AppendLine("Audio channels: " + (tfile.Properties.AudioChannels == 1 ? "Mono" : "Stereo"));
                }

                if (tfile.Properties.MediaTypes.HasFlag(TagLib.MediaTypes.Video))
                {
                    sb.AppendLine($"Video resolution: {tfile.Properties.VideoWidth} x {tfile.Properties.VideoHeight}");
                }

                MessageBox.Show(sb.ToString(), "Properties");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error reading file properties: " + ex.Message);
            }
        }

        private void AddToPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            MediaOpenDialog.Multiselect = true;

            if (MediaOpenDialog.ShowDialog() == true)
            {
                foreach (var file in MediaOpenDialog.FileNames)
                {
                    Playlist.Add(file);
                    PlaylistBox.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void PlaylistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = PlaylistBox.SelectedIndex;
            if (index >= 0 && index < Playlist.Count)
            {
                string selectedFile = Playlist[index];
                Player.Source = new Uri(selectedFile);
                TitleLbl.Content = Path.GetFileName(selectedFile);

                UpdateThumbnail(selectedFile);
                Player.Play();
                IsPlaying = true;
                PlayPauseBtn.IsEnabled = true;
                (PlayPauseIcon.Content as TextBlock)!.Text = "⏸️";
            }
        }

        private void Player_MediaOpened(object? sender, EventArgs e)
        {
            if (Player.NaturalVideoWidth <= 0 || Player.NaturalVideoHeight <= 0)
                return;
            Player.ClearValue(WidthProperty);
            Player.ClearValue(HeightProperty);

            ThumbnailImage.Visibility = Visibility.Collapsed;
            Player.Stretch = Stretch.Uniform;
        }

        #endregion
    }
}
