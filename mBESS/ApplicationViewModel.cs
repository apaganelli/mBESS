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

        public struct StructDoubleStanceReference
        {
            public Joint footRight;
            public Joint footLeft;
            public Joint handRight;
            public Joint handLeft;
            public Joint head;
            public Joint spineBase;
            public Joint spineMid;
        }


        public static int MaxFramesReference = 150;

        public StructDoubleStanceReference[] DSReference = new StructDoubleStanceReference[MaxFramesReference];

        // Counter for calibrating pose with kinect.
        public int BodyCalibrationCounter { get; set; }

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
