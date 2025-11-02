using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/attendance")]
    public class AttendanceController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpGet]
        [Route("getAllLeaves")]
        public IHttpActionResult GetAllRequest(int propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("Invalid PropertyId");

            List<LeaveRequestModel> list = new List<LeaveRequestModel>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"SELECT EmployeeName, LeaveId, lr.MobileNo, lr.FromDate, lr.ToDate, lr.Reason, lr.Status, lr.AppliedOn, lr.LeaveType, 
                        lr.isActive, lr.isApproved, lr.isRejected, lr.actionBy, lr.actionOn, lr.actionRemarks, 
                        lr.LeaveTypeId, lr.LeaveCount
                 FROM LeaveRequest lr
                 LEFT JOIN EmployeeList el ON lr.MobileNo = el.MobileNo
                 LEFT JOIN app.FacilityMember fm ON lr.MobileNo = fm.MobileNumber
                 WHERE lr.isActive = 1 AND fm.PropertyId = @PropertyId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new LeaveRequestModel
                            {
                                EmployeeName = reader["EmployeeName"] != DBNull.Value ? reader["EmployeeName"].ToString() : "",
                                LeaveId = Convert.ToInt32(reader["LeaveId"]),
                                MobileNo = reader["MobileNo"].ToString(),
                                FromDate = Convert.ToDateTime(reader["FromDate"]),
                                ToDate = Convert.ToDateTime(reader["ToDate"]),
                                Reason = reader["Reason"].ToString(),
                                AppliedOn = Convert.ToDateTime(reader["AppliedOn"]),
                                LeaveType = reader["LeaveType"] != DBNull.Value ? reader["LeaveType"].ToString() : "",
                                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                                IsApproved = reader["IsApproved"] != DBNull.Value && Convert.ToBoolean(reader["IsApproved"]),
                                IsRejected = reader["IsRejected"] != DBNull.Value && Convert.ToBoolean(reader["IsRejected"]),
                                Status = reader["Status"].ToString(),
                                ActionBy = reader["actionBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["actionBy"]),
                                ActionOn = reader["actionOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["actionOn"]),
                                ActionRemarks = reader["actionRemarks"].ToString(),
                                LeaveTypeId = reader["LeaveTypeId"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["LeaveTypeId"]),
                                LeaveCount = reader["LeaveCount"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["LeaveCount"]),

                            });
                        }
                    }
                }
            }
            return Ok(list);
        }

        [HttpPost]
        [Route("leave-request")]
        public IHttpActionResult CreateLeaveRequest([FromBody] LeaveRequestModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.MobileNo))
                return BadRequest("Invalid Leave Request");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"
            INSERT INTO LeaveRequest
                (MobileNo, FromDate, ToDate, Reason, Status, AppliedOn, LeaveType, 
                 isActive, isApproved, isRejected, actionBy, actionOn, actionRemarks, LeaveTypeId, LeaveCount)
            VALUES
                (@MobileNo, @FromDate, @ToDate, @Reason, @Status, GETDATE(), @LeaveType,
                 1, 0, 0, NULL, NULL, NULL, @LeaveTypeId, @LeaveCount);
            SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MobileNo", (object)model.MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FromDate", (object)model.FromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object)model.ToDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Reason", (object)model.Reason ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", (object)model.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("@LeaveType", (object)model.LeaveType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LeaveTypeId", (object)model.LeaveTypeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LeaveCount", (object)model.LeaveCount ?? DBNull.Value);

                    conn.Open();
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                    return Ok(new { message = "Leave request created successfully", LeaveId = newId });
                }
            }
        }


        [HttpPost]
        [Route("updateLeaves")]
        public IHttpActionResult UpdateRequest([FromBody] LeaveRequestModel model)
        {
            if (model.LeaveId <= 0)
                return BadRequest("Invalid LeaveId");

            // Decide Status based on IsApproved / IsRejected
            string status = "Pending";
            if (model.IsApproved)
                status = "Approved";
            else if (model.IsRejected)
                status = "Rejected";
            else if (!string.IsNullOrEmpty(model.Status))
                status = model.Status; // fallback to provided status if neither Approved nor Rejected

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"UPDATE LeaveRequest
                  SET Status = @Status,
                      IsApproved = @IsApproved,
                      IsRejected = @IsRejected,
                      IsActive = @IsActive,
                      ActionBy = @ActionBy,
                      ActionOn = GETDATE(),
                      ActionRemarks = @ActionRemarks
                  WHERE LeaveId = @LeaveId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@LeaveId", model.LeaveId);
                    cmd.Parameters.AddWithValue("@Status", status);
                    cmd.Parameters.AddWithValue("@IsApproved", model.IsApproved ? 1 : 0);
                    cmd.Parameters.AddWithValue("@IsRejected", model.IsRejected ? 1 : 0);
                    cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                    cmd.Parameters.AddWithValue("@ActionBy", (object)model.ActionBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ActionRemarks", (object)model.ActionRemarks ?? DBNull.Value);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    return rows > 0 ? (IHttpActionResult)Ok("Leave request updated successfully") : BadRequest("Update failed");
                }
            }
        }


        [HttpGet] // Converted on 15/07/25
        [Route("monthly-summary")]
        public IHttpActionResult GetMonthlyAttendanceSummary(
  int PropertyId,
  DateTime? FromDate = null,
  DateTime? ToDate = null,
  string MobileNo = null,
  string EmployeeName = null)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            string connStr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand("usp_GetMonthlyAttendanceSummaryByProperty", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Required parameter
                    cmd.Parameters.AddWithValue("@PropertyId", PropertyId);

                    // Optional parameters
                    cmd.Parameters.AddWithValue("@FromDate", (object)FromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ToDate", (object)ToDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MobileNo", (object)MobileNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@EmployeeName", (object)EmployeeName ?? DBNull.Value);

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetValue(i));
                        }
                        result.Add(row);
                    }
                }
            }

            return Ok(result);
        }

        [HttpGet]
        [Route("leave-master/byProperty/{propertyId:int}")]
        public async Task<IHttpActionResult> GetLeaveMasterByPropertyId(int propertyId)
        {
            var result = new List<LeaveMasterDto>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT * FROM dbo.leave_master WHERE property_id = @propertyId AND is_active = 1", conn);
                cmd.Parameters.AddWithValue("@propertyId", propertyId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        result.Add(MapReaderToDtos(reader));
                }
            }
            return Ok(result);
        }

        [HttpPost]
        [Route("leave-master")]
        public async Task<IHttpActionResult> CreateLeaveMaster([FromBody] LeaveMasterDto dto)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"INSERT INTO dbo.leave_master 
                              (property_id, leave_type, leave_description, created_on, created_by, is_active) 
                              VALUES (@property_id, @leave_type, @leave_description, GETDATE(), @created_by, @is_active)";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@property_id", (object)dto.PropertyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@leave_type", (object)dto.LeaveType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@leave_description", (object)dto.LeaveDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@created_by", (object)dto.CreatedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@is_active", dto.IsActive);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Leave record created successfully" });
        }

        [HttpPut]
        [Route("leave-master/{id:int}")]
        public async Task<IHttpActionResult> UpdateLeaveMaster(int id, [FromBody] LeaveMasterDto dto)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"UPDATE dbo.leave_master 
                              SET property_id = @property_id, 
                                  leave_type = @leave_type, 
                                  leave_description = @leave_description, 
                                  updated_on = GETDATE(), 
                                  updated_by = @updated_by, 
                                  is_active = @is_active
                              WHERE id = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@property_id", (object)dto.PropertyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@leave_type", (object)dto.LeaveType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@leave_description", (object)dto.LeaveDescription ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@updated_by", (object)dto.UpdatedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@is_active", dto.IsActive);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) return NotFound();
                }
            }
            return Ok(new { message = "Leave record updated successfully" });
        }

        [HttpDelete]
        [Route("leave-master/{id:int}")]
        public async Task<IHttpActionResult> SoftDeleteLeaveMaster(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE dbo.leave_master SET is_active = 0, updated_on = GETDATE() WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Leave record soft-deleted successfully" });
        }

        private LeaveMasterDto MapReaderToDtos(SqlDataReader reader)
        {
            return new LeaveMasterDto
            {
                Id = Convert.ToInt32(reader["id"]),
                PropertyId = reader["property_id"] != DBNull.Value ? (int?)Convert.ToInt32(reader["property_id"]) : null,
                LeaveType = reader["leave_type"].ToString(),
                LeaveDescription = reader["leave_description"].ToString(),
                CreatedOn = reader["created_on"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["created_on"]) : null,
                CreatedBy = reader["created_by"] != DBNull.Value ? (int?)Convert.ToInt32(reader["created_by"]) : null,
                UpdatedOn = reader["updated_on"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["updated_on"]) : null,
                UpdatedBy = reader["updated_by"] != DBNull.Value ? (int?)Convert.ToInt32(reader["updated_by"]) : null,
                IsActive = Convert.ToBoolean(reader["is_active"])
            };
        }

        [HttpGet]
        [Route("employeeleave/{id:int}")]
        public IHttpActionResult GetById(int id)
        {
            EmployeeLeave leave = null;
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = "SELECT * FROM dbo.employee_leave WHERE id = @id AND is_active = 1";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            leave = MapReaderToDto(dr);
                        }
                    }
                }
            }
            if (leave == null)
                return NotFound();
            return Ok(leave);
        }

        // GET: api/employeeleave/property/{propertyId}
        [HttpGet]
        [Route("employeeleave/property/{propertyId:int}")]
        public IEnumerable<EmployeeLeave> GetByPropertyId(int propertyId)
        {
            var list = new List<EmployeeLeave>();
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = "SELECT * FROM dbo.employee_leave WHERE property_id = @property_id AND is_active = 1";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@property_id", propertyId);
                    con.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(MapReaderToDto(dr));
                        }
                    }
                }
            }
            return list;
        }

        // POST: api/employeeleave
        [HttpPost]
        [Route("employeeleave")]
        public IHttpActionResult Create(EmployeeLeave leave)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = @"INSERT INTO dbo.employee_leave
                                (property_id, employee_id, leave_type_id, balance, financial_year, created_on, created_by, is_active) 
                                VALUES (@property_id, @employee_id, @leave_type_id, @balance, @financial_year, GETDATE(), @created_by, 1)";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@property_id", leave.PropertyId);
                    cmd.Parameters.AddWithValue("@employee_id", leave.EmployeeId);
                    cmd.Parameters.AddWithValue("@leave_type_id", leave.LeaveTypeId);
                    cmd.Parameters.AddWithValue("@balance", leave.Balance);
                    cmd.Parameters.AddWithValue("@financial_year", leave.FinancialYear);
                    cmd.Parameters.AddWithValue("@created_by", (object)leave.CreatedBy ?? DBNull.Value);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Ok("Record inserted successfully.");
        }

        // PUT: api/employeeleave/{id}
        [HttpPut]
        [Route("employeeleave/{id:int}")]
        public IHttpActionResult Update(int id, EmployeeLeave leave)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = @"UPDATE dbo.employee_leave 
                                SET property_id=@property_id, employee_id=@employee_id, leave_type_id=@leave_type_id,
                                    balance=@balance,  financial_year=@financial_year,
                                    updated_on=GETDATE(), updated_by=@updated_by
                                WHERE id=@id AND is_active = 1";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@property_id", leave.PropertyId);
                    cmd.Parameters.AddWithValue("@employee_id", leave.EmployeeId);
                    cmd.Parameters.AddWithValue("@leave_type_id", leave.LeaveTypeId);
                    cmd.Parameters.AddWithValue("@balance", leave.Balance);
                    cmd.Parameters.AddWithValue("@financial_year", leave.FinancialYear);
                    cmd.Parameters.AddWithValue("@updated_by", (object)leave.UpdatedBy ?? DBNull.Value);

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }
            }
            return Ok("Record updated successfully.");
        }

        // DELETE (Soft Delete): api/employeeleave/{id}
        [HttpDelete]
        [Route("employeeleave/{id:int}")]
        public IHttpActionResult SoftDelete(int id)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                string query = @"UPDATE dbo.employee_leave 
                                SET is_active = 0, updated_on=GETDATE() 
                                WHERE id=@id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }
            }
            return Ok("Record deleted (soft) successfully.");
        }

        // Helper method
        private EmployeeLeave MapReaderToDto(SqlDataReader dr)
        {
            return new EmployeeLeave
            {
                Id = Convert.ToInt64(dr["id"]),
                PropertyId = Convert.ToInt32(dr["property_id"]),
                EmployeeId = Convert.ToInt32(dr["employee_id"]),
                LeaveTypeId = Convert.ToInt32(dr["leave_type_id"]),
                Balance = Convert.ToDecimal(dr["balance"]),
                FinancialYear = Convert.ToInt32(dr["financial_year"]),
                CreatedOn = Convert.ToDateTime(dr["created_on"]),
                CreatedBy = dr["created_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["created_by"]),
                UpdatedOn = dr["updated_on"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(dr["updated_on"]),
                UpdatedBy = dr["updated_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(dr["updated_by"]),
                IsActive = Convert.ToBoolean(dr["is_active"])
            };
        }

        [HttpGet]
        [Route("leave-summary")]
        public IHttpActionResult GetLeaveSummary(string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo))
                return BadRequest("Mobile number is required.");

            List<LeaveSummaryDto> list = new List<LeaveSummaryDto>();

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"
            SELECT 
                el.leave_type_id,
                lm.leave_type,
                COALESCE(SUM(CASE WHEN lr.Status = 'Approved' THEN lr.LeaveCount ELSE 0 END), 0) AS TakenLeaves,
                (el.balance - COALESCE(SUM(CASE WHEN lr.Status = 'Approved' THEN lr.LeaveCount ELSE 0 END), 0)) AS RemainingLeaves
            FROM dbo.employee_leave el
            INNER JOIN app.FacilityMember fm 
                ON el.employee_id = fm.FacilityMemberId
            INNER JOIN dbo.leave_master lm 
                ON el.leave_type_id = lm.id
            LEFT JOIN LeaveRequest lr 
                ON lr.MobileNo = fm.MobileNumber 
                AND lr.LeaveTypeId = el.leave_type_id
            WHERE fm.MobileNumber = @MobileNo
            GROUP BY el.leave_type_id, lm.leave_type, el.balance;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MobileNo", mobileNo);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new LeaveSummaryDto
                            {
                                LeaveTypeId = Convert.ToInt32(reader["leave_type_id"]),
                                LeaveType = reader["leave_type"].ToString(),
                                TakenLeaves = Convert.ToInt32(reader["TakenLeaves"]),
                                RemainingLeaves = Convert.ToInt32(reader["RemainingLeaves"])
                            });
                        }
                    }
                }
            }

            return Ok(list);
        }

        // CREATE
        [HttpPost]
        [Route("manualattendance/add")]
        public IHttpActionResult AddAttendance(ManualAttendance attendance)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string sql = @"
        INSERT INTO dbo.ManualAttendance
        (employee_id, check_in_time, check_out_time, gate_no, created_by, mobile_no, emp_id, status, image_file_name,
         is_approved, is_rejected, rejection_remark, property_id, LocationName)
        VALUES (@employee_id, @check_in_time, @check_out_time, @gate_no, @created_by, @mobile_no, @emp_id, @status, @image_file_name,
                0, 0, NULL, @property_id, @LocationName)";   // Force defaults here

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@employee_id", attendance.EmployeeId);
                cmd.Parameters.AddWithValue("@check_in_time", (object)attendance.CheckInTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@check_out_time", (object)attendance.CheckOutTime ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gate_no", (object)attendance.GateNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", (object)attendance.CreatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mobile_no", (object)attendance.MobileNo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@emp_id", (object)attendance.EmpId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", (object)attendance.Status ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@image_file_name", (object)attendance.ImageFileName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@property_id", (object)attendance.PropertyId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@LocationName", (object)attendance.LocationName ?? DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            return Ok(new { message = "Attendance added successfully" });
        }


        // READ (BY PROPERTY ID)
        [HttpGet]
        [Route("manualattendance/getbyproperty/{propertyId}")]
        public IHttpActionResult GetManualAttendance(int propertyId)
        {
            List<ManualAttendance> list = new List<ManualAttendance>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string sql = "SELECT * FROM dbo.ManualAttendance WHERE property_id = @PropertyId";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                conn.Open();
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new ManualAttendance
                    {
                        Id = Convert.ToInt32(rdr["id"]),
                        EmployeeId = Convert.ToInt32(rdr["employee_id"]),
                        CheckInTime = rdr["check_in_time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["check_in_time"]),
                        CheckOutTime = rdr["check_out_time"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["check_out_time"]),
                        GateNo = rdr["gate_no"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["gate_no"]),
                        CreatedBy = rdr["created_by"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["created_by"]),
                        CreatedOn = rdr["created_on"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(rdr["created_on"]),
                        MobileNo = rdr["mobile_no"]?.ToString(),
                        EmpId = rdr["emp_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["emp_id"]),
                        Status = rdr["status"]?.ToString(),
                        ImageFileName = rdr["image_file_name"]?.ToString(),
                        IsApproved = Convert.ToBoolean(rdr["is_approved"]),
                        IsRejected = Convert.ToBoolean(rdr["is_rejected"]),
                        RejectionRemark = rdr["rejection_remark"]?.ToString(),
                        PropertyId = rdr["property_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(rdr["property_id"]),
                        LocationName = rdr["LocationName"]?.ToString()
                    });
                }
            }

            if (list.Count == 0)
            {
                return NotFound(); // 404 if no records found
            }

            return Ok(list);
        }

        [HttpPost]
        [Route("ManualAttendance/ProcessManualAttendance")]
        public IHttpActionResult ProcessManualAttendance(int id, bool approve, string rejectionRemark = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    // Get the record from ManualAttendance
                    string selectSql = @"SELECT * FROM dbo.ManualAttendance WHERE Id = @Id";
                    SqlCommand selectCmd = new SqlCommand(selectSql, conn);
                    selectCmd.Parameters.AddWithValue("@Id", id);

                    SqlDataReader reader = selectCmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        return NotFound();
                    }

                    // Extract data
                    int employeeId = Convert.ToInt32(reader["employee_id"]);
                    DateTime? checkInTime = reader["check_in_time"] as DateTime?;
                    DateTime? checkOutTime = reader["check_out_time"] as DateTime?;
                    int? gateNo = reader["gate_no"] as int?;
                    int? createdBy = reader["created_by"] as int?;
                    DateTime createdOn = Convert.ToDateTime(reader["created_on"]);
                    string mobileNo = reader["mobile_no"].ToString();
                    string empId = reader["emp_id"]?.ToString();
                    string status = reader["status"]?.ToString();
                    string imageFileName = reader["image_file_name"]?.ToString();
                    string locationName = reader["LocationName"]?.ToString();

                    reader.Close();

                    if (approve)
                    {
                        // Insert into PunchTable
                        string insertSql = @"
                    INSERT INTO dbo.AttendanceLogs
                    (EmployeeId, PunchTime, PunchType, GateNo, CreatedBy, CreatedOn, MobileNo, EMP_ID, Status, ImageFileName, LocationName)
                    VALUES 
                    (@EmployeeId, @PunchTime, @PunchType, @GateNo, @CreatedBy, @CreatedOn, @MobileNo, @EMP_ID, @Status, @ImageFileName, @LocationName)";

                        // For check-in
                        if (checkInTime.HasValue)
                        {
                            SqlCommand insertCmdIn = new SqlCommand(insertSql, conn);
                            insertCmdIn.Parameters.AddWithValue("@EmployeeId", employeeId);
                            insertCmdIn.Parameters.AddWithValue("@PunchTime", checkInTime);
                            insertCmdIn.Parameters.AddWithValue("@PunchType", "Check In");
                            insertCmdIn.Parameters.AddWithValue("@GateNo", (object)gateNo ?? DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@CreatedBy", (object)createdBy ?? DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@CreatedOn", createdOn);
                            insertCmdIn.Parameters.AddWithValue("@MobileNo", mobileNo ?? (object)DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@EMP_ID", empId ?? (object)DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@ImageFileName", imageFileName ?? (object)DBNull.Value);
                            insertCmdIn.Parameters.AddWithValue("@LocationName", locationName ?? (object)DBNull.Value);
                            insertCmdIn.ExecuteNonQuery();
                        }

                        // For check-out
                        if (checkOutTime.HasValue)
                        {
                            SqlCommand insertCmdOut = new SqlCommand(insertSql, conn);
                            insertCmdOut.Parameters.AddWithValue("@EmployeeId", employeeId);
                            insertCmdOut.Parameters.AddWithValue("@PunchTime", checkOutTime);
                            insertCmdOut.Parameters.AddWithValue("@PunchType", "Check Out");
                            insertCmdOut.Parameters.AddWithValue("@GateNo", (object)gateNo ?? DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@CreatedBy", (object)createdBy ?? DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@CreatedOn", createdOn);
                            insertCmdOut.Parameters.AddWithValue("@MobileNo", mobileNo ?? (object)DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@EMP_ID", empId ?? (object)DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@ImageFileName", imageFileName ?? (object)DBNull.Value);
                            insertCmdOut.Parameters.AddWithValue("@LocationName", locationName ?? (object)DBNull.Value);
                            insertCmdOut.ExecuteNonQuery();
                        }

                        // Delete after successful insert
                        string deleteSql = "DELETE FROM ManualAttendance WHERE Id = @Id";
                        SqlCommand deleteCmd = new SqlCommand(deleteSql, conn);
                        deleteCmd.Parameters.AddWithValue("@Id", id);
                        deleteCmd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Reject: just update
                        string updateSql = @"UPDATE ManualAttendance 
                                     SET is_rejected = 1, rejection_remark = @RejectionRemark 
                                     WHERE Id = @Id";
                        SqlCommand updateCmd = new SqlCommand(updateSql, conn);
                        updateCmd.Parameters.AddWithValue("@Id", id);
                        updateCmd.Parameters.AddWithValue("@RejectionRemark", rejectionRemark ?? (object)DBNull.Value);
                        updateCmd.ExecuteNonQuery();
                    }

                    conn.Close();
                    return Ok(new { Success = true, Message = approve ? "Approved & moved to PunchTable" : "Rejected successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // DELETE
        [HttpDelete]
        [Route("manualattendance/delete")]
        public IHttpActionResult DeleteAttendance(int id, int employeeId)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string sql = "DELETE FROM dbo.ManualAttendance WHERE id = @id AND employee_id = @employee_id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@employee_id", employeeId);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0 ? Ok(new { message = "Attendance deleted successfully" }) : (IHttpActionResult)NotFound();
            }
        }


    }
}
