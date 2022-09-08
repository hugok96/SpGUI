using System.Windows;
using System.Windows.Controls;

namespace SpGUI
{
    public partial class AudioTrack : UserControl
    {
        private readonly MixerTrack _track;

        public AudioTrack(MixerTrack track)
        {
            InitializeComponent();
            _track = track;
            trackName.Content = track.Name;
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            _track.ToggleMute();
        }

        private void CheckSolo_Checked(object sender, RoutedEventArgs e)
        {
            _track.ToggleSolo();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_track != null && sender != null)
                _track.SetVolume(((Slider)sender).Value / 2d);
        }

        private void BalanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_track != null && sender != null)
                _track.GetPlayer().Balance = (((Slider)sender).Value) - 2d;
        }
    }
}
