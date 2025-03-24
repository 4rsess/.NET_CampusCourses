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
    public class CourseController : ControllerBase
    {
        private readonly DBConnect dbData;
        private readonly IConfiguration dbDataConf;

        public CourseController(DBConnect context, IConfiguration conf)
        {
            dbData = context;
            dbDataConf = conf;
        }


        [HttpGet("courses/{id}/details")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get campus course's detailed info")]
        public IActionResult GetCampusCourseInfo(Guid id)
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

                var campusCourse = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (campusCourse == null)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Курс с данным id не найден"));
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

                bool isAdmin = user.isAdmin;
                bool isTeacher = dbData.Teachers.Any(t => t.CourseId == id && t.teacherId == userId);
                bool isStudent = user.isStudent;

                var currentStudent = dbData.Students.FirstOrDefault(s => s.CourseId == id && s.studentId == userId);
                bool isAcceptedStudent = currentStudent != null && currentStudent.Status == StudentStatuses.Accepted;

                var studentsQuery = dbData.Students.Where(s => s.CourseId == id);

                if (!isAdmin && !isTeacher)
                {
                    studentsQuery = studentsQuery.Where(s => s.Status == StudentStatuses.Accepted);
                }

                var studentsAcceptedCount = dbData.Students
                    .Where(s => s.CourseId == id && s.Status == StudentStatuses.Accepted)
                    .Count();

                var studentsInQueueCount = dbData.Students
                    .Where(s => s.CourseId == id && s.Status == StudentStatuses.InQueue)
                    .Count();

                var students = studentsQuery
                    .Select(s => new CampusCourseStudentModel
                    {
                        id = s.studentId,
                        name = s.Name,
                        email = s.Email,
                        status = s.Status,
                        midtermResult = (isAdmin || isTeacher || (isAcceptedStudent && s.studentId == userId)) ? s.MidtermResult : null,
                        finalResult = (isAdmin || isTeacher || (isAcceptedStudent && s.studentId == userId)) ? s.FinalResult : null
                    }).ToList();

                var teachers = dbData.Teachers
                    .Where(t => t.CourseId == id)
                    .Select(t => new CampusCourseTeacherModel
                    {
                        name = t.Name,
                        email = t.Email,
                        isMain = t.IsMain
                    }).ToList();

                var notifications = dbData.Notifications
                    .Where(n => n.CourseId == id)
                    .Select(n => new CampusCourseNotificationModel
                    {
                        text = n.Text,
                        isImportant = n.IsImportant
                    }).ToList();

                var courseDetails = new CampusCourseDetailsModel
                {
                    id = campusCourse.Id,
                    name = campusCourse.Name,
                    startYear = campusCourse.StartYear,
                    maximumStudentCount = campusCourse.MaximumStudentCount,
                    studentsEnrolledCount = studentsAcceptedCount,
                    studentsInQueueCount = studentsInQueueCount,
                    requirements = campusCourse.Requirements,
                    annotations = campusCourse.Annotations,
                    status = campusCourse.Status,
                    semester = campusCourse.Semester,
                    students = students,
                    teachers = teachers,
                    notifications = notifications
                };

                return Ok(courseDetails);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpPost("courses/{id}/sign-up")]
        [SwaggerResponse(200, "Success")]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Sign up for a campus course")]
        public IActionResult SignUpToCourse(Guid id)
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

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с указанным id не найден"));
                }
                if (course.Status == CourseStatuses.Finished || course.Status == CourseStatuses.Created || course.Status == CourseStatuses.Started)
                {
                    return BadRequest(new Response("Ошибка 400", "Курс не соответствует статусу 'Открыт для заявок'"));
                }

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "ID");
                if (userIdClaim == null)
                {
                    return Unauthorized(new Response("Ошибка 401", "ID не найден в токене"));
                }

                Guid userId = Guid.Parse(userIdClaim.Value);

                var user = dbData.Users.FirstOrDefault(s => s.Id == userId);
                if (user == null)
                {
                    return BadRequest(new Response("Ошибка 400", "Пользователь не найден"));
                }

                if (user.isAdmin || user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет присоединиться на курс"));
                }

                var existingStudent = dbData.Students.FirstOrDefault(s => s.studentId == userId && s.CourseId == id);
                if (existingStudent != null)
                {
                    return BadRequest(new Response("Ошибка 400", "Вы уже записаны на этот курс"));
                }

                var student = new Student
                {
                    studentId = userId,
                    CourseId = id,
                    Name = user.FullName,
                    Email = user.Email,
                    Status = StudentStatuses.InQueue,
                    MidtermResult = StudentMarks.NotDefined,
                    FinalResult = StudentMarks.NotDefined
                };

                dbData.Students.Add(student);
                dbData.SaveChanges();

                return Ok();
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpPost("courses/{id}/status")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit campus course's status")]
        public IActionResult EditCourseStatus(Guid id, [FromBody] EditCourseStatusModel editModel)
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

                if (editModel == null || !Enum.IsDefined(typeof(CourseStatuses), editModel.status))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
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
                if (!user.isAdmin && !user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс не найден"));
                }

                if (course.Status == CourseStatuses.OpenForAssigning && editModel.status == CourseStatuses.Created)
                {
                    return BadRequest(new Response("Ошибка 400", "Нельзя изменить статус с 'OpenForAssigning' на 'Created'"));
                }

                if (course.Status == CourseStatuses.Started &&
                    (editModel.status == CourseStatuses.OpenForAssigning || editModel.status == CourseStatuses.Created))
                {
                    return BadRequest(new Response("Ошибка 400", "Нельзя изменить статус с 'Started' на 'OpenForAssigning' или 'Created'"));
                }

                if (course.Status == CourseStatuses.Finished &&
                    (editModel.status == CourseStatuses.OpenForAssigning || editModel.status == CourseStatuses.Created || editModel.status == CourseStatuses.Started))
                {
                    return BadRequest(new Response("Ошибка 400", "Нельзя изменить статус с 'Finished' на любой другой"));
                }

                course.Status = editModel.status;
                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpPost("courses/{id}/student-status/{studentId}")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit status of the student that signed up for the course")]
        public IActionResult EditStudentStatus(Guid id, Guid studentId, [FromBody] EditCourseStudentStatusModel studentEditModel)
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
                if (!user.isAdmin && !user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                if (studentEditModel == null || !Enum.IsDefined(typeof(StudentStatuses), studentEditModel.status))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
                }

                var student = dbData.Students.FirstOrDefault(s => s.CourseId == id && s.studentId == studentId);
                if (student == null)
                {
                    return NotFound(new Response("Ошибка 404", "Студент данного курса не найден"));
                }


                bool wasAccepted = student.Status == StudentStatuses.Accepted;
                student.Status = studentEditModel.status;

                if (!wasAccepted && student.Status == StudentStatuses.Accepted)
                {
                    var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                    if (course == null)
                    {
                        return NotFound(new Response("Ошибка 404", "Курс не найден"));
                    }

                    if (course.RemainingSlotsCount == 0)
                    {
                        return BadRequest(new Response("Ошибка 400", "Нет свободных мест на курсе"));
                    }

                    course.RemainingSlotsCount -= 1;

                    var userr = dbData.Users.FirstOrDefault(u => u.Id == studentId);
                    if (userr != null)
                    {
                        userr.isStudent = true;
                    }
                }

                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }





        [HttpPost("courses/{id}/notifications")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Create new course notification")]
        public IActionResult CteateNotification(Guid id, [FromBody] AddCampusCourseNotificationModel notificationModel)
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
                if (!user.isAdmin && !user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                if (notificationModel == null || string.IsNullOrWhiteSpace(notificationModel.text))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
                }

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с данным id не найден"));
                }

                var notification = new Notification
                {
                    CourseId = id,
                    Text = notificationModel.text,
                    IsImportant = notificationModel.isImportant
                };

                dbData.Notifications.Add(notification);
                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpPost("courses/{id}/marks/{studentId}")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit mark of the student studying the campus course")]
        public IActionResult EditStudentMark(Guid id, Guid studentId, [FromBody] EditCourseStudentMarkModel markEditModel)
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
                if (!user.isAdmin && !user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                if (markEditModel == null || !Enum.IsDefined(typeof(MarkType), markEditModel.markType) || !Enum.IsDefined(typeof(StudentMarks), markEditModel.mark))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
                }

                var student = dbData.Students.FirstOrDefault(s => s.CourseId == id && s.studentId == studentId);
                if (student == null)
                {
                    return NotFound(new Response("Ошибка 404", "Студент данного курса не найден"));
                }

                if (student.Status != StudentStatuses.Accepted)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Студент не зачислен на данный курс"));
                }

                if (markEditModel.markType == MarkType.Midterm)
                {
                    student.MidtermResult = markEditModel.mark;
                }
                else if (markEditModel.markType == MarkType.Final)
                {
                    student.FinalResult = markEditModel.mark;
                }

                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }





        [HttpPost("groups/{groupId}")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Create new campus course for the campus group")]
        public IActionResult CreateCampusCourse(Guid groupId, [FromBody] CreateCampusCourseModel createModel)
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
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                var group = dbData.Groups.FirstOrDefault(g => g.Id == groupId);
                if (group == null)
                {
                    return BadRequest("Группа с указанным Id не найдена");
                }

                if (string.IsNullOrWhiteSpace(createModel.name) || createModel.maximumStudentsCount <= 0)
                {
                    return BadRequest("Некорректные данные для создания курса");
                }

                var teacherUser = dbData.Users.FirstOrDefault(u => u.Id == createModel.mainteacherId);
                if (teacherUser == null)
                {
                    return BadRequest("Учитель с указанным Id не найден");
                }

                if (!Enum.IsDefined(typeof(Semesters), createModel.semester))
                {
                    return BadRequest("Указан недопустимый семестр");
                }


                var campusCourse = new CampusCourse
                {
                    GroupId = groupId,
                    Name = createModel.name,
                    StartYear = createModel.startYear,
                    MaximumStudentCount = createModel.maximumStudentsCount,
                    Semester = createModel.semester,
                    Requirements = createModel.requirements,
                    Annotations = createModel.annotations,
                    MainTeacherId = createModel.mainteacherId,
                    RemainingSlotsCount = createModel.maximumStudentsCount,
                    Status = CourseStatuses.Created
                };

                dbData.CampusCourses.Add(campusCourse);
                dbData.SaveChanges();

                var teacher = new Teacher
                {
                    teacherId = createModel.mainteacherId,
                    CourseId = campusCourse.Id,
                    Name = teacherUser.FullName,
                    Email = teacherUser.Email,
                    IsMain = true
                };

                dbData.Teachers.Add(teacher);
                teacherUser.isTeacher = true;
                dbData.Users.Update(teacherUser);
                dbData.SaveChanges();

                var allCourses = dbData.CampusCourses
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


                return Ok(allCourses);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }



        [HttpPut("courses/{id}/requirements-and-annotations")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit campus course's annotation and requirements")]
        public IActionResult EditAnnotationAndRequirements(Guid id, [FromBody] EditCampusCourseRequirementsAndAnnotationsModel model)
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
                if (!user.isAdmin && !user.isTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                if (model == null || string.IsNullOrWhiteSpace(model.requirements) || string.IsNullOrWhiteSpace(model.annotations))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
                }

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с данным id не найден"));
                }

                course.Annotations = model.annotations;
                course.Requirements = model.requirements;
                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }



        [HttpPut("courses/{id}")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Edit campus course")]
        public IActionResult EditCampusCourse(Guid id, [FromBody] EditCampusCourseModel courseEditModel)
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
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                if (courseEditModel == null ||
                    string.IsNullOrEmpty(courseEditModel.name) ||
                    !Enum.IsDefined(typeof(Semesters), courseEditModel.semester) ||
                    string.IsNullOrEmpty(courseEditModel.requirements) ||
                    string.IsNullOrEmpty(courseEditModel.annotations))
                {
                    return BadRequest(new Response("Ошибка 400", "Некорректные входные данные"));
                }

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с данным ID не найден"));
                }

                course.Name = courseEditModel.name;
                course.StartYear = courseEditModel.startYear;
                course.MaximumStudentCount = courseEditModel.maximumStudentsCount;
                course.Semester = courseEditModel.semester;
                course.Requirements = courseEditModel.requirements;
                course.Annotations = courseEditModel.annotations;

                if (course.MainTeacherId != courseEditModel.mainTeacherId)
                {
                    var oldTeacher = dbData.Teachers.FirstOrDefault(t => t.teacherId == course.MainTeacherId && t.CourseId == id);
                    if (oldTeacher != null)
                    {
                        oldTeacher.IsMain = false;
                    }

                    var newTeacher = dbData.Teachers.FirstOrDefault(t => t.teacherId == courseEditModel.mainTeacherId && t.CourseId == id);
                    if (newTeacher == null)
                    {
                        return NotFound(new Response("Ошибка 404", "Новый учитель не найден или же он не является учителем указанного курса"));
                    }

                    newTeacher.IsMain = true;
                    course.MainTeacherId = courseEditModel.mainTeacherId;
                }

                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpDelete("courses/{id}")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Delete campus course")]
        public IActionResult DeleteCourse(Guid id)
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
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                var course = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (course == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с данным id не найден"));
                }

                var mainTeacher = dbData.Teachers.FirstOrDefault(t => t.CourseId == id && t.IsMain);

                var students = dbData.Students.Where(s => s.CourseId == id).ToList();
                if (students.Any()) dbData.Students.RemoveRange(students);

                var teachers = dbData.Teachers.Where(t => t.CourseId == id).ToList();
                if (teachers.Any()) dbData.Teachers.RemoveRange(teachers);

                var notifications = dbData.Notifications.Where(n => n.CourseId == id).ToList();
                if (notifications.Any()) dbData.Notifications.RemoveRange(notifications);

                dbData.CampusCourses.Remove(course);
                dbData.SaveChanges();

                if (mainTeacher != null)
                {
                    bool stillATeacher = dbData.Teachers.Any(t => t.teacherId == mainTeacher.teacherId);
                    if (!stillATeacher)
                    {
                        var teacherUser = dbData.Users.FirstOrDefault(u => u.Id == mainTeacher.teacherId);
                        if (teacherUser != null)
                        {
                            teacherUser.isTeacher = false;
                            dbData.SaveChanges();
                        }
                    }
                }


                var remainingCourses = dbData.CampusCourses
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

                return Ok(remainingCourses);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpPost("courses/{id}/teachers")]
        [SwaggerResponse(200, "Success", typeof(CampusCourseDetailsModel))]
        [SwaggerResponse(400, "Bad Request")]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(403, "Forbidden")]
        [SwaggerResponse(404, "Not Found")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Add campus course's teacher role to a user")]
        public IActionResult AddTeacherToCourse(Guid id, [FromBody] AddTeacherToCourseModel model)
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
                var userr = dbData.Users.FirstOrDefault(u => u.Id == userId);
                if (userr == null)
                {
                    return Unauthorized(new Response("Ошибка 401", "Пользователь не найден"));
                }

                bool isMainTeacher = dbData.Teachers.Any(t => t.CourseId == id && t.teacherId == userId && t.IsMain);
                if (!userr.isAdmin && !isMainTeacher)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Ваша роль не позволяет выполнить данную функцию"));
                }

                var campusCourse = dbData.CampusCourses.FirstOrDefault(c => c.Id == id);
                if (campusCourse == null)
                {
                    return NotFound(new Response("Ошибка 404", "Курс с данным id не найден"));
                }

                var user = dbData.Users.FirstOrDefault(u => u.Id == model.userId);
                if (user == null)
                {
                    return NotFound(new Response("Ошибка 404", "Пользователь с данным id не найден"));
                }

                var existingTeacher = dbData.Teachers
                    .FirstOrDefault(t => t.CourseId == id && t.teacherId == model.userId);
                if (existingTeacher != null)
                {
                    return StatusCode(403, new Response("Ошибка 403", "Пользователь уже является учителем данного курса"));
                }

                var newTeacher = new Teacher
                {
                    teacherId = model.userId,
                    CourseId = id,
                    Name = user.FullName,
                    Email = user.Email,
                    IsMain = false
                };
                dbData.Teachers.Add(newTeacher);
                user.isTeacher = true;
                dbData.Users.Update(user);
                dbData.SaveChanges();

                return GetCampusCourseInfo(id);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }




        [HttpGet("courses/my")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get list of campus courses user has signed up for")]
        public IActionResult GetListMyCourses()
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

                var studentCourses = dbData.Students.Where(s => s.studentId == userId).ToList();
                if (studentCourses.Count == 0)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Вы не зарегестрированы ни на один курс"));
                }

                var courseIds = studentCourses.Select(s => s.CourseId).ToList();
                var courses = dbData.CampusCourses.Where(c => courseIds.Contains(c.Id)).ToList();

                var coursePreviewList = courses.Select(c => new CampusCoursePreviewModel
                {
                    id = c.Id,
                    name = c.Name,
                    startYear = c.StartYear,
                    maximumStudentCount = c.MaximumStudentCount,
                    remainingSlotsCount = c.RemainingSlotsCount,
                    status = c.Status,
                    semester = c.Semester
                }).ToList();

                return Ok(coursePreviewList);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }





        [HttpGet("courses/teaching")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(401, "Unauthorized")]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        [EndpointSummary("Get list of campus courses user is teaching")]
        public IActionResult GetTeachingCoursesList()
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

                var courseId = dbData.Teachers
                    .Where(t => t.teacherId == userId)
                    .Select(t => t.CourseId)
                    .Distinct()
                    .ToList();

                if (courseId == null || courseId.Count == 0)
                {
                    return StatusCode(500, new Response("Ошибка 500", "Вы не преподаете никакие курсы"));
                }

                var courses = dbData.CampusCourses
                    .Where(c => courseId.Contains(c.Id))
                    .Select(course => new CampusCoursePreviewModel
                    {
                        id = course.Id,
                        name = course.Name,
                        startYear = course.StartYear,
                        maximumStudentCount = course.MaximumStudentCount,
                        remainingSlotsCount = course.RemainingSlotsCount,
                        status = course.Status,
                        semester = course.Semester
                    })
                    .ToList();

                return Ok(courses);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }


        [HttpGet("courses/list")]
        [SwaggerResponse(200, "Success", typeof(List<CampusCoursePreviewModel>))]
        [SwaggerResponse(500, "InternalServerError", typeof(Response))]
        public IActionResult GetCoursesList(
            [FromQuery] SortOption? sort = SortOption.CreatedAsc,
            [FromQuery] string search = null,
            [FromQuery] bool? hasPlacesAndOpen = null,
            [FromQuery] Semesters? semester = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = dbData.CampusCourses.AsQueryable();

                if (hasPlacesAndOpen.HasValue && hasPlacesAndOpen.Value)
                {
                    query = query.Where(c => c.Status == CourseStatuses.OpenForAssigning && c.RemainingSlotsCount > 0);
                }

                if (semester.HasValue)
                {
                    query = query.Where(c => c.Semester == semester.Value);
                }

                query = sort switch
                {
                    SortOption.CreatedAsc => query.OrderBy(c => c.StartYear),
                    SortOption.CreatedDesc => query.OrderByDescending(c => c.StartYear),
                    _ => query
                };


                var courses = query
                    .AsEnumerable()
                    .Where(c => string.IsNullOrEmpty(search) ||
                        c.Id.ToString().Contains(search) ||
                        c.Name.ToString().Contains(search) ||
                        c.StartYear.ToString().Contains(search) ||
                        c.MaximumStudentCount.ToString().Contains(search) ||
                        c.RemainingSlotsCount.ToString().Contains(search) ||
                        c.Status.ToString().Contains(search) ||
                        c.Semester.ToString().Contains(search))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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

                return Ok(courses);
            }
            catch (Exception er)
            {
                return StatusCode(500, new Response("Ошибка 500", $"{er.Message}"));
            }
        }
    }
}
