using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/tasks")]
    public class TaskSummaryController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpGet]
        [Route("summary")]
        public async Task<IHttpActionResult> GetTaskDailySummary([FromUri] int propertyId)
        {
            var list = new List<TaskDailySummary>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"
            SELECT DISTINCT
                p.Name AS PropertyName,
                f.FacilityMemberId,
                f.Name AS FacilityMemberName,
                t.TaskID,
                CAST(t.updatedon AS date) AS TaskDate,
                COUNT(*) AS TaskCount
            FROM taskwisedailystatusfinaldashreco t
            INNER JOIN app.PropertyMaster p ON t.PropertyId = p.PropertyId
            INNER JOIN app.Ufirm_Employee_Locations l ON t.AssignTo = l.FacilityMemberId
            INNER JOIN app.FacilityMember f ON l.FacilityMemberId = f.FacilityMemberId
            WHERE l.type = 'S'
              AND t.PropertyId = @PropertyId
            GROUP BY p.Name, f.Name, CAST(t.updatedon AS date), t.TaskID, f.FacilityMemberId
            ORDER BY CAST(t.updatedon AS date);";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new TaskDailySummary
                            {
                                PropertyName = reader["PropertyName"].ToString(),
                                FacilityMemberId = Convert.ToInt32(reader["FacilityMemberId"]),
                                FacilityMemberName = reader["FacilityMemberName"].ToString(),
                                TaskId = Convert.ToInt32(reader["TaskID"]),
                                TaskDate = Convert.ToDateTime(reader["TaskDate"]),
                                TaskCount = Convert.ToInt32(reader["TaskCount"])
                            });
                        }
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet]
        [Route("details")]
        public async Task<IHttpActionResult> GetTaskQuestionnaireDetails(
            [FromUri] int taskId,
            [FromUri] DateTime taskDate,
            [FromUri] int assignTo)
        {
            var list = new List<TaskQuestionnaireDetail>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"
                    SELECT DISTINCT 
                        t.TaskName,
                        tq.QuestionName,
                        t.Remarks,
                        t.Action
                    FROM TaskWiseTransaction t
                    INNER JOIN dbo.TaskWiseQuestionnaire tq ON tq.QuestionID = t.QuestID
                    INNER JOIN dbo.taskwisedailystatusfinaldashreco tt ON tt.TaskID = @taskId
                    WHERE CAST(t.UpdatedOn AS date) = @taskDate
                      AND t.TaskId = @taskId
                      AND tt.AssignTo = @assignTo;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@taskId", taskId);
                    cmd.Parameters.AddWithValue("@taskDate", taskDate);
                    cmd.Parameters.AddWithValue("@assignTo", assignTo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var actionValue = reader["Action"]?.ToString();

                            // Convert values as per requirement
                            string finalAction = string.Empty;
                            if (!string.IsNullOrEmpty(actionValue))
                            {
                                if (actionValue.Equals("No", StringComparison.OrdinalIgnoreCase))
                                    finalAction = "Actionable";
                                else if (actionValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                                    finalAction = ""; // blank
                                else
                                    finalAction = actionValue; // keep original if not Yes/No
                            }

                            list.Add(new TaskQuestionnaireDetail
                            {
                                TaskName = reader["TaskName"].ToString(),
                                QuestionName = reader["QuestionName"].ToString(),
                                Remarks = reader["Remarks"]?.ToString(),
                                Action = finalAction
                            });
                        }
                    }
                }
            }
            return Ok(list);
        }
    }
}
