using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/allowancedeductions")]
    public class AllowanceDeductionController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ---------------------------
        // GET ALL BY PROPERTY
        // ---------------------------
        [HttpGet]
        [Route("byProperty/{propertyId:int}")]
        public async Task<IHttpActionResult> GetByProperty(int propertyId)
        {
            var list = new List<AllowanceDeduction>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT 
                        ID, Type, Name, Property_ID,
                        formula_id, calculated_amount,
                        [Created On] AS CreatedOn, [Created By] AS CreatedBy,
                        [Updated On] AS UpdatedOn, [Updated By] AS UpdatedBy,
                        is_active AS IsActive
                    FROM app.Payroll_SGT
                    WHERE Property_ID=@propertyId AND is_active=1", conn);

                cmd.Parameters.AddWithValue("@propertyId", propertyId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapReader(reader));
                }
            }

            // Evaluate formulas if formula_id exists
            await EvaluateFormulas(list);

            return Ok(list);
        }

        // ---------------------------
        // GET BY ID
        // ---------------------------
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            AllowanceDeduction item = null;
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT 
                        ID, Type, Name, Property_ID,
                        formula_id, calculated_amount,
                        [Created On] AS CreatedOn, [Created By] AS CreatedBy,
                        [Updated On] AS UpdatedOn, [Updated By] AS UpdatedBy,
                        is_active AS IsActive
                    FROM app.Payroll_SGT
                    WHERE ID=@id", conn);

                cmd.Parameters.AddWithValue("@id", id);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        item = MapReader(reader);
                }
            }

            if (item == null) return NotFound();

            // Evaluate formula if present
            if (item.FormulaId.HasValue)
            {
                item.CalculatedAmount = await EvaluateFormula(item);
            }

            return Ok(item);
        }

        // ---------------------------
        // CREATE
        // ---------------------------
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] AllowanceDeduction model)
        {
            try
            {
                using (var conn = new SqlConnection(constr))
                {
                    await conn.OpenAsync();
                    var query = @"
                        INSERT INTO app.Payroll_SGT
                            (Type, Name, Property_ID, formula_id, calculated_amount, [Created On], [Created By], is_active)
                        VALUES (@Type, @Name, @Property_ID, @FormulaId, @CalculatedAmount, GETDATE(), @CreatedBy, 1);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Type", model.Type);
                    cmd.Parameters.AddWithValue("@Name", model.Name);
                    cmd.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                    cmd.Parameters.AddWithValue("@FormulaId", (object)model.FormulaId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CalculatedAmount", (object)model.CalculatedAmount ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                    model.ID = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    model.CreatedOn = DateTime.Now;
                    model.IsActive = true;

                    // Recalculate amount if formula exists
                    if (model.FormulaId.HasValue)
                    {
                        model.CalculatedAmount = await EvaluateFormula(model);

                        // Update calculated amount in DB
                        var updCmd = new SqlCommand("UPDATE app.Payroll_SGT SET calculated_amount=@CalculatedAmount WHERE ID=@ID", conn);
                        updCmd.Parameters.AddWithValue("@CalculatedAmount", model.CalculatedAmount);
                        updCmd.Parameters.AddWithValue("@ID", model.ID);
                        await updCmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { message = "Allowance/Deduction created successfully", data = model });
            }
            catch (Exception ex)
            {
                return BadRequest("Error creating Allowance/Deduction: " + ex.Message);
            }
        }

        // ---------------------------
        // UPDATE
        // ---------------------------
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] AllowanceDeduction model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"
                    UPDATE app.Payroll_SGT
                    SET Type=@Type,
                        Name=@Name,
                        Property_ID=@Property_ID,
                        formula_id=@FormulaId,
                        calculated_amount=@CalculatedAmount,
                        [Updated By]=@UpdatedBy,
                        [Updated On]=GETDATE()
                    WHERE ID=@id";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@Type", model.Type);
                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@Property_ID", model.Property_ID);
                cmd.Parameters.AddWithValue("@FormulaId", (object)model.FormulaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CalculatedAmount", (object)model.CalculatedAmount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UpdatedBy", model.UpdatedBy ?? (object)DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();

                // Recalculate if formula exists
                if (model.FormulaId.HasValue)
                {
                    model.CalculatedAmount = await EvaluateFormula(model);

                    var updCmd = new SqlCommand("UPDATE app.Payroll_SGT SET calculated_amount=@CalculatedAmount WHERE ID=@ID", conn);
                    updCmd.Parameters.AddWithValue("@CalculatedAmount", model.CalculatedAmount);
                    updCmd.Parameters.AddWithValue("@ID", model.ID);
                    await updCmd.ExecuteNonQueryAsync();
                }
            }
            return Ok(new { message = "Allowance/Deduction updated successfully", data = model });
        }

        // ---------------------------
        // DELETE (SOFT DELETE)
        // ---------------------------
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE app.Payroll_SGT SET is_active=0 WHERE ID=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Allowance/Deduction deleted successfully" });
        }

        // ---------------------------
        // HELPER MAPPER
        // ---------------------------
        private AllowanceDeduction MapReader(SqlDataReader reader)
        {
            return new AllowanceDeduction
            {
                ID = Convert.ToInt32(reader["ID"]),
                Type = reader["Type"].ToString(),
                Name = reader["Name"].ToString(),
                Property_ID = Convert.ToInt32(reader["Property_ID"]),
                FormulaId = reader["formula_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["formula_id"]),
                CalculatedAmount = reader["calculated_amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["calculated_amount"]),
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),
                CreatedBy = Convert.ToInt32(reader["CreatedBy"]),
                UpdatedOn = reader["UpdatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["UpdatedOn"]),
                UpdatedBy = reader["UpdatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UpdatedBy"]),
                IsActive = Convert.ToBoolean(reader["IsActive"])
            };
        }

        // ---------------------------
        // FORMULA EVALUATION
        // ---------------------------
        private async Task<decimal?> EvaluateFormula(AllowanceDeduction ad)
        {
            if (!ad.FormulaId.HasValue) return ad.CalculatedAmount;

            // Fetch formula from FormulaMaster table
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT Formula, FixedValue FROM app.FormulaMaster WHERE ID=@id", conn);
                cmd.Parameters.AddWithValue("@id", ad.FormulaId.Value);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var formula = reader["Formula"] == DBNull.Value ? null : reader["Formula"].ToString();
                        var fixedValue = reader["FixedValue"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["FixedValue"]);

                        if (fixedValue.HasValue) return fixedValue.Value;

                        if (!string.IsNullOrEmpty(formula))
                        {
                            // Basic evaluation: Replace "BaseSalary" or other variables here
                            // TODO: You can use a proper expression evaluator (NCalc, DataTable.Compute, etc.)
                            if (formula.Contains("Basic"))
                            {
                                // Example: Basic*50% → 0.5 * BaseSalary
                                decimal baseSalary = 10000; // Replace with actual base salary input
                                var result = 0.5m * baseSalary; // Simplified example
                                return result;
                            }
                        }
                    }
                }
            }

            return ad.CalculatedAmount;
        }

        private async Task EvaluateFormulas(List<AllowanceDeduction> list)
        {
            foreach (var ad in list.Where(x => x.FormulaId.HasValue))
            {
                ad.CalculatedAmount = await EvaluateFormula(ad);
            }
        }
    }
}
