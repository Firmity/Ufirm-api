using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class VisitorResponseModel 
    {
        public int ID { get; set; }
        public string Fname { get; set; }
        public string Lname { get; set; }
        public string MobileNo { get; set; }
        public DateTime? MeetingStartTime { get; set; }
        public DateTime? MeetingEndTime { get; set; }
        public string Address { get; set; }
        public int? ContactPersonFacilityMemberId { get; set; }
        public string ContactPersonName { get; set; }
        public int? ContactPersonEmployeeId { get; set; }
        public string Status { get; set; }
        public string MPurpose { get; set; }
        public string ACarrying { get; set; }
        public bool IsMeetingOver { get; set; }
    }
}