using System;
using System.Data.SQLite;
using System.Threading;


namespace WindowTrackerApp
{
    public class TrackingViewModel : ObservableObject, IPageViewModel
    {
        private DatabaseGateway _DBGateway;

        public string Name { get { return "Tracking"; } }
        public TrackerService TrackerVM { get; set; }

        public TrackingViewModel(DatabaseGateway dbGateway, TrackerService trackingService)
        {
            _DBGateway = dbGateway;

            TrackerVM = trackingService;
        }

    }
}
