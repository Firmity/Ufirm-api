using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class LeaveRequestModel
    {
        public int LeaveId { get; set; }

        public string EmployeeName { get; set; }
        public string MobileNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; } = "Pending"; // Default value
        public DateTime? AppliedOn { get; set; } = DateTime.Now;
        public string LeaveType { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;
        public int? ActionBy { get; set; }
        public DateTime? ActionOn { get; set; }
        public string ActionRemarks { get; set; }
        public int? LeaveTypeId { get; set; }
        public int? LeaveCount { get; set; }
    }

    public class LeaveSummaryDto
    {
        public int LeaveTypeId { get; set; }
        public string LeaveType { get; set; }
        public int TakenLeaves { get; set; }
        public int RemainingLeaves { get; set; }
    }

    public class MonthlyAttendanceSummaryFilter
    {
        public int PropertyId { get; set; }                // Mandatory
        public DateTime? FromDate { get; set; }            // Optional
        public DateTime? ToDate { get; set; }              // Optional
        public string MobileNo { get; set; }               // Optional
        public string EmployeeName { get; set; }           // Optional
    }

    public class LeaveMasterDto
    {
        public int Id { get; set; }
        public int? PropertyId { get; set; }
        public string LeaveType { get; set; }
        public string LeaveDescription { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeLeave
    {
        public long Id { get; set; }
        public int PropertyId { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        public decimal Balance { get; set; }
        public int FinancialYear { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
    }

    public class ManualAttendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        // Nullable DateTime (because DB allows NULL)
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public DateTime? CreatedOn { get; set; }

        public int? GateNo { get; set; }
        public int? CreatedBy { get; set; }
        public string MobileNo { get; set; }
        public int? EmpId { get; set; }
        public string Status { get; set; }
        public string ImageFileName { get; set; }

        // Manual approval process
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string RejectionRemark { get; set; }

        // Newly added fields
        public int? PropertyId { get; set; }
        public string LocationName { get; set; }
    }

        public class EmployeeDetailsDto
        {
            public string EmployeeCode { get; set; }
            public string EmployeeName { get; set; }
            public string CardNumber { get; set; }
            public string LocationCode { get; set; }
            public string EmployeeRole { get; set; }
            public string VerificationType { get; set; }
        }

        public class PunchLogDto
        {
            public string EmployeeCode { get; set; }
            public DateTime PunchTime { get; set; }
            public string Direction { get; set; } // IN / OUT
            public string DeviceName { get; set; }
            public string DeviceLocation { get; set; }
        }

        public class AttendanceDto
        {
            public EmployeeDetailsDto Employee { get; set; }
            public string AttendanceDate { get; set; }
            public PunchLogDto FirstIn { get; set; }
            public PunchLogDto LastOut { get; set; }
            public PunchLogDto[] AllPunches { get; set; }
        }
    }

