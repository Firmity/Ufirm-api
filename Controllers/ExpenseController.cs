using ExcelDataReader;
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
    [RoutePrefix("api/expenses")]
    public class ExpenseController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ---------------------------
        // EXPENSE MASTER CRUD
        // ---------------------------

        [HttpGet]
        [Route("master/byOffice/{officeId:int}")]
        public async Task<IHttpActionResult> GetExpensesByOffice(int officeId)
        {
            var list = new List<ExpenseMaster>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT * FROM app.ExpenseMaster WHERE office_id=@officeId AND is_active=1", conn);
                cmd.Parameters.AddWithValue("@officeId", officeId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapReaderToExpense(reader));
                }
            }
            return Ok(list);
        }

        [HttpPost]
        [Route("master")]
        public async Task<IHttpActionResult> CreateExpense([FromBody] ExpenseMaster model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"INSERT INTO app.ExpenseMaster
                              (expense_type, expense_subtype, date_from, date_to, amount, description, bill_image, office_id, is_active, created_by, created_on)
                              VALUES (@expense_type, @expense_subtype, @date_from, @date_to, @amount, @description, @bill_image, @office_id, 1, @created_by, GETDATE());
                              SELECT SCOPE_IDENTITY();";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@expense_type", model.ExpenseType);
                cmd.Parameters.AddWithValue("@expense_subtype", (object)model.ExpenseSubtype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@date_from", model.DateFrom);
                cmd.Parameters.AddWithValue("@date_to", model.DateTo);
                cmd.Parameters.AddWithValue("@amount", model.Amount);
                cmd.Parameters.AddWithValue("@description", (object)model.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bill_image", (object)model.BillImage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@office_id", model.OfficeId);
                cmd.Parameters.AddWithValue("@created_by", model.CreatedBy);

                var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                model.Id = insertedId;
                model.CreatedOn = DateTime.Now;
                model.IsActive = true;
            }
            return Ok(new { message = "Expense created successfully", data = model });
        }

        [HttpPut]
        [Route("master/{id:int}")]
        public async Task<IHttpActionResult> UpdateExpense(int id, [FromBody] ExpenseMaster model)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"UPDATE app.ExpenseMaster
                              SET expense_type=@expense_type, expense_subtype=@expense_subtype,
                                  date_from=@date_from, date_to=@date_to, amount=@amount,
                                  description=@description, bill_image=@bill_image, office_id=@office_id,
                                  updated_by=@updated_by, updated_on=GETDATE()
                              WHERE id=@id AND is_active=1";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@expense_type", model.ExpenseType);
                cmd.Parameters.AddWithValue("@expense_subtype", (object)model.ExpenseSubtype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@date_from", model.DateFrom);
                cmd.Parameters.AddWithValue("@date_to", model.DateTo);
                cmd.Parameters.AddWithValue("@amount", model.Amount);
                cmd.Parameters.AddWithValue("@description", (object)model.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@bill_image", (object)model.BillImage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@office_id", model.OfficeId);
                cmd.Parameters.AddWithValue("@updated_by", (object)model.UpdatedBy ?? DBNull.Value);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Expense updated successfully", data = model });
        }

        [HttpDelete]
        [Route("master/{id:int}")]
        public async Task<IHttpActionResult> SoftDeleteExpense(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE app.ExpenseMaster SET is_active=0, updated_on=GETDATE() WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Expense soft-deleted successfully" });
        }

        // ---------------------------
        // EXPENSE TYPE CRUD
        // ---------------------------

        [HttpGet]
        [Route("types/{officeId:int}")]
        public async Task<IHttpActionResult> GetExpenseTypes(int officeId)
        {
            var list = new List<ExpenseType>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT * FROM app.expense_types WHERE office_id=@officeId AND is_active=1", conn);
                cmd.Parameters.AddWithValue("@officeId", officeId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        list.Add(MapReaderToExpenseType(reader));
                }
            }
            return Ok(list);
        }

        [HttpPost]
        [Route("types")]
        public async Task<IHttpActionResult> CreateExpenseType([FromBody] ExpenseType dto)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"INSERT INTO app.expense_types
                              (expense_type, expense_subtype, created_by, created_at, is_active, office_id)
                              VALUES (@expense_type, @expense_subtype, @created_by, GETDATE(), 1, @office_id);
                              SELECT SCOPE_IDENTITY();";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@expense_type", dto.ExpenseTypeName);
                cmd.Parameters.AddWithValue("@expense_subtype", (object)dto.ExpenseSubtype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@created_by", dto.CreatedBy);
                cmd.Parameters.AddWithValue("@office_id", dto.OfficeId);

                var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                dto.ExpenseId = insertedId;
                dto.CreatedAt = DateTime.Now;
                dto.IsActive = true;
            }
            return Ok(new { message = "Expense type created successfully", data = dto });
        }

        [HttpPut]
        [Route("types/{id:int}")]
        public async Task<IHttpActionResult> UpdateExpenseType(int id, [FromBody] ExpenseType dto)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var query = @"UPDATE app.expense_types
                              SET expense_type=@expense_type, expense_subtype=@expense_subtype,
                                  updated_by=@updated_by, updated_at=GETDATE(), is_active=@is_active
                              WHERE expense_id=@id";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@expense_type", dto.ExpenseTypeName);
                cmd.Parameters.AddWithValue("@expense_subtype", (object)dto.ExpenseSubtype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@updated_by", (object)dto.UpdatedBy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@is_active", dto.IsActive);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Expense type updated successfully", data = dto });
        }

        [HttpDelete]
        [Route("types/{id:int}")]
        public async Task<IHttpActionResult> SoftDeleteExpenseType(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("UPDATE app.expense_types SET is_active=0, updated_at=GETDATE() WHERE expense_id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return NotFound();
            }
            return Ok(new { message = "Expense type soft-deleted successfully" });
        }

        // ---------------------------
        // 1. Get ExpenseType Names by OfficeId
        // ---------------------------
        [HttpGet]
        [Route("types/names/byOffice/{officeId:int}")]
        public async Task<IHttpActionResult> GetExpenseTypeNamesByOffice(int officeId)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "SELECT DISTINCT expense_type FROM app.expense_types WHERE office_id=@officeId AND is_active=1", conn);
                cmd.Parameters.AddWithValue("@officeId", officeId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(reader["expense_type"].ToString());
                    }
                }
            }
            return Ok(list);
        }

        // ---------------------------
        // 2. Get ExpenseSubtypes by ExpenseTypeName
        // ---------------------------
        [HttpGet]
        [Route("types/subtypes/byType")]
        public async Task<IHttpActionResult> GetExpenseSubtypesByTypeName([FromUri] string expenseTypeName)
        {
            var list = new List<string>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "SELECT DISTINCT expense_subtype FROM app.expense_types WHERE expense_type=@expenseType AND is_active=1", conn);
                cmd.Parameters.AddWithValue("@expenseType", expenseTypeName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (reader["expense_subtype"] != DBNull.Value)
                            list.Add(reader["expense_subtype"].ToString());
                    }
                }
            }
            return Ok(list);
        }
        [HttpGet]
        [Route("all-expense")]
        public async Task<IHttpActionResult> GetExpenseReportAll(
    [FromUri] DateTime dateFrom,
    [FromUri] DateTime dateTo,
    [FromUri] int officeId)   // 👈 only officeId needed
        {
            var result = new List<object>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                var query = @"
        SELECT 
    CASE 
        WHEN src.RecordType = 'Expense' THEN e.expense_type
        WHEN src.RecordType = 'AssetService' THEN 'Asset Service'
        WHEN src.RecordType = 'AssetRental' THEN 'Asset Rental'
    END AS ExpenseType,
    CASE 
        WHEN src.RecordType = 'Expense' THEN e.expense_subtype
        WHEN src.RecordType = 'AssetService' THEN a.Name
        WHEN src.RecordType = 'AssetRental' THEN a.Name
    END AS ExpenseSubType,
    SUM(src.Amount) AS TotalAmount
FROM (
    -- Expenses
    SELECT 
        'Expense' AS RecordType,
        e.expense_type,
        e.expense_subtype,
        e.amount,
        NULL AS AssetId
    FROM app.ExpenseMaster e
    WHERE e.office_id = @office_id
      AND e.date_from >= @date_from 
      AND e.date_to <= @date_to 
      AND e.is_active = 1

    UNION ALL

    -- Rentals
    SELECT 
        'AssetRental' AS RecordType,
        NULL AS expense_type,
        NULL AS expense_subtype,
        ISNULL(r.MonthlyRent,0) AS amount,
        r.AssetId
    FROM dbo.RentalAssets r
    INNER JOIN dbo.AssetMaster a ON r.AssetId = a.Id
    WHERE r.RentedOutDate >= @date_from 
      AND (r.ReturnDate IS NULL OR r.ReturnDate <= @date_to)
      AND a.propertyId = @office_id

    UNION ALL

    -- Services
    SELECT 
        'AssetService' AS RecordType,
        NULL AS expense_type,
        NULL AS expense_subtype,
        ISNULL(s.ServiceCost,0) AS amount,
        s.AssetId
    FROM dbo.AssetServiceRecord s
    INNER JOIN dbo.AssetMaster a ON s.AssetId = a.Id
    WHERE s.ServiceDate BETWEEN @date_from AND @date_to
      AND s.IsDeleted = 0
      AND a.propertyId = @office_id
) src
LEFT JOIN app.ExpenseMaster e ON src.RecordType = 'Expense' AND e.expense_subtype = src.expense_subtype
LEFT JOIN dbo.AssetMaster a ON src.AssetId = a.Id
GROUP BY 
    CASE 
        WHEN src.RecordType = 'Expense' THEN e.expense_type
        WHEN src.RecordType = 'AssetService' THEN 'Asset Service'
        WHEN src.RecordType = 'AssetRental' THEN 'Asset Rental'
    END,
    CASE 
        WHEN src.RecordType = 'Expense' THEN e.expense_subtype
        WHEN src.RecordType = 'AssetService' THEN a.Name
        WHEN src.RecordType = 'AssetRental' THEN a.Name
    END
ORDER BY ExpenseType, ExpenseSubType;
";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@office_id", officeId);
                    cmd.Parameters.AddWithValue("@date_from", dateFrom);
                    cmd.Parameters.AddWithValue("@date_to", dateTo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new
                            {
                                ExpenseType = reader["ExpenseType"].ToString(),
                                ExpenseSubType = reader["ExpenseSubType"].ToString(),
                                TotalAmount = reader["TotalAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalAmount"])
                            });
                        }
                    }
                
                }
            }

            return Ok(new { result });
        }

        // ---------------------------
        // HELPER MAPPERS
        // ---------------------------

        private ExpenseMaster MapReaderToExpense(SqlDataReader reader)
        {
            return new ExpenseMaster
            {
                Id = Convert.ToInt32(reader["id"]),
                ExpenseType = reader["expense_type"].ToString(),
                ExpenseSubtype = reader["expense_subtype"] == DBNull.Value ? null : reader["expense_subtype"].ToString(),
                DateFrom = Convert.ToDateTime(reader["date_from"]),
                DateTo = Convert.ToDateTime(reader["date_to"]),
                Amount = Convert.ToDecimal(reader["amount"]),
                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                BillImage = reader["bill_image"] == DBNull.Value ? null : (byte[])reader["bill_image"],
                OfficeId = Convert.ToInt32(reader["office_id"]),
                IsActive = Convert.ToBoolean(reader["is_active"]),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                CreatedOn = Convert.ToDateTime(reader["created_on"]),
                UpdatedBy = reader["updated_by"] == DBNull.Value ? null : (int?)reader["updated_by"],
                UpdatedOn = reader["updated_on"] == DBNull.Value ? null : (DateTime?)reader["updated_on"]
            };
        }

        private ExpenseType MapReaderToExpenseType(SqlDataReader reader)
        {
            return new ExpenseType
            {
                ExpenseId = Convert.ToInt32(reader["expense_id"]),
                ExpenseTypeName = reader["expense_type"].ToString(),
                ExpenseSubtype = reader["expense_subtype"] == DBNull.Value ? null : reader["expense_subtype"].ToString(),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                OfficeId = Convert.ToInt32(reader["office_id"]),
                IsActive = Convert.ToBoolean(reader["is_active"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                UpdatedBy = reader["updated_by"] == DBNull.Value ? null : (int?)reader["updated_by"],
                UpdatedAt = reader["updated_at"] == DBNull.Value ? null : (DateTime?)reader["updated_at"]
            };
        }

    }
}
