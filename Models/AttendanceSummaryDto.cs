using System;

namespace UrestComplaintWebApi.Models
{
    public class AttendanceSummaryDto
    {
        public int EmpID { get; set; }
        public string EmployeeName { get; set; }
        public int? WorkingDays { get; set; }
        public int? LeaveDays { get; set; }
        public int? WeekDaysOff { get; set; }
        public int? PropertyID { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string monthyear { get; set; }
        public int? OtDays { get; set; }
        public decimal? OtHours { get; set; }
    }
}
