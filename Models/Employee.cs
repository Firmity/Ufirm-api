using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{

    public class EmployeeProfile
    {
        public int EmployeeId { get; set; }
        public int OfficeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmploymentType { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool IsActive { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PanCard { get; set; }
        public string AadharCard { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    public class EmployeeWorkHistory
    {
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime DateOfJoining { get; set; }
        public DateTime RelievingDate { get; set; }
        public bool ThirdPartyVerification { get; set; }
        public string UploadResume { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool IsActive { get; set; }
    }

    public class EmployeeFinancialInfo
    {
        public string BankAccountNumber { get; set; }
        public string BankIFSCCode { get; set; }
        public string BankName { get; set; }
        public string UANNumber { get; set; }
        public string PANNumber { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool IsActive { get; set; }
        public string PFNumber { get; set; }
        public string ESINumber { get; set; }
    }

    public class EmployeeRequest
    {
        public EmployeeProfile Profile { get; set; }
        public EmployeeWorkHistory WorkHistory { get; set; }
        public EmployeeFinancialInfo FinancialInfo { get; set; }
        public FacilityMember FacilityMember { get; set; }
        public EmployeeList EmployeeList { get; set; }
    }

    public class FacilityMember
    {
        public int FacilityMemberId { get; set; }   // missing
        public int PropertyId { get; set; }
        public string Name { get; set; }            // missing
        public string Gender { get; set; }          // missing
        public string MobileNumber { get; set; }    // missing
        public string Address { get; set; }
        public int FacilityMasterId { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsBlocked { get; set; }
        public string AccessCode { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? ApprovedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public int? oldID { get; set; }
        public string Password { get; set; }
        public int? SG_Link_ID { get; set; }
        public int? tax_amount { get; set; }
    }

    public class EmployeeList
    {
        public int Id { get; set; }                 // missing
        public string EmployeeName { get; set; }    // missing
        public string FatherName { get; set; }
        public string Designation { get; set; }     // missing
        public string MobileNo { get; set; }        // missing
        public int IsDeleted { get; set; }
        public int Approved { get; set; }
    }

    public class  EmployeeGeneratedSalary 
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public int OfficeId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Month { get; set; }
        public int Year { get; set; }
        public bool is_active { get; set; }
    }

    public class EmployeeUngeneratedSalary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public string Designation { get; set; }
    }

}