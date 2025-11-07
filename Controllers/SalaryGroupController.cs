using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/salaryallowances")]
    public class SalaryAllowanceController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ---------------------------
        // GET BY PROPERTY
        // ---------------------------
        [HttpGet]
        [Route("byProperty/{propertyId:int}")]
        public async Task<IHttpActionResult> GetByProperty(int propertyId)
        {
            var list = new List<SalaryAllowanceDto>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"
                     SELECT 
     sg.SalaryGroup_ID, 
     sg.[Salary Group], 
     sg.[Fixed Salary],
     sg.basic_salary AS BaseSalary,
     sg.Property_ID,
     sg.[Created On], sg.[Created By], sg.[Updated On], sg.[Updated By],
     sg.is_active,
     adm.AD_id AS AD_Id,
     adm2.Name AS AD_Name,
     adm2.Type AS AD_Type,
     pf.Id AS FormulaId,
     pf.Name AS FormulaName,
     pf.Formula,
     pf.FixedValue,
     lnk.CalculatedAmount
 FROM app.Payroll_SG sg
 LEFT JOIN app.Payroll_SG_SGT_Link adm 
     ON sg.SalaryGroup_ID = adm.SalaryGroup_ID AND adm.isActive = 1
 LEFT JOIN app.Payroll_SGT adm2 
     ON adm.AD_id = adm2.Id
 LEFT JOIN app.FormulaMaster pf
     ON pf.Id = adm2.Formula_Id
 LEFT JOIN app.Payroll_SG_SGT_Link lnk
    ON lnk.SalaryGroup_ID = sg.SalaryGroup_ID AND lnk.AD_id = adm.AD_id
 WHERE sg.Property_ID = @propertyId AND sg.is_active = 1
 ORDER BY sg.SalaryGroup_ID";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@propertyId", propertyId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var groupMap = new Dictionary<int, SalaryAllowanceDto>();

                    while (await reader.ReadAsync())
                    {
                        var sgId = Convert.ToInt32(reader["SalaryGroup_ID"]);

                        if (!groupMap.ContainsKey(sgId))
                        {
                            groupMap[sgId] = new SalaryAllowanceDto
                            {
                                SalaryGroup_ID = sgId,
                                SalaryGroup = reader["Salary Group"].ToString(),
                                FixedSalary = Convert.ToDecimal(reader["Fixed Salary"]),
                                BaseSalary = reader["BaseSalary"] == DBNull.Value
                                    ? Convert.ToDecimal(reader["Fixed Salary"])
                                    : Convert.ToDecimal(reader["BaseSalary"]),
                                Property_ID = Convert.ToInt32(reader["Property_ID"]),
                                CreatedOn = Convert.ToDateTime(reader["Created On"]),
                                CreatedBy = Convert.ToInt32(reader["Created By"]),
                                UpdatedOn = reader["Updated On"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Updated On"]),
                                UpdatedBy = reader["Updated By"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Updated By"]),
                                IsActive = Convert.ToBoolean(reader["is_active"]),
                                AllowancesDeductions = new List<AllowanceDeductionDto>()
                            };
                        }

                        // Only add AllowanceDeduction if AD_Id is valid (> 0 and not null)
                        if (reader["AD_Id"] != DBNull.Value && Convert.ToInt32(reader["AD_Id"]) > 0)
                        {
                            groupMap[sgId].AllowancesDeductions.Add(new AllowanceDeductionDto
                            {
                                AD_Id = Convert.ToInt32(reader["AD_Id"]),
                                Name = reader["AD_Name"].ToString(),
                                Type = reader["AD_Type"].ToString(),
                                Formula = new FormulaDto
                                {
                                    Id = reader["FormulaId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FormulaId"]),
                                    Name = reader["FormulaName"] == DBNull.Value ? null : reader["FormulaName"].ToString(),
                                    Formula = reader["Formula"] == DBNull.Value ? null : reader["Formula"].ToString(),
                                    FixedValue = reader["FixedValue"] == DBNull.Value ? null : (int?)Convert.ToDecimal(reader["FixedValue"])
                                },
                                CalculatedAmount = reader["CalculatedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CalculatedAmount"])
                            });
                        }
                    }

                    list.AddRange(groupMap.Values);
                }
            }

            return Ok(list);
        }

        // ---------------------------
        // CREATE
        // ---------------------------
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] SalaryAllowanceDto model)
        {
            if (model == null)
                return BadRequest("Invalid request body.");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var tran = conn.BeginTransaction();

                try
                {
                    // Insert Salary Group
                    var query1 = @"
                        INSERT INTO app.Payroll_SG 
                        ([Salary Group], [Fixed Salary], basic_salary, Property_ID, [Created On], [Created By], is_active)
                        VALUES (@SalaryGroup, @FixedSalary, @BaseSalary, @Property_ID, GETDATE(), @CreatedBy, 1);
                        SELECT SCOPE_IDENTITY();";

                    var cmd1 = new SqlCommand(query1, conn, tran);
                    cmd1.Parameters.AddWithValue("@SalaryGroup", (object)model.SalaryGroup ?? DBNull.Value);
                    cmd1.Parameters.AddWithValue("@FixedSalary", model.FixedSalary);
                    cmd1.Parameters.AddWithValue("@BaseSalary", model.BaseSalary);
                    cmd1.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                    cmd1.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                    var newGroupId = Convert.ToInt32(await cmd1.ExecuteScalarAsync());
                    model.SalaryGroup_ID = newGroupId;

                    // Insert Allowances & Deductions if provided
                    if (model.AllowancesDeductions != null && model.AllowancesDeductions.Any())
                    {
                        foreach (var ad in model.AllowancesDeductions)
                        {
                            var query2 = @"
                                INSERT INTO app.Payroll_SG_SGT_Link 
                                (SalaryGroup_ID, AD_id, Property_ID, [Created On], [Created By], isActive, CalculatedAmount)
                                VALUES (@SalaryGroup_ID, @AD_id, @Property_ID, GETDATE(), @CreatedBy, 1, @CalculatedAmount);";

                            var cmd2 = new SqlCommand(query2, conn, tran);
                            cmd2.Parameters.AddWithValue("@SalaryGroup_ID", newGroupId);
                            cmd2.Parameters.AddWithValue("@AD_id", ad.AD_Id);
                            cmd2.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                            cmd2.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                            cmd2.Parameters.AddWithValue("@CalculatedAmount", ad.CalculatedAmount);

                            await cmd2.ExecuteNonQueryAsync();
                        }
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return BadRequest("Database insert failed: " + ex.Message);
                }
            }

            return Ok(new { message = "Inserted successfully", data = model });
        }

        // ---------------------------
        // UPDATE
        // ---------------------------
        [HttpPut]
        [Route("{salaryGroupId:int}")]
        public async Task<IHttpActionResult> Update(int salaryGroupId, [FromBody] SalaryAllowanceDto model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var tran = conn.BeginTransaction();

                try
                {
                    var query1 = @"
                        UPDATE app.Payroll_SG
                        SET [Salary Group]=@SalaryGroup, [Fixed Salary]=@FixedSalary, basic_salary=@BaseSalary,
                            Property_ID=@Property_ID, [Updated On]=GETDATE(), [Updated By]=@UpdatedBy
                        WHERE SalaryGroup_ID=@SalaryGroup_ID";

                    var cmd1 = new SqlCommand(query1, conn, tran);
                    cmd1.Parameters.AddWithValue("@SalaryGroup_ID", salaryGroupId);
                    cmd1.Parameters.AddWithValue("@SalaryGroup", model.SalaryGroup);
                    cmd1.Parameters.AddWithValue("@FixedSalary", model.FixedSalary);
                    cmd1.Parameters.AddWithValue("@BaseSalary", model.BaseSalary);
                    cmd1.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                    cmd1.Parameters.AddWithValue("@UpdatedBy", model.UpdatedBy ?? (object)DBNull.Value);
                    await cmd1.ExecuteNonQueryAsync();

                    // Delete old links
                    var delCmd = new SqlCommand("DELETE FROM app.Payroll_SG_SGT_Link WHERE SalaryGroup_ID=@SalaryGroup_ID", conn, tran);
                    delCmd.Parameters.AddWithValue("@SalaryGroup_ID", salaryGroupId);
                    await delCmd.ExecuteNonQueryAsync();

                    // Insert new links
                    foreach (var ad in model.AllowancesDeductions)
                    {
                        var insCmd = new SqlCommand(@"
                            INSERT INTO app.Payroll_SG_SGT_Link
                                (SalaryGroup_ID, AD_id, Property_ID, [Created On], [Created By], isActive, CalculatedAmount)
                            VALUES (@SalaryGroup_ID, @AD_id, @Property_ID, GETDATE(), @CreatedBy, 1, @CalculatedAmount)", conn, tran);

                        insCmd.Parameters.AddWithValue("@SalaryGroup_ID", salaryGroupId);
                        insCmd.Parameters.AddWithValue("@AD_id", ad.AD_Id);
                        insCmd.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                        insCmd.Parameters.AddWithValue("@CreatedBy", model.UpdatedBy ?? model.CreatedBy);
                        insCmd.Parameters.AddWithValue("@CalculatedAmount", ad.CalculatedAmount);

                        await insCmd.ExecuteNonQueryAsync();
                    }

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return BadRequest(ex.Message);
                }
            }

            return Ok(new { message = "Updated successfully", data = model });
        }

        // ---------------------------
        // DELETE (soft)
        // ---------------------------
        [HttpDelete]
        [Route("{salaryGroupId:int}")]
        public async Task<IHttpActionResult> Delete(int salaryGroupId)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var tran = conn.BeginTransaction();

                try
                {
                    var cmd1 = new SqlCommand("UPDATE app.Payroll_SG_SGT_Link SET isActive=0 WHERE SalaryGroup_ID=@id", conn, tran);
                    cmd1.Parameters.AddWithValue("@id", salaryGroupId);
                    await cmd1.ExecuteNonQueryAsync();

                    var cmd2 = new SqlCommand("UPDATE app.Payroll_SG SET is_active=0 WHERE SalaryGroup_ID=@id", conn, tran);
                    cmd2.Parameters.AddWithValue("@id", salaryGroupId);
                    await cmd2.ExecuteNonQueryAsync();

                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return BadRequest(ex.Message);
                }
            }

            return Ok(new { message = "Deleted successfully (soft delete)" });
        }

        // ---------------------------
        // ASSIGN / REMOVE SALARY GROUPS
        // ---------------------------
        [HttpPost]
        [Route("assignSalaryGroup")]
        public async Task<IHttpActionResult> AssignSalaryGroup(FacilityMemberCreateDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.FacilityMemberIds) || dto.SalaryGroup_ID <= 0)
                return BadRequest("Invalid FacilityMemberIds or SalaryGroup_ID");

            // Convert comma-separated string to list of integers
            var memberIds = dto.FacilityMemberIds
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id.Trim()))
                .ToList();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var memberId in memberIds)
                        {
                            // 1️⃣ Update FacilityMember
                            var queryUpdate = @"
                    UPDATE app.FacilityMember 
                    SET SG_Link_ID = @SalaryGroup_ID, tax_amount = @tax_amount
                    WHERE FacilityMemberId = @FacilityMemberId";

                            using (var cmd = new SqlCommand(queryUpdate, conn, transaction))
                            {
                                cmd.Parameters.Add("@FacilityMemberId", SqlDbType.Int).Value = memberId;
                                cmd.Parameters.Add("@SalaryGroup_ID", SqlDbType.Int).Value = dto.SalaryGroup_ID;
                                cmd.Parameters.Add("@tax_amount", SqlDbType.Int).Value = dto.Taxamount;
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 2️⃣ Check for existing active link
                            var checkExistingQuery = @"
                    SELECT TOP 1 LinkId
                    FROM SalaryGroupFacilityLinking
                    WHERE FacilityMemberId = @FacilityMemberId AND End_date IS NULL
                    ORDER BY Start_date DESC";

                            int? existingLinkId = null;

                            using (var cmd = new SqlCommand(checkExistingQuery, conn, transaction))
                            {
                                cmd.Parameters.Add("@FacilityMemberId", SqlDbType.Int).Value = memberId;
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                        existingLinkId = reader.GetInt32(0);
                                }
                            }

                            DateTime startDate = DateTime.Now;

                            // 3️⃣ If active record exists, update its EndDate
                            if (existingLinkId.HasValue)
                            {
                                var updateOldLinkQuery = @"
                        UPDATE SalaryGroupFacilityLinking
                        SET End_date = @EndDate
                        WHERE LinkId = @LinkId";

                                using (var cmd = new SqlCommand(updateOldLinkQuery, conn, transaction))
                                {
                                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = startDate;
                                    cmd.Parameters.Add("@LinkId", SqlDbType.Int).Value = existingLinkId.Value;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            // 4️⃣ Insert new SalaryGroupFacilityLinking record
                            var queryInsertLink = @"
                    INSERT INTO SalaryGroupFacilityLinking (FacilityMemberId, SalaryGroup_ID, Start_date, End_date)
                    VALUES (@FacilityMemberId, @SalaryGroup_ID, @StartDate, @EndDate)";

                            using (var cmd = new SqlCommand(queryInsertLink, conn, transaction))
                            {
                                cmd.Parameters.Add("@FacilityMemberId", SqlDbType.Int).Value = memberId;
                                cmd.Parameters.Add("@SalaryGroup_ID", SqlDbType.Int).Value = dto.SalaryGroup_ID;
                                cmd.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = startDate;

                                if (dto.EndDate.HasValue)
                                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = dto.EndDate.Value;
                                else
                                    cmd.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = DBNull.Value;

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = "Salary group assigned successfully to all selected members." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }


        // ---------------------------
        // GET BY FACILITY MEMBER (linked salary group)
        // ---------------------------
        [HttpGet]
        [Route("byFacilityMember/{facilityMemberId:int}")]
        public async Task<IHttpActionResult> GetByFacilityMember(int facilityMemberId)
        {
            var salaryGroups = new List<SalaryAllowanceDto>();
            LoanData loanData = new LoanData
            {
                LoanMaster = null,
                LoanEMIs = new List<LoanEMI>()
            };

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                // 1️⃣ Fetch salary group with Start_date and End_date
                var querySalaryGroup = @"
            SELECT 
                fm.tax_amount,
                sg.SalaryGroup_ID, 
                sg.[Salary Group], 
                sg.[Fixed Salary],
                sg.basic_salary AS BaseSalary,
                sg.Property_ID,
                sg.[Created On], 
                sg.[Created By], 
                sg.[Updated On], 
                sg.[Updated By],
                sg.is_active,
                lnk.Start_date,
                lnk.End_date,
                adm.AD_id AS AD_Id,
                adm2.Name AS AD_Name,
                adm2.Type AS AD_Type,
                pf.Id AS FormulaId,
                pf.Name AS FormulaName,
                pf.Formula,
                pf.FixedValue,
                adm.CalculatedAmount
            FROM app.FacilityMember fm
            LEFT JOIN app.Payroll_SG sg 
                ON fm.SG_Link_ID = sg.SalaryGroup_ID
            LEFT JOIN SalaryGroupFacilityLinking lnk
                ON fm.FacilityMemberId = lnk.FacilityMemberId AND sg.SalaryGroup_ID = lnk.SalaryGroup_ID
            LEFT JOIN app.Payroll_SG_SGT_Link adm 
                ON sg.SalaryGroup_ID = adm.SalaryGroup_ID AND adm.isActive = 1
            LEFT JOIN app.Payroll_SGT adm2 
                ON adm.AD_id = adm2.Id
            LEFT JOIN app.FormulaMaster pf
                ON pf.Id = adm2.Formula_Id
            WHERE fm.FacilityMemberId = @facilityMemberId
              AND sg.is_active = 1";

                using (var cmd = new SqlCommand(querySalaryGroup, conn))
                {
                    cmd.Parameters.AddWithValue("@facilityMemberId", facilityMemberId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var groupMap = new Dictionary<int, SalaryAllowanceDto>();

                        while (await reader.ReadAsync())
                        {
                            var sgId = Convert.ToInt32(reader["SalaryGroup_ID"]);

                            if (!groupMap.ContainsKey(sgId))
                            {
                                groupMap[sgId] = new SalaryAllowanceDto
                                {
                                    SalaryGroup_ID = sgId,
                                    SalaryGroup = reader["Salary Group"].ToString(),
                                    FixedSalary = Convert.ToDecimal(reader["Fixed Salary"]),
                                    BaseSalary = reader["BaseSalary"] == DBNull.Value
                                                    ? Convert.ToDecimal(reader["Fixed Salary"])
                                                    : Convert.ToDecimal(reader["BaseSalary"]),
                                    Property_ID = Convert.ToInt32(reader["Property_ID"]),
                                    CreatedOn = Convert.ToDateTime(reader["Created On"]),
                                    CreatedBy = Convert.ToInt32(reader["Created By"]),
                                    UpdatedOn = reader["Updated On"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Updated On"]),
                                    UpdatedBy = reader["Updated By"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Updated By"]),
                                    IsActive = Convert.ToBoolean(reader["is_active"]),
                                    TaxAmount = reader["tax_amount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["tax_amount"]),
                                    StartDate = reader["Start_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Start_date"]),
                                    EndDate = reader["End_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["End_date"]),
                                    AllowancesDeductions = new List<AllowanceDeductionDto>()
                                };
                            }

                            if (reader["AD_Id"] != DBNull.Value && Convert.ToInt32(reader["AD_Id"]) > 0)
                            {
                                groupMap[sgId].AllowancesDeductions.Add(new AllowanceDeductionDto
                                {
                                    AD_Id = Convert.ToInt32(reader["AD_Id"]),
                                    Name = reader["AD_Name"].ToString(),
                                    Type = reader["AD_Type"].ToString(),
                                    Formula = new FormulaDto
                                    {
                                        Id = reader["FormulaId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FormulaId"]),
                                        Name = reader["FormulaName"] == DBNull.Value ? null : reader["FormulaName"].ToString(),
                                        Formula = reader["Formula"] == DBNull.Value ? null : reader["Formula"].ToString(),
                                        FixedValue = reader["FixedValue"] == DBNull.Value ? null : (int?)Convert.ToDecimal(reader["FixedValue"])
                                    },
                                    CalculatedAmount = reader["CalculatedAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CalculatedAmount"])
                                });
                            }
                        }

                        salaryGroups.AddRange(groupMap.Values);
                    }
                }

                // 2️⃣ Fetch loan data using same logic as /api/loan/get/{employeeId}
                using (var cmd = new SqlCommand("SELECT TOP 1 * FROM App.LoanMaster WHERE EmployeeID = @EmployeeID", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", facilityMemberId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            loanData.LoanMaster = new LoanMaster
                            {
                                LoanID = Convert.ToInt32(reader["LoanID"]),
                                EmployeeID = Convert.ToInt32(reader["EmployeeID"]),
                                LoanAdvanceAmount = Convert.ToDecimal(reader["LoanAdvanceAmount"]),
                                CurrentMonth = reader["CurrentMonth"].ToString(),
                                TenureMonths = Convert.ToInt32(reader["TenureMonths"]),
                                IssueDate = Convert.ToDateTime(reader["IssueDate"]),
                                RepaymentStartDate = Convert.ToDateTime(reader["RepaymentStartDate"])
                            };
                        }
                    }
                }

                using (var cmd = new SqlCommand("SELECT * FROM App.LoanEMI WHERE EmployeeID = @EmployeeID", conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeeID", facilityMemberId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            loanData.LoanEMIs.Add(new LoanEMI
                            {
                                RowID = Convert.ToInt32(reader["RowID"]),
                                LoanID = Convert.ToInt32(reader["LoanID"]),
                                EmployeeID = Convert.ToInt32(reader["EmployeeID"]),
                                MonthlyInstallment = Convert.ToDecimal(reader["MonthlyInstallment"]),
                                BalanceAmount = Convert.ToDecimal(reader["BalanceAmount"]),
                                RepaymentDoneDate = reader["RepaymentDoneDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["RepaymentDoneDate"])
                            });
                        }
                    }
                }
            }

            return Ok(new
            {
                SalaryGroups = salaryGroups,
                LoanData = loanData
            });
        }

    }
}
