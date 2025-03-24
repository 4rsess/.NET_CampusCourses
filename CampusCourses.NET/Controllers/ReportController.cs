using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CampusCourses.NET.Models;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Mvc;
using CampusCourses.NET.DB;
using System.IdentityModel.Tokens.Jwt;
namespace CampusCourses.NET.Controllers
{
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly DBConnect dbData;
        private readonly IConfiguration dbDataConf;

        public ReportController(DBConnect context, IConfiguration conf)
        {
            dbData = context;
            dbDataConf = conf;
        }

        [HttpGet("report")]
        [SwaggerResponse(200, "Success", typeof(List<TeacherReportRecordModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        public IActionResult GetReport([FromQuery] Semesters? semester, [FromQuery] List<Guid> campusGroupIds)
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

                var reportList = new List<TeacherReportRecordModel>();

                foreach (var groupId in campusGroupIds)
                {
                    var courses = dbData.CampusCourses
                        .Where(c => c.GroupId == groupId && (semester == null || c.Semester == semester))
                        .ToList();

                    foreach (var course in courses)
                    {
                        var mainTeacher = dbData.Teachers
                            .FirstOrDefault(t => t.CourseId == course.Id && t.IsMain);

                        if (mainTeacher == null) continue;

                        var students = dbData.Students.Where(s => s.CourseId == course.Id).ToList();

                        double passedCount = students.Count(s => s.FinalResult == StudentMarks.Passed);
                        double failedCount = students.Count(s => s.FinalResult == StudentMarks.Failed);

                        var group = dbData.Groups.FirstOrDefault(g => g.Id == groupId);
                        if (group == null) continue;

                        var campusGroupReport = new CampusGroupReportModel
                        {
                            id = course.Id,
                            name = course.Name,
                            averagePassed = passedCount,
                            averageFailed = failedCount
                        };

                        var teacherReport = reportList.FirstOrDefault(r => r.id == mainTeacher.teacherId);
                        if (teacherReport == null)
                        {
                            teacherReport = new TeacherReportRecordModel
                            {
                                id = mainTeacher.teacherId,
                                fullName = mainTeacher.Name,
                                campusGroupReports = new List<CampusGroupReportModel>()
                            };
                            reportList.Add(teacherReport);
                        }

                        teacherReport.campusGroupReports.Add(campusGroupReport);
                    }
                }

                reportList = reportList.OrderBy(t => t.fullName).ToList();

                return Ok(reportList);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }

    }
}
