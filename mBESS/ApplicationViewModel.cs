using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Microsoft.Kinect;

namespace mBESS
{
    class ApplicationViewModel: ObservableObject
    {
        private ICommand _changePageCommand;

        private IPageViewModel _previousPageViewModel;
        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;


        #region Global variables

        public int TestTime = 30;                                   // sets the time in seconds of each test
        public int CalibrationTime = 5;                             // sets the time in seconds of calibration process
        public double JointAxisPrecision = 0.03;                    // sets the precision in meters of the joint precision
        public double AnglePrecision = 3;                           // sets the angle precision in degrees of trunk position (head, spine_mid, spine_base).

        public int participantId = 0;                               // Selected participant id.

        /// <summary>
        /// This list of float values stores axis positions of the analyzed joints during pose calibration.
        /// They are used to calculated standard values of joint position.
        /// </summary>

        // Feet
        public List<float> DSFR_X = new List<float>();
        public List<float> DSFR_Y = new List<float>();
        public List<float> DSFR_Z = new List<float>();
        public List<float> DSFL_X = new List<float>();
        public List<float> DSFL_Y = new List<float>();
        public List<float> DSFL_Z = new List<float>();
        // Hands
        public List<float> DSHR_X = new List<float>();
        public List<float> DSHR_Y = new List<float>();
        public List<float> DSHR_Z = new List<float>();
        public List<float> DSHL_X = new List<float>();
        public List<float> DSHL_Y = new List<float>();
        public List<float> DSHL_Z = new List<float>();
        // Spine-Head
        public List<float> DSHE_X = new List<float>();
        public List<float> DSHE_Y = new List<float>();
        public List<float> DSHE_Z = new List<float>();
        public List<float> DSSM_X = new List<float>();
        public List<float> DSSM_Y = new List<float>();
        public List<float> DSSM_Z = new List<float>();
        public List<float> DSSB_X = new List<float>();
        public List<float> DSSB_Y = new List<float>();
        public List<float> DSSB_Z = new List<float>();

        public static int MaxFramesReference = 150;                     // Number of frames - Pose calibration. Based on 30 frames per second.
        
        // Counter for calibrating pose with kinect.
        public int PoseCalibrationCounter { get; set; }
#endregion


        /// <summary>
        /// The constructor activate the application default page and selects it as the active page.
        /// </summary>
        public ApplicationViewModel()
        {
            // Add all navigation pages.
            // PageViewModels.Add();

            // Set up initial page.
            //CurrentPageViewModel = PageViewModels[0];
        }

        /// <summary>
        /// Interface command to execute the change of pages.
        /// </summary>
        public ICommand ChangePageCommand
        {
            get
            {
                if (_changePageCommand == null)
                {
                    _changePageCommand = new RelayCommand(
                        p => ChangeViewModel((IPageViewModel)p),
                        p => p is IPageViewModel);
                }
                return _changePageCommand;
            }
        }

        /// <summary>
        /// Gets/sets the current view model page.
        /// </summary>
        public IPageViewModel CurrentPageViewModel
        {
            get
            {
                return _currentPageViewModel;
            }
            set
            {
                if (_currentPageViewModel != value)
                {
                    _currentPageViewModel = value;
                    OnPropertyChanged("CurrentPageViewModel");
                }
            }
        }

        public IPageViewModel PreviousPageViewModel
        {
            get { return _previousPageViewModel; }
            set
            {
                if(_previousPageViewModel != value)
                {
                    _previousPageViewModel = value;
                    OnPropertyChanged("PreviousPageViewModel");
                }
            }
        }

        /// <summary>
        /// Gets the list of all view model pages that had been instantiated.
        /// </summary>
        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                    _pageViewModels = new List<IPageViewModel>();

                return _pageViewModels;
            }
        }

        /// <summary>
        /// Changes the active selected view model. If it is a new view model that had not been activated before,
        /// adds it to the list of view model pages.
        /// </summary>
        /// <param name="viewModel"></param>
        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
                PageViewModels.Add(viewModel);

            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }
    }
}
