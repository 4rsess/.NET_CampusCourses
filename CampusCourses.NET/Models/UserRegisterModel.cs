using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class UserRegisterModel
    {
        [MinLength(1), Required]
        public string fullName { get; set; }

        [Required]
        public DateTime birthDate { get; set; }

        [MinLength(1), Required, EmailAddress]
        public string email { get; set; }

        [MinLength(6), MaxLength(32), Required]
        public string password { get; set; }

        [MinLength(6), MaxLength(32), Required]
        public string confirmPassword { get; set; }
    }
}
