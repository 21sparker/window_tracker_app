using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Diagnostics;
using Dapper;
using System.Data.SQLite;
using System.Threading;
using System;

namespace WindowTrackerApp
{
    public class MainViewModel : ObservableObject
    {

        //TODO: Need to add something that closes the database when the window closes
        // See here: https://stackoverflow.com/questions/3683450/handling-the-window-closing-event-with-wpf-mvvm-light-toolkit

        #region Fields

        private ICommand _changePageCommand;

        private IPageViewModel _currentPageViewModel;
        private List<IPageViewModel> _pageViewModels;

        /// <summary>
        /// Data application EF Core context
        /// </summary>
        private DatabaseGateway _DBGateway;
        private TrackerService _trackerService;
        private CancellationTokenSource _cancelTokenSource;
        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Connect to database
            _DBGateway = new DatabaseGateway($"Data Source={App.databasePath};");
            
            // Start Tracking Service
            _trackerService = new TrackerService(_DBGateway);
            BeginTracking();

            // Add available pages
            PageViewModels.Add(new TrackingViewModel(_DBGateway, _trackerService));
            PageViewModels.Add(new TrackToProjectViewModel(_DBGateway));

            // Set starting page
            CurrentPageViewModel = PageViewModels[0];

        }

        #endregion

        #region Properties / Commands

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

        public List<IPageViewModel> PageViewModels
        {
            get
            {
                if (_pageViewModels == null)
                {
                    _pageViewModels = new List<IPageViewModel>();
                }

                return _pageViewModels;
            }
        }

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

        /// <summary>
        /// Event Handler for when the application is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            //Close database connection
            _DBGateway.Close();

            // Cancel Tracking Service
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
        }

        #endregion

        #region Methods

        private void ChangeViewModel(IPageViewModel viewModel)
        {
            if (!PageViewModels.Contains(viewModel))
            {
                PageViewModels.Add(viewModel);
            }

            CurrentPageViewModel = PageViewModels.FirstOrDefault(vm => vm == viewModel);
        }

        /// <summary>
        /// Start Tracker Service
        /// </summary>
        private async void BeginTracking()
        {
            _cancelTokenSource = new CancellationTokenSource();
            await _trackerService.StartTracking(new TimeSpan(0, 0, 1), _cancelTokenSource.Token);

        }
        #endregion
    }
}
