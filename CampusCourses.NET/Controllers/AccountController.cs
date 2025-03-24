using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CampusCourses.NET.DB;
using CampusCourses.NET.Models;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace CampusCourses.NET.Controllers
{
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly DBConnect dbData;
        private readonly IConfiguration dbDataConf;

        public AccountController(DBConnect context, IConfiguration conf)
        {
            dbData = context;
            dbDataConf = conf;
        }

        [HttpPost("registration")]
        [SwaggerResponse(200, "Success", typeof(TokenResponse))]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Register new user")]
        public IActionResult Register([FromBody] UserRegisterModel registerModel)
        {
            try
            {
                if (registerModel.password != registerModel.confirmPassword)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Пароли не совпадают"));
                }

                if (dbData.Users.Any(u => u.Email == registerModel.email))
                {
                    return StatusCode(500, new Response("Ошибка 500", "Пользователь с таким email уже существует"));
                }

                if (registerModel.birthDate > DateTime.UtcNow)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Дата рождения не может быть в будущем"));
                }

                var user = new User
                {
                    FullName = registerModel.fullName,
                    BirthDate = registerModel.birthDate,
                    Email = registerModel.email,
                    Password = registerModel.password,
                    isTeacher = false,
                    isStudent = false,
                    isAdmin = false
                };

                dbData.Users.Add(user);
                dbData.SaveChangesAsync();

                var token = GenerateJwtToken(user);
                var tokenResponse = new TokenResponse(token);
                return Ok(tokenResponse);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }

        }

        [HttpPost("login")]
        [SwaggerResponse(200, "Success", typeof(TokenResponse))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Log into the system")]
        public IActionResult Login([FromBody] UserLoginModel loginModel)
        {
            try
            {
                var user = dbData.Users.FirstOrDefault(e => e.Email == loginModel.email);

                if (user == null || user.Password != loginModel.password)
                {
                    return BadRequest(new Response("Ошибка 400", "Неверный email или пароль"));
                }

                var token = GenerateJwtToken(user);
                var tokenResponse = new TokenResponse(token);
                return Ok(tokenResponse);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpPost("logout")]
        [SwaggerResponse(200, "Success", typeof(Response))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Log out the system")]
        public IActionResult Logout()
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Токен деактивирован"));
                }            

                TokenBlackList.DeactivateToken(token);
                var response = new Response("Success", "Вы вышли из системы");
                return Ok(response);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }

        [HttpGet("profile")]
        [SwaggerResponse(200, "Success", typeof(UserProfileModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get current user's profile info")]
        public IActionResult Profile()
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

                var userProfile = new UserProfileModel
                {
                    fullName = user.FullName,
                    email = user.Email,
                    birthDate = user.BirthDate
                };

                return Ok(userProfile);

            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPut("profile")]
        [SwaggerResponse(200, "Success", typeof(UserProfileModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit current user's profile info")]
        public IActionResult EditProfile([FromBody] EditUserProfileModel model)
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

                if (model.birthDate > DateTime.UtcNow)
                {
                    return BadRequest("Дата рождения не может быть в будущем");
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

                if (user.FullName == model.fullName)
                {
                    return BadRequest(new Response("Ошибка 400", "Новое имя должно отличаться от текущего"));
                }

                if (ModelState.IsValid)
                {
                    user.FullName = model.fullName;
                    user.BirthDate = model.birthDate;

                    var teacher = dbData.Teachers.FirstOrDefault(t => t.teacherId == userId);
                    if (teacher != null)
                    {
                        teacher.Name = model.fullName;
                    }

                    var student = dbData.Students.FirstOrDefault(s => s.studentId == userId);
                    if (student != null)
                    {
                        student.Name = model.fullName;
                    }

                    dbData.SaveChanges();

                    var updatedProfile = new UserProfileModel
                    {
                        fullName = user.FullName,
                        email = user.Email,
                        birthDate = user.BirthDate
                    };

                    return Ok(updatedProfile);
                }
                else
                {
                    return BadRequest(new Response("Ошибка 400","Неверные данные"));
                }
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }

        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(dbDataConf["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("ID", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: dbDataConf["Jwt:Issuer"], 
                audience: dbDataConf["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
