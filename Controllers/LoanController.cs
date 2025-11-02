using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    public class LoanController : ApiController
    {
        string constr = string.Empty;

        public LoanController()
        {
            constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;
        }

        // -------- POST: Insert LoanMaster + LoanEMI --------
        [HttpPost]
        [Route("api/loan/create")]
        public IHttpActionResult CreateLoan([FromBody] LoanData request)
        {
            if (request == null || request.LoanMaster == null || request.LoanEMIs == null)
                return BadRequest("Invalid request payload.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        int loanId;

                        // --- Insert into LoanMaster and get generated LoanID ---
                        string insertMaster = @"
                    INSERT INTO App.LoanMaster
                    (EmployeeID, LoanAdvanceAmount, CurrentMonth, TenureMonths, IssueDate, RepaymentStartDate)
                    VALUES (@EmployeeID, @LoanAdvanceAmount, @CurrentMonth, @TenureMonths, @IssueDate, @RepaymentStartDate);
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                        using (SqlCommand cmd = new SqlCommand(insertMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@EmployeeID", request.LoanMaster.EmployeeID);
                            cmd.Parameters.AddWithValue("@LoanAdvanceAmount", request.LoanMaster.LoanAdvanceAmount);
                            cmd.Parameters.AddWithValue("@CurrentMonth", request.LoanMaster.CurrentMonth);
                            cmd.Parameters.AddWithValue("@TenureMonths", request.LoanMaster.TenureMonths);
                            cmd.Parameters.AddWithValue("@IssueDate", request.LoanMaster.IssueDate);
                            cmd.Parameters.AddWithValue("@RepaymentStartDate", request.LoanMaster.RepaymentStartDate);

                            // Get the auto-generated LoanID
                            loanId = (int)cmd.ExecuteScalar();
                        }

                        // --- Insert LoanEMIs using the generated LoanID ---
                        foreach (var emi in request.LoanEMIs)
                        {
                            string insertEmi = @"
                        INSERT INTO App.LoanEMI
                        (LoanID, EmployeeID, MonthlyInstallment, RepaymentDoneDate, BalanceAmount)
                        VALUES (@LoanID, @EmployeeID, @MonthlyInstallment, @RepaymentDoneDate, @BalanceAmount)";

                            using (SqlCommand cmd = new SqlCommand(insertEmi, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", loanId); // use generated LoanID
                                cmd.Parameters.AddWithValue("@EmployeeID", emi.EmployeeID);
                                cmd.Parameters.AddWithValue("@MonthlyInstallment", emi.MonthlyInstallment);

                                // Handle null for C# 7.3
                                if (emi.RepaymentDoneDate == DateTime.MinValue)
                                    cmd.Parameters.AddWithValue("@RepaymentDoneDate", DBNull.Value);
                                else
                                    cmd.Parameters.AddWithValue("@RepaymentDoneDate", emi.RepaymentDoneDate);

                                cmd.Parameters.AddWithValue("@BalanceAmount", emi.BalanceAmount);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return Ok(new { Success = true, Message = "LoanMaster and LoanEMIs inserted successfully", LoanID = loanId });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error inserting loan data: " + ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Connection failed: " + ex.Message));
            }
        }


        // -------- GET: Fetch LoanMaster + LoanEMI by EmployeeID --------
        [HttpGet]
        [Route("api/loan/get/{employeeId}")]
        public IHttpActionResult GetLoanByEmployee(int employeeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    var response = new LoanData
                    {
                        LoanMaster = null,
                        LoanEMIs = new System.Collections.Generic.List<LoanEMI>()
                    };

                    // Get LoanMaster
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT TOP 1 *
                        FROM App.LoanMaster
                        WHERE EmployeeID = @EmployeeID", conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                response.LoanMaster = new LoanMaster
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

                    // Get LoanEMIs for this EmployeeID
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT *
                        FROM App.LoanEMI
                        WHERE EmployeeID = @EmployeeID", conn))
                    {
                        cmd.Parameters.AddWithValue("@EmployeeID", employeeId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var emi = new LoanEMI
                                {
                                    RowID = Convert.ToInt32(reader["RowID"]),
                                    LoanID = Convert.ToInt32(reader["LoanID"]),
                                    EmployeeID = Convert.ToInt32(reader["EmployeeID"]),
                                    MonthlyInstallment = Convert.ToDecimal(reader["MonthlyInstallment"]),
                                    BalanceAmount = Convert.ToDecimal(reader["BalanceAmount"])
                                };

                                // Handle null RepaymentDoneDate manually
                                if (reader["RepaymentDoneDate"] != DBNull.Value)
                                    emi.RepaymentDoneDate = Convert.ToDateTime(reader["RepaymentDoneDate"]);
                                else
                                    emi.RepaymentDoneDate = DateTime.MinValue; // sentinel value

                                response.LoanEMIs.Add(emi);
                            }
                        }
                    }

                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching loan data: " + ex.Message));
            }
        }
    }
}
