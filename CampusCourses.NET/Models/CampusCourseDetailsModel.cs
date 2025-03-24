namespace CampusCourses.NET.Models
{
    public class CampusCourseDetailsModel
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public int startYear { get; set; }
        public int maximumStudentCount { get; set; }
        public int studentsEnrolledCount { get; set; }
        public int studentsInQueueCount { get; set; }
        public string requirements { get; set; }
        public string annotations { get; set; }
        public CourseStatuses status { get; set; }
        public Semesters semester { get; set; }
        public List<CampusCourseStudentModel> students { get; set; }
        public List<CampusCourseTeacherModel> teachers { get; set; }
        public List<CampusCourseNotificationModel> notifications { get; set; }
    }
}
