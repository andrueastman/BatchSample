using System.ComponentModel;
using Microsoft.Graph;

namespace EnhancedBatch
{
    /// <summary>
    /// Model class for holding data that comes back as a response.
    /// One could probable extend this class and add more properties to it.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private User _me;
        private Calendar _calendar;
        private Drive _drive;

        public User Me
        {
            get => _me;
            set {
                _me = value;
                RaisePropertyChanged(nameof(Me));
            }
        }

        public Calendar Calendar
        {
            get => _calendar;
            set
            {
                _calendar = value;
                RaisePropertyChanged(nameof(Calendar));
            }
        }

        public Drive Drive
        {
            get => _drive;
            set
            {
                _drive = value;
                RaisePropertyChanged(nameof(Drive));
            }
        }
        
        private void RaisePropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}