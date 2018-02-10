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
#region ICommnad attributes

        ICommand _connectWBBCommand;
        ICommand _cancelCommand;
        ICommand _zeroCommand;
        ICommand _startPoseCalibrationCommand;
        ICommand _startTestCommand;
        ICommand _saveCommand;
        #endregion

#region WBB variable attributes
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
        string _testTime;
#endregion

        bool _canConnectWBB = true;
        bool _startPoseCalibration = false;
        bool _poseCalibrationDone = false;
        bool _zeroCalibrationDone = false;
        bool _finishedTest = false;

        bool _doZeroCalibration = false;
        int i = 0;
        float zeroWeight = 0;

        KinectBodyView _kinectBV;

#region Filter declaration variables
        FilterARMA filterTR;
        FilterARMA filterTL;
        FilterARMA filterBR;
        FilterARMA filterBL;
        FilterARMA filterCoMX;
        FilterARMA filterCoMY;
        FilterARMA filterWeight;
        FilterARMA filterCoPX;
        FilterARMA filterCoPY;
#endregion

        string _leftFoot;
        string _rightFoot;
        string _leftHand;
        string _rightHand;
        string _trunkSway;


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
            _app.PoseCalibrationCounter = 0;
            TotalDoubleStanceError = 0;

            // Setup a timer which controls the rate at which updates are processed.
            infoUpdateTimer.Elapsed += new ElapsedEventHandler(infoUpdateTimer_Elapsed);
        }

        public DateTime DoubleStanceStartTestTime;

        private void infoUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Start timer of recording double stance joint positions for reference.
            if(_startPoseCalibration)
            {
                StatusText = "Start timer for recording double stance pose "  + _app.PoseCalibrationCounter;
                LeftFoot = "";
                RightFoot = "";
                LeftHand = "";
                RightHand = "";
                TrunkSway = "";

                // 5000 mseconds 
                if (_app.PoseCalibrationCounter >= 5000)
                {
                    _app.PoseCalibrationCounter = 0;
                    _poseCalibrationDone = true;
                    _startPoseCalibration = false;
                    _kinectBV.RecordDoubleStance = false;
                    StatusText = "Finished timer for recording double stance pose";
                }

            if(_kinectBV.ExecuteDoubleStanceTest)
            {
                int seconds = (DateTime.Now - DoubleStanceStartTestTime).Seconds;
                this.TestTime = "Test time in seconds: " + String.Format("{0:00}", (DateTime.Now - DoubleStanceStartTestTime).Seconds);

                if(seconds >= _app.TestTime)
                {
                    _kinectBV.ExecuteDoubleStanceTest = false;
                    _kinectBV.poseDoubleStance.StopWatch.Stop();
                    StatusText = "Test finished with " + TotalDoubleStanceError + " errors.";
                    _finishedTest = true;
                }
            }

            // working without WBB
            // return;

            // Get the current raw sensor KG values.
            var rwWeight = wiiDevice.WiimoteState.BalanceBoardState.WeightKg;
            var rwTopLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopLeft;
            var rwTopRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.TopRight;
            var rwBottomLeft = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomLeft;
            var rwBottomRight = wiiDevice.WiimoteState.BalanceBoardState.SensorValuesKg.BottomRight;

            // Calibrate zero weight when the board is unloaded.
            if (_doZeroCalibration && i < 100)
            {
                i++;
                zeroWeight += rwWeight;
                StatusText = "Calibrating Zero Weight.";
            } else
            {
                if (_doZeroCalibration)
                {
                    _zeroCalibrationDone = true;
                    zeroWeight = zeroWeight / 100;
                    StatusText = "Calibrate Zero Weight FINISHED.";
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

        public ICommand StartPoseCalibrationCommand
        {
            get
            {
                if (_startPoseCalibrationCommand == null)
                {
                    _startPoseCalibrationCommand = new RelayCommand(param => StartPoseCalibration(), param => _zeroCalibrationDone);
                }

                return _startPoseCalibrationCommand;
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
                    _startTestCommand = new RelayCommand(param => StartTest(), param => _poseCalibrationDone);
                }

                return _startTestCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if(_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(param => Save(), param => _finishedTest);
                }

                return _saveCommand;
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
        private void ConnectWiiBalanceBoard()
        {
            // Uncomment next 4 lines to run without a WBB connected
            // infoUpdateTimer.Enabled = true;
            // _canConnectWBB = false;
            // _zeroCalibrationDone = true;
            // return;

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

        private void StartPoseCalibration()
        {
            _poseCalibrationDone = false;

            ClearAppDSRecords();
            _startPoseCalibration = true;           // flag that signals timer to count five seconds.
            _kinectBV.RecordDoubleStance = true;   
        }

        private void StartTest()
        {
            TotalDoubleStanceError = 0;
            _finishedTest = false;
            DoubleStanceStartTestTime = DateTime.Now;
            _kinectBV.poseDoubleStance.StopWatch.Reset();
            _kinectBV.poseDoubleStance.StopWatch.Start();
            _kinectBV.poseDoubleStance.FrameStatus = "";
            _kinectBV.ExecuteDoubleStanceTest = true;
        }

        private void Cancel()
        {
            infoUpdateTimer.Enabled = false;
            wiiDevice.Disconnect();
            _canConnectWBB = true;
            _app.CurrentPageViewModel = _app.PreviousPageViewModel;
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
        #endregion


        // Save test results in xML file.
        private void Save()
        {

        }

        private void ClearAppDSRecords()
        {
            // Feet
            _app.DSFL_X.Clear();
            _app.DSFL_Y.Clear();
            _app.DSFL_Z.Clear();
            _app.DSFR_X.Clear();
            _app.DSFR_Y.Clear();
            _app.DSFR_Z.Clear();
            // Hands
            _app.DSHL_X.Clear();
            _app.DSHL_Y.Clear();
            _app.DSHL_Z.Clear();
            _app.DSHR_X.Clear();
            _app.DSHR_Y.Clear();
            _app.DSHR_Z.Clear();
            // Spine-Head
            _app.DSHE_X.Clear();
            _app.DSHE_Y.Clear();
            _app.DSHE_Z.Clear();

            _app.DSSM_X.Clear();
            _app.DSSM_Y.Clear();
            _app.DSSM_Z.Clear();

            _app.DSSB_X.Clear();
            _app.DSSB_Y.Clear();
            _app.DSSB_Z.Clear();
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

        public string Name => throw new NotImplementedException();

        #region Properties

        public string TrunkSway
        {
            get { return _trunkSway; }
            set
            {
                if (_trunkSway != value)
                {
                    _trunkSway = value;
                    OnPropertyChanged("TrunkSway");
                }
            }
        }

        public string LeftHand {
            get { return _leftHand; }
            set
            {
                if (_leftHand != value)
                {
                    _leftHand = value;
                    OnPropertyChanged("LeftHand");
                }
            }
        }

        public string RightHand
        {
            get { return _rightHand; }
            set
            {
                if (_rightHand != value)
                {
                    _rightHand = value;
                    OnPropertyChanged("RightHand");
                }
            }
        }

        public string LeftFoot
        {
            get { return _leftFoot; }
            set
            {
                if (_leftFoot != value)
                {
                    _leftFoot = value;
                    OnPropertyChanged("LeftFoot");
                }
            }
        }

        public string RightFoot
        {
            get { return _rightFoot; }
            set
            {
                if (_rightFoot != value)
                {
                    _rightFoot = value;
                    OnPropertyChanged("RightFoot");
                }
            }
        }



        public int TotalDoubleStanceError { get; set; }

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


        public string TestTime
        {
            get { return _testTime; }
            set
            {
                if (_testTime != value)
                {
                    _testTime = value;
                    OnPropertyChanged("TestTime");
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
