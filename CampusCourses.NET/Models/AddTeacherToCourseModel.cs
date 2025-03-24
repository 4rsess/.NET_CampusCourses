using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class AddTeacherToCourseModel
    {
        [Required]
        public Guid userId { get; set; }
    }
}
