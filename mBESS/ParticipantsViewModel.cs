using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace mBESS
{
    /// <summary>
    /// Handles the list of participants and all operations over it (new, delete, edit and select).
    /// </summary>
    class ParticipantsViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app = null;

        ICommand _newCommand;
        ICommand _editCommand;
        ICommand _deleteCommand;
        ICommand _selectCommand;

        int _sessionId;
        int _selectedId = 0;
        string _status;

        public ParticipantsViewModel(ApplicationViewModel app)
        {
            _app = app;
        }

        public string Name => throw new NotImplementedException();

        public int SessionId
        {
            get { return _sessionId; }
            set
            {
                if (_sessionId != value)
                {
                    _sessionId = value;
                    OnPropertyChanged("SessionId");
                }
            }
        }

        public int SelectedId
        {
            get { return _selectedId; }
            set
            {
                if (_selectedId != value)
                {
                    _selectedId = value;
                    OnPropertyChanged("SelectedId");
                }
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged("Status");
                }
            }
        }

        public ICommand NewCommand
        {
            get
            {
                if (_newCommand == null)
                {
                    _newCommand = new RelayCommand(param => NewParticipant());
                }

                return _newCommand;
            }
        }

        public ICommand EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new RelayCommand(param => EditParticipant(), param => (SessionId > 0));
                }
                return _editCommand;
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand == null)
                {
                    _deleteCommand = new RelayCommand(param => DeleteParticipant(), param => (SessionId > 0));
                }
                return _deleteCommand;
            }
        }

        public ICommand SelectCommand
        {
            get
            {
                if (_selectCommand == null)
                {
                    _selectCommand = new RelayCommand(param => SelectParticipant(), param => (SessionId > 0));
                }
                return _selectCommand;
            }
        }

        private void NewParticipant()
        {
            // Open a new user control for entering new participant entry.
            _app.CurrentPageViewModel = new ParticipantViewModel(_app);
        }

        private void EditParticipant()
        {
            // Pass a reference for main application, id key, and toggle for delete/edit
            _app.CurrentPageViewModel = new ParticipantViewModel(_app, SessionId, false);
        }

        private void DeleteParticipant()
        {
            // Pass a reference for main application, id key, and toggle for delete/edit
            _app.CurrentPageViewModel = new ParticipantViewModel(_app, SessionId, true);
        }

        private void SelectParticipant()
        {
            SelectedId = SessionId;
            Status = "Session Id " + SessionId.ToString() + " was selected.";
        }
    }
}
