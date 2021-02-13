using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WindowTracker;
using System.Collections.Generic;

namespace WindowTrackerApp
{
    public class TrackerService : ObservableObject
    {
        private DatabaseGateway _DBGateway;

        public TrackerService(DatabaseGateway dbGateway)
        {
            _DBGateway = dbGateway;
        }

        private string _windowTitle;
        public string WindowTitle
        {
            get { return _windowTitle; }
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged("WindowTitle");
                }
            }
        }

        private string _windowExecutable;
        public string WindowExecutable
        {
            get { return _windowExecutable; }
            set
            {
                if (_windowExecutable != value)
                {
                    _windowExecutable = value;
                    OnPropertyChanged("WindowExecutable");
                }
            }
        }

        private string _windowName;
        public string WindowName
        {
            get { return _windowName; }
            set
            {
                if (_windowName != value)
                {
                    _windowName = value;
                    OnPropertyChanged("WindowName");
                }
            }
        }

        private long _lastUpdateTime;
        private Process _foregroundProcess;

        private ApplicationItem _foregroundAppItem;
        private FileItem _foregroundFileItem;
        private WindowItem _foregroundWindowItem;

        public async Task StartTracking(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Run(() => UpdateCurrentWindowProcess());
                await Task.Run(() => SaveToDatabase());
                await Task.Delay(interval, cancellationToken);                    
            }
        }

        private void UpdateCurrentWindowProcess()
        {
            _foregroundProcess = GetForegroundProcess();

            WindowName = _foregroundProcess.ProcessName;
            WindowTitle = _foregroundProcess.MainWindowTitle;
            WindowExecutable = ProcessHelper.GetProcessFileName(_foregroundProcess);

            _lastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        }

        private void SaveToDatabase()
        {
            // Parse Window Data
            ParsedWindow parsedWindow = ParserService.ParseProcess(WindowName, WindowTitle, _foregroundProcess);

            // Save Window Data To Database and Update the object
            UpdateAndSaveForegroundItems(parsedWindow);
        }

        private Process GetForegroundProcess()
        {
            IntPtr foregroundWindowHandle = WindowHelper.GetForegroundWindowHandle();
            int foregroundProcessId = (int)WindowHelper.GetProcessIdFromWindowHandle(foregroundWindowHandle);
            
            return Process.GetProcessById(foregroundProcessId);
        }

        private void UpdateAndSaveForegroundItems(ParsedWindow parsedWindow)
        {
            if (parsedWindow.ApplicationName != null)
            {
                _foregroundAppItem = _DBGateway.GetOrCreateApplicationItemFromDB(parsedWindow.ApplicationName, _foregroundProcess);

                if (parsedWindow.FileName != null)
                {
                    _foregroundFileItem = _DBGateway.GetOrCreateFileItemFromDB(parsedWindow.FileName, parsedWindow.FilePath, _foregroundAppItem.ApplicationId);
                }
            }

            int? appId = _foregroundAppItem != null ? _foregroundAppItem.ApplicationId : (int?)null;
            int? fileId = _foregroundFileItem != null ? _foregroundFileItem.FileId : (int?)null;
            string windowText = parsedWindow.WindowTitle;

            _foregroundWindowItem = _DBGateway.GetOrCreateWindowItemFromDB(appId, fileId, windowText);

            _DBGateway.InsertWindowHistoryItem(_lastUpdateTime, _foregroundWindowItem.WindowId);
        }








    }
}
