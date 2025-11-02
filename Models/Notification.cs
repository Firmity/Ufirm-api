using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int TicketId { get; set; }
        public string TicketNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string TicketType { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public DateTime? ActualCloseDate { get; set; }
        public string SupRemark { get; set; }
        public string SupName { get; set; }
        public int ReportedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string LatestComment { get; set; }
        public string AttachmentUrl { get; set; }
        public string AttachmentName { get; set; }
        public string LatestLog { get; set; }
        public bool IsSeen { get; set; }

        public DateTime TaskDate { get; set; }

        // Dashboard helpers
        public string TimeAgo { get; set; }
        public string ActionUrl { get; set; }
    }


    public class AssetDto
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; }
        public DateTime SupDateTime { get; set; }  // Same as SupDateTime if not separate
        public string Location { get; set; }     // TaskName mapped as Location
        public string SupRemark { get; set; }
        public int SupId { get; set; }
        public string CurrentStatus { get; set; }
        public string SupName { get; set; }

    }


}