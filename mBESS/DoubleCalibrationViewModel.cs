using System;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using WiimoteLib;

namespace mBESS
{
    class DoubleCalibrationViewModel : ObservableObject, IPageViewModel
    {
        ApplicationViewModel _app;

        Wiimote wiiDevice = new Wiimote();

        ICommand _connectWBBCommand;
        ICommand _cancelCommand;
        ICommand _zeroCommand;
        ICommand _startBodyCalibrationCommand;
        ICommand _startTestCommand;

        float _rwWeight;
        float _rwTopLeft;
        float _rwTopRight;
        float _rwBottomLeft;
        float _rwBottomRight;

        float _owWeight;
        float _owTopLeft;
        float _owTopRight;
        float _owBottomLeft;
        float _owBottomRight;

        bool _setCenterOffset = false;
        float _naCorners = 0f;
        float _oaTopLeft = 0f;
        float _oaTopRight = 0f;
        float _oaBottomLeft = 0f;
        float _oaBottomRight = 0f;

        float _CoGX;
        float _CoGY;

        float _calculatedCoPX;
        float _calculatedCoPY;

        float _zeroCalWeight;
        string _statusText;

        bool _canConnectWBB = true;
        bool _startBodyCalibration = false;
        bool _bodyCalibrationDone = false;
        bool _zeroCalibrationDone = false;

        bool _doZeroCalibration = false;
        int i = 0;
        float zeroWeight = 0;

        KinectBodyView _kinectBV;

        FilterARMA filterTR;
        FilterARMA filterTL;
        FilterARMA filterBR;
        FilterARMA filterBL;
        FilterARMA filterCoMX;
        FilterARMA filterCoMY;
        FilterARMA filterWeight;
        FilterARMA filterCoPX;
        FilterARMA filterCoPY;

        /// <summary>
        /// Timer to read WBB info. Interval in ms, enabled if the event should be triggered.
        /// </summary>
        System.Timers.Timer infoUpdateTimer = new System.Timers.Timer() { Interval = 50, Enabled = false };

#region Constructors
        public DoubleCalibrationViewModel(ApplicationViewModel app)
        {
            int filterSize = 10;

            _app = app;
            _kinectBV = new KinectBodyView(app);

            filterTR = new FilterARMA(filterSize);
            filterTL = new FilterARMA(filterSize);
            filterBR = new FilterARMA(filterSize);
            filterBL = new FilterARMA(filterSize);
            filterCoMX = new FilterARMA(filterSize);
            filterCoMY = new FilterARMA(filterSize);
            filterWeight = new FilterARMA(filterSize);
            filterCoPX = new FilterARMA(filterSize * 2);
            filterCoPY = new FilterARMA(filterSize * 2);
            LoadCalibrationViewModel();
            StatusText = "Ready";
        }
#endregion

        public void LoadCalibrationViewModel()
        {
            _app.BodyCalibrationCounter = 0;
            // Setup a timer which controls the rate at which updates are processed.
            infoUpdateTimer.Elapsed += new ElapsedEventHandler(infoUpdateTimer_Elapsed);
        }

        private void infoUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Start timer of recording double stance joint positions for reference.
            if(_startBodyCalibration)
            {
                StatusText = "Start timer for recording double stance pose "  + _app.BodyCalibrationCounter;

                // 5000 mseconds 
                if (_app.BodyCalibrationCounter >= 5000)
                {
                    _app.BodyCalibrationCounter = 0;
                    _bodyCalibrationDone = true;
                    _startBodyCalibration = false;
                    _kinectBV.RecordDoubleStance = false;
                    StatusText = "Finished timer for recording double stance pose";
                } 
            }

            // working without WBB
            return;

            // Get the current raw sensor KG values.
            var rwWeight = wiiDevice.WiimoteState.BalanceBoardState.WeightKg;
            var rwTopLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopLeft;
            var rwTopRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopRight;
            var rwBottomLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft;
            var rwBottomRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomRight;

