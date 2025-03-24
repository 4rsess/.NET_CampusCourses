namespace CampusCourses.NET.Models
{
    public class CampusGroupReportModel
    {
        public string name { get; set; }
        public Guid id { get; set; }
        public double averagePassed { get; set; }
        public double averageFailed { get; set; }
    }
}
