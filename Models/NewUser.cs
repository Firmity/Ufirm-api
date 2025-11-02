using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class UserType
    {
        public int UserTypeId { get; set; }
        public string UserTypeName { get; set; }
        public string Description { get; set; }
        public DateTime UpdateOn { get; set; }
        public DateTime CreateOn { get; set; }
        public int UpdatedBy { get; set; }
        public int CreateBy { get; set; }
    }

    public class City
    {
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int StateId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class UserRole
    {
        public int UserRoleId { get; set; }
        public string UserRoleName { get; set; }
    }

    public class Branch
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
    }

    public class UserData
    {
        public string EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int CityId { get; set; }
        public int PropertyId { get; set; }
        public int RoleId { get; set; }
        public int BranchId { get; set; }
        public int UserTypeId { get; set; }
        public int FacilityMemberId { get; set; }
        public object ProfileImage { get; set; }
    }

}