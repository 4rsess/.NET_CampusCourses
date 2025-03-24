using CampusCourses.NET.DB;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CampusCourses.NET.Models;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
namespace CampusCourses.NET.Controllers
{
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DBConnect dbData;
        private readonly IConfiguration dbDataConf;

        public UsersController(DBConnect context, IConfiguration conf)
        {
            dbData = context;
            dbDataConf = conf;
        }

        [HttpGet("users")]
        [SwaggerResponse(200, "Success", typeof(IEnumerable<UserShortModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get list of all users")]
        public IActionResult GetUsers()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Токен деактивирован"));
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "ID");
                if (userIdClaim == null)
                {
                    return Unauthorized(new Response("Ошибка 401", "ID не найден в токене"));
                }

                Guid userId = Guid.Parse(userIdClaim.Value);
                var user = dbData.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return Unauthorized(new Response("Ошибка 401", "Пользователь не найден"));
                }
                if (!user.isAdmin)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Воспользоваться данной функцией может только администратор"));
                }

                var users = dbData.Users
                    .Select(user => new UserShortModel
                    {
                        id = user.Id,
                        fullName = user.FullName
                    }).ToList();

                return Ok(users);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpGet("roles")]
        [SwaggerResponse(200, "Success", typeof(UserRolesModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get info about current user's roles")]
        public IActionResult GetRoles()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Токен деактивирован"));
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "ID");
                if (userIdClaim == null)
                {
                    return Unauthorized(new Response("Ошибка 401", "ID не найден в токене"));
                }

                Guid userId = Guid.Parse(userIdClaim.Value);

                var user = dbData.Users.Find(userId);
                if (user == null)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Пользователь не найден"));
                }

                var userRoles = new UserRolesModel
                {
                    isTeacher = user.isTeacher,
                    isStudent = user.isStudent,
                    isAdmin = user.isAdmin
                };

                return Ok(userRoles);

            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }
    }
}
