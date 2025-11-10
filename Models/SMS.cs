using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UrestComplaintWebApi.Models
{
    public class TicketIntimationRequest
    {

        public string PropertyId { get; set; }
        public string SupervisorId { get; set; }
        public string TicketId { get; set; }
    }

    public class VisitorNotificationRequest
    {
        /// <summary>
        /// Recipient’s mobile number (must be valid 10-digit number).
        /// </summary>
        public string MobileNo { get; set; }

        /// <summary>
        /// Visitor's name (used in the greeting).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Either a visitor ID (like "44322") or a full URL (like "https://rebrand.ly/fu?I=44322").
        /// The system will detect and generate the correct full link automatically.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Date and time string in "yyyy-MM-dd HH:mm" format (optional).
        /// If not provided or invalid, current time will be used.
        /// </summary>
        public string VisitorName { get; set; }

        /// <summary>
        /// Optional — override base link if you want to change from default (e.g. rebrand.ly/fu)
        /// </summary>
    }

    public class VisitorApprovalRequest
    {
        public string MobileNo { get; set; }
        public string VisitorName { get; set; }
        public string ApprovedBy { get; set; }
    }
    public class AssetServiceRequest
    {
        public string propertyId { get; set; }
        public string ItemDetails { get; set; }
    }

    public class UserContact
    {
        public string Name { get; set; }
        public string MobileNumber { get; set; }
    }

    public class UserComplaint
    {
        public string MobileNumber { get; set; }
        public string ComplaintId { get; set; }
    }
}