using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class EditCourseStudentStatusModel
    {
        [Required]
        public StudentStatuses status { get; set; }
    }
}