            // Calibrate zero weight when the board is unloaded.
            if (_doZeroCalibration && i < 120)
            {
                i++;
                zeroWeight += rwWeight;
                StatusText = "Calibrating zero weight";
            } else
            {
                if (_doZeroCalibration)
                {
                    _zeroCalibrationDone = true;
                    zeroWeight = zeroWeight / 120;
                    StatusText = "Calibrate zero weight finished";
                }
                _doZeroCalibration = false;
            }

            RWTopLeft =  filterTL.GetPoint(rwTopLeft);
            RWTopRight = filterTR.GetPoint(rwTopRight);
            RWBottomLeft = filterBL.GetPoint(rwBottomLeft);
            RWBottomRight = filterBR.GetPoint(rwBottomRight);
            RWTotalWeight = filterWeight.GetPoint(rwWeight);

            // Calculate CoP using formula
            // CoPx = (L/2) * ((TR + BR) - (TL + BL)) / TR + BR + TL + BL
            // CoPy = (W/2) * ((TL + TR) - (BL + BR)) / TR + BR + TL + BL
            // W = 228 mm and L = 433mm

            CalculatedCoPX =  filterCoPX.GetPoint(21 * (((RWTopRight + RWBottomRight) - (RWTopLeft + RWBottomLeft)) / (RWBottomLeft + RWBottomRight + RWTopLeft + RWTopRight)));
            CalculatedCoPY = filterCoPY.GetPoint(12 * (((RWTopRight + RWTopLeft) - (RWBottomRight + RWBottomLeft)) / (RWBottomLeft + RWBottomRight + RWTopLeft + RWTopRight)));

            // Discount read value when board is unloaded.
            ZeroCalWeight = RWTotalWeight - zeroWeight;

            // Prevent negative values by tracking lowest possible value and making it a zero based offset.
            if (rwTopLeft < _naCorners) _naCorners = rwTopLeft;
            if (rwTopRight < _naCorners) _naCorners = rwTopRight;
            if (rwBottomLeft < _naCorners) _naCorners = rwBottomLeft;
            if (rwBottomRight < _naCorners) _naCorners = rwBottomRight;

            // Negative total weight is reset to zero as jumping or lifting the board causes negative spikes, which would break 'in use' checks.
            var owWeight = rwWeight < 0f ? 0f : rwWeight;
            var owTopLeft = rwTopLeft -= _naCorners;
            var owTopRight = rwTopRight -= _naCorners;
            var owBottomLeft = rwBottomLeft -= _naCorners;
            var owBottomRight = rwBottomRight -= _naCorners;

            // Get offset that would make current values the center of mass.
            if (_setCenterOffset)
            {
                _setCenterOffset = false;

                var rwHighest = Math.Max(Math.Max(rwTopLeft, rwTopRight), Math.Max(rwBottomLeft, rwBottomRight));

                _oaTopLeft = rwHighest - rwTopLeft;
                _oaTopRight = rwHighest - rwTopRight;
                _oaBottomLeft = rwHighest - rwBottomLeft;
                _oaBottomRight = rwHighest - rwBottomRight;
            }

            // Keep values only when board is being used, otherwise offsets and small value jitters can trigger unwanted actions.
            if (owWeight > 0f)
            {
                owTopLeft += _oaTopLeft;
                owTopRight += _oaTopRight;
                owBottomLeft += _oaBottomLeft;
                owBottomRight += _oaBottomRight;
            }
            else
            {
                owTopLeft = 0;
                owTopRight = 0;
                owBottomLeft = 0;
                owBottomRight = 0;
            }

            OWBottomLeft = owBottomLeft;
            OWBottomRight = owBottomRight;
            OWTopLeft = owTopLeft;
            OWTopRight = owTopRight;
            OWTotalWeight = owWeight;

            float CoG_X = wiiDevice.WiimoteState.BalanceBoardState.CenterOfGravity.X;
            float CoG_Y = wiiDevice.WiimoteState.BalanceBoardState.CenterOfGravity.Y;

            CoGX = filterCoMX.GetPoint(CoG_X);
            CoGY = filterCoMY.GetPoint(CoG_Y);
        }

        #region Commands
        public ICommand ConnectWBBCommand
        {
            get
            {
                if(_connectWBBCommand == null)
                {
                    _connectWBBCommand = new RelayCommand(param => ConnectWiiBalanceBoard(), param => _canConnectWBB);
                }
                return _connectWBBCommand;
            }
        }

