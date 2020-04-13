using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;


namespace WindowTrackerApp
{

    public class ParsedWindow
    {
        public string ApplicationName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string WindowName { get; set; }
        public string WindowTitle { get; set; }
    }
    /// <summary>
    /// Responsible for converting the window title to something more manageable. Every application/website
    /// will have different conventions. We'll only parse the ones of interest.
    /// </summary>
    public static class ParserService
    {
        private static List<string> customChromeDomains = new List<string>() { "Gmail", "Google Sheets", "Google Drive", "YouTube", "Google Calendar", "Google Docs"};
        


        public static ParsedWindow ParseProcess(string procName, string procWindowTitle, Process proc)
        {
            ParsedWindow parsedWindow = new ParsedWindow();

            // Add app, file name, file path to parsedWindow object
            GetAppAndFileName(procName, procWindowTitle, proc, parsedWindow);

            parsedWindow.WindowName = procName;
            parsedWindow.WindowTitle = CleanWindowTitle(procWindowTitle);

            return parsedWindow;
        }

        private static string CleanWindowTitle(string windowTitle)
        {
            if (windowTitle.Contains("Gmail"))
            {
                string[] titleSplit = windowTitle.Split('-');
                if (titleSplit.Length > 2)
                {
                    titleSplit[0] = Regex.Replace(titleSplit[0], @"\([\d,]*\)", "");
                    return String.Join(" - ", titleSplit);
                }
            }

            return windowTitle;
        }


        private static void GetAppAndFileName(string procName, string procWindowTitle, Process proc, ParsedWindow parsedWindow)
        {
            string appName = null;
            string filePath = null;
            string fileName = null;

            if (procName == "chrome")
            {
                appName = GetChromeDomainWebsite(procWindowTitle);
                if (appName != null)
                {
                    fileName = GetChromeAppFile(procWindowTitle, appName);
                }
            }
            else if (procName == "UiPath.Studio")
            {
                appName = "UiPath Studio";
                fileName = procWindowTitle.Contains("-") ? null : procWindowTitle.Split('-')[1];
            }
            else if (procName == "explorer")
            {
                appName = "Explorer";
            }
            else if (procName == "EXCEL")
            {
                appName = "Excel";
                Microsoft.Office.Interop.Excel.Application excelApp = WindowTracker.ExcelInteropService.GetOpenExcelApplication(proc);
                try
                {
                    fileName = excelApp.ActiveWorkbook.FullName;
                    filePath = excelApp.ActiveWorkbook.Path;
                }
                catch
                {
                    // Do nothing, excel is open but we're not focused on a file.
                }
            }

            if (appName == null)
            {
                appName = procName;
            }

            parsedWindow.ApplicationName = appName;
            parsedWindow.FileName = fileName;
            parsedWindow.FilePath = filePath;
            
        }

        /// <summary>
        /// Returns Domain of Chrome Window Title, if found.
        /// </summary>
        /// <example>
        /// GetChromeDomainWebsite("Inbox(1,233) - seanparker@email.com - Gmail - Google Chrome")
        /// ---> "Gmail"
        /// </example>
        /// <param name="procWindowTitle"></param>
        /// <returns>Domain of Chrome Title or null</returns>
        private static string GetChromeDomainWebsite(string procWindowTitle)
        {
            // Google Apps will typically follow the convention
            // 'Something Something Something - Gmail - Google Chrome
            // 'Something Something Something - Google Sheets - Google Chrome
            if (procWindowTitle.Count((c) => c == '-') >= 2)
            {
                // Look for some google apps
                foreach (string customDomain in customChromeDomains)
                {
                    if (procWindowTitle.Contains(customDomain))
                    {
                        return customDomain;
                    }
                }
            }
            return null;   
        }

        /// <summary>
        /// Returns the file currently open by a Chrome application. Only applicable
        /// to some apps such as Google Sheets, Google Docs, etc.
        /// </summary>
        /// <example>
        /// GetChromeAppFile("My Resume - Google Docs - Google Chrome", "Google Docs")
        /// -> "My Resume"
        /// </example>
        /// <param name="procWindowTitle"></param>
        /// <param name="appName"></param>
        /// <returns>File Name</returns>
        private static string GetChromeAppFile(string procWindowTitle, string appName)
        {
            if (appName == "Google Sheets" |
                appName == "Google Docs")
            {
                return procWindowTitle.Split('-')[0];
            }

            return null;
        }
    }

}
