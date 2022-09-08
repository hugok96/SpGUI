using System.Windows;
using System.Windows.Controls;

namespace SpGUI
{
    public partial class MasterTrack : UserControl
    {
        private Mixer _mixer;

        public MasterTrack()
        {
            InitializeComponent();
            trackName.Content = "Master";
        }

        public void SetMixer(Mixer m)
        {
            this._mixer = m;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(_mixer != null && sender != null)
                _mixer.SetVolume(((Slider)sender).Value / 2d);
        }
    }
}