        public ICommand StartBodyCalibrationCommand
        {
            get
            {
                if (_startBodyCalibrationCommand == null)
                {
                    _startBodyCalibrationCommand = new RelayCommand(param => StartBodyCalibration(), param => _zeroCalibrationDone);
                }

                return _startBodyCalibrationCommand;
            }
        }

        public ICommand ZeroCommand
        {
            get
            {
                if (_zeroCommand == null)
                {
                    _zeroCommand = new RelayCommand(param => ZeroCalibration(), param => !_doZeroCalibration);
                }

                return _zeroCommand;
            }
        }

        public ICommand StartTestCommand
        {
            get
            {
                if (_startTestCommand == null)
                {
                    _startTestCommand = new RelayCommand(param => StartTest(), param => _bodyCalibrationDone);
                }

                return _startTestCommand;
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                if (_cancelCommand == null)
                {
                    _cancelCommand = new RelayCommand(param => Cancel());
                }

                return _cancelCommand;
            }
        }
        #endregion

        #region CommandFunctions

        private void StartBodyCalibration()
        {
            _bodyCalibrationDone = false;
            _startBodyCalibration = true;           // flag that signals timer to count five seconds.
            _kinectBV.RecordDoubleStance = true;   
        }

        private void StartTest()
        {

        }

        private void Cancel()
        {
            infoUpdateTimer.Enabled = false;
            wiiDevice.Disconnect();
            _canConnectWBB = true;
            _app.CurrentPageViewModel = _app.PreviousPageViewModel;
        }

        public string Name => throw new NotImplementedException();
        #endregion

