using System;
using System.Data.SQLite;
using System.Threading;


namespace WindowTrackerApp
{
    public class TrackingViewModel : ObservableObject, IPageViewModel
    {
        private SQLiteConnection _DBConnection;

        public string Name { get { return "Tracking"; } }
        public TrackerService TrackerVM { get; set; }

        public TrackingViewModel(SQLiteConnection dbConnection, TrackerService _trackingService)
        {
            _DBConnection = dbConnection;

            TrackerVM = _trackingService;
        }
    }
}
