using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class EditUserProfileModel
    {
        [MinLength(1), Required]
        public string fullName { get; set; }

        [Required]
        public DateTime birthDate { get; set; }
    }
}
