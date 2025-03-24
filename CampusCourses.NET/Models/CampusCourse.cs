using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CampusCourses.NET.Models
{
    public class CampusCourse
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; } 
        public string Name { get; set; }
        public int StartYear { get; set; }
        public int MaximumStudentCount { get; set; }
        public Semesters Semester { get; set; } 
        public string Requirements { get; set; } 
        public string Annotations { get; set; } 
        public Guid MainTeacherId { get; set; } 
        public int RemainingSlotsCount { get; set; } 
        public CourseStatuses Status { get; set; } 
    }
}
