using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class TokenResponse
    {
        [MinLength(1), Required]
        public string? token { get; set; }

        public TokenResponse(string Token)
        {
            token = Token;
        }
    }
}