        private void ConnectWiiBalanceBoard()
        {

            // Next 4 lines Working without WBB
            infoUpdateTimer.Enabled = true;
            _canConnectWBB = false;
            _zeroCalibrationDone = true;
            return;

            try
            {
                // Find all connected Wii devices.
                var deviceCollection = new WiimoteCollection();
                deviceCollection.FindAllWiimotes();

                for (int i = 0; i < deviceCollection.Count; i++)
                {
                    wiiDevice = deviceCollection[i];

                    // Device type can only be found after connection, so prompt for multiple devices.

                    if (deviceCollection.Count > 1)
                    {
                        var devicePathId = new Regex("e_pid&.*?&(.*?)&").Match(wiiDevice.HIDDevicePath).Groups[1].Value.ToUpper();

                        var response = MessageBox.Show("Connect to HID " + devicePathId + " device " + (i + 1) + " of " + deviceCollection.Count + " ?", "Multiple Wii Devices Found", MessageBoxButtons.YesNoCancel);
                        if (response == DialogResult.Cancel) return;
                        if (response == DialogResult.No) continue;
                    }

                    // Setup update handlers.
                    wiiDevice.WiimoteChanged += wiiDevice_WiimoteChanged;
                    wiiDevice.WiimoteExtensionChanged += wiiDevice_WiimoteExtensionChanged;

                    // Connect and send a request to verify it worked.
                    wiiDevice.Connect();
                    wiiDevice.SetReportType(InputReport.IRAccel, false); // FALSE = DEVICE ONLY SENDS UPDATES WHEN VALUES CHANGE!
                    wiiDevice.SetLEDs(true, false, false, false);

                    // Enable processing of updates.
                    infoUpdateTimer.Enabled = true;

                    // Prevent connect being pressed more than once.
                    _canConnectWBB = false;
                    break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

   
        /// <summary>
        /// Sets flag to calibabre zero weight load on the WBB.
        /// </summary>
        private void ZeroCalibration()
        {
            i = 0;
            filterTR.ClearSerie();
            filterTL.ClearSerie();
            filterBR.ClearSerie();
            filterBL.ClearSerie();
            filterCoMX.ClearSerie();
            filterCoMY.ClearSerie();
            filterWeight.ClearSerie();
            filterCoPX.ClearSerie();
            filterCoPY.ClearSerie();

            zeroWeight = 0;
            _doZeroCalibration = true;
        }

        private void wiiDevice_WiimoteChanged(object sender, WiimoteChangedEventArgs e)
        {
            // Called every time there is a sensor update, values available using e.WiimoteState.
            // Use this for tracking and filtering rapid accelerometer and gyroscope sensor data.
            // The balance board values are basic, so can be accessed directly only when needed.
        }

        private void wiiDevice_WiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs e)
        {
            // This is not needed for balance boards.
        }

        public KinectBodyView GetKinectBodyView()
        {
            return _kinectBV;
        }


        #region Properties
        public ImageSource ImageSource
        {
            get { return _kinectBV.ImageSource; }
        }

        public float CalculatedCoPX
        {
            get { return _calculatedCoPX; }
            set
            {
                if (_calculatedCoPX != value)
                {
                    _calculatedCoPX = value;
                    OnPropertyChanged("CalculatedCoPX");
                }
            }
        }

        public float CalculatedCoPY
        {
            get { return _calculatedCoPY; }
            set
            {
                if (_calculatedCoPY != value)
                {
                    _calculatedCoPY = value;
                    OnPropertyChanged("CalculatedCoPY");
                }
            }
        }

        public float RWTopLeft
        {
            get { return _rwTopLeft; }
            set
            {
                if(_rwTopLeft != value)
                {
                    _rwTopLeft = value;
                    OnPropertyChanged("RWTopLeft");
                }
            }
        }

        public float RWTopRight
        {
            get { return _rwTopRight; }
            set
            {
                if (_rwTopRight != value)
                {
                    _rwTopRight = value;
                    OnPropertyChanged("RWTopRight");
                }
            }

        }

        public float RWBottomLeft
        {
            get { return _rwBottomLeft; }
            set
            {
                if (_rwBottomLeft != value)
                {
                    _rwBottomLeft = value;
                    OnPropertyChanged("RWBottomLeft");
                }
            }

        }

        public float RWBottomRight
        {
            get { return _rwBottomRight; }
            set
            {
                if (_rwBottomRight != value)
                {
                    _rwBottomRight = value;
                    OnPropertyChanged("RWBottomRight");
                }
            }

        }

        public float RWTotalWeight {
            get { return _rwWeight; }
            set
            {
                if (_rwWeight != value)
                {
                    _rwWeight = value;
                    OnPropertyChanged("RWTotalWeight");
                }
            }

        }

        //
        // Offset Weight
        //
        public float OWTopLeft
        {
            get { return _owTopLeft; }
            set
            {
                if (_owTopLeft != value)
                {
                    _owTopLeft = value;
                    OnPropertyChanged("OWTopLeft");
                }
            }
        }

        public float OWTopRight
        {
            get { return _owTopRight; }
            set
            {
                if (_owTopRight != value)
                {
                    _owTopRight = value;
                    OnPropertyChanged("OWTopRight");
                }
            }
        }

        public float OWBottomLeft
        {
            get { return _owBottomLeft; }
            set
            {
                if (_owBottomLeft != value)
                {
                    _owBottomLeft = value;
                    OnPropertyChanged("OWBottomLeft");
                }
            }

        }

        public float OWBottomRight
        {
            get { return _owBottomRight; }
            set
            {
                if (_owBottomRight != value)
                {
                    _owBottomRight = value;
                    OnPropertyChanged("OWBottomRight");
                }
            }

        }

        public float OWTotalWeight
        {
            get { return _owWeight; }
            set
            {
                if (_owWeight != value)
                {
                    _owWeight = value;
                    OnPropertyChanged("OWTotalWeight");
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged("Statustext");
                }
            }
        }

        public float ZeroCalWeight
        {
            get { return _zeroCalWeight; }
            set
            {
                if (_zeroCalWeight != value)
                {
                    _zeroCalWeight = value;
                    OnPropertyChanged("zeroCalWeight");
                }
            }
        }

        public float CoGX
        {
            get { return _CoGX; }
            set
            {
                if (_CoGX != value)
                {
                    _CoGX = value;
                    OnPropertyChanged("CoGX");
                }
            }
        }

        public float CoGY
        {
            get { return _CoGY; }
            set
            {
                if (_CoGY != value)
                {
                    _CoGY = value;
                    OnPropertyChanged("CoGY");
                }
            }
        }
        #endregion
    }






}
