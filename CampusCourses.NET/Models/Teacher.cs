namespace CampusCourses.NET.Models
{
    public class Teacher
    {
        public Guid Id { get; set; }
        public Guid teacherId { get; set; }
        public Guid CourseId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsMain { get; set; }
    }
}
