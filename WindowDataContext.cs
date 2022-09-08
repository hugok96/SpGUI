using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace SpGUI
{
    class WindowDataContext : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _windowStatus = "Initializing...";
        private bool _windowReady = false;



        public string WindowStatus 
        { 
            get => _windowStatus; 
            set 
            {
                if (value != _windowStatus)
                {
                    _windowStatus = value;
                    OnPropertyChanged("WindowStatus");
                }
            }
        }
        public bool WindowReady
        {
            get => _windowReady;
            set
            {
                if (value != _windowReady)
                {
                    _windowReady = value;
                    OnPropertyChanged("WindowReady");
                }
            }
        }
    }
}
