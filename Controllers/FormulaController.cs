using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/formulas")]
    public class FormulaController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ---------------------------
        // GET ALL FORMULAS
        // ---------------------------
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAllFormulas()
        {
            var list = new List<FormulaDto>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM app.formulamaster WHERE IsActive = 1", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapReaderToFormula(reader));
                }
            }
            return Ok(list);
        }

        // ---------------------------
        // GET FORMULA BY ID
        // ---------------------------
        [HttpGet]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> GetFormulaById(int id)
        {
            FormulaDto formula = null;
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM app.formulamaster WHERE ID=@ID AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                            formula = MapReaderToFormula(reader);
                    }
                }
            }
            if (formula == null) return NotFound();
            return Ok(formula);
        }

        // ---------------------------
        // CREATE FORMULA
        // ---------------------------
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateFormula([FromBody] FormulaDto model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"INSERT INTO app.formulamaster
                              (NAME, Formula, FixedValue, IsActive, CreatedOn)
                              VALUES (@NAME, @Formula, @FixedValue, 1, GETDATE());
                              SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Formula", model.Formula ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FixedValue", (object)model.FixedValue ?? DBNull.Value);

                    var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    model.Id = insertedId;
                }
            }
            return Ok(new { message = "Formula created successfully", data = model });
        }

        // ---------------------------
        // UPDATE FORMULA
        // ---------------------------
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> UpdateFormula(int id, [FromBody] FormulaDto model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"UPDATE app.formulamaster
                              SET NAME=@NAME, Formula=@Formula, FixedValue=@FixedValue, UpdatedOn=GETDATE()
                              WHERE ID=@ID AND IsActive=1";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Formula", model.Formula ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FixedValue", (object)model.FixedValue ?? DBNull.Value);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) return NotFound();
                }
            }
            return Ok(new { message = "Formula updated successfully", data = model });
        }

        // ---------------------------
        // SOFT DELETE FORMULA
        // ---------------------------
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> SoftDeleteFormula(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("UPDATE app.formulamaster SET IsActive=0, UpdatedOn=GETDATE() WHERE ID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0) return NotFound();
                }
            }
            return Ok(new { message = "Formula soft-deleted successfully" });
        }

        // ---------------------------
        // HELPER
        // ---------------------------
        private FormulaDto MapReaderToFormula(SqlDataReader reader)
        {
            return new FormulaDto
            {
                Id = Convert.ToInt32(reader["ID"]),
                Name = reader["NAME"].ToString(),
                Formula = reader["Formula"].ToString(),
                FixedValue = reader["FixedValue"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["FixedValue"])
            };
        }
    }
}
