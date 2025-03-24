using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class EditCourseStudentMarkModel
    {
        [Required]
        public MarkType markType { get; set; }

        [Required]
        public StudentMarks mark { get; set; }
    }
}
