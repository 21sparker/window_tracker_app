using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowTrackerApp
{
    public class TrackToProjectViewModel : ObservableObject, IPageViewModel
    {
        private DatabaseGateway _DBGateway;

        private ObservableCollection<WindowItem> _windowItems;
        public ObservableCollection<WindowItem> WindowItems
        {
            get { return _windowItems; }
            set
            {
                _windowItems = value;
                OnPropertyChanged("WindowItems");
            }
        }

        private DateTime _startDate;
        public DateTime StartDate
        {
            get { return _startDate; }
            set
            {
                _startDate = value;
                OnPropertyChanged("StartDate");
            }
        }



        public TrackToProjectViewModel(DatabaseGateway dbGateway)
        {
            _DBGateway = dbGateway;

            StartDate = DateTime.UtcNow.Date;

            WindowItems = GetAllWindowItems();
        }



        public string Name { get { return "Track Windows"; } }


        public ObservableCollection<WindowItem> GetAllWindowItems()
        {
            long start = (long)(StartDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            List<WindowItem> winItems = _DBGateway.GetAllWindowItemsInDateRange(start, null);

            return new ObservableCollection<WindowItem>(winItems);
        }


    }
}
