using Dapper.Contrib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowTrackerApp
{
    [Table("Window")]
    public class WindowItem
    {
        [Key]
        public int WindowId { get; set; }

        public int ApplicationId { get; set; }

        public int FileId { get; set; }

        public int ProjectId { get; set; }

        public string WindowText { get; set; }

        [Write(false)]
        public TimeSpan TimeSpent { get; set; }

        [Write(false)]
        public ApplicationItem Application { get; set; }

        [Write(false)]
        public FileItem File { get; set; }
    }
}
