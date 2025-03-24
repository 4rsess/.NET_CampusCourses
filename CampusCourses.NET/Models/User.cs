namespace CampusCourses.NET.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public bool isTeacher { get; set; }
        public bool isStudent { get; set; }
        public bool isAdmin { get; set; }
    }
}
