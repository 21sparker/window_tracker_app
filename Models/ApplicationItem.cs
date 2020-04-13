using Dapper.Contrib.Extensions;

namespace WindowTrackerApp
{
    [Table("Application")]
    public class ApplicationItem
    {
        [Key]
        public int ApplicationId { get; set; }

        public string Name { get; set; }

        public string Executable { get; set; }
    }
}
