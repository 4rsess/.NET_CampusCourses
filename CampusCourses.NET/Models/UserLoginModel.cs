using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class UserLoginModel
    {
        [MinLength(1), Required, EmailAddress]
        public string email { get; set; }

        [MinLength(1), Required]
        public string password { get; set; }
    }
}
