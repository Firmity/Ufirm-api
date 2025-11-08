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

        // ✅ GET all active items by propertyId (with details)
        [HttpGet]
        [Route("getAll/propertyId/{propertyId:int}")]
        public async Task<IHttpActionResult> GetAll(int propertyId)
        {
            var items = new List<ItemSpecification>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                string query = @"SELECT * FROM app.ItemSpecifications 
                                 WHERE propertyId = @propertyId AND is_active = 1";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@propertyId", propertyId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new ItemSpecification
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                ItemId = reader["item_id"] != DBNull.Value ? Convert.ToInt32(reader["item_id"]) : 0,
                                Item_Name = reader["item_name"] != DBNull.Value ? reader["item_name"].ToString() : null,
                                Gender = reader["gender"] != DBNull.Value ? reader["gender"].ToString() : null,
                                Quantity = reader["quantity"] != DBNull.Value ? Convert.ToInt32(reader["quantity"]) : 0,
                                PropertyId = reader["propertyId"] != DBNull.Value ? Convert.ToInt32(reader["propertyId"]) : 0,
                                IsRequisition = reader["isRequisition"] != DBNull.Value && Convert.ToBoolean(reader["isRequisition"]),
                                IsHandover = reader["isHandover"] != DBNull.Value && Convert.ToBoolean(reader["isHandover"]),
                                Is_Active = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"]),
                                Created_On = reader["created_on"] != DBNull.Value ? Convert.ToDateTime(reader["created_on"]) : DateTime.MinValue,
                                Updated_On = reader["updated_on"] != DBNull.Value ? Convert.ToDateTime(reader["updated_on"]) : DateTime.MinValue
                            });

                        }
                    }
                }

                // Fetch active details for each item
                foreach (var item in items)
                {
                    item.Details = new List<ItemSpecificationDetail>();

                    string detailsQuery = @"SELECT * FROM app.ItemSpecificationDetails 
                                            WHERE item_specification_id = @id AND is_active = 1";
                    using (var cmdDetails = new SqlCommand(detailsQuery, conn))
                    {
                        cmdDetails.Parameters.AddWithValue("@id", item.Id);
                        using (var readerDetails = await cmdDetails.ExecuteReaderAsync())
                        {
                            while (await readerDetails.ReadAsync())
                            {
                                item.Details.Add(new ItemSpecificationDetail
                                {
                                    Id = Convert.ToInt32(readerDetails["id"]),
                                    Item_Specification_Id = item.Id,
                                    Specification_Name = readerDetails["specification_name"].ToString(),
                                    Specification_Value = readerDetails["specification_value"].ToString(),
                                    Is_Active = Convert.ToBoolean(readerDetails["is_active"])
                                });
                            }
                        }
                    }
                }
            }

            if (items.Count == 0)
                return Content(HttpStatusCode.NotFound, "No active items found for the given propertyId");

            return Ok(items);
        }

        // ✅ GET by ID (with details)
        [HttpGet]
        [Route("getById/{id:int}")]
        public async Task<IHttpActionResult> GetById(int id)
        {
            ItemSpecification item = null;

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                string query = @"SELECT * FROM app.ItemSpecifications 
                                 WHERE id = @id AND is_active = 1";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            item = new ItemSpecification
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                ItemId = reader["item_id"] != DBNull.Value ? Convert.ToInt32(reader["item_id"]) : 0,
                                Item_Name = reader["item_name"] != DBNull.Value ? reader["item_name"].ToString() : null,
                                Gender = reader["gender"] != DBNull.Value ? reader["gender"].ToString() : null,
                                Quantity = reader["quantity"] != DBNull.Value ? Convert.ToInt32(reader["quantity"]) : 0,
                                PropertyId = reader["propertyId"] != DBNull.Value ? Convert.ToInt32(reader["propertyId"]) : 0,
                                IsRequisition = reader["isRequisition"] != DBNull.Value && Convert.ToBoolean(reader["isRequisition"]),
                                IsHandover = reader["isHandover"] != DBNull.Value && Convert.ToBoolean(reader["isHandover"]),
                                Is_Active = reader["is_active"] != DBNull.Value && Convert.ToBoolean(reader["is_active"]),
                                Created_On = reader["created_on"] != DBNull.Value ? Convert.ToDateTime(reader["created_on"]) : DateTime.MinValue,
                                Updated_On = reader["updated_on"] != DBNull.Value ? Convert.ToDateTime(reader["updated_on"]) : DateTime.MinValue
                            };

                        }
                    }
                }

                if (item == null)
                    return NotFound();

                string detailsQuery = @"SELECT * FROM app.ItemSpecificationDetails 
                                        WHERE item_specification_id = @id AND is_active = 1";
                using (var cmdDetails = new SqlCommand(detailsQuery, conn))
                {
                    cmdDetails.Parameters.AddWithValue("@id", id);
                    using (var readerDetails = await cmdDetails.ExecuteReaderAsync())
                    {
                        while (await readerDetails.ReadAsync())
                        {
                            item.Details.Add(new ItemSpecificationDetail
                            {
                                Id = Convert.ToInt32(readerDetails["id"]),
                                Item_Specification_Id = id,
                                Specification_Name = readerDetails["specification_name"].ToString(),
                                Specification_Value = readerDetails["specification_value"].ToString(),
                                Is_Active = Convert.ToBoolean(readerDetails["is_active"])
                            });
                        }
                    }
                }
            }

            return Ok(item);
        }

        // ✅ POST multiple (main + details)
        [HttpPost]
        [Route("createMultiple")]
        public async Task<IHttpActionResult> CreateMultiple([FromBody] List<ItemSpecification> models)
        {
            if (models == null || models.Count == 0)
                return BadRequest("No data provided");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string mainInsertQuery = @"
                            INSERT INTO app.ItemSpecifications 
                            (item_id,item_name, gender, quantity, propertyId, isRequisition, isHandover, is_active, created_on, updated_on)
                            VALUES (@item_id,@item_name, @gender, @quantity, @propertyId, @isRequisition, @isHandover, 1, GETDATE(), GETDATE());
                            SELECT SCOPE_IDENTITY();";

                        string detailInsertQuery = @"
                            INSERT INTO app.ItemSpecificationDetails
                            (item_specification_id, specification_name, specification_value, is_active)
                            VALUES (@item_specification_id, @specification_name, @specification_value, 1);";

                        var insertedIds = new List<int>();

                        foreach (var model in models)
                        {
                            int newId;
                            using (var cmd = new SqlCommand(mainInsertQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@item_id", model.ItemId);
                                cmd.Parameters.AddWithValue("@item_name", model.Item_Name ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@gender", model.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@quantity", model.Quantity);
                                cmd.Parameters.AddWithValue("@propertyId", model.PropertyId);
                                cmd.Parameters.AddWithValue("@isRequisition", model.IsRequisition);
                                cmd.Parameters.AddWithValue("@isHandover", model.IsHandover);

                                newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                insertedIds.Add(newId);
                            }

                            if (model.Details != null)
                            {
                                foreach (var detail in model.Details)
                                {
                                    using (var cmdDetail = new SqlCommand(detailInsertQuery, conn, transaction))
                                    {
                                        cmdDetail.Parameters.AddWithValue("@item_specification_id", newId);
                                        cmdDetail.Parameters.AddWithValue("@specification_name", detail.Specification_Name);
                                        cmdDetail.Parameters.AddWithValue("@specification_value", detail.Specification_Value);
                                        await cmdDetail.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = "Records added successfully", ids = insertedIds });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        // ✅ PUT (update + details)
        [HttpPut]
        [Route("update/{id:int}")]
        public async Task<IHttpActionResult> Update(int id, [FromBody] ItemSpecification model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string updateMain = @"
                            UPDATE app.ItemSpecifications
                            SET item_id= @item_id,
                                item_name = @item_name,
                                gender = @gender,
                                quantity = @quantity,
                                propertyId = @propertyId,
                                isRequisition = @isRequisition,
                                isHandover = @isHandover,
                                is_active = @is_active,
                                updated_on = GETDATE()
                            WHERE id = @id";

                        using (var cmd = new SqlCommand(updateMain, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@item_id", model.ItemId);
                            cmd.Parameters.AddWithValue("@item_name", model.Item_Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@gender", model.Gender ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@quantity", model.Quantity);
                            cmd.Parameters.AddWithValue("@propertyId", model.PropertyId);
                            cmd.Parameters.AddWithValue("@isRequisition", model.IsRequisition);
                            cmd.Parameters.AddWithValue("@isHandover", model.IsHandover);
                            cmd.Parameters.AddWithValue("@is_active", model.Is_Active);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete existing details (soft delete)
                        string softDeleteDetails = @"UPDATE app.ItemSpecificationDetails 
                                                     SET is_active = 0 
                                                     WHERE item_specification_id = @id";
                        using (var cmdDelete = new SqlCommand(softDeleteDetails, conn, transaction))
                        {
                            cmdDelete.Parameters.AddWithValue("@id", id);
                            await cmdDelete.ExecuteNonQueryAsync();
                        }

                        // Insert/Update new details
                        if (model.Details != null)
                        {
                            string insertDetail = @"
                                INSERT INTO app.ItemSpecificationDetails
                                (item_specification_id, specification_name, specification_value, is_active)
                                VALUES (@item_specification_id, @specification_name, @specification_value, 1);";

                            foreach (var detail in model.Details)
                            {
                                using (var cmdDetail = new SqlCommand(insertDetail, conn, transaction))
                                {
                                    cmdDetail.Parameters.AddWithValue("@item_specification_id", id);
                                    cmdDetail.Parameters.AddWithValue("@specification_name", detail.Specification_Name);
                                    cmdDetail.Parameters.AddWithValue("@specification_value", detail.Specification_Value);
                                    await cmdDetail.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = "Record updated successfully" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }

        // ✅ SOFT DELETE main + details (set is_active = 0)
        [HttpDelete]
        [Route("delete/{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string deactivateDetails = "UPDATE app.ItemSpecificationDetails SET is_active = 0 WHERE item_specification_id = @id";
                        string deactivateMain = "UPDATE app.ItemSpecifications SET is_active = 0 WHERE id = @id";

                        using (var cmd1 = new SqlCommand(deactivateDetails, conn, transaction))
                        {
                            cmd1.Parameters.AddWithValue("@id", id);
                            await cmd1.ExecuteNonQueryAsync();
                        }

                        using (var cmd2 = new SqlCommand(deactivateMain, conn, transaction))
                        {
                            cmd2.Parameters.AddWithValue("@id", id);
                            int rows = await cmd2.ExecuteNonQueryAsync();
                            if (rows == 0)
                            {
                                transaction.Rollback();
                                return Content(HttpStatusCode.NotFound, "Item not found");
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = "Record deactivated successfully" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(ex);
                    }
                }
            }
        }
    }
}
