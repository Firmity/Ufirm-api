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
        public EmpProfile Profile { get; set; }
        public List<EmpWorkHistory> WorkHistories { get; set; } // plural
        public EmpWorkHistory WorkHistory { get; set; } // optional (for update)
        public EmpFinancialInfo FinancialInfo { get; set; }
        public FacilityMember FacilityMember { get; set; }
        public EmployeeList EmployeeList { get; set; }
    }

    // 1️⃣ Profile Model (App.Emp_Profile_Info)
    public class EmpProfile
    {
        public int? OfficeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmploymentType { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PanCard { get; set; }
        public string AadharCard { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
    }

    // 2️⃣ Work History Model (App.Emp_Work_History)
    public class EmpWorkHistory
    {
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool ThirdPartyVerification { get; set; }
        public string UploadResume { get; set; }  // Store relative path here
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }
    }

    // 3️⃣ Financial Info Model (App.Emp_Financial_Info)
    public class EmpFinancialInfo
    {
        public string BankAccountNumber { get; set; }
        public string BankIFSCCode { get; set; }
        public string BankName { get; set; }
        public string UANNumber { get; set; }
        public string PANNumber { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsActive { get; set; }
        public string PFNumber { get; set; }
        public string ESINumber { get; set; }
    }

    // 4️⃣ Facility Member Model (App.FacilityMember)
    public class FacilityMember
    {
        public int FacilityMemberId { get; set; }
        public int? PropertyId { get; set; }
        public string Address { get; set; }
        public int? FacilityMasterId { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool IsBlocked { get; set; }
        public string AccessCode { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int ApprovedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string oldID { get; set; }
        public string SG_Link_ID { get; set; }
        public decimal? tax_amount { get; set; }
    }

    // 5️⃣ Employee List Model (dbo.EmployeeList)
    public class EmployeeList
    {
        public string FatherName { get; set; }
        public bool IsDeleted { get; set; }
        public bool Approved { get; set; }

        public string Designation {  get; set; }
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

    public class OTHoursMaster
    {
        public int ID { get; set; }
        public int Property_id { get; set; }
        public string designation { get; set; }
        public decimal price { get; set; }
    }


}