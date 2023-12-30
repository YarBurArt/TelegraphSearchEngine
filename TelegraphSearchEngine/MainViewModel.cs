using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TelegraphSearchEngine
{
    class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private int _StatusValue;
        public int StatusValue 
        { 
            get { return _StatusValue; } 
            set
            {
                _StatusValue = value;
                OnPropertyChanged();
            } 
        }

        public MainViewModel() {
            Task.Factory.StartNew(() =>
            {
                while (StatusValue <= 100)
                {
                    Task.Delay(1000).Wait();
                    StatusValue++;
                }
            });
        }

        public ICommand ClickStartSearch
        {
            get
            {
                return new StartSearchCommand((obj) =>
                {

                });
            }
        }
    }
}
