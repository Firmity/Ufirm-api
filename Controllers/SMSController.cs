using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml.Linq;
using UrestComplaintWebApi.Models;
namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/sms")]
    public class SMSController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;
        Integrations integrations = new Integrations();

        [HttpPost]
        [Route("TicketIntimation")]

        public async Task<IHttpActionResult> TicketIntimation([FromBody] TicketIntimationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TicketId))
            {
                return BadRequest("TicketId, PropertyId are required.");
            }
            try
            {
                string senderName = "URSTCP";
                string smsMessage = $"A ticket {request.TicketId} has been raised and needs to be attended shortly.\n-Team Urest\nUFIRM TECHNOLOGIES PVT LTD";
                List<string> mobileNumbers = await GetMobileNumbersFromDatabase(request.PropertyId, request.SupervisorId);

                if (mobileNumbers == null || mobileNumbers.Count == 0)
                {
                    return BadRequest("Mobile numbers not found for the given PropertyId or Supervisor Id.");
                }

                foreach (string mobileNumber in mobileNumbers)
                {
                    bool smsSent = await integrations.SendSMSAsync(mobileNumber, smsMessage, senderName);

                    if (!smsSent)
                    {
                        return InternalServerError(new Exception($"Failed to send SMS to {mobileNumber}."));
                    }
                }
                return Ok("SMS messages sent successfully to all numbers: " + string.Join(", ", mobileNumbers));
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Error sending SMS: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        private async Task<List<string>> GetMobileNumbersFromDatabase(string propertyId, string supId)
        {
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

        [HttpPost]
        [Route("VisitorNotification")]
        public async Task<IHttpActionResult> VisitorNotification([FromBody] VisitorNotificationRequest request)
        { // ✅ Step 1: Validate Input
            if (request == null || string.IsNullOrEmpty(request.MobileNo) || string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.VisitorName) || string.IsNullOrEmpty(request.Details))
            {
                return BadRequest("MobileNo, Name, VisitorName, and Details are required.");
            }
            try
            { // ✅ Step 2: Define DLT Sender ID
                string senderName = "VISMGT"; // Must match your DLT-approved Sender ID // ✅ Step 3: Prepare Link
                string baseUrl = "https://rebrand.ly/fu";
                string detailsLink = request.Details; // If user passed only numeric ID, append the base URL
                if (!request.Details.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    detailsLink = $"{baseUrl}?I={request.Details}";
                } // ✅ Step 4: Construct Message // DLT Template: "Hello {#var#}, visitor {#var#} has come. Approve visitor entry link {#var#}. - Urest UFIRM TECH"
                string smsMessage = $"Hello {request.Name}, visitor {request.VisitorName} has come. Approve visitor entry link {detailsLink}. - Urest UFIRM TECH"; // ✅ Step 5: Send SMS
                bool smsSent = await integrations.SendSMSAsync(request.MobileNo, smsMessage, senderName);
                if (!smsSent)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to send SMS to {request.MobileNo}");
                    return Content(HttpStatusCode.InternalServerError, new { Message = $"Failed to send SMS to {request.MobileNo}.", SMSContent = smsMessage });
                } // ✅ Step 6: Log Success
                System.Diagnostics.Debug.WriteLine($"✅ SMS sent successfully to {request.MobileNo}: {smsMessage}"); // ✅ Step 7: Return Response
                return Ok(new { Message = "Visitor notification sent successfully.", MobileNo = request.MobileNo, VisitorName = request.VisitorName, LinkUsed = detailsLink, SMSContent = smsMessage });
            }
            catch (Exception ex)
            { // ✅ Step 8: Exception Handling
                System.Diagnostics.Debug.WriteLine($"🔥 Error in VisitorNotification: {ex.Message}");
                return Content(HttpStatusCode.InternalServerError, new { Message = "An unexpected error occurred while sending the visitor notification.", Error = ex.Message });
            }
        }


        [HttpPost]
        [Route("VisitorApprovalNotification")]
        public async Task<IHttpActionResult> VisitorApprovalNotification([FromBody] VisitorApprovalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNo)
                || string.IsNullOrEmpty(request.VisitorName) || string.IsNullOrEmpty(request.ApprovedBy))
            {
                return BadRequest("MobileNo, VisitorName, and ApprovedBy are required.");
            }

            try
            {
                string senderName = "VISMGT"; // DLT-approved sender ID

                // ✅ DLT Template Format
                string smsMessage = $"Visitor {request.VisitorName} approved by {request.ApprovedBy}. Allow entry. - Urest UFIRM Tech";

                // Replace template variables with actual values


                // Send SMS
                bool smsSent = await integrations.SendSMSAsync(request.MobileNo, smsMessage, senderName);

                if (!smsSent)
                    return InternalServerError(new Exception($"Failed to send SMS to {request.MobileNo}."));

                return Ok(new
                {
                    Message = "Visitor approval notification sent successfully.",
                    MobileNo = request.MobileNo,
                    SMSContent = smsMessage
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in VisitorApprovalNotification: {ex.Message}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    Message = "An unexpected error occurred while sending the visitor approval notification.",
                    Error = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("VisitorRejectionsNotification")]
        public async Task<IHttpActionResult> VisitorRejectionNotification([FromBody] VisitorApprovalRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.MobileNo)
                || string.IsNullOrEmpty(request.VisitorName) || string.IsNullOrEmpty(request.ApprovedBy))
            {
                return BadRequest("MobileNo, VisitorName, and ApprovedBy are required.");
            }

            try
            {
                string senderName = "VISMGT"; // DLT-approved sender ID

                // ✅ DLT Template Format
                string smsMessage = $"Visitor {request.VisitorName} rejected by {request.ApprovedBy}. Deny entry. - Urest UFIRM Tech";

                // Replace template variables with actual values


                // Send SMS
                bool smsSent = await integrations.SendSMSAsync(request.MobileNo, smsMessage, senderName);

                if (!smsSent)
                    return InternalServerError(new Exception($"Failed to send SMS to {request.MobileNo}."));

                return Ok(new
                {
                    Message = "Visitor approval notification sent successfully.",
                    MobileNo = request.MobileNo,
                    SMSContent = smsMessage
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in VisitorApprovalNotification: {ex.Message}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    Message = "An unexpected error occurred while sending the visitor approval notification.",
                    Error = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("AssetServiceNotification")]
        public async Task<IHttpActionResult> AssetServiceNotification([FromBody] AssetServiceRequest request)
        {

                string senderName = "SRVNOT"; // DLT-approved sender ID

                // ✅ DLT Template Format
                var userContacts = await GetMobileNumbersFromDatabases(request.propertyId);

                if (userContacts == null || userContacts.Count == 0)
                {
                    return BadRequest("No valid users found for the given PropertyId.");
                }

                foreach (var user in userContacts)
                {
                    string personalizedMessage = $"Hello {user.Name}, your {request.ItemDetails} requires servicing. Team Urest UFIRM TECHNOLOGIES PVT LTD";
                    bool smsSent = await integrations.SendSMSAsync(user.MobileNumber, personalizedMessage, senderName);

                    if (!smsSent)
                    {
                        return InternalServerError(new Exception($"Failed to send SMS to {user.MobileNumber}."));
                    }
                }

                return Ok("SMS messages sent successfully to: " +
                          string.Join(", ", userContacts.Select(u => $"{u.Name} ({u.MobileNumber})")));
            }


        [HttpPost]
        [Route("FMComplaintNotification")]
        public async Task<IHttpActionResult> FMComplaintNotification([FromBody] TicketIntimationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.TicketId) || string.IsNullOrEmpty(request.PropertyId))
            {
                return BadRequest("TicketId and PropertyId are required.");
            }

            try
            {
                string senderName = "URSTCP"; // DLT-approved sender ID

                // ✅ Fetch all user contacts (Name + MobileNumber)
                List<UserContact> contacts = await GetMobileNumbersFromDatabases(request.PropertyId);

                if (contacts == null || contacts.Count == 0)
                {
                    return BadRequest("No valid contacts found for the given PropertyId.");
                }

                // ✅ Create all SMS send tasks concurrently
                var smsTasks = contacts.Select(async contact =>
                {
                    string personalizedMessage =
                        $"A Ticket {request.TicketId} has been resolved. Please close the ticket. -Team Urest UFIRM TECH";

                    bool sent = await integrations.SendSMSAsync(contact.MobileNumber, personalizedMessage, senderName);
                    return new { contact.Name, contact.MobileNumber, Sent = sent };
                }).ToList();

                // Wait for all to complete
                var results = await Task.WhenAll(smsTasks);

                // Check if any failed
                var failed = results.Where(r => !r.Sent).ToList();

                if (failed.Any())
                {
                    return Content(HttpStatusCode.PartialContent, new
                    {
                        Message = "Some SMS messages failed to send.",
                        Failed = failed.Select(f => $"{f.Name} ({f.MobileNumber})")
                    });
                }

                // ✅ All successful
                return Ok(new
                {
                    Message = "SMS messages sent successfully to all recipients.",
                    Recipients = results.Select(r => $"{r.Name} ({r.MobileNumber})")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error sending FM Complaint SMS: {ex.Message}");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    Message = "An error occurred while sending FM Complaint notifications.",
                    Error = ex.Message
                });
            }
        }


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