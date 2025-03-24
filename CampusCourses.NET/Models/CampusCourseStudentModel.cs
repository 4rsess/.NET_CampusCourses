namespace CampusCourses.NET.Models
{
    public class CampusCourseStudentModel
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public StudentStatuses status { get; set; }
        public StudentMarks? midtermResult { get; set; }
        public StudentMarks? finalResult { get; set; }
    }
}
