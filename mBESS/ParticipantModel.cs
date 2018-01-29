using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mBESS
{
    class ParticipantModel : ObservableObject
    {
        int _participantId;
        string _name;
        string _date;
        string _altitude;
        int _serie;



        public int ParticipantId
        {
            get { return _participantId; }
            set
            {
                if (value != _participantId)
                {
                    _participantId = value;
                    OnPropertyChanged("ParticipantId");
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string Date
        {
            get { return _date; }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    OnPropertyChanged("Date");
                }
            }
        }

        public string Altitude
        {
            get { return _altitude; }
            set
            {
                if (value != _altitude)
                {
                    _altitude = value;
                    OnPropertyChanged("Altitude");
                }
            }
        }

        public int Serie
        {
            get { return _serie; }
            set
            {
                if (value != _serie)
                {
                    _serie = value;
                    OnPropertyChanged("Serie");
                }
            }
        }

    }
}
