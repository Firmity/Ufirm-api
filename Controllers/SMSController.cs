using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/sms")]
    public class SMSController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;
        private readonly Integrations integrations = new Integrations();

        // ✅ Centralized DLT Templates Configuration
        private readonly Dictionary<string, (string senderId, string templateId)> SmsTemplates =
            new Dictionary<string, (string, string)>()
            {
                { "URESTOTP", ("URSTOP", "201973") },
                { "COMPLAINT_NOTIFICATION", ("URSTCP", "201980") },
                { "ASSET_SERVICE_ALERT", ("SRVNOT", "201982") },
                { "VISITOR_NOTIFICATION_SMS_LINK", ("VISMGT", "201983") },
                { "GAURD_VISITOR_REJECTION", ("VISMGT", "201984") },
                { "GAURD_VISITOR_APPROVAL", ("VISMGT", "201985") },
                { "COMPLAINT_NOTIFICATION_FM", ("URSTCP", "201986") }
            };

        // ✅ Ticket Intimation Notification
        [HttpPost]
        [Route("TicketIntimation")]
        public async Task<IHttpActionResult> TicketIntimation([FromBody] TicketIntimationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TicketId))
                return BadRequest("TicketId and PropertyId are required.");

            try
            {
                var template = SmsTemplates["COMPLAINT_NOTIFICATION"];
                string smsVariables = request.TicketId;
                var mobileNumbers = await GetMobileNumbersFromDatabase(request.PropertyId, request.SupervisorId);

                if (mobileNumbers == null || mobileNumbers.Count == 0)
                    return BadRequest("No mobile numbers found for the given PropertyId or Supervisor Id.");

                foreach (var mobile in mobileNumbers)
                {
                    bool sent = await integrations.SendDLTSMSAsync(mobile, template.templateId, template.senderId, smsVariables);
                    if (!sent)
                        return InternalServerError(new Exception($"Failed to send SMS to {mobile}."));
                }

                return Ok("✅ Ticket intimation sent successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        // ✅ Visitor Notification
        [HttpPost]
        [Route("VisitorNotification")]
        public async Task<IHttpActionResult> VisitorNotification([FromBody] VisitorNotificationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNo)
                || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.VisitorName))
                return BadRequest("MobileNo, Name, and VisitorName are required.");

            try
            {
                var template = SmsTemplates["VISITOR_NOTIFICATION_SMS_LINK"];
                string detailsLink = request.Details.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                    ? request.Details
                    : $"https://rebrand.ly/fu?I={request.Details}";

                string variables = $"{request.Name}|{request.VisitorName}|{detailsLink}";
                bool sent = await integrations.SendDLTSMSAsync(request.MobileNo, template.templateId, template.senderId, variables);

                if (!sent)
                    return InternalServerError(new Exception($"Failed to send SMS to {request.MobileNo}."));

                return Ok("✅ Visitor notification sent successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ Visitor Approval
        [HttpPost]
        [Route("VisitorApprovalNotification")]
        public async Task<IHttpActionResult> VisitorApprovalNotification([FromBody] VisitorApprovalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNo)
                || string.IsNullOrEmpty(request.VisitorName) || string.IsNullOrEmpty(request.ApprovedBy))
                return BadRequest("MobileNo, VisitorName, and ApprovedBy are required.");

            try
            {
                var template = SmsTemplates["GAURD_VISITOR_APPROVAL"];
                string variables = $"{request.VisitorName}|{request.ApprovedBy}";
                bool sent = await integrations.SendDLTSMSAsync(request.MobileNo, template.templateId, template.senderId, variables);

                if (!sent)
                {
                    return Content(HttpStatusCode.InternalServerError, new
                    {
                        Message = "SMS send failed.",
                        Mobile = request.MobileNo,
                        Variables = variables,
                        Template = template.templateId,
                        Sender = template.senderId
                    });
                }

                return Ok("✅ Visitor approval notification sent successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ Visitor Rejection
        [HttpPost]
        [Route("VisitorRejectionNotification")]
        public async Task<IHttpActionResult> VisitorRejectionNotification([FromBody] VisitorApprovalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNo)
                || string.IsNullOrEmpty(request.VisitorName) || string.IsNullOrEmpty(request.ApprovedBy))
                return BadRequest("MobileNo, VisitorName, and ApprovedBy are required.");

            try
            {
                var template = SmsTemplates["GAURD_VISITOR_REJECTION"];
                string variables = $"{request.VisitorName}|{request.ApprovedBy}";
                bool sent = await integrations.SendDLTSMSAsync(request.MobileNo, template.templateId, template.senderId, variables);

                if (!sent)
                    return InternalServerError(new Exception($"Failed to send SMS to {request.MobileNo}."));

                return Ok("✅ Visitor rejection notification sent successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ Asset Service Notification
        [HttpPost]
        [Route("AssetServiceNotification")]
        public async Task<IHttpActionResult> AssetServiceNotification([FromBody] AssetServiceRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.propertyId))
                return BadRequest("PropertyId is required.");

            var template = SmsTemplates["ASSET_SERVICE_ALERT"];
            var contacts = await GetMobileNumbersFromDatabases(request.propertyId);

            if (contacts == null || contacts.Count == 0)
                return BadRequest("No valid users found for the given PropertyId.");

            foreach (var user in contacts)
            {
                string variables = $"{user.Name}|{request.ItemDetails}";
                bool sent = await integrations.SendDLTSMSAsync(user.MobileNumber, template.templateId, template.senderId, variables);
                if (!sent)
                    return InternalServerError(new Exception($"Failed to send SMS to {user.MobileNumber}."));
            }

            return Ok("✅ Asset service notifications sent successfully.");
        }
        private async Task<List<string>> GetMobileNumbersFromDatabase(string propertyId, string supId) { 
            List<string> mobileNumbers = new List<string>();

            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    // If propertyId is not provided or is "0", run only the second query
                    if (string.IsNullOrEmpty(propertyId) || propertyId == "0")
                    {
                        if (!string.IsNullOrEmpty(supId))
                        {
                            string query2 = @"
                        SELECT MobileNumber
                        FROM app.FacilityMember
                        WHERE FacilityMemberId = @SupId;";

                            using (SqlCommand command2 = new SqlCommand(query2, connection))
                            {
                                command2.Parameters.AddWithValue("@SupId", supId);

                                using (SqlDataReader reader2 = await command2.ExecuteReaderAsync())
                                {
                                    if (await reader2.ReadAsync())
                                    {
                                        mobileNumbers.Add(reader2["MobileNumber"].ToString());
                                    }
                                }
                            }
                        }
                    }
                    else
{
    // First Query (based on propertyId)
    string query1 = @"
                    SELECT ContactNumber
                    FROM [Identity].Users u
                    LEFT JOIN [Identity].UserRoleMapping ur ON u.UserId = ur.UserId
                    LEFT JOIN app.UserPropertyAssignment upa ON u.UserId = upa.UserId
                    WHERE u.IsDeleted = 0
                      AND ur.RoleId = 5
                      AND upa.PropertyId = @PropertyId;";

    using (SqlCommand command1 = new SqlCommand(query1, connection))
    {
        command1.Parameters.AddWithValue("@PropertyId", propertyId);

        using (SqlDataReader reader1 = await command1.ExecuteReaderAsync())
        {
            while (await reader1.ReadAsync())
            {
                mobileNumbers.Add(reader1["ContactNumber"].ToString());
            }
        }
    }

    // Optionally, run second query if supId is provided
    if (!string.IsNullOrEmpty(supId))
    {
        string query2 = @"
                        SELECT MobileNumber
                        FROM app.FacilityMember
                        WHERE FacilityMemberId = @SupId;";

        using (SqlCommand command2 = new SqlCommand(query2, connection))
        {
            command2.Parameters.AddWithValue("@SupId", supId);

            using (SqlDataReader reader2 = await command2.ExecuteReaderAsync())
            {
                if (await reader2.ReadAsync())
                {
                    mobileNumbers.Add(reader2["MobileNumber"].ToString());
                }
            }
        }
    }
}
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving mobile numbers: {ex.Message}");
return null;
            }

            return mobileNumbers;
        }

        // ✅ FM Complaint Notification
        [HttpPost]
        [Route("FMComplaintNotification")]
        public async Task<IHttpActionResult> FMComplaintNotification([FromBody] TicketIntimationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TicketId) || string.IsNullOrEmpty(request.PropertyId))
                return BadRequest("TicketId and PropertyId are required.");

            try
            {
                var template = SmsTemplates["COMPLAINT_NOTIFICATION_FM"];
                var contacts = await GetMobileNumbersFromDatabases(request.PropertyId);

                if (contacts == null || contacts.Count == 0)
                    return BadRequest("No valid contacts found.");

                var smsTasks = contacts.Select(async contact =>
                {
                    string variables = $"{contact.Name}|{request.TicketId}";
                    bool sent = await integrations.SendDLTSMSAsync(contact.MobileNumber, template.templateId, template.senderId, variables);
                    return new { contact.Name, contact.MobileNumber, Sent = sent };
                }).ToList();

                var results = await Task.WhenAll(smsTasks);
                var failed = results.Where(r => !r.Sent).ToList();

                if (failed.Any())
                {
                    return Content(HttpStatusCode.PartialContent, new
                    {
                        Message = "Some SMS messages failed to send.",
                        Failed = failed.Select(f => $"{f.Name} ({f.MobileNumber})")
                    });
                }

                return Ok("✅ FM Complaint notifications sent successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ------------------------- Helper Methods -------------------------

        private async Task<List<UserContact>> GetMobileNumbersFromDatabases(string propertyId)
        {
            var userContacts = new List<UserContact>();

            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    // Query to fetch ContactNumber (2nd row if both sources contain same PropertyId)
                    string query = @"
                SELECT ContactNumber, FirstName
                FROM (
                    SELECT 
                        u.ContactNumber,
u.FirstName,
                        ROW_NUMBER() OVER (ORDER BY u.ContactNumber) AS RowNum
                    FROM app.UserPropertyAssignment AS upa
                    FULL OUTER JOIN [Identity].[Users] AS u
                        ON u.UserId = upa.UserId
                    WHERE 
                        (
                            upa.PropertyId = @PropertyId
                            OR u.PropertyId = @PropertyId
                        )
                        AND ISNULL(upa.IsDeleted, 0) = 0
                        AND ISNULL(upa.Status, 1) = 1
                        AND ISNULL(u.IsDeleted, 0) = 0
                        AND ISNULL(u.IsActive, 1) = 1
                ) AS X
                WHERE X.RowNum = 2;
            ";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PropertyId", propertyId);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var number = reader["ContactNumber"]?.ToString();
                                var name = reader["FirstName"]?.ToString();

                                if (!string.IsNullOrWhiteSpace(number))
                                {
                                    userContacts.Add(new UserContact
                                    {
                                        Name = name ?? "User",
                                        MobileNumber = number
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error retrieving mobile numbers: {ex.Message}");
            }

            return userContacts;
        }

    }

}