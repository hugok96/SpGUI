using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpGUI
{
    public class Mixer
    {
        private readonly Dictionary<int, MixerTrack> _tracks = new Dictionary<int, MixerTrack>();
        private MixerTrack _firstTrack;
        private int _trackCount;
        public string StemType;

        public bool IsSoloMode { get => _tracks.Values.Any(t => t.IsSolo); }
        public bool IsPlaying = false;
        public bool IsReady = false;
        public double MasterVolume = 1d;

        public event EventHandler OnPlay;
        public event EventHandler OnPause;

        public void SetVolume(double v)
        {
            this.MasterVolume = v;
            ForceVolumeUpdate();
        }

        public Duration Duration {
            get => null == _firstTrack ? TimeSpan.Zero : _firstTrack.GetPlayer().NaturalDuration;
        }

        public TimeSpan CurrentPosition {
            get => null == _firstTrack ? TimeSpan.Zero : _firstTrack.GetPlayer().Position;
        }

        public void Initialize(int trackCount, string stemType)
        {
            _trackCount = trackCount;
            StemType = stemType;
        }

        public void Clear()
        {
            _firstTrack = null;
            IsPlaying = false;
            IsReady = false;
            _trackCount = 0;

            foreach (MixerTrack mt in _tracks.Values)
            {
                mt.Dispose();
            }
            _tracks.Clear();
        }

        public int AddTrack(MixerTrack track)
        {
            int id = _tracks.Count;
            _tracks.Add(id, track);
            if (id == 0)
                RegisterFirstTrack(track);
            return id;
        }

        private void RegisterFirstTrack(MixerTrack track)
        {
            _firstTrack = track;            
        }

        public async void OnMixerTrackLoaded(MixerTrack at)
        {
            if (_tracks.Count == _trackCount && _tracks.All(t => t.Value.IsReady))
            {
                // ready to go
                IsReady = true;
                await Task.Delay(1000);
                Play();
            }
        }

        public void Play()
        {
            if (IsPlaying)
                return;

            ForceVolumeUpdate();

            foreach (MixerTrack mt in _tracks.Values)
                mt.Seek(_firstTrack.GetPlayer().Position);

            IsPlaying = true;
            foreach (MixerTrack mt in _tracks.Values)
                mt.Play();

            OnPlay.Invoke(null, null);
        }

        public void Seek(TimeSpan position)
        {
            foreach(MixerTrack mt in _tracks.Values)
            {
                mt.Seek(position);
            }
        }

        public void ForceVolumeUpdate()
        {
            foreach (MixerTrack mt in _tracks.Values)
                mt.UpdateVolume();
        }

        public void Pause()
        {
            if (!IsPlaying)
                return;

            IsPlaying = false;
            foreach (MixerTrack mt in _tracks.Values)
                mt.Pause();

            OnPause.Invoke(null, null);
        }

        public MixerTrack GetFirstTrack()
        {
            return _firstTrack;
        }
    }
}
