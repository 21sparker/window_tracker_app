using System;
using System.Collections.Generic;
using System.Diagnostics;
using Dapper;
using WindowTracker;

namespace WindowTrackerApp
{
    public class DatabaseGateway
    {
        private System.Data.SQLite.SQLiteConnection _DBConnection;
        
        public DatabaseGateway(string databasePath)
        {
            _DBConnection = new System.Data.SQLite.SQLiteConnection(databasePath);

            if (!DatabaseConnectionIsValid())
            {
                // i'm not sure yet what should happen here
            }
        }

        public void Close()
        {
            _DBConnection.Close();
        }

        public List<WindowItem> GetAllWindowItemsInDateRange(long? startDate, long? endDate)
        {
            List<WindowItem> windowItems = new List<WindowItem>();
            string sql = @"SELECT * FROM WindowHistory 
            LEFT JOIN Window ON Window.WindowId = WindowHistory.WindowId
            LEFT JOIN Application ON Window.ApplicationId = Application.ApplicationId
            LEFT JOIN File ON Window.FileId = File.FileId";

            string whereClause = null;

            if (startDate != null)
            {
                if (whereClause == null)
                {
                    whereClause = " WHERE";
                }
                whereClause += " LoggedDateTime >= @StartDate";
            }
            
            if (endDate != null)
            {
                if (whereClause == null)
                {
                    whereClause = " WHERE";
                }
                else
                {
                    whereClause = " AND";
                }
                whereClause += " LoggedDateTime <= @EndDate";
            }

            if (whereClause != null)
            {
                sql += whereClause;
            }


            Dictionary<int, WindowItem> windowItemDictionary = new Dictionary<int, WindowItem>();

            windowItems = _DBConnection.Query<WindowHistoryItem, WindowItem, ApplicationItem, FileItem, WindowItem>(
                sql,
                (windowHistoryItem, windowItem, applicationItem, fileItem) =>
                {
                    WindowItem windowItemEntry;

                    if (!windowItemDictionary.TryGetValue(windowItem.WindowId, out windowItemEntry))
                    {
                        windowItemEntry = windowItem;
                        windowItemEntry.Application = applicationItem;
                        windowItemEntry.File = fileItem;
                        windowItemEntry.TimeSpent = new TimeSpan();

                        windowItemDictionary.Add(windowItemEntry.WindowId, windowItemEntry);
                    }

                    windowItemEntry.TimeSpent += TimeSpan.FromSeconds(1);
                    return windowItem;
                },
                splitOn: "WindowId,ApplicationId,FileId",
                param: new { StartDate = startDate, EndDate = endDate })
                .AsList();

            return windowItems;
        }

        public WindowItem GetOrCreateWindowItemFromDB(int? appId, int? fileId, string windowText)
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


        public ApplicationItem GetOrCreateApplicationItemFromDB(string appName, Process proc)
        {
            string selectSql = "SELECT * FROM Application WHERE Name = @Name;";
            ApplicationItem app = _DBConnection.QueryFirstOrDefault<ApplicationItem>(selectSql, new { Name = appName });

            if (app == null)
            {
                // Get the executable
                string exe = ProcessHelper.GetProcessFileName(proc);

                string insertSql = "INSERT INTO Application (Name, Executable) VALUES (@Name, @Executable);";
                _DBConnection.Execute(insertSql, new { Name = appName, Executable = exe });

                app = _DBConnection.QueryFirst<ApplicationItem>(selectSql, new { Name = appName });
            }

            return app;
        }

        public FileItem GetOrCreateFileItemFromDB(string fileName, string filePath, int? appId)
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

                fileItem = _DBConnection.QueryFirst<FileItem>(selectSql, new { Name = fileName, Location = filePath, ApplicationId = appId });
            }

            return fileItem;
        }

        public void InsertWindowHistoryItem(long loggedDateTime, int windowId)
        {
            string sql = "INSERT INTO WindowHistory (LoggedDateTime, WindowId) VALUES (@LoggedDateTime, @WindowId);";
            _DBConnection.Execute(sql, new { LoggedDateTime = loggedDateTime, WindowId = windowId });
        }

        private string GenerateInsertClauseWithNull(Dictionary<string, object> props)
        {
            List<string> vals = new List<string>();

            foreach (KeyValuePair<string, object> item in props)
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

        private bool DatabaseConnectionIsValid()
        {
            return true;
        }
    }
}
