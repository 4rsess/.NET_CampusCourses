namespace CampusCourses.NET.Models
{
    public class TokenBlackList
    {
        public static List<string> deactivatedToken = new List<string>();

        public static bool isTokenDeactivated (string token)
        {
            return deactivatedToken.Contains(token);
        }

        public static void DeactivateToken(string token)
        {
            if (!deactivatedToken.Contains(token))
            {
                deactivatedToken.Add(token);
            }
        }
    }
}
