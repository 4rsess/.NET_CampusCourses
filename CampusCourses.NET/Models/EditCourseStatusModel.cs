using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class EditCourseStatusModel
    {
        [Required]
        public CourseStatuses status { get; set; }
    }
}
