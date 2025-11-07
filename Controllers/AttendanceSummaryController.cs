using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/attendance-summary")]
    public class AttendanceSummaryController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ---------------------------
        // GET ATTENDANCE BY EmpID
        // ---------------------------
        [HttpGet]
        [Route("{empId:int}")]
        public async Task<IHttpActionResult> GetAttendanceByEmpId(int empId)
        {
            AttendanceSummaryDto record = null;

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM App.AttendanceSummary WHERE EmpID=@EmpID AND is_active=1", conn))
                {
                    cmd.Parameters.AddWithValue("@EmpID", empId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            record = MapReaderToAttendance(reader);
                    }
                }
            }

            if (record == null) return NotFound();
            return Ok(record);
        }

        // ---------------------------
        // GET ATTENDANCE BY PropertyID
        // ---------------------------
        [HttpGet]
        [Route("by-property/{propertyId:int}")]
        public async Task<IHttpActionResult> GetAttendanceByPropertyId(int propertyId)
        {
            var list = new List<AttendanceSummaryDto>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM App.AttendanceSummary WHERE PropertyID=@PropertyID AND is_active=1", conn))
                {
                    cmd.Parameters.AddWithValue("@PropertyID", propertyId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                            list.Add(MapReaderToAttendance(reader));
                    }
                }
            }

            if (list.Count == 0) return NotFound();
            return Ok(list);
        }

        // ---------------------------
        // CREATE ATTENDANCE RECORD
        // ---------------------------
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateAttendance([FromBody] AttendanceSummaryDto model)
        {
            if (model == null)
                return BadRequest("Invalid data.");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                var query = @"INSERT INTO App.AttendanceSummary
                      (EmpID, EmployeeName, WorkingDays, LeaveDays, WeekDaysOff, PropertyID, is_active, CreatedOn, monthyear)
                      VALUES (@EmpID, @EmployeeName, @WorkingDays, @LeaveDays, @WeekDaysOff, @PropertyID, 1, GETDATE(), @monthyear);";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmpID", model.EmpID);
                    cmd.Parameters.AddWithValue("@EmployeeName", model.EmployeeName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@WorkingDays", (object)model.WorkingDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LeaveDays", (object)model.LeaveDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WeekDaysOff", (object)model.WeekDaysOff ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PropertyID", (object)model.PropertyID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@monthyear", (object)model.monthyear ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return Ok(new { message = "Attendance record created successfully", data = model });
        }

        // ---------------------------
        // UPDATE ONLY WORKING, LEAVE & WEEKDAYS OFF
        // ---------------------------
        // ---------------------------
        // UPDATE WORKING, LEAVE & WEEKDAYS OFF BY EmpID AND monthyear
        // ---------------------------
        [HttpPut]
        [Route("{empId:int}/{monthyear}")]
        public async Task<IHttpActionResult> UpdateAttendance(int empId, string monthyear, [FromBody] AttendanceSummaryDto model)
        {
            if (model == null)
                return BadRequest("Invalid data.");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                var query = @"
            UPDATE App.AttendanceSummary
            SET WorkingDays = @WorkingDays,
                LeaveDays = @LeaveDays,
                WeekDaysOff = @WeekDaysOff
            WHERE EmpID = @EmpID 
              AND monthyear = @monthyear
              AND is_active = 1;
        ";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmpID", empId);
                    cmd.Parameters.AddWithValue("@monthyear", monthyear);
                    cmd.Parameters.AddWithValue("@WorkingDays", (object)model.WorkingDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LeaveDays", (object)model.LeaveDays ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@WeekDaysOff", (object)model.WeekDaysOff ?? DBNull.Value);

                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                        return NotFound();
                }
            }

            return Ok(new { message = "Attendance record updated successfully", data = model });
        }

        // ---------------------------
        // SOFT DELETE ATTENDANCE RECORD
        // ---------------------------
        [HttpDelete]
        [Route("delete-multiple")]
        public async Task<IHttpActionResult> SoftDeleteMultipleByEmpIds(string empIds, string monthyear)
        {
            if (string.IsNullOrWhiteSpace(empIds) || string.IsNullOrWhiteSpace(monthyear))
                return BadRequest("EmpIDs and monthyear are required.");

            // Split comma-separated IDs into a list of integers
            var empIdList = new List<int>();
            foreach (var id in empIds.Split(','))
            {
                if (int.TryParse(id.Trim(), out int empId))
                    empIdList.Add(empId);
            }

            if (empIdList.Count == 0)
                return BadRequest("No valid EmpIDs provided.");

            int totalUpdated = 0;

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                // Build a dynamic SQL IN clause safely
                var parameters = new List<string>();
                for (int i = 0; i < empIdList.Count; i++)
                    parameters.Add($"@EmpID{i}");

                var query = $@"
            UPDATE App.AttendanceSummary
            SET is_active = 0
            WHERE EmpID IN ({string.Join(",", parameters)})
              AND monthyear = @monthyear
              AND is_active = 1;
        ";

                using (var cmd = new SqlCommand(query, conn))
                {
                    for (int i = 0; i < empIdList.Count; i++)
                        cmd.Parameters.AddWithValue($"@EmpID{i}", empIdList[i]);

                    cmd.Parameters.AddWithValue("@monthyear", monthyear);

                    totalUpdated = await cmd.ExecuteNonQueryAsync();
                }
            }

            if (totalUpdated == 0)
                return NotFound();

            return Ok(new
            {
                message = "Selected attendance records soft-deleted successfully",
                deletedCount = totalUpdated
            });
        }


        // ---------------------------
        // HELPER: MAP SQL DATA TO DTO
        // ---------------------------
        private AttendanceSummaryDto MapReaderToAttendance(SqlDataReader reader)
        {
            return new AttendanceSummaryDto
            {
                EmpID = Convert.ToInt32(reader["EmpID"]),
                EmployeeName = reader["EmployeeName"].ToString(),
                WorkingDays = reader["WorkingDays"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["WorkingDays"]),
                LeaveDays = reader["LeaveDays"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["LeaveDays"]),
                WeekDaysOff = reader["WeekDaysOff"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["WeekDaysOff"]),
                PropertyID = reader["PropertyID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PropertyID"]),
                CreatedOn = reader["CreatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["CreatedOn"]),
                IsActive = reader["is_active"] == DBNull.Value ? false : Convert.ToBoolean(reader["is_active"]),
                monthyear = reader["monthyear"] == DBNull.Value ? null : reader["monthyear"].ToString()
            };
        }
    }
}