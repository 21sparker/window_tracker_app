using Dapper.Contrib.Extensions;

namespace WindowTrackerApp
{
    [Table("File")]
    public class FileItem
    {
        [Key]
        public int FileId { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public int ApplicationId { get; set; }
    }
}
