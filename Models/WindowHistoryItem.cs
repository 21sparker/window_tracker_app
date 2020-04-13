using Dapper.Contrib.Extensions;

namespace WindowTrackerApp
{
    [Table("WindowHistory")]
    public class WindowHistoryItem
    {
        [Key]
        public long LoggedDateTime { get; set; }

        public int WindowId { get; set; }
    }

}
