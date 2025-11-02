using System;
using System.Collections.Generic;
using System.Configuration;
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

        // ✅ GET: api/itemspecifications/getAll/propertyId/{propertyId}
        [HttpGet]
        [Route("getAll/propertyId/{propertyId:int}")]
        public async Task<IHttpActionResult> GetAll(int propertyId)
        {
            var items = new List<ItemSpecification>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"SELECT id, item_name, gender, quantity, specification_id, specification_value, 
                                        propertyId, created_on, updated_on, property_id, isRequisition, isHandover
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
                                Property_Id = Convert.ToInt32(reader["property_id"]),
                                IsRequisition = Convert.ToBoolean(reader["isRequisition"]),
                                IsHandover = Convert.ToBoolean(reader["isHandover"])
                            });
                        }
                    }
                }
            }

            if (items.Count == 0)
                return Content(HttpStatusCode.NotFound, "No items found for the given propertyId");

            return Ok(items);
        }

        // ✅ GET by ID
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
                                Property_Id = Convert.ToInt32(reader["property_id"]),
                                IsRequisition = Convert.ToBoolean(reader["isRequisition"]),
                                IsHandover = Convert.ToBoolean(reader["isHandover"])
                            };
                        }
                    }
                }
            }

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpGet]
        [Route("getGroupedByItem/propertyId/{propertyId:int}")]
        public async Task<IHttpActionResult> GetGroupedByItem(int propertyId)
        {
            var groupedItems = new List<object>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                string query = @"
            SELECT 
                item_name,
                MAX(gender) AS gender,
                SUM(quantity) AS total_quantity,
                MAX(created_on) AS last_created,
                MAX(updated_on) AS last_updated,
                MAX(specification_id) AS specification_id,
                MAX(specification_value) AS specification_value,
                MAX(isRequisition) AS isRequisition,
                MAX(isHandover) AS isHandover
            FROM app.ItemSpecifications
            WHERE propertyId = @propertyId
            GROUP BY item_name
            ORDER BY item_name;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@propertyId", propertyId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            groupedItems.Add(new
                            {
                                Item_Name = reader["item_name"].ToString(),
                                Gender = reader["gender"].ToString(),
                                Total_Quantity = Convert.ToInt32(reader["total_quantity"]),
                                Specification_Id = Convert.ToInt32(reader["specification_id"]),
                                Specification_Value = Convert.ToInt32(reader["specification_value"]),
                                IsRequisition = Convert.ToBoolean(reader["isRequisition"]),
                                IsHandover = Convert.ToBoolean(reader["isHandover"]),
                                Last_Created = Convert.ToDateTime(reader["last_created"]),
                                Last_Updated = Convert.ToDateTime(reader["last_updated"])
                            });
                        }
                    }
                }
            }

            if (groupedItems.Count == 0)
                return Content(HttpStatusCode.NotFound, "No grouped items found for the given propertyId");

            return Ok(groupedItems);
        }



        // ✅ POST
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
                        (item_name, gender, quantity, specification_id, specification_value, 
                         propertyId, created_on, updated_on, property_id, isRequisition, isHandover)
                    VALUES 
                        (@item_name, @gender, @quantity, @specification_id, @specification_value, 
                         @propertyId, GETDATE(), GETDATE(), @property_id, @isRequisition, @isHandover);
                    SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@item_name", model.Item_Name ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@gender", model.Gender ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@quantity", model.Quantity);
                    cmd.Parameters.AddWithValue("@specification_id", model.Specification_Id);
                    cmd.Parameters.AddWithValue("@specification_value", model.Specification_Value);
                    cmd.Parameters.AddWithValue("@propertyId", model.PropertyId);
                    cmd.Parameters.AddWithValue("@property_id", model.Property_Id);
                    cmd.Parameters.AddWithValue("@isRequisition", model.IsRequisition);
                    cmd.Parameters.AddWithValue("@isHandover", model.IsHandover);

                    int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new { message = "Record added successfully", id = newId });
                }
            }
        }

        // ✅ PUT
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
                        property_id = @property_id,
                        isRequisition = @isRequisition,
                        isHandover = @isHandover,
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
                    cmd.Parameters.AddWithValue("@property_id", model.Property_Id);
                    cmd.Parameters.AddWithValue("@isRequisition", model.IsRequisition);
                    cmd.Parameters.AddWithValue("@isHandover", model.IsHandover);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                        return Content(HttpStatusCode.NotFound, "Item not found");

                    return Ok(new { message = "Record updated successfully" });
                }
            }
        }

        // ✅ DELETE
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
