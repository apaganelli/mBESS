using System;
using System.Windows.Input;

namespace mBESS
{
    class DoubleViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;

        ICommand _startRecordingCommand;

        DoubleCalibrationViewModel _doubleCalibrationVM;

        public DoubleViewModel(ApplicationViewModel app)
        {
            _app = app;
        }

        public string Name => throw new NotImplementedException();

        public ICommand StartRecordingCommand
        {
            get
            {
                if (_startRecordingCommand == null)
                {
                    _startRecordingCommand = new RelayCommand(param => StartRecording());
                }

                return _startRecordingCommand;
            }
        }

        private void StartRecording()
        {
           _app.PreviousPageViewModel =_app.CurrentPageViewModel;

            if (_doubleCalibrationVM == null)
            {
                _doubleCalibrationVM = new DoubleCalibrationViewModel(_app);
            }

            _app.CurrentPageViewModel = _doubleCalibrationVM;
        }
    }
}
