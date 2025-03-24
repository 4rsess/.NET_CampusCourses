namespace CampusCourses.NET.Models
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid CourseId { get; set; }
        public string Text { get; set; }
        public bool IsImportant { get; set; }
    }
}
