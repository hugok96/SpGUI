using System;
using System.Linq;
using System.Windows.Media;

namespace SpGUI
{
    public class MixerTrack
    {
        private readonly Mixer _mixer;
        private readonly MediaPlayer _player;
        private readonly int _trackId;
        public readonly string Name;
        public bool IsReady = false;
        public bool IsMuted = false;
        public bool IsSolo = false;
        private double _volume = 0.8;

        public MixerTrack(Mixer mixer, string outputPath, string part)
        {
            _mixer = mixer;

            Name = part.Split('.').First() ?? "unknown";

            string rawUrl = @$"{Environment.CurrentDirectory}\out\{_mixer.StemType}\{outputPath}\{part}";
            Uri uri = new Uri(rawUrl);   
            _player = new MediaPlayer();
            _player.MediaOpened += Player_MediaOpened;
            _player.Open(uri);
            _trackId = _mixer.AddTrack(this);
        }

        public void SetVolume(double value)
        {
            this._volume = value;
            UpdateVolume();
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            UpdateVolume();
        }

        public void ToggleSolo()
        {
            IsSolo = !IsSolo;
            _mixer.ForceVolumeUpdate();
        }

        public void UpdateVolume()
        {
            double volume = 0;
            if (IsSolo || (!_mixer.IsSoloMode && !IsMuted))
            {
                volume = _volume * _mixer.MasterVolume;
            }
            _player.Volume = volume;
        }

        private void Player_MediaOpened(object sender, EventArgs e)
        {
            IsReady = true;
            _mixer.OnMixerTrackLoaded(this);
        }

        public void Play()
        {
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Stop()
        {
            _player.Stop();
        }

        public void Dispose()
        {
            Stop();
            _player.Close();
        }

        public void Seek(TimeSpan position)
        {
            _player.Position = position;
        }

        public MediaPlayer GetPlayer()
        {
            return _player;
        }
    }
}
