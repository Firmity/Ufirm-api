using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Inventory.Controllers
{
    //[ApiExplorerSettings(SettingsGroupNameAttribute = "Inventory")]
    [RoutePrefix("api/inventory")]
    public class InventoryController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpGet]
        [Route("categories")]
        public async Task<IHttpActionResult> GetAllCategories([FromUri] int? propertyId)
        {
            if (!propertyId.HasValue)
            {
                return BadRequest("PropertyId is required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = "SELECT Id, PropertyId, Name, Description, IsActive, IsApproved, CreatedOn, CreatedBy " +
                                   "FROM Inventory.Category " +
                                   "WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();

                    var reader = await cmd.ExecuteReaderAsync();
                    var categories = new List<CategoryDto>();

                    while (await reader.ReadAsync())
                    {
                        categories.Add(new CategoryDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4),
                            IsApproved = reader.GetBoolean(5),
                            CreatedOn = reader.GetDateTime(6),
                            CreatedBy = reader.GetInt32(7)
                        });
                    }

                    if (categories.Count == 0)
                    {
                        return NotFound(); // If no categories found, return 404
                    }

                    return Ok(categories); // Return the list of categories
                }
            }
            catch (SqlException ex)
            {
                // Handle SQL-related errors (e.g., connection issues, query errors)
                return InternalServerError(new Exception("Database error occurred: " + ex.Message));
            }
            catch (Exception ex)
            {
                // Handle general errors (e.g., unexpected issues)
                return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("category/{id}")]
        public async Task<IHttpActionResult> GetCategoryById(int id)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"SELECT Id, PropertyId, Name, Description, IsActive, IsApproved, CreatedOn, CreatedBy 
                         FROM Inventory.Category 
                         WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                await conn.OpenAsync();

                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var category = new CategoryDto
                    {
                        Id = reader.GetInt32(0),
                        PropertyId = reader.GetInt32(1),
                        Name = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                        IsActive = reader.GetBoolean(4),
                        IsApproved = reader.GetBoolean(5),
                        CreatedOn = reader.GetDateTime(6),
                        CreatedBy = reader.GetInt32(7)
                    };

                    return Ok(category); // Return the category data
                }
                else
                {
                    return NotFound(); // Return 404 if category with the given Id is not found
                }
            }
        }

        [HttpPost]
        [Route("category")]
        public async Task<IHttpActionResult> CreateCategory([FromBody] CategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid category data.");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"INSERT INTO Inventory.Category 
                             (PropertyId, Name, Description, CreatedBy, CreatedOn, IsActive, IsApproved)
                             VALUES 
                             (@PropertyId, @Name, @Description, @CreatedBy, GETDATE(), 1, 0);
                             SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PropertyId", dto.PropertyId);
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

                await conn.OpenAsync();
                var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return Ok(new { message = "Category created successfully", Id = insertedId });
            }
        }

        [HttpPut]
        [Route("category/{id}")]
        public async Task<IHttpActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid category data.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Category 
                             SET Name = @Name, 
                                 Description = @Description, 
                                 IsActive = @IsActive, 
                                 IsApproved = @IsApproved, 
                                 ApprovedBy = @ApprovedBy 
                             WHERE Id = @Id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@IsApproved", dto.IsApproved);
                    cmd.Parameters.AddWithValue("@ApprovedBy", (object)dto.ApprovedBy ?? DBNull.Value);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                        return NotFound(); // No record with given Id

                    return Ok(new { message = "Category updated successfully" });
                }
            }
            catch (SqlException ex)
            {
                return InternalServerError(new Exception("Database error occurred: " + ex.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("category/{id}")]
        public async Task<IHttpActionResult> DeleteCategory(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Category 
                             SET IsActive = 0 
                             WHERE Id = @Id AND IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                        return NotFound(); // No active record found with given Id

                    return Ok(new { message = "Category deleted (soft) successfully" });
                }
            }
            catch (SqlException ex)
            {
                return InternalServerError(new Exception("Database error occurred: " + ex.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("categories/pendingApproval")]
        public async Task<IHttpActionResult> GetPendingApprovalCategories([FromUri] int? propertyId)
        {
            if (!propertyId.HasValue)
            {
                return BadRequest("PropertyId is required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"SELECT Id, PropertyId, Name, Description, IsActive, IsApproved, CreatedOn, CreatedBy
                             FROM Inventory.Category
                             WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 0";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();

                    var reader = await cmd.ExecuteReaderAsync();
                    var pendingCategories = new List<CategoryDto>();

                    while (await reader.ReadAsync())
                    {
                        pendingCategories.Add(new CategoryDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                            IsActive = reader.GetBoolean(4),
                            IsApproved = reader.GetBoolean(5),
                            CreatedOn = reader.GetDateTime(6),
                            CreatedBy = reader.GetInt32(7)
                        });
                    }
                   return Ok(pendingCategories); // Return the pending list
                }
            }
            catch (SqlException ex)
            {
                return InternalServerError(new Exception("Database error occurred: " + ex.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("items")]
        public async Task<IHttpActionResult> GetAllItems([FromUri] int? propertyId, [FromUri] int? categoryId = null)
        {
            if (!propertyId.HasValue)
                return BadRequest("PropertyId is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    var query = @"SELECT Id, PropertyId, CategoryId, Name, Description, MeasurementUnit, MinStockLevel,
                                 IsActive, IsApproved, ApprovedBy, CreatedOn, CreatedBy, BrandName, HSNCode
                          FROM Inventory.Item
                          WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 1";

                    if (categoryId.HasValue)
                        query += " AND CategoryId = @CategoryId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId.Value);

                    if (categoryId.HasValue)
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    var items = new List<ItemDto>();

                    while (await reader.ReadAsync())
                    {
                        items.Add(new ItemDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            CategoryId = reader.GetInt32(2),
                            Name = reader.GetString(3),
                            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                            MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                            MinStockLevel = reader.GetInt32(6),
                            IsActive = reader.GetBoolean(7),
                            IsApproved = reader.GetBoolean(8),
                            ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                            CreatedOn = reader.GetDateTime(10),
                            CreatedBy = reader.GetInt32(11),
                            BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                            HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                        });
                    }

                    return Ok(items);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching items: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("item/{id}")]
        public async Task<IHttpActionResult> GetItemById(int id)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"SELECT Id, PropertyId, CategoryId, Name, Description, MeasurementUnit, MinStockLevel,
                                IsActive, IsApproved, ApprovedBy, CreatedOn, CreatedBy ,BrandName,HSNCode
                         FROM Inventory.Item 
                         WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    var item = new ItemDto
                    {
                        Id = reader.GetInt32(0),
                        PropertyId = reader.GetInt32(1),
                        CategoryId = reader.GetInt32(2),
                        Name = reader.GetString(3),
                        Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                        MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                        MinStockLevel = reader.GetInt32(6),
                        IsActive = reader.GetBoolean(7),
                        IsApproved = reader.GetBoolean(8),
                        ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                        CreatedOn = reader.GetDateTime(10),
                        CreatedBy = reader.GetInt32(11),
                        BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                        HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                    };

                    return Ok(item);
                }

                return NotFound();
            }
        }

        [HttpPost]
        [Route("item")]
        public async Task<IHttpActionResult> CreateItem([FromBody] ItemDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid item data.");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"INSERT INTO Inventory.Item 
                        (PropertyId, CategoryId, Name, Description, MeasurementUnit, MinStockLevel, CreatedBy, CreatedOn, IsActive, IsApproved,BrandName,HSNCode)
                         VALUES 
                        (@PropertyId, @CategoryId, @Name, @Description, @MeasurementUnit, @MinStockLevel, @CreatedBy, GETDATE(), 1, 0,@BrandName,@HSNCode);
                         SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PropertyId", dto.PropertyId);
                cmd.Parameters.AddWithValue("@CategoryId", dto.CategoryId);
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MeasurementUnit", (object)dto.MeasurementUnit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MinStockLevel", dto.MinStockLevel);
                cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);
                cmd.Parameters.AddWithValue("@BrandName", (object)dto.BrandName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@HSNCode", (object)dto.HSNCode ?? DBNull.Value);

                await conn.OpenAsync();
                var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                return Ok(new { message = "Item created successfully", Id = insertedId });
            }
        }

        [HttpPut]
        [Route("item/{id}")]
        public async Task<IHttpActionResult> UpdateItem(int id, [FromBody] ItemDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid item data.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Item 
                             SET Name = @Name, 
                                 Description = @Description, 
                                 CategoryId = @CategoryId,
                                 MeasurementUnit = @MeasurementUnit,
                                 MinStockLevel = @MinStockLevel,
                                 IsActive = @IsActive, 
                                 IsApproved = @IsApproved, 
                                 ApprovedBy = @ApprovedBy ,
                                 BrandName = @BrandName ,
                                 HSNCode = @HSNCode
                             WHERE Id = @Id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@Description", (object)dto.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryId", dto.CategoryId);
                    cmd.Parameters.AddWithValue("@MeasurementUnit", (object)dto.MeasurementUnit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MinStockLevel", dto.MinStockLevel);
                    cmd.Parameters.AddWithValue("@IsActive", dto.IsActive);
                    cmd.Parameters.AddWithValue("@IsApproved", dto.IsApproved);
                    cmd.Parameters.AddWithValue("@ApprovedBy", (object)dto.ApprovedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@BrandName", (object)dto.BrandName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@HSNCode", (object)dto.HSNCode ?? DBNull.Value);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                        return NotFound();

                    return Ok(new { message = "Item updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error updating item: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("item/{id}")]
        public async Task<IHttpActionResult> DeleteItem(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Item 
                             SET IsActive = 0 
                             WHERE Id = @Id AND IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected == 0)
                        return NotFound();

                    return Ok(new { message = "Item deleted (soft) successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error deleting item: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("items/pendingApproval")]
        public async Task<IHttpActionResult> GetUnapprovedItems([FromUri] int? propertyId)
        {
            if (!propertyId.HasValue)
                return BadRequest("PropertyId is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"SELECT Id, PropertyId, CategoryId, Name, Description, MeasurementUnit, MinStockLevel,
                            IsActive, IsApproved, ApprovedBy, CreatedOn, CreatedBy ,BrandName,HSNCode
                     FROM Inventory.Item 
                     WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 0";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    var items = new List<ItemDto>();

                    while (await reader.ReadAsync())
                    {
                        items.Add(new ItemDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            CategoryId = reader.GetInt32(2),
                            Name = reader.GetString(3),
                            Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                            MeasurementUnit = reader.IsDBNull(5) ? null : reader.GetString(5),
                            MinStockLevel = reader.GetInt32(6),
                            IsActive = reader.GetBoolean(7),
                            IsApproved = reader.GetBoolean(8),
                            ApprovedBy = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9),
                            CreatedOn = reader.GetDateTime(10),
                            CreatedBy = reader.GetInt32(11),
                            BrandName = reader.IsDBNull(12) ? null : reader.GetString(12),
                            HSNCode = reader.IsDBNull(13) ? null : reader.GetString(13)
                        });
                    }

                    return Ok(items);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching unapproved items: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("vendor")]
        public async Task<IHttpActionResult> CreateVendor([FromBody] VendorDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid vendor data.");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"
            INSERT INTO Inventory.Vendor 
                (PropertyId, Name, ContactPerson, ContactNumber, Email, Address, GSTNumber, PANNumber, 
                 CreatedBy, CreatedOn, IsActive, IsApproved)
            VALUES 
                (@PropertyId, @Name, @ContactPerson, @ContactNumber, @Email, @Address, @GSTNumber, @PANNumber, 
                 @CreatedBy, GETDATE(), 1, 0);
            SELECT SCOPE_IDENTITY();";

                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@PropertyId", dto.PropertyId);
                cmd.Parameters.AddWithValue("@Name", dto.Name);
                cmd.Parameters.AddWithValue("@ContactPerson", (object)dto.ContactPerson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ContactNumber", (object)dto.ContactNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object)dto.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", (object)dto.GSTNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PANNumber", (object)dto.PANNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

                try
                {
                    await conn.OpenAsync();
                    var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return Ok(new { message = "Vendor created successfully", Id = insertedId });
                }
                catch (SqlException ex)
                {
                    return InternalServerError(new Exception("Database error occurred: " + ex.Message));
                }
                catch (Exception ex)
                {
                    return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
                }
            }
        }

        [HttpPost]
        [Route("vendor1")]
        public async Task<IHttpActionResult> CreateVendor1()
        {
            if (!Request.Content.IsMimeMultipartContent())
                return BadRequest("Unsupported media type.");

            var root = HttpContext.Current.Server.MapPath("~/Uploads/Vendors");
            Directory.CreateDirectory(root);

            var provider = new MultipartFormDataStreamProvider(root);
            await Request.Content.ReadAsMultipartAsync(provider);

            var form = provider.FormData;

            // Parse DTO manually
            var dto = new VendorDto
            {
                PropertyId = int.Parse(form["PropertyId"]),
                Name = form["Name"],
                ContactPerson = form["ContactPerson"],
                ContactNumber = form["ContactNumber"],
                Email = form["Email"],
                Address = form["Address"],
                GSTNumber = form["GSTNumber"],
                PANNumber = form["PANNumber"],
                CreatedBy = int.Parse(form["CreatedBy"]),
            };

            using (SqlConnection conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Step 1: Insert Vendor
                    string insertQuery = @"
                INSERT INTO Inventory.Vendor 
                    (PropertyId, Name, ContactPerson, ContactNumber, Email, Address, GSTNumber, PANNumber,
                     CreatedBy, CreatedOn, IsActive, IsApproved)
                VALUES 
                    (@PropertyId, @Name, @ContactPerson, @ContactNumber, @Email, @Address, @GSTNumber, @PANNumber,
                     @CreatedBy, GETDATE(), 1, 0);
                SELECT SCOPE_IDENTITY();";

                    SqlCommand insertCmd = new SqlCommand(insertQuery, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@PropertyId", dto.PropertyId);
                    insertCmd.Parameters.AddWithValue("@Name", dto.Name);
                    insertCmd.Parameters.AddWithValue("@ContactPerson", (object)dto.ContactPerson ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@ContactNumber", (object)dto.ContactNumber ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Address", (object)dto.Address ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@GSTNumber", (object)dto.GSTNumber ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@PANNumber", (object)dto.PANNumber ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

                    var insertedId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    // Step 2: Process Files
                    string vendorFolder = Path.Combine(root, insertedId.ToString());
                    Directory.CreateDirectory(vendorFolder);

                    foreach (var file in provider.FileData)
                    {
                        var fieldName = file.Headers.ContentDisposition.Name?.Trim('"').ToLower(); // e.g. "pan", "gst", "brochure"
                        var originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
                        var extension = Path.GetExtension(originalFileName);
                        var destFileName = "";

                        switch (fieldName)
                        {
                            case "pan":
                                destFileName = "PAN" + extension;
                                dto.PANFileUrl = $"/Uploads/Vendors/{insertedId}/{destFileName}";
                                break;
                            case "gst":
                                destFileName = "GSTCertificate" + extension;
                                dto.GSTCertificateUrl = $"/Uploads/Vendors/{insertedId}/{destFileName}";
                                break;
                            case "brochure":
                                destFileName = "Brochure" + extension;
                                dto.BrochureUrl = $"/Uploads/Vendors/{insertedId}/{destFileName}";
                                break;
                            default:
                                continue; // skip unknown files
                        }

                        File.Move(file.LocalFileName, Path.Combine(vendorFolder, destFileName));
                    }

                    // Optional website URL from form
                    dto.WebsiteUrl = form["WebsiteUrl"];

                    // Step 3: Update with file URLs
                    string updateQuery = @"
                UPDATE Inventory.Vendor 
                SET 
                    PANFileUrl = @PANFileUrl,
                    GSTCertificateUrl = @GSTCertificateUrl,
                    BrochureUrl = @BrochureUrl,
                    WebsiteUrl = @WebsiteUrl
                WHERE Id = @Id";

                    SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@PANFileUrl", (object)dto.PANFileUrl ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@GSTCertificateUrl", (object)dto.GSTCertificateUrl ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@BrochureUrl", (object)dto.BrochureUrl ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@WebsiteUrl", (object)dto.WebsiteUrl ?? DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@Id", insertedId);

                    await updateCmd.ExecuteNonQueryAsync();

                    transaction.Commit();

                    return Ok(new { message = "Vendor created successfully", Id = insertedId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return InternalServerError(new Exception("Error occurred while creating vendor: " + ex.Message));
                }
            }
        }

        [HttpGet]
        [Route("vendors")]
        public async Task<IHttpActionResult> GetAllVendors([FromUri] int? propertyId)
        {
            if (!propertyId.HasValue)
                return BadRequest("PropertyId is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"SELECT Id, PropertyId, Name, ContactPerson, ContactNumber, Email, Address, 
                                    GSTNumber, PANNumber, IsApproved, ApprovedBy, 
                                    CreatedBy, CreatedOn, IsActive 
                             FROM Inventory.Vendor 
                             WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    var vendors = new List<VendorDto>();

                    while (await reader.ReadAsync())
                    {
                        vendors.Add(new VendorDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                            GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                            PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                            IsApproved = reader.GetBoolean(9),
                            ApprovedBy = reader.IsDBNull(10)? (int?)null : reader.GetInt32(10),
                            CreatedBy = reader.GetInt32(11),
                            CreatedOn = reader.GetDateTime(12),
                            IsActive = reader.GetBoolean(13)
                        });
                    }

                    return Ok(vendors);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("vendor/{id}")]
        public async Task<IHttpActionResult> UpdateVendor(int id, [FromBody] VendorDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid vendor data.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Vendor 
                             SET Name = @Name,
                                 ContactPerson = @ContactPerson,
                                 ContactNumber = @ContactNumber,
                                 Email = @Email,
                                 Address = @Address,
                                 GSTNumber = @GSTNumber,
                                 PANNumber = @PANNumber,
                                 IsApproved = @IsApproved,
                                 ApprovedBy = @ApprovedBy
                             WHERE Id = @Id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Name", dto.Name);
                    cmd.Parameters.AddWithValue("@ContactPerson", (object)dto.ContactPerson ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContactNumber", (object)dto.ContactNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)dto.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Address", (object)dto.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@GSTNumber", (object)dto.GSTNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PANNumber", (object)dto.PANNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsApproved", dto.IsApproved);
                    cmd.Parameters.AddWithValue("@ApprovedBy", (object)dto.ApprovedBy ?? DBNull.Value);

                    await conn.OpenAsync();
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                        return NotFound();

                    return Ok(new { message = "Vendor updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("vendor/{id}")]
        public async Task<IHttpActionResult> DeleteVendor(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE Inventory.Vendor 
                             SET IsActive = 0 
                             WHERE Id = @Id AND IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    await conn.OpenAsync();
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                        return NotFound();

                    return Ok(new { message = "Vendor soft-deleted successfully" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("vendor/{id}")]
        public async Task<IHttpActionResult> GetVendorById(int id)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"SELECT Id, PropertyId, Name, ContactPerson, ContactNumber, Email, Address, 
                                    GSTNumber, PANNumber, IsApproved, ApprovedBy, 
                                    CreatedBy, CreatedOn, IsActive 
                             FROM Inventory.Vendor 
                             WHERE Id = @Id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Id", id);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        var vendor = new VendorDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                            GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                            PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                            IsApproved = reader.GetBoolean(19),
                            ApprovedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                            CreatedBy = reader.GetInt32(11),
                            CreatedOn = reader.GetDateTime(12),
                            IsActive = reader.GetBoolean(13)
                        };

                        return Ok(vendor);
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("vendors/pendingApproval")]
        public async Task<IHttpActionResult> GetPendingApprovalVendors([FromUri] int? propertyId)
        {
            if (!propertyId.HasValue)
                return BadRequest("PropertyId is required.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"SELECT Id, PropertyId, Name, ContactPerson, ContactNumber, Email, Address, 
                                    GSTNumber, PANNumber, IsApproved, ApprovedBy, 
                                    CreatedBy, CreatedOn, IsActive 
                             FROM Inventory.Vendor 
                             WHERE PropertyId = @PropertyId AND IsActive = 1 AND IsApproved = 0";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();
                    var pendingVendors = new List<VendorDto>();

                    while (await reader.ReadAsync())
                    {
                        pendingVendors.Add(new VendorDto
                        {
                            Id = reader.GetInt32(0),
                            PropertyId = reader.GetInt32(1),
                            Name = reader.GetString(2),
                            ContactPerson = reader.IsDBNull(3) ? null : reader.GetString(3),
                            ContactNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Address = reader.IsDBNull(6) ? null : reader.GetString(6),
                            GSTNumber = reader.IsDBNull(7) ? null : reader.GetString(7),
                            PANNumber = reader.IsDBNull(8) ? null : reader.GetString(8),
                            IsApproved = reader.GetBoolean(9),
                            ApprovedBy = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                            CreatedBy = reader.GetInt32(11),
                            CreatedOn = reader.GetDateTime(12),
                            IsActive = reader.GetBoolean(13)
                        });
                    }

                    if (pendingVendors.Count == 0)
                        return NotFound();

                    return Ok(pendingVendors);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching unapproved vendors: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("ratecard")]
        public async Task<IHttpActionResult> CreateRateCard([FromBody] RateCardDto dto)
        {
            if (dto == null || dto.ItemId <= 0 || dto.VendorId <= 0 || dto.Price <= 0 || dto.CreatedBy <= 0)
                return BadRequest("Invalid input data.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"INSERT INTO Inventory.RateCard 
                            (ItemId, VendorId, Price, ValidTill, CreatedBy, IsApproved)
                             VALUES 
                            (@ItemId, @VendorId, @Price, @ValidTill, @CreatedBy, @IsApproved)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ItemId", dto.ItemId);
                    cmd.Parameters.AddWithValue("@VendorId", dto.VendorId);
                    cmd.Parameters.AddWithValue("@Price", dto.Price);
                    cmd.Parameters.AddWithValue("@ValidTill", (object)dto.ValidTill ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);
                    cmd.Parameters.AddWithValue("@IsApproved", dto.IsApproved);

                    await conn.OpenAsync();
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows > 0)
                        return Ok(new { message = "Rate card created successfully." });
                    else
                        return BadRequest("Insert failed.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error creating rate card: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("ratecards")]
        public async Task<IHttpActionResult> GetAllRateCards([FromUri] int? propertyId = null, [FromUri] int? categoryId = null, [FromUri] int? itemId = null, [FromUri] int? vendorId = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"
                SELECT rc.Id, 
                       cat.Name AS CategoryName,
                       i.Id as ItemId,
                       i.Name AS ItemName,
                       i.BrandName,
                       i.Description,
                       i.HSNCode,
                       v.Id as VendorId,
                       v.Name AS VendorName,
                       rc.Price,
                       i.MeasurementUnit,
                       rc.ValidTill,
                       rc.IsApproved,
                       rc.CreatedOn
                FROM Inventory.RateCard rc
                INNER JOIN Inventory.Item i ON rc.ItemId = i.Id
                INNER JOIN Inventory.Vendor v ON rc.VendorId = v.Id
                INNER JOIN Inventory.Category cat ON i.CategoryId = cat.Id
                WHERE rc.IsApproved = 1                
                      AND (rc.IsActive>0 and rc.ValidTill IS NOT NULL OR rc.ValidTill >= GETDATE())
                      AND i.IsApproved = 1 AND i.IsActive = 1
                      AND v.IsApproved = 1 AND v.IsActive = 1
                      AND cat.IsApproved = 1 AND cat.IsActive = 1
                      AND (@CategoryId IS NULL OR i.CategoryId = @CategoryId)
                      AND (@PropertyId IS NULL OR i.PropertyId = @PropertyId)
                      AND (@ItemId IS NULL OR rc.ItemId = @ItemId)
                      AND (@VendorId IS NULL OR rc.VendorId = @VendorId)

                ORDER BY rc.CreatedOn DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", (object)propertyId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CategoryId", (object)categoryId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ItemId", (object)itemId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VendorId", (object)vendorId ?? DBNull.Value);

                    await conn.OpenAsync();
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    var rateCards = new List<RateCardDto>();

                    while (await reader.ReadAsync())
                    {
                        rateCards.Add(new RateCardDto
                        {
                            Id = reader.GetInt32(0),
                            CategoryName = reader.GetString(1),
                            ItemId = reader.GetInt32(2),
                            ItemName = reader.GetString(3),
                            BrandName = reader.IsDBNull(4) ? null : reader.GetString(4),
                            Description = reader.IsDBNull(5) ? null : reader.GetString(5),
                            HSNCode = reader.IsDBNull(6) ? null : reader.GetString(6),
                            VendorId = reader.GetInt32(7),
                            VendorName = reader.GetString(8),
                            Price = reader.GetDecimal(9),
                            MeasurementUnit = reader.IsDBNull(10) ? null : reader.GetString(10),
                            ValidTill = reader.IsDBNull(11) ? (DateTime?)null : reader.GetDateTime(11),
                            IsApproved = reader.GetBoolean(12),
                            CreatedOn = reader.GetDateTime(13)
                        });
                    }

                    return Ok(rateCards);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching rate cards: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("GetGroupedPurchaseOrderDetails")]
        public async Task<IHttpActionResult> GetGroupedPurchaseOrderDetails([FromUri] int propertyId, [FromUri] int? poId = null, [FromUri] int? vendorId = null)
        {
            var flatList = new List<dynamic>();
            string query = @"SELECT 
    po.PurchaseOrderId,
    po.PONumber,
    po.PODateTime,
    po.BillingAddress,
    po.ShippingAddress,
    po.PropertyId,
    po.IsApproved,
    po.VendorId,
    po.VendorName,
    po.ContactPerson,
    po.ContactNumber,
    po.Email,
    po.PurchaseOrderItemId,
    po.ItemId,
    po.ItemName,
    po.Quantity,
    po.Rate,
    po.LineTotal,

    ISNULL(SUM(rec.QuantityReceived), 0) AS QuantityReceived,
    MAX(CAST(rec.IsRejected AS INT)) AS IsRejected,
    STRING_AGG(rec.RejectionRemarks, '; ') AS RejectionRemarks,
MAX(CAST(rec.IsCompleted AS INT)) AS IsCompleted


FROM Inventory.vw_PurchaseOrderDetails AS po
LEFT JOIN dbo.ScannedPOData AS rec 
    ON po.PurchaseOrderItemId = rec.POItemId
WHERE po.PropertyId = @propertyId
  AND (@poId IS NULL OR po.PurchaseOrderId = @poId)
  AND (@VendorId IS NULL OR po.VendorId = @VendorId)
GROUP BY 
    po.PurchaseOrderId,
    po.PONumber,
    po.PODateTime,
    po.BillingAddress,
    po.ShippingAddress,
    po.PropertyId,
    po.IsApproved,
    po.VendorId,
    po.VendorName,
    po.ContactPerson,
    po.ContactNumber,
    po.Email,
    po.PurchaseOrderItemId,
    po.ItemId,
    po.ItemName,
    po.Quantity,
    po.Rate,
    po.LineTotal
";


            using (SqlConnection conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);
                    cmd.Parameters.AddWithValue("@POId", poId.HasValue ? (object)poId.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@VendorId", vendorId.HasValue ? (object)vendorId.Value : DBNull.Value);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            flatList.Add(new
                            {
                                PurchaseOrderId = reader.GetInt32(0),
                                PONumber = reader.GetString(1),
                                PODateTime = reader.GetDateTime(2),
                                BillingAddress = reader.GetString(3),
                                ShippingAddress = reader.GetString(4),
                                PropertyId = reader.GetInt32(5),
                                IsApproved = reader.GetBoolean(6),

                                VendorId = reader.GetInt32(7),
                                VendorName = reader.GetString(8),
                                ContactPerson = reader.GetString(9),
                                ContactNumber = reader.GetString(10),
                                Email = reader.GetString(11),

                                PurchaseOrderItemId = reader.GetInt32(12),
                                ItemId = reader.GetInt32(13),
                                ItemName = reader.GetString(14),
                                Quantity = Convert.ToDecimal(reader["Quantity"]),
                                Rate = Convert.ToDecimal(reader["Rate"]),
                                LineTotal = Convert.ToDecimal(reader["LineTotal"]),
                                QuantityReceived = reader["QuantityReceived"] != DBNull.Value
                            ? Convert.ToDecimal(reader["QuantityReceived"]) : (decimal?)null,
                                IsCompleted = reader["IsCompleted"] != DBNull.Value && Convert.ToInt32(reader["IsCompleted"]) == 1,
                                IsRejected = reader["IsRejected"] != DBNull.Value && Convert.ToInt32(reader["IsRejected"]) == 1,
                                RejectionRemarks = reader["RejectionRemarks"] != DBNull.Value
                            ? reader["RejectionRemarks"].ToString() : null
                            });

                        }
                    }
                }
            }

            var groupedResult = flatList
                .GroupBy(x => x.PurchaseOrderId)
                .Select(g => new PurchaseOrderGroupedDto
                {
                    PurchaseOrderId = g.Key,
                    PONumber = g.First().PONumber,
                    PODateTime = g.First().PODateTime,
                    BillingAddress = g.First().BillingAddress,
                    ShippingAddress = g.First().ShippingAddress,
                    PropertyId = g.First().PropertyId,
                    IsApproved = g.First().IsApproved,

                    VendorId = g.First().VendorId,
                    VendorName = g.First().VendorName,
                    ContactPerson = g.First().ContactPerson,
                    ContactNumber = g.First().ContactNumber,
                    Email = g.First().Email,

                    Items = g.Select(item => new PurchaseOrderGroupedItemDto
                    {
                        PurchaseOrderItemId = item.PurchaseOrderItemId,
                        ItemId = item.ItemId,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        Rate = item.Rate,
                        LineTotal = item.LineTotal,
                        QuantityReceived = item.QuantityReceived,
                        IsCompleted = item.IsCompleted,
                        IsRejected = item.IsRejected,
                        RejectionRemarks = item.RejectionRemarks
                    }).ToList()

                }).ToList();

            return Ok(groupedResult);
        }


        [HttpPost]
        [Route("CreatePurchaseOrders")]
        public async Task<IHttpActionResult> CreatePurchaseOrder(List<PurchaseOrderDto> orders)
        {
            if (orders == null || !orders.Any())
                return BadRequest("No purchase orders provided.");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                await conn.OpenAsync().ConfigureAwait(false);
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var createdOrders = new List<object>();

                        foreach (var dto in orders)
                        {
                            if (dto == null || dto.Items == null || !dto.Items.Any())
                                continue;

                            string tempPONumber = $"TEMP-{dto.PropertyId}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                            string insertPOQuery = @"
                        INSERT INTO Inventory.PurchaseOrder 
                            (PONumber, VendorId, BillingAddress, ShippingAddress, CreatedBy, PropertyId, IsApproved, IsDeleted)
                        VALUES 
                            (@PONumber, @VendorId, @BillingAddress, @ShippingAddress, @CreatedBy, @PropertyId, 0, 0);
                        SELECT CAST(SCOPE_IDENTITY() AS int);";

                            int purchaseOrderId;
                            using (SqlCommand poCmd = new SqlCommand(insertPOQuery, conn, transaction))
                            {
                                poCmd.Parameters.AddWithValue("@PONumber", tempPONumber);
                                poCmd.Parameters.Add("@VendorId", SqlDbType.Int).Value = dto.VendorId;
                                poCmd.Parameters.Add("@BillingAddress", SqlDbType.VarChar, 500).Value = (object)dto.BillingAddress ?? DBNull.Value;
                                poCmd.Parameters.Add("@ShippingAddress", SqlDbType.VarChar, 500).Value = (object)dto.ShippingAddress ?? DBNull.Value;
                                poCmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = dto.CreatedBy;
                                poCmd.Parameters.Add("@PropertyId", SqlDbType.Int).Value = dto.PropertyId;

                                purchaseOrderId = (int)await poCmd.ExecuteScalarAsync().ConfigureAwait(false);
                            }

                            string poNumber = $"PO-{dto.PropertyId}{purchaseOrderId}";

                            string updatePOQuery = "UPDATE Inventory.PurchaseOrder SET PONumber = @PONumber WHERE Id = @Id";
                            using (SqlCommand updateCmd = new SqlCommand(updatePOQuery, conn, transaction))
                            {
                                updateCmd.Parameters.Add("@PONumber", SqlDbType.VarChar, 50).Value = poNumber;
                                updateCmd.Parameters.Add("@Id", SqlDbType.Int).Value = purchaseOrderId;

                                await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                            }

                            foreach (var item in dto.Items)
                            {
                                string insertItemQuery = @"
                            INSERT INTO Inventory.PurchaseOrderItems 
                                (PurchaseOrderId, ItemId, Quantity, Rate, CreatedBy)
                            VALUES 
                                (@PurchaseOrderId, @ItemId, @Quantity, @Rate, @CreatedBy);";

                                using (SqlCommand itemCmd = new SqlCommand(insertItemQuery, conn, transaction))
                                {
                                    itemCmd.Parameters.Add("@PurchaseOrderId", SqlDbType.Int).Value = purchaseOrderId;
                                    itemCmd.Parameters.Add("@ItemId", SqlDbType.Int).Value = item.ItemId;
                                    itemCmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = item.Quantity;
                                    itemCmd.Parameters.Add("@Rate", SqlDbType.Decimal).Value = item.Price;
                                    itemCmd.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = dto.CreatedBy;

                                    await itemCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                                }
                            }

                            createdOrders.Add(new
                            {
                                PurchaseOrderId = purchaseOrderId,
                                PONumber = poNumber,
                                Message = "Purchase Order created successfully."
                            });
                        }

                        transaction.Commit();

                        return Ok(createdOrders);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error while creating Purchase Order: " + ex.Message));
                    }
                }
            }
        }

        [HttpGet]  
        [Route("GetStock")]
        public IHttpActionResult GetStock(int propId)
        {
            List<StockViewModel> stockList = new List<StockViewModel>();
            string connStr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    string query = @"
                SELECT
                    Cat.Name AS CategoryName,
                    Cat.PropertyId,
                    Cat.Id AS CategoryId,
                    Item.Name AS ItemName,
                    Item.Description AS ItemDescription,
                    Item.MinStockLevel,
                    Item.Id AS ItemId,
                    Stock.CurrentQty,
                    Stock.Id AS StockId
                FROM [Inventory].[Stock] Stock
                LEFT JOIN [Inventory].[Item] Item ON Stock.ItemId = Item.Id
                LEFT JOIN [Inventory].[Category] Cat ON Item.CategoryId = Cat.Id
                WHERE Cat.PropertyId = @propId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@propId", propId); // 🔐 Parameterized to prevent SQL injection

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        stockList.Add(new StockViewModel
                        {
                            CategoryName = reader["CategoryName"] as string,
                            PropertyId = reader["PropertyId"] as int?,
                            CategoryId = reader["CategoryId"] as int?,
                            ItemName = reader["ItemName"] as string,
                            ItemDescription = reader["ItemDescription"] as string,
                            MinStockLevel = reader["MinStockLevel"] as int?,
                            ItemId = reader["ItemId"] as int?,
                            CurrentQty = reader["CurrentQty"] as int?,
                            StockId = Convert.ToInt32(reader["StockId"]) // ✅ safer cast
                        });
                    }
                }

                return Ok(stockList);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex); // 🔴 Optional: return full error during development
            }
        }

    }
}