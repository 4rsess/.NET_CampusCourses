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
    public class GroupController : ControllerBase
    {
        private readonly DBConnect dbData;
        private readonly IConfiguration dbDataConf;

        public GroupController(DBConnect context, IConfiguration conf)
        {
            dbData = context;
            dbDataConf = conf;
        }


        [HttpGet("groups")]
        [SwaggerResponse(200, "Success", typeof(IEnumerable<CampusGroupModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get list of all campis groups")]
        public IActionResult GetGroups()
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

                var groups = dbData.Groups
                    .Select(group => new CampusGroupModel
                    {
                        id = group.Id,
                        name = group.Name
                    }).ToList();

                return Ok(groups);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPost("groups")]
        [SwaggerResponse(200, "Success", typeof(CampusGroupModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Create campus group")]
        public IActionResult CreateGroup([FromBody] CreateCampusGroupModel createModel)
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return StatusCode(403, new Response("Ошибка 403", "Токен деактивирован"));
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                if (string.IsNullOrWhiteSpace(createModel.name))
                {
                    return BadRequest(new Response("Ошибка 400", "Название группы не может быть пустым"));
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
                    return StatusCode(403, new Response("Ошибка 403", "Группу может создать только администратор"));
                }

                var existingGroup = dbData.Groups.FirstOrDefault(g => g.Name == createModel.name);
                if (existingGroup != null)
                {
                    return BadRequest(new Response("Ошибка 400", "Группа с таким названием уже существует"));
                }

                var group = new Group
                {
                    Name = createModel.name
                };

                dbData.Groups.Add(group);
                dbData.SaveChanges();

                var groupModel = new CampusGroupModel
                {
                    id = group.Id,
                    name = group.Name
                };

                return Ok(groupModel);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpPut("groups/{id}")]
        [SwaggerResponse(200, "Success", typeof(CampusGroupModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit campus group group name")]
        public IActionResult EditGroup(Guid id, [FromBody] EditCampusGroupModel editModel)
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return StatusCode(403, new Response("Ошибка 403", "Токен деактивирован"));
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                if (string.IsNullOrWhiteSpace(editModel.name))
                {
                    return BadRequest(new Response("Ошибка 400", "Название группы не может быть пустым"));
                }

                var group = dbData.Groups.FirstOrDefault(g => g.Id == id);
                if (group == null)
                {
                    return NotFound(new Response("Ошибка 404","Группа не найдена"));
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
                    return StatusCode(403, new Response("Ошибка 403", "Изменять группу может только администратор"));
                }

                if (group.Name == editModel.name)
                {
                    return BadRequest(new Response("Ошибка 400", "Новое название группы должно отличаться от текущего"));
                }

                group.Name = editModel.name;
                dbData.SaveChanges();

                var updatedGroup = new CampusGroupModel
                {
                    id = group.Id,
                    name = group.Name
                };

                return Ok(updatedGroup);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpDelete("groups/{id}")]
        [SwaggerResponse(200, "Success")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Delete campus group")]
        public IActionResult DeleteGroup(Guid id)
        {
            try
            {
                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                if (TokenBlackList.isTokenDeactivated(token))
                {
                    return StatusCode(403, new Response("Ошибка 403", "Токен деактивирован"));
                }

                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new Response("Ошибка 401", "Вы еще не зарегистрированы"));
                }

                var group = dbData.Groups.FirstOrDefault(g => g.Id == id);
                if (group == null)
                {
                    return NotFound(new Response("Ошибка 404", "Группа не найдена"));
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
                    return StatusCode(403, new Response("Ошибка 403", "Удалять группу может только администратор"));
                }

                var coursesToDelete = dbData.CampusCourses.Where(c => c.GroupId == id).ToList();

                foreach (var course in coursesToDelete)
                {
                    dbData.Students.RemoveRange(dbData.Students.Where(s => s.CourseId == course.Id));
                    dbData.Teachers.RemoveRange(dbData.Teachers.Where(t => t.CourseId == course.Id));
                    dbData.Notifications.RemoveRange(dbData.Notifications.Where(n => n.CourseId == course.Id));

                    dbData.CampusCourses.Remove(course);
                }

                dbData.Groups.Remove(group);
                dbData.SaveChanges();

                return Ok();
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpGet("groups/{id}")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get list of campus courses of the campus group")]
        public IActionResult GetCoursesList(Guid id)
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

                var courses = dbData.CampusCourses
                    .Where(c => c.GroupId == id)
                    .Select(c => new CampusCoursePreviewModel
                    {
                        id = c.Id,
                        name = c.Name,
                        startYear = c.StartYear,
                        maximumStudentCount = c.MaximumStudentCount,
                        remainingSlotsCount = c.RemainingSlotsCount,
                        status = c.Status,
                        semester = c.Semester
                    })
                    .ToList();

                if (!courses.Any())
                {
                    return NotFound(new Response("Ошибка 404", "У данной группы нет курсов"));
                }

                return Ok(courses);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }

    }
}
