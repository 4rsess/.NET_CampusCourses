namespace CampusCourses.NET.Models
{
    public class CampusCoursePreviewModel
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public int startYear { get; set; }
        public int maximumStudentCount { get; set; }
        public int remainingSlotsCount { get; set; }
        public CourseStatuses status { get; set; }
        public Semesters semester { get; set; }
    }
}
