using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class ResetPasswordRequest
    {
        public string MobileNumber { get; set; }
    }
    public class User
    {
        public int UserIdId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string EmailAddress { get; set; }
        public string MobileNumber { get; set; }
        public string ProfileImageUrl { get; set; }
        public string UserRole { get; set; }

        //[JsonIgnore]
        //public string Password { get; set; }
    }
    public static class UserDetails
    {
        //public static User GetUserData(HttpContext contect)
        //{
        //    //var email = contect.User.Claims.First(x => x.Type == "EmailAddress").Value;
        //    //var fistname = contect.User.Claims.First(x => x.Type == "FirstName").Value;
        //    //var lastname = contect.User.Claims.First(x => x.Type == "LastName").Value;
        //    //var mobilenumber = contect.User.Claims.First(x => x.Type == "MobileNumber").Value;
        //    //var userid = Convert.ToInt32(contect.User.Claims.First(x => x.Type == "UserIdId").Value);
        //    User oUser = new User();
        //    oUser.EmailAddress = contect.User.Claims.First(x => x.Type == "EmailAddress").Value; ;
        //    //oUser.MobileNumber = "6260983195";
        //    oUser.MobileNumber = contect.User.Claims.First(x => x.Type == "MobileNumber").Value; ;
        //    oUser.UserIdId = Convert.ToInt32(contect.User.Claims.First(x => x.Type == "UserIdId").Value); ;
        //    oUser.FirstName = contect.User.Claims.First(x => x.Type == "FirstName").Value; ;
        //    oUser.LastName = contect.User.Claims.First(x => x.Type == "LastName").Value; ;
        //    oUser.UserRole = contect.User.Claims.First(x => x.Type == "UserRole").Value; ;
        //    oUser.ProfileImageUrl = contect.User.Claims.First(x => x.Type == "ProfileImageUrl").Value; ;
        //    return oUser;
        //}
    }

    public class EventTask
    {
        public int Id { get; set; }
        public int EventID { get; set; }
        public int TaskID { get; set; }
        public string EventName { get; set; }
        public string TaskName { get; set; }
        public string BookedByName { get; set; }
        public int CreateBy { get; set; }
        public DateTime CreateOn { get; set; }
        public int IsActive { get; set; }
        public int IsDeleted { get; set; }
        public int BookedBy { get; set; }
        public int IsApproved { get; set; }
    }

    public class FromDataFacilityMemberModel
    {
        public int FacilityMemberId { get; set; }
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public int FacilityMasterId { get; set; }
        public int PropertyTowerId { get; set; }
        public int PropertyFloorId { get; set; }
        public int PropertyFlatId { get; set; }
        public string PropertyDetailsIds { get; set; }
        public int ApprovedBy { get; set; }
        public string ApprovedOn { get; set; }
        public byte[] ImageFile { get; set; }
        public string ImageFileName { get; set; }
        public string ImageExt { get; set; }
        public string Document { get; set; }
        public IList<byte[]> Files { get; set; }
        public string SaveType { get; set; }
    }
    public class DocumentModelNew
    {
        public int facilityMemberDocumentId { get; set; }
        public string DocumentTypeName { get; set; }
        public string DocumentTypeId { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentName { get; set; }
        public byte[] DocumentURL { get; set; }
        public string DocumentFileName { get; set; }
        public string DocumentExt { get; set; }
    }

    public class AssetsMaster
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string QRCode { get; set; }
        public string AssetType { get; set; }
        public string Manufacturer { get; set; }
        public string AssetModel { get; set; }
        public int AssetValue { get; set; }
        public bool IsMoveable { get; set; }
        public string AssetImage { get; set; }
        public string AMCdoc { get; set; }

        public string LastServiceDate { get; set; }
        public string NextServiceDate { get; set; }
        public bool IsRentable { get; set; }
        public string Flag { get; set; } // For differentiating between insert, update, delete
        public string AssetStatus { get; set; } 
         public int PropertyId { get; set; }
        public bool Status { get; set; }
        public string Location { get; set; }
        public string Category { get; set; }
    }

    public class AssetTransaction
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime? ReturnDate { get; set; }
        public DateTime? RentedOutDate { get; set; }
        public string Purpose { get; set; }
        public DateTime? CheckOutDateTime { get; set; }
        public DateTime? TentativeReturnDate { get; set; }
        public string ApprovedBy { get; set; }
        public string ReturnedBy { get; set; }

    }

    public class CheckOutModel
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssigneeName { get; set; }
        public string Purpose { get; set; }
        public DateTime? CheckOutDateTime { get; set; }
        public string OutFrom { get; set; }
        public string SentTo { get; set; }
        public DateTime? TentativeReturnDate { get; set; }
        public string ImageOut { get; set; }
        public List<SpareFieldModel> SpareFields { get; set; }
        public string ApprovedBy { get; set; }
    }

    public class SpareFieldModel
    {
        public int Id { get; set; }
        public string SpareName { get; set; }
        public DateTime TentativeReturnDate { get; set; }

        public DateTime ? ReturnDateTime { get; set; }
    }

    public class CheckInModel
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string ReturnedBy { get; set; }
        public DateTime? ReturnDateTime { get; set; }
        public string ImageIn { get; set; }
        public List<SpareFieldModel> SpareFields { get; set; }
    }

    public class RentalAsset
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string AssigneeName { get; set; }
        public DateTime? RentOutDateTime { get; set; }
        public DateTime? TentativeReturnDate { get; set; }
        public string RentedTo { get; set; }
        public string OutFrom { get; set; }
        public int RentalCharges { get; set; }
        public string ImageOut { get; set; }
        public string ApprovedBy { get; set; }
    }

    public class ReturnRentalAssetModel
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public string ReturnedBy { get; set; }
        public DateTime? ReturnDateTime { get; set; }
        public string ReturnedFrom { get; set; }
        public string ImageIn { get; set; } // Base64 string representation of the image
    }

    public class Category
    {
        public int SubCategoryId { get; set; }

        public int CategoryId { get; set; }

        public string SubCategoryName { get; set; }


    }

    public class Employee
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string FatherName { get; set; }
        public string Designation { get; set; }
        public string MobileNo { get; set; }
        public int IsDeleted { get; set; }
        public int Approved { get; set; }
    }

    public class GuardMaster
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string FatherName { get; set; }
        public string Designation { get; set; }
        public string MobileNo { get; set; }
        public int IsDeleted { get; set; }
        public int Approved { get; set; }
    }

    public class AttendanceLogs
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string PunchTime { get; set; }
        public string PunchType { get; set; }
        public string GateNo { get; set; }
    }

    public class AmenitiesBookings
    {
        public int Id { get; set; }
        public int AmenitiesId { get; set; }
        public int PropertyId { get; set; }
        public string AmenitiesName { get; set; }
        public string TimeSlot { get; set; }
        public DateTime TimeSlotFr { get; set; }
        public DateTime TimeSlotTo { get; set; }
        public int NosOfPersons { get; set; }
        public int Approved { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string MobileNo { get; set; }
        public string ApproveStatus { get; set; }

    }

    public class KycDetails
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Gender { get; set; }
        public string JobProfile { set; get; }
        public string MobileNo { set; get; }
        public string IdDoc { get; set; }
        public string Image { get; set; }
        public string ImageExt { get; set; }
        public byte[] ImageData { get; set; }
        public string IdImage { get; set; }
        public string ApproveStatus { get; set; }
    }

    public class TaskWiseQuestionnaire
    {
        public int TransactionID { get; set; }
        public int CategoryID { get; set; }
        public int SubCategoryID { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int TaskID { get; set; }
        public string TaskName { get; set; }
        public string Occurance { get; set; }
        public int QuestID { get; set; }
        public string QuestionName { get; set; }
        public string Action { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
    }

    public class TaskWiseQuestions
    {
        public int TaskID { get; set; }
        public int QuestID { get; set; }
        public string QuestionName { get; set; }
    }

    public class AssignToList
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TaskTransactionModel
    {
        public TaskTransactionModel()
        {
            this.TaskTransactionModel1 = new HashSet<TaskTransactionModel>();
        }

        public virtual ICollection<TaskTransactionModel> TaskTransactionModel1 { get; set; }
        public virtual TaskTransactionModel TaskTransactionModel2 { get; set; }
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int TaskCategoryId { get; set; }
        public int TaskSubCategoryId { get; set; }
        public string TaskStatus { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? TimeFrom { get; set; }
        public DateTime? TimeTo { get; set; }
        public string Remarks { get; set; }
        public string Occurence { get; set; }
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int EntryType { get; set; }
        public string AssignedTo { get; set; }
        public int PropertyId { get; set; }
        public int AssignedToId { get; set; }
        public string QRCode { get; set; }
        public string QuestionName { get; set; }
        public string RemarksQuestion { get; set; }
        public string UpdatedOn { get; set; }
        public string AssetName { get; set; }
        public int AssetId { get; set; }
        public int Duration { get; set; }
        public string Location { get; set; }
        public int TaskPriorityId { get; set; }
        public string TaskPriority { get; set; }
    }

    public class TaskMaster
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? TimeFrom { get; set; }
        public DateTime? TimeTo { get; set; }
        public string Remarks { get; set; }
        public string Occurence { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int AssignTo { get; set; }
        public string RemindMe { get; set; }
        public string Location { get; set; }
        public int AssetsID { get; set; }
        public string QRCode { get; set; }

        public string Type { get; set; }
    }

    public class Product
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } // = product.ProductName,
        public decimal UnitPrice { get; set; } //  = product.UnitPrice,
        public int UnitsInStock { get; set; } //  = product.UnitsInStock,
        public int TotalSales { get; set; }
        public string QuantityPerUnit { get; set; } //  = product.QuantityPerUnit,
        public bool Discontinued { get; set; } //  = rand.Next(1, 3) % 2 == 0 ? true : false,
        public int? CategoryID { get; set; }
        public int? CountryID { get; set; }
        public int UnitsOnOrder { get; set; } //  = product.UnitsOnOrder,
        //public string Country { get; set; } //  = countries[rand.Next(0, 7)],
        public int CustomerRating { get; set; } //  = rand.Next(0, 6),
                                                   //public double TargetSales { get; set; }// = rand.Next(7, 101),
        private int targetSales;
        public int TargetSales
        {
            get
            {
                return targetSales;
            }
            set
            {
                targetSales = value;
                TotalSales = value * 100;
            }
        }
        public CategoryViewModel Category { get; set; }// = new CategoryViewModel()
                                                       //{
                                                       //    CategoryID = product.Category.CategoryID,
                                                       //    CategoryName = product.Category.CategoryName
                                                       //},
        public DateTime LastSupply { get; set; }

        public CountryViewModel Country { get; set; }
    }

    public class SpotVisitDetail
    {
        public int Id { get; set; }
        public string MobileNo { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime? VisitDate { get; set; }
    }

    public class CategoryViewModel
    {
        public string CategoryName { get; set; }
        public int CategoryID { get; set; }
    }

    public class CountryViewModel
    {
        public string CountryNameLong { get; set; }
        public string CountryNameShort { get; set; }
    }

    public class EmployeeDesignationCountModel
    {
        public string Designation { get; set; }
        public int Count { get; set; }
    }

    public class EmployeeAttendanceSummaryCountModel
    {
        public string Leave { get; set; }
        public int Count { get; set; }
    }

    public class EmployeeWiseTaskSummaryModel
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public decimal CompletionPercentage { get; set; }
        public string Attendance { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AssignTo { get; set; }
        public int ActionItem { get; set; }
    }

    public class EmployeeWiseTaskSummaryChartModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double CompletionPercentage { get; set; }
        public DateTime CreatedOn { get; set; }
        public int ActionItem { get; set; }
    }

    public class EmployeeAttendanceSummaryModel
    {
        public int Id { get; set; }
        public int FacilityMemberId { get; set; }
        public string FacilityMemberName { get; set; }
        public DateTime Date { get; set; }
        public string Leave { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class CategoryWiseTasksModel
    {
        public string Category { get; set; }
        public int Count { get; set; }
    }

    public class CategoryWiseTasksSummaryModel
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int ActionableTasks { get; set; }
    }

    public class TaskWiseSummaryModel
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string TaskName { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public decimal CompletionPercentage { get; set; }
        public int ActionItem { get; set; }
    }

    public class TaskWiseSummaryChartModel
    {
        public string CategoryName { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionPercentage { get; set; }
        public int ActionItem { get; set; }
    }

    public class TaskWiseFmStatusModel 
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int QuestId { get; set; }
        public DateTime Date { get; set; }
        public string Remarks { get; set; }
    }

    public class PropertyModel
    {
        public int PropertyId { get; set; }
        public int PropertyTypeId { get; set; }
        public string Name { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine12 { get; set; }
        public int CityId { get; set; }
        public string ContactNumber { get; set; }
        public int? LanguageId { get; set; }
        public decimal? ProjectArea { get; set; } = null;
        public int? TotalTowers { get; set; }
        public int? Totalunits { get; set; }
        public int? TotalCommercialUnits { get; set; }
        public string Landmark { get; set; }
        public string Pincode { get; set; }
        public bool IsActive { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdateOn { get; set; }
        public int? updatedby { get; set; }
        public bool? IsDeleted { get; set; }
    }

    public class FacilityMemberModel
    {
        public int FacilityMemberId { get; set; }
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public int FacilityMasterId { get; set; }
        public string AccessCode { get; set; }
    }

    public class TaskPriorityModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class AoaLoginResp
    {
        public string Name { get; set; }
        public string MobileNumber { get; set; }
        public int PropertyId { get; set; }
    }

    public class TaskWiseDailyStatusFinalDashModel
    {
        public int TaskID { get; set; }
        public string TaskName { get; set; }
        public int QuestID { get; set; }
        public string Remarks { get; set; }
        public DateTime Updatedon { get; set; }
        public string TaskStatus { get; set; }
        public string TaskPriority { get; set; }
        public int TaskPriorityId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int AssignTo { get; set; }
        public string Occurence { get; set; }
        public string AssignToName { get; set; }
    }

    public class TaskStatusSummaryDtoAllSubCat
    {
        public int PropertyId { get; set; }
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string TaskStatus { get; set; }
        public int Count { get; set; }
    }


    public class TaskWiseDailyStatusFinalCountDashModel
    {
        public string TaskStatus { get; set; }
        public int Count { get; set; }
    }

    public class PriorityCountDashModel
    {
        public string TaskPriority { get; set; }
        public int Count { get; set; }
    }

    public class TaskNotification
    {
        public int TaskId { get; set; }            // Assuming TaskId is an integer
        public int QuestionId { get; set; }        // Nullable, assuming QuestionId might be null
        public string TaskName { get; set; }        // TaskName is a string
        public DateTime SUPdateTime { get; set; }   // Assuming SUPdateTime is a DateTime field
        public string SupRemark { get; set; }       // SupRemark is a string
        public string SupName { get; set; }         // Name of the supervisor from FacilityMember
        public int PropertyId { get; set; }

        public int FmId { get; set; }
        public string FmRemark { get; set; }
        public DateTime FMdateTime { get; set; }

        public string CurrentStatus { get; set; }
        public int SupId { get; set; }

        public string Remark { get; set; }
        public DateTime DateTime { get; set; }

        public DateTime TaskDate { get; set; }

        public string Source {  get; set; }
  
    }
    public class ComplaintNotificationFM
    {
        public int TicketId { get; set; }
        public string LocationName { get; set; }
        public string CurrentStatus { get; set; }
        public DateTime FMdateTime { get; set; }
        public string FMRemark { get; set; }
        public DateTime TicketDate { get; set; }
        public DateTime TransactionDate { get; set; }
    }

    public class ComplaintChatNotification
    {
        public int TicketId { get; set; }
        public string CurrentStatus { get; set; }
        public string Remark { get; set; }
        public DateTime DateTime { get; set; }
        public string Source { get; set; }   // "FM" or "SUP"
    }

    public class TestInputData
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Remarks { get; set; }
        public string Occurence { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int AssignTo { get; set; }
        public string RemindMe { get; set; }
        public int AssetsID { get; set; }
        public string QRCode { get; set; }
        public string Description { get; set; }
        public List<QuestionModel> Questions { get; set; }
    }

    public class QuestionModel
    {
        public string QuestionName { get; set; }

        public int QuestionId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class FacilityMemberDto
    {
        public int FacilityMemberId { get; set; }
        public int? PropertyId { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }
        public int? FacilityMasterId { get; set; }
        public string ProfileImageUrl { get; set; }
        public bool? IsBlocked { get; set; }
        public string AccessCode { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? ApprovedBy { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int? OldID { get; set; }
        public string Password { get; set; }

        // Extra fields from JOINs
        public string PropertyName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

    }

    public class UfirmEmployeeLocation
    {
        public int FacilityMemberId { get; set; }
        public string MobileNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsActive { get; set; }
        public string LocationName { get; set; }

        public string Type { get; set; } 
    }

}