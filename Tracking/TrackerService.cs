using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WindowTracker;
using Dapper;
using System.Collections.Generic;

namespace WindowTrackerApp
{
    public class TrackerService : ObservableObject
    {
        private System.Data.SQLite.SQLiteConnection _DBConnection;

        public TrackerService(System.Data.SQLite.SQLiteConnection dbConnection)
        {
            _DBConnection = dbConnection;
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


        private Process _foregroundProcess { get; set; }

        public async Task StartTracking(TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Run(() => GetCurrentWindowProcess());
                await Task.Run(() => SaveToDatabase());
                await Task.Delay(interval, cancellationToken);                    
            }
        }

        private void GetCurrentWindowProcess()
        {
            IntPtr foregroundWindowHandle = WindowHelper.GetForegroundWindowHandle();
            int foregroundProcessId = (int)WindowHelper.GetProcessIdFromWindowHandle(foregroundWindowHandle);

            _foregroundProcess = Process.GetProcessById(foregroundProcessId);

            WindowName = _foregroundProcess.ProcessName;
            WindowTitle = _foregroundProcess.MainWindowTitle;
            WindowExecutable = ProcessHelper.GetProcessFileName(_foregroundProcess);
        }

        private void SaveToDatabase()
        {
            ParsedWindow parsedWindow = ParserService.ParseProcess(WindowName, WindowTitle, _foregroundProcess);
            ApplicationItem app = null;
            FileItem fileItem = null;

            if (parsedWindow.ApplicationName != null)
            {
                app = GetOrCreateApplicationItemFromDB(parsedWindow.ApplicationName);

                if (parsedWindow.FileName != null)
                {
                    fileItem = GetOrCreateFileItemFromDB(parsedWindow.FileName, parsedWindow.FilePath, app.ApplicationId);
                }
            }

            int? appId = app != null ? app.ApplicationId : (int?)null;
            int? fileId = fileItem != null ? fileItem.FileId : (int?)null;
            string windowText = parsedWindow.WindowTitle;

            WindowItem windowItem = GetOrCreateWindowItemFromDB(appId, fileId, windowText);

            string sql = "INSERT INTO WindowHistory (LoggedDateTime, WindowId) VALUES (@LoggedDateTime, @WindowId);";
            long unixTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int windowId = windowItem.WindowId;
            _DBConnection.Execute(sql, new { LoggedDateTime = unixTimeSeconds, WindowId = windowId });


        }

        private WindowItem GetOrCreateWindowItemFromDB(int? appId, int? fileId, string windowText)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("ApplicationId", appId);
            args.Add("FileId", fileId);
            args.Add("WindowText", windowText);

            string selectSql = $"SELECT * FROM Window { GenerateWhereClauseWithNull(args) }";

            WindowItem windowItem = _DBConnection.QueryFirstOrDefault<WindowItem>(selectSql, new { ApplicationId = appId, FileId = fileId, WindowText = windowText });
            
            if (windowItem == null)
            {
                string insertSql = $"INSERT INTO Window { GenerateInsertClauseWithNull(args) }";
                _DBConnection.Execute(insertSql, new { ApplicationId = appId, FileId = fileId, WindowText = windowText });

                windowItem = _DBConnection.QueryFirst<WindowItem>(selectSql, new { ApplicationId = appId, FileId = fileId, WindowText = windowText });
            }

            return windowItem;

        }

        private ApplicationItem GetOrCreateApplicationItemFromDB(string appName)
        {
            string selectSql = "SELECT * FROM Application WHERE Name = @Name;";
            ApplicationItem app = _DBConnection.QueryFirstOrDefault<ApplicationItem>(selectSql, new { Name = appName });

            if (app == null)
            {
                // Get the executable
                string exe = ProcessHelper.GetProcessFileName(_foregroundProcess);

                string insertSql = "INSERT INTO Application (Name, Executable) VALUES (@Name, @Executable);";
                _DBConnection.Execute(insertSql, new { Name = appName, Executable = exe });

                app = _DBConnection.QueryFirst<ApplicationItem>(selectSql, new { Name = appName });
            }

            return app;
        }

        private FileItem GetOrCreateFileItemFromDB(string fileName, string filePath, int? appId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("Name", fileName);
            args.Add("Location", filePath);
            args.Add("ApplicationId", appId);

            string selectSql = "SELECT * FROM File " + GenerateWhereClauseWithNull(args) + ";";

            FileItem fileItem = _DBConnection.QueryFirstOrDefault<FileItem>(selectSql, new { Name = fileName, Location = filePath, ApplicationId = appId });

            if (fileItem == null)
            {
                string insertSql = "INSERT INTO File " + GenerateInsertClauseWithNull(args) + ";";
                _DBConnection.Execute(insertSql, new { Name = fileName, Location = filePath, ApplicationId = appId });

                fileItem = _DBConnection.QueryFirst<FileItem>(selectSql, new { Name = fileName, Location = filePath, ApplicationId = appId});
            }

            return fileItem;
        }


        private string GenerateInsertClauseWithNull(Dictionary<string, object> props)
        {
            List<string> vals = new List<string>();

            foreach(KeyValuePair<string, object> item in props)
            {
                if (item.Value != null)
                {
                    vals.Add(item.Key);
                }
            }

            string columnsToInsert = $"({ String.Join(", ", vals) })";
            string valuesToInsert = $"({ String.Join(", ", vals.ConvertAll((s) => "@" + s)) })";

            return columnsToInsert + " VALUES " + valuesToInsert;
        }

        private string GenerateWhereClauseWithNull(Dictionary<string, object> props)
        {
            List<string> whereClauses = new List<string>();

            foreach (KeyValuePair<string, object> item in props)
            {
                if (item.Value != null)
                {
                    whereClauses.Add($"{item.Key} = @{item.Key}");
                }
                else
                {
                    whereClauses.Add($"{item.Key} IS NULL");
                }
            }

            return "WHERE " + String.Join(" AND ", whereClauses);
        }
    }
}
