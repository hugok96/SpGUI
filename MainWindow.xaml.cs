using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpGUI
{
    public partial class MainWindow : Window
    {
        private readonly WindowDataContext db = new WindowDataContext();
        private string _outputPath = null;
        private readonly List<string> _outputFiles = new List<string>();
        private readonly List<AudioTrack> _audioTracks = new List<AudioTrack>();
        private string _stemType;

        private readonly Mixer _mixer = new Mixer();
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        private bool _updatingTime = false;

        private double _test;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = db;
            this.PreviewMouseUp += MainWindow_PreviewMouseLeftButtonUp;
        }

        private async void MainWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("UP!");
            if (TrackPosition.IsMouseOver) {
                if (_updatingTime)
                    return;

                _updatingTime = true;
                _timer.Stop();
                _mixer.Pause();
                _mixer.Seek(new TimeSpan(0, 0, 0, 0, (int)(_test * 1000)));
                await Task.Delay(500);
                _mixer.Play();
                _timer.Start();
                _updatingTime = false;
            }
        }

        private void SetStatusText(String status, bool? loading = null)
        {
            db.WindowStatus = status;
            if (null != loading)
                SetLoading(loading.Value);
        }

        private void SetLoading(bool loading) => db.WindowReady = !loading;

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            SetStatusText("Selecting file...", true);
            var dialog = new OpenFileDialog{CheckFileExists = true, Filter = "Audio files (*.mp3;*.wav;*.m4a;*.flac)|*.mp3;*.wav;*.m4a;*.flac"};
            dialog.ShowDialog();

            if (string.IsNullOrWhiteSpace(dialog.FileName)) {
                SetStatusText("No file selected!", false);
                return;
            }

            SetStatusText("Checking cache...");
            string folderName = dialog.FileName.Split('\\').Last().Split('.').First();
            _stemType = (string)((ComboBoxItem)stemTypeBox.SelectedItem).Tag;
            string fullPath = @$"{Environment.CurrentDirectory}\out\{_stemType}\{folderName}";
            if (Directory.Exists(fullPath))
            {
                SetStatusText("Cache hit!");
                _outputPath = null;
                _outputFiles.Clear();
                _outputPath = folderName;
                foreach (var fileFull in Directory.GetFiles(fullPath))
                {
                    string filename = fileFull.Replace($"{fullPath}\\", "");
                    SetStatusText($"[INFO] Found part: {filename}");
                    _outputFiles.Add(filename);
                }

                if (_outputFiles.Count() > 0) 
                {
                    Process_Exited(null, null);
                    return;
                }
                _outputPath = null;
            }

            SetStatusText("Preparing spleeter");
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "spleeter",
                    Arguments = $"separate -o \"out\\{_stemType}\" -p spleeter:{_stemType} \"{dialog.FileName}\"",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_OutputDataReceived;
            process.Exited += Process_Exited;

            _outputPath = null;
            _outputFiles.Clear();

            SetStatusText("Executing spleeter command...");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();/**/
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            SetStatusText("Done!", false);
            this.Dispatcher.Invoke(() =>
            {
                _mixer.Clear();
                _audioTracks.Clear();
                trackGrid.Children.Clear();
                trackGrid.ColumnDefinitions.Clear();

                _mixer.Initialize(_outputFiles.Count, _stemType);
                _mixer.OnPlay += Mixer_OnPlay;
                _mixer.OnPause += Mixer_OnPause;
                foreach (string part in _outputFiles)
                {
                    MixerTrack mt = new MixerTrack(_mixer, _outputPath, part);
                    AudioTrack at = new AudioTrack(mt);
                    trackGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                    trackGrid.Children.Add(at);
                    Grid.SetColumn(at, trackGrid.ColumnDefinitions.Count() - 1);
                    Grid.SetRow(at, 0);
                }
            });           
        }

        private void Mixer_OnPause(object sender, EventArgs e)
        {
            _timer.Stop();
            UpdateTrackPosition();
        }

        private void Mixer_OnPlay(object sender, EventArgs e)
        {
            _timer.Start();
            UpdateTrackPosition();
            if (_mixer.Duration != null && _mixer.Duration.HasTimeSpan && _mixer.Duration.TimeSpan != null)
                TrackPosition.Maximum = _mixer.Duration.TimeSpan.TotalSeconds;
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            SetStatusText(e.Data);

            Regex outputRegex = new Regex($"out[/\\\\]{_stemType}[/\\\\]([^/\\\\]*?)[/\\\\](.*?\\.wav)");
            Match m = outputRegex.Match(e.Data);
            if (m.Groups.Count > 0) 
            {
                _outputPath = m.Groups[1].Value;
                _outputFiles.Add(m.Groups[2].Value);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Master.SetMixer(_mixer);
            SetStatusText("Ready!", false);
            _timer.Tick += Timer_Tick;
            _timer.Interval = new TimeSpan(0, 0, 0, 1, 500);
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (_mixer.Duration != null && _mixer.Duration.HasTimeSpan && _mixer.Duration.TimeSpan != null)
                if(TrackPosition.Maximum != _mixer.Duration.TimeSpan.TotalSeconds)
                    TrackPosition.Maximum = _mixer.Duration.TimeSpan.TotalSeconds;

            if (_updatingTime)
                return;

            _timer.Stop();
            UpdateTrackPosition();
            _timer.Start();
        }

        private void UpdateTrackPosition()
        {
            if (_updatingTime)
                return;
            Debug.WriteLine("UPDATE!");
            TrackPosition.Value = _mixer.CurrentPosition.TotalSeconds;
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_mixer.IsReady)
                return;

            if (_mixer.IsPlaying)
                _mixer.Pause();
            else
                _mixer.Play();
        }

        private void TrackPosition_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(TrackPosition.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                _test = e.NewValue;
                Debug.WriteLine("CHANGE!");
            }
        }
    }
}
