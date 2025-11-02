using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Inventory.Controllers
{
    [RoutePrefix("api/notification")]
    public class NotificationController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpGet]
        [Route("complaint")]
        public async Task<IHttpActionResult> GetNotifications([FromUri] int propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("Invalid property id.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"
                    SELECT 
    n.TransactionId AS NotificationId,
    t.TicketId,
    t.TicketNumber,
    t.Title,
    t.Description,
    p.Priority,
    tt.Type AS TicketType,
    s.Name AS Status,
    t.Label AS Location,
    t.ExpectedCloseDate,
    t.ActualCloseDate,
    n.SupRemark,
    fm.Name AS SupName,
    t.ReportedBy,
    n.SUPdateTime AS CreatedOn,
    n.TaskDate,
    c.Comment AS LatestComment,
    a.AttachmentUrl,
    a.AttachmentName,
    l.log AS LatestLog,
    n.IsSeen
FROM TaskNotification n
INNER JOIN app.Ticket t ON n.TicketId = t.TicketId
LEFT JOIN app.TicketPriority p ON t.TicketPriorityId = p.Id
LEFT JOIN app.TicketType tt ON t.TicketTypeId = tt.TicketTypeId
LEFT JOIN app.StatusType s ON t.StatusTypeId = s.StatusTypeId
LEFT JOIN app.FacilityMember fm ON n.SupId = fm.FacilityMemberId
OUTER APPLY (
    SELECT TOP 1 Comment 
    FROM app.TicketComments 
    WHERE TicketId = t.TicketId 
    ORDER BY CreatedOn DESC
) c
OUTER APPLY (
    SELECT TOP 1 AttachmentUrl, AttachmentName 
    FROM app.TicketAttachment 
    WHERE TicketId = t.TicketId AND IsDeleted IS NULL
    ORDER BY CreatedOn DESC
) a
OUTER APPLY (
    SELECT TOP 1 Log 
    FROM app.TicketLog 
    WHERE TicketId = t.TicketId 
    ORDER BY LoggedOn DESC
) l
WHERE n.TypeId = 3 
  AND t.PropertyId = @PropertyId
  AND n.IsSeen = 0
ORDER BY n.TransactionId DESC;";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    var notifications = new List<NotificationDto>();

                    while (await reader.ReadAsync())
                    {
                        notifications.Add(new NotificationDto
                        {
                            NotificationId = reader.GetInt32(0),
                            TicketId = reader.GetInt32(1),
                            TicketNumber = reader.GetString(2),
                            Title = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Priority = reader.IsDBNull(5) ? null : reader.GetString(5),
                            TicketType = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Status = reader.IsDBNull(7) ? null : reader.GetString(7),
                            Location = reader.IsDBNull(8) ? null : reader.GetString(8),
                            ExpectedCloseDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                            ActualCloseDate = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                            SupRemark = reader.IsDBNull(11) ? null : reader.GetString(11),
                            SupName = reader.IsDBNull(12) ? null : reader.GetString(12),
                            ReportedBy = reader.GetInt32(13),
                            CreatedOn = reader.GetDateTime(14),
                            TaskDate= reader.GetDateTime(15),
                            LatestComment = reader.IsDBNull(16) ? null : reader.GetString(16),
                            AttachmentUrl = reader.IsDBNull(17) ? null :
            reader.GetString(17)
                  .Replace(@"C:\inetpub\wwwroot\urest-dashboard\", "https://admin.urest.in:9051/")
                  .Replace("\\", "/"),
                            AttachmentName = reader.IsDBNull(18) ? null : reader.GetString(18),
                            LatestLog = reader.IsDBNull(19) ? null : reader.GetString(19),
                            IsSeen = reader.GetBoolean(20),

                            // Helpers
                            TimeAgo = GetTimeAgo(reader.GetDateTime(14)),
                            ActionUrl = "/tickets/" + reader.GetInt32(1) + "/details"
                        });
                    }

                    if (notifications.Count == 0)
                        return NotFound();

                    return Ok(notifications);

                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching notifications: " + ex.Message));
            }
        }

        private string GetTimeAgo(DateTime createdOn)
        {
            var ts = DateTime.Now - createdOn;
            if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} min ago";
            if (ts.TotalHours < 24) return $"{(int)ts.TotalHours} hrs ago";
            return $"{(int)ts.TotalDays} days ago";
        }


        [HttpGet]
        [Route("task")]
        public async Task<IHttpActionResult> GetTaskNotifications([FromUri] int propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("Invalid property id.");

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    await con.OpenAsync();

                    SqlCommand command = new SqlCommand(@"
                SELECT tn.TaskId, tn.QuestionId, tn.TaskName, tn.SUPdateTime, tn.SupRemark, tn.TaskDate, 
                       fm.Name, fm.PropertyId
                FROM TaskNotification AS tn
                LEFT JOIN app.FacilityMember AS fm ON tn.SupId = fm.FacilityMemberId
                WHERE tn.CurrentStatus = 'Actionable' AND fm.PropertyId = @PropertyId
                ORDER BY tn.SUPdateTime DESC", con);

                    command.Parameters.AddWithValue("@PropertyId", propertyId);

                    var recentTasks = new List<TaskNotification>();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var task = new TaskNotification
                            {
                                TaskId = reader.GetInt32(0),
                                QuestionId = reader.GetInt32(1),
                                TaskName = reader.GetString(2),
                                SUPdateTime = reader.GetDateTime(3),
                                SupRemark = reader.IsDBNull(4) ? null : reader.GetString(4),
                                TaskDate = reader.GetDateTime(5),
                                SupName = reader.IsDBNull(6) ? null : reader.GetString(6),
                                PropertyId = reader.GetInt32(7)
                            };
                            recentTasks.Add(task);
                        }
                    }

                    return Ok(recentTasks);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving task notifications: " + ex.Message));
            }
        }
    }
}