using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class AddCampusCourseNotificationModel
    {
        [Required, MinLength(1)]
        public string text { get; set; }

        [Required]
        public bool isImportant { get; set; }
    }
}
