using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class CreateCampusCourseModel
    {
        [MinLength(1), Required]
        public string name { get; set; }

        [Required, Range(2000, 2029)]
        public int startYear { get; set; }

        [Required, Range(1, 200)]
        public int maximumStudentsCount { get; set; }

        [Required]
        public Semesters semester { get; set; }

        [MinLength(1), Required]
        public string requirements { get; set; }

        [MinLength(1), Required]
        public string annotations { get; set; }

        [Required]
        public Guid mainteacherId { get; set; }
    }
}
