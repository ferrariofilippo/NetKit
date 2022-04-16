using System.ComponentModel;
using System.Collections.Generic;

namespace NetKit.Model
{
    public class DataList : INotifyPropertyChanged
    {
        List<string> data;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<string> Data
        {
            get { return data; }
            set
            {
                data = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Data"));
            }
        }
    }
}