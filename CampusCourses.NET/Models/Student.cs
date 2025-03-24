namespace CampusCourses.NET.Models
{
    public class Student
    {
        public Guid Id { get; set; }
        public Guid studentId { get; set; }
        public Guid CourseId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public StudentStatuses Status { get; set; }
        public StudentMarks MidtermResult { get; set; }
        public StudentMarks FinalResult { get; set; }
    }
}
