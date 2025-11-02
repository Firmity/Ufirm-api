using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/itemspecifications")]
    public class ItemSpecificationsController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ✅ GET: api/itemspecifications/getAll
        [HttpGet]
        [Route("getAll/propertyId/{propertyId:int}")]
        public async Task<IHttpActionResult> GetAll(int propertyId)
        {
            var items = new List<ItemSpecification>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                string query = @"SELECT id, item_name, gender, quantity, specification_id, 
                                specification_value, propertyId, created_on, updated_on, employee_id
                         FROM app.ItemSpecifications
                         WHERE propertyId = @propertyId";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@propertyId", propertyId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new ItemSpecification
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Item_Name = reader["item_name"].ToString(),
                                Gender = reader["gender"].ToString(),
                                Quantity = Convert.ToInt32(reader["quantity"]),
                                Specification_Id = Convert.ToInt32(reader["specification_id"]),
                                Specification_Value = Convert.ToInt32(reader["specification_value"]),
                                PropertyId = Convert.ToInt32(reader["propertyId"]),
                                Created_On = Convert.ToDateTime(reader["created_on"]),
                                Updated_On = Convert.ToDateTime(reader["updated_on"]),
                                Employee_Id = Convert.ToInt32(reader["employee_id"])
                            });
                        }
                    }
                }
            }

            if (items.Count == 0)
                return Content(HttpStatusCode.NotFound, "No items found for the given propertyId");

            return Ok(items);
        }

        [HttpGet]
        [Route("getAll/EmployeeId/{employee_id:int}")]
        public async Task<IHttpActionResult> GetAllEmployee(int employee_id)
        {
            var items = new List<ItemSpecification>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                string query = @"SELECT id, item_name, gender, quantity, specification_id, 
                                specification_value, propertyId, created_on, updated_on, employee_id
                         FROM app.ItemSpecifications
                         WHERE employee_id = @employee_id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@employee_id", employee_id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new ItemSpecification
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Item_Name = reader["item_name"].ToString(),
                                Gender = reader["gender"].ToString(),
                                Quantity = Convert.ToInt32(reader["quantity"]),
                                Specification_Id = Convert.ToInt32(reader["specification_id"]),
                                Specification_Value = Convert.ToInt32(reader["specification_value"]),
                                PropertyId = Convert.ToInt32(reader["propertyId"]),
                                Created_On = Convert.ToDateTime(reader["created_on"]),
                                Updated_On = Convert.ToDateTime(reader["updated_on"]),
                                Employee_Id = Convert.ToInt32(reader["employee_id"])
                            });
                        }
                    }
                }
            }

            if (items.Count == 0)
                return Content(HttpStatusCode.NotFound, "No items found for the given propertyId");

            return Ok(items);
        }

        // ✅ GET by ID: api/itemspecifications/getById/{id}
        [HttpGet]
        [Route("getById/{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            ItemSpecification item = null;

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"SELECT * FROM app.ItemSpecifications WHERE id = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            item = new ItemSpecification
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Item_Name = reader["item_name"].ToString(),
                                Gender = reader["gender"].ToString(),
                                Quantity = Convert.ToInt32(reader["quantity"]),
                                Specification_Id = Convert.ToInt32(reader["specification_id"]),
                                Specification_Value = Convert.ToInt32(reader["specification_value"]),
                                PropertyId = Convert.ToInt32(reader["propertyId"]),
                                Created_On = Convert.ToDateTime(reader["created_on"]),
                                Updated_On = Convert.ToDateTime(reader["updated_on"]),
                                Employee_Id = Convert.ToInt32(reader["employee_id"])
                            };
                        }
                    }
                }
            }

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        // ✅ POST: api/itemspecifications/create
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> Create([FromBody] ItemSpecification model)
        {
            if (model == null || string.IsNullOrEmpty(model.Item_Name))
                return BadRequest("Invalid data");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"
                    INSERT INTO app.ItemSpecifications 
                        (item_name, gender, quantity, specification_id, specification_value, propertyId, created_on, updated_on, employee_id)
                    VALUES 
                        (@item_name, @gender, @quantity, @specification_id, @specification_value, @propertyId, GETDATE(), GETDATE(), @employee_id);
                    SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item_name", model.Item_Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", model.Gender ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@specification_id", model.Specification_Id);
                    cmd.Parameters.AddWithValue("@specification_value", model.Specification_Value);
                    cmd.Parameters.AddWithValue("@propertyId", model.PropertyId);
                    cmd.Parameters.AddWithValue("@employee_id", model.Employee_Id);

                    int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new { message = "Record added successfully", id = newId });
                }
            }
        }

        // ✅ PUT: api/itemspecifications/update/{id}
        [HttpPut]
        [Route("update/{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] ItemSpecification model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"
                    UPDATE app.ItemSpecifications
                    SET item_name = @item_name,
                        gender = @gender,
                        quantity = @quantity,
                        specification_id = @specification_id,
                        specification_value = @specification_value,
                        employee_id = @employee_id,
                        updated_on = GETDATE()
                    WHERE id = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@item_name", model.Item_Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", model.Gender ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@specification_id", model.Specification_Id);
                    cmd.Parameters.AddWithValue("@specification_value", model.Specification_Value);
                    cmd.Parameters.AddWithValue("@employee_id", model.Employee_Id);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                        return Content(HttpStatusCode.NotFound, "Item not found");

                    return Ok(new { message = "Record updated successfully" });
                }
            }
        }

        // ✅ DELETE: api/itemspecifications/delete/{id}
        [HttpDelete]
        [Route("delete/{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = "DELETE FROM app.ItemSpecifications WHERE id = @id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                        return Content(HttpStatusCode.NotFound, "Item not found");

                    return Ok(new { message = "Record deleted successfully" });
                }
            }
        }
    }
}
