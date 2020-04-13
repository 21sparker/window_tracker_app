using Dapper.Contrib.Extensions;

namespace WindowTrackerApp
{
    [Table("Project")]
    public class ProjectItem
    {
        [Key]
        public int ProjectId { get; set; }

        public string Name { get; set; }

    }
}
