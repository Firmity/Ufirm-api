using ExcelDataReader;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using UrestComplaintWebApi.Models;
using Swashbuckle.Swagger.Annotations;

namespace UrestComplaintWebApi.Controllers
{
    public class AssetController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpPost]
        [Route("Uploadasset")]
        public IHttpActionResult Uploadasset()
        {

            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count == 0)
            {
                return BadRequest("No file uploaded.");
            }


            var file = httpRequest.Files[0];
            var fileExtension = Path.GetExtension(file.FileName).ToLower();


            if (fileExtension == ".xls" || fileExtension == ".xlsx")
            {
                return UploadExcel(file);
            }
            else if (fileExtension == ".csv")
            {
                return UploadCsv(file);
            }
            else
            {
                return BadRequest("Unsupported file format. Please upload an Excel or CSV file.");
            }
        }

        private IHttpActionResult UploadExcel(HttpPostedFile file)
        {
            try
            {
                using (var stream = file.InputStream)
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                        });

                        DataTable dataTable = dataSet.Tables[0];

                        using (SqlConnection con = new SqlConnection(constr))
                        {
                            con.Open();

                            foreach (DataRow row in dataTable.Rows)
                            {
                                using (SqlCommand cmd = new SqlCommand(
                                    "INSERT INTO AssetMaster (Name, Description, QRCode, IsDeleted, AssetType, Manufacturer, AssetModel, IsMoveable,AssetValue, LastServiceDate, NextServiceDate, IsRentable, Status, Category, Location, PropertyId) VALUES ( @Name, @Description, @QRCode, @IsDeleted, @AssetType, @Manufacturer, @AssetModel, @IsMoveable, @AssetValue, @LastServiceDate, @NextServiceDate,@IsRentable,  @Status, @Category, @Location,@PropertyId)", con))
                                {
                                    cmd.Parameters.AddWithValue("@Name", row["Name"].ToString());
                                    cmd.Parameters.AddWithValue("@Description", row["Description"].ToString());
                                    cmd.Parameters.AddWithValue("@QRCode", row["QRCode"].ToString());
                                    cmd.Parameters.AddWithValue("@IsDeleted", Convert.ToBoolean(row["isdeleted"]));
                                    cmd.Parameters.AddWithValue("@AssetType", row["AssetType"].ToString());
                                    cmd.Parameters.AddWithValue("@Manufacturer", row["Manufacturer"].ToString());
                                    cmd.Parameters.AddWithValue("@AssetModel", row["AssetModel"].ToString());
                                    cmd.Parameters.AddWithValue("@IsMoveable", Convert.ToBoolean(row["IsMoveable"]));
                                    cmd.Parameters.AddWithValue("@AssetValue", Convert.ToDecimal(row["AssetValue"]));
                                    cmd.Parameters.AddWithValue("@LastServiceDate", row["LastServiceDate"] == DBNull.Value ? (object)DBNull.Value : Convert.ToDateTime(row["LastServiceDate"]));
                                    cmd.Parameters.AddWithValue("@NextServiceDate", row["NextServiceDate"] == DBNull.Value ? (object)DBNull.Value : Convert.ToDateTime(row["NextServiceDate"]));
                                    cmd.Parameters.AddWithValue("@IsRentable", Convert.ToBoolean(row["IsRentable"]));
                                    cmd.Parameters.AddWithValue("@PropertyId", Convert.ToInt32(row["PropertyId"]));
                                    cmd.Parameters.AddWithValue("@Status", Convert.ToBoolean(row["Status"]));
                                    cmd.Parameters.AddWithValue("@Category", row["Category"].ToString());
                                    cmd.Parameters.AddWithValue("@Location", row["Location"].ToString());
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                return Ok("Excel file uploaded and data inserted successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private IHttpActionResult UploadCsv(HttpPostedFile file)
        {
            try
            {
                string uploadPath = HttpContext.Current.Server.MapPath("~/Uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                string filePath = Path.Combine(uploadPath, file.FileName);
                file.SaveAs(filePath);

                InsertCsvDataToDatabase(filePath);

                return Ok("CSV file uploaded and data inserted successfully.");
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private void InsertCsvDataToDatabase(string filePath)
        {
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] values = line.Split(',');

                        using (SqlCommand cmd = new SqlCommand(
                        @"INSERT INTO AssetMaster (Name, Description, QRCode, IsDeleted, AssetType, Manufacturer, AssetModel, IsMoveable, AssetValue, LastServiceDate, NextServiceDate, IsRentable, Password, PropertyId, Status, Category, Location) 
VALUES 
(@Name, @Description, @QRCode, @IsDeleted, @AssetType, @Manufacturer, @AssetModel, 
@IsMoveable, @AssetValue, @LastServiceDate, @NextServiceDate, @IsRentable, @Password,  @PropertyId, @Status, @Category, @Location)", con))
                        {
                            cmd.Parameters.AddWithValue("@Name", values[0]);
                            cmd.Parameters.AddWithValue("@Description", values[1]);
                            cmd.Parameters.AddWithValue("@QRCode", values[2]);
                            cmd.Parameters.AddWithValue("@IsDeleted", Convert.ToBoolean(values[3]));
                            cmd.Parameters.AddWithValue("@AssetType", values[4]);
                            cmd.Parameters.AddWithValue("@Manufacturer", values[5]);
                            cmd.Parameters.AddWithValue("@AssetModel", values[6]);
                            cmd.Parameters.AddWithValue("@IsMoveable", Convert.ToBoolean(values[7]));
                            cmd.Parameters.AddWithValue("@AssetValue", Convert.ToDecimal(values[9]));
                            cmd.Parameters.AddWithValue("@LastServiceDate", values[10] == "" ? (object)DBNull.Value : Convert.ToDateTime(values[10]));
                            cmd.Parameters.AddWithValue("@NextServiceDate", values[11] == "" ? (object)DBNull.Value : Convert.ToDateTime(values[11]));
                            cmd.Parameters.AddWithValue("@IsRentable", Convert.ToBoolean(values[13]));
                            cmd.Parameters.AddWithValue("@PropertyId", Convert.ToInt32(values[15]));
                            cmd.Parameters.AddWithValue("@Status", Convert.ToBoolean(values[15]));
                            cmd.Parameters.AddWithValue("@Category", values[16]);
                            cmd.Parameters.AddWithValue("@Location", values[17]);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        [HttpGet]
        [Route("api/Asset/GetServiceHistory")]
        public IHttpActionResult GetServiceHistory(int assetId)
        {
            try
            {
                using (var connection = new SqlConnection(constr))
                {
                    connection.Open();
                    using (var command = new SqlCommand(
                        @"SELECT Id, AssetId, ServiceDate, NextServiceDate, Image, Remark, ServiceDoc, 
                         ServiceCost, ServicedBy, ApprovedBy
                  FROM AssetServiceRecord 
                  WHERE AssetId = @AssetId 
                  ORDER BY ServiceDate DESC", connection))
                    {
                        command.Parameters.AddWithValue("@AssetId", assetId);

                        using (var reader = command.ExecuteReader())
                        {
                            var serviceRecords = new List<AssetServiceRecordResponse>();

                            while (reader.Read())
                            {
                                var record = new AssetServiceRecordResponse
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    AssetId = reader.GetInt32(reader.GetOrdinal("AssetId")),
                                    ServiceDate = reader.GetDateTime(reader.GetOrdinal("ServiceDate")).ToString("yyyy-MM-dd"),
                                    NextServiceDate = reader.GetDateTime(reader.GetOrdinal("NextServiceDate")).ToString("yyyy-MM-dd"),
                                    // ✅ Use string URLs directly, not Base64
                                    Image = reader["Image"] != DBNull.Value ? reader["Image"].ToString() : null,
                                    Remark = reader["Remark"] as string,
                                    ServiceDoc = reader["ServiceDoc"] != DBNull.Value ? reader["ServiceDoc"].ToString() : null,
                                    ServiceCost = reader["ServiceCost"] != DBNull.Value ? Convert.ToDecimal(reader["ServiceCost"]) : 0,
                                    ServicedBy = reader["ServicedBy"] as string,
                                    ApprovedBy = reader["ApprovedBy"] as string
                                };

                                serviceRecords.Add(record);
                            }

                            return Ok(serviceRecords);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An error occurred: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/Asset/GetServiceNotifications/{propertyId}")]
        public IHttpActionResult GetServiceNotificationsByProperty(int propertyId)
        {
            try
            {
                using (var connection = new SqlConnection(constr))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    am.Id AS AssetId,
                    am.Name,
                    am.PropertyId,
                    am.LastServiceDate,
                    am.NextServiceDate,
                    DATEDIFF(DAY, GETDATE(), am.NextServiceDate) AS DaysRemaining,
                    CASE 
                        WHEN am.NextServiceDate < GETDATE() THEN 'Overdue'
                        WHEN am.NextServiceDate BETWEEN GETDATE() AND DATEADD(DAY, 7, GETDATE()) THEN 'Upcoming'
                        ELSE 'Future'
                    END AS ServiceStatus,
                    ISNULL(sr.Remark, '') AS LastServiceRemark,
                    ISNULL(sr.ServicedBy, '') AS LastServicedBy,
                    ISNULL(sr.ServiceCost, 0) AS LastServiceCost
                FROM AssetMaster am
                LEFT JOIN (
                    SELECT AssetId, MAX(ServiceDate) AS LastServiceDate
                    FROM AssetServiceRecord
                    GROUP BY AssetId
                ) AS latest ON am.Id = latest.AssetId
                LEFT JOIN AssetServiceRecord sr 
                    ON am.Id = sr.AssetId AND sr.ServiceDate = latest.LastServiceDate
                WHERE am.NextServiceDate IS NOT NULL 
                      AND am.PropertyId = @PropertyId
                ORDER BY am.NextServiceDate ASC;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PropertyId", propertyId);

                        using (var reader = command.ExecuteReader())
                        {
                            var serviceList = new List<object>();

                            while (reader.Read())
                            {
                                serviceList.Add(new
                                {
                                    AssetId = reader["AssetId"],
                                    AssetName = reader["Name"],
                                    PropertyId = reader["PropertyId"],
                                    LastServiceDate = reader["LastServiceDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["LastServiceDate"]).ToString("yyyy-MM-dd")
                                        : null,
                                    NextServiceDate = reader["NextServiceDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["NextServiceDate"]).ToString("yyyy-MM-dd")
                                        : null,
                                    DaysRemaining = Convert.ToInt32(reader["DaysRemaining"]),
                                    ServiceStatus = reader["ServiceStatus"].ToString(),
                                    LastServiceRemark = reader["LastServiceRemark"].ToString(),
                                    LastServicedBy = reader["LastServicedBy"].ToString(),
                                    LastServiceCost = reader["LastServiceCost"].ToString()
                                });
                            }

                            var upcoming = serviceList.Where(x =>
                                x.GetType().GetProperty("ServiceStatus").GetValue(x).ToString() == "Upcoming");
                            var overdue = serviceList.Where(x =>
                                x.GetType().GetProperty("ServiceStatus").GetValue(x).ToString() == "Overdue");

                            return Ok(new
                            {
                                Upcoming = upcoming,
                                Overdue = overdue
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An error occurred while fetching service notifications: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("SaveServiceRecord")]
        [SwaggerOperation("SaveServiceRecord")]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, "Service record saved successfully.")]
        [SwaggerResponse(System.Net.HttpStatusCode.BadRequest, "Invalid input data.")]
        [SwaggerResponse(System.Net.HttpStatusCode.InternalServerError, "Server error.")]
        public IHttpActionResult SaveServiceRecord()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // Read form fields
                int assetId = Convert.ToInt32(httpRequest.Form["AssetId"]);
                string serviceDate = httpRequest.Form["ServiceDate"];
                string nextServiceDate = httpRequest.Form["NextServiceDate"];
                string remark = httpRequest.Form["Remark"];
                string serviceCost = httpRequest.Form["ServiceCost"];
                string servicedBy = httpRequest.Form["ServicedBy"];
                string approvedBy = httpRequest.Form["ApprovedBy"];

                // Validation
                if (assetId <= 0 || string.IsNullOrWhiteSpace(serviceDate) || string.IsNullOrWhiteSpace(nextServiceDate))
                    return BadRequest("Invalid input data.");

                string uploadRoot = HttpContext.Current.Server.MapPath("~/Uploads/");
                if (!Directory.Exists(uploadRoot))
                    Directory.CreateDirectory(uploadRoot);

                string imageUrl = null;
                string serviceDocUrl = null;

                var imageFile = httpRequest.Files["Image"];
                var docFile = httpRequest.Files["ServiceDoc"];

                // Save image file
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid() + "_" + Path.GetFileName(imageFile.FileName);
                    string savePath = Path.Combine(uploadRoot, fileName);
                    imageFile.SaveAs(savePath);
                    imageUrl = "/Uploads/Serviceimg/" + fileName;
                }

                // Save doc file
                if (docFile != null && docFile.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid() + "_" + Path.GetFileName(docFile.FileName);
                    string savePath = Path.Combine(uploadRoot, fileName);
                    docFile.SaveAs(savePath);
                    serviceDocUrl = "/Uploads/Servicedoc/" + fileName;
                }

                // Save record to database
                using (var con = new SqlConnection(constr))
                {
                    con.Open();
                    string query = @"
                    INSERT INTO AssetServiceRecord 
                    (AssetId, ServiceDate, NextServiceDate, Image, Remark, ServiceDoc, ServiceCost, ServicedBy, ApprovedBy)
                    VALUES 
                    (@assetid, @servicedate, @nextservicedate, @image, @remark, @servicedoc, @servicecost, @servicedby, @approvedby);

                    UPDATE AssetMaster
                    SET LastServiceDate = @servicedate,
                        NextServiceDate = @nextservicedate
                    WHERE Id = @assetid;
                ";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@assetid", assetId);
                        cmd.Parameters.AddWithValue("@servicedate", serviceDate);
                        cmd.Parameters.AddWithValue("@nextservicedate", nextServiceDate);
                        cmd.Parameters.AddWithValue("@remark", (object)remark ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@servicecost", (object)serviceCost ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@servicedby", (object)servicedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@approvedby", (object)approvedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@image", (object)imageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@servicedoc", (object)serviceDocUrl ?? DBNull.Value);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                            return Ok(new { Message = "Service record saved successfully.", ImageUrl = imageUrl, ServiceDocUrl = serviceDocUrl });
                        else
                            return InternalServerError(new Exception("Database insert failed."));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An error occurred: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("SaveFMServiceResponse")]
        public IHttpActionResult SaveFMServiceResponse()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // ✅ Map form data to model
                var model = new FMServiceResponseModel
                {
                    AssetId = Convert.ToInt32(httpRequest.Form["AssetId"]),
                    ServiceDate = httpRequest.Form["ServiceDate"],
                    NextServiceDate = httpRequest.Form["NextServiceDate"],
                    Remark = httpRequest.Form["Remark"],
                    ServicedBy = httpRequest.Form["ServicedBy"],
                    ApprovedBy = httpRequest.Form["ApprovedBy"],
                    ServiceCost = string.IsNullOrEmpty(httpRequest.Form["ServiceCost"])
                        ? 0
                        : Convert.ToDecimal(httpRequest.Form["ServiceCost"])
                };

                string imagePath = null;
                string docPath = null;

                // ✅ Handle image upload
                var imageFile = httpRequest.Files["Image"];
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    string imageName = model.AssetId + "_service" + Path.GetExtension(imageFile.FileName);
                    string imageSavePath = HttpContext.Current.Server.MapPath("~/Uploads/Serviceimg/" + imageName);
                    imageFile.SaveAs(imageSavePath);
                    imagePath = "https://api.urest.in:8096/Uploads/Serviceimg/" + imageName;
                }

                // ✅ Handle service document upload
                var docFile = httpRequest.Files["ServiceDoc"];
                if (docFile != null && docFile.ContentLength > 0)
                {
                    string docName = model.AssetId + "_record" + Path.GetExtension(docFile.FileName);
                    string docSavePath = HttpContext.Current.Server.MapPath("~/Uploads/Servicedoc/" + docName);
                    docFile.SaveAs(docSavePath);
                    docPath = "https://api.urest.in:8096/Uploads/Servicedoc/" + docName;
                }

                // ✅ Insert into database
                using (var con = new SqlConnection(constr))
                {
                    con.Open();
                    string query = @"
                        INSERT INTO AssetServiceRecord
                        (AssetId, ServiceDate, NextServiceDate, Image, Remark, ServiceDoc, ServiceCost, ServicedBy, ApprovedBy)
                        VALUES (@AssetId, @ServiceDate, @NextServiceDate, @Image, @Remark, @ServiceDoc, @ServiceCost, @ServicedBy, @ApprovedBy)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AssetId", model.AssetId);
                        cmd.Parameters.AddWithValue("@ServiceDate", model.ServiceDate);
                        cmd.Parameters.AddWithValue("@NextServiceDate", model.NextServiceDate);
                        cmd.Parameters.AddWithValue("@Image", (object)imagePath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remark", (object)model.Remark ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ServiceDoc", (object)docPath ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ServiceCost", model.ServiceCost);
                        cmd.Parameters.AddWithValue("@ServicedBy", (object)model.ServicedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ApprovedBy", (object)model.ApprovedBy ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new
                {
                    Message = "Service record saved successfully",
                    Image = imagePath,
                    ServiceDoc = docPath
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }




        private void PopulateCommandParameters(SqlCommand command, AssetServiceRecord record)
        {
            command.Parameters.AddWithValue("@AssetId", record.AssetId);
            command.Parameters.AddWithValue("@LastServiceDate", ConvertToDateTime(record.ServiceDate) ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NextServiceDate", ConvertToDateTime(record.NextServiceDate) ?? (object)DBNull.Value);

            // Handle Image
            if (record.Image != null && record.Image.ContentLength > 0)
            {
                string imagePath = SaveFile(record.Image, "~/Uploads/Serviceimg/");
                command.Parameters.AddWithValue("@Image", imagePath);
            }
            else
            {
                command.Parameters.AddWithValue("@Image", DBNull.Value);
            }

            // Handle ServiceDoc
            if (record.ServiceDoc != null && record.ServiceDoc.ContentLength > 0)
            {
                string docPath = SaveFile(record.ServiceDoc, "~/Uploads/Servicedoc/");
                command.Parameters.AddWithValue("@ServiceDoc", docPath);
            }
            else
            {
                command.Parameters.AddWithValue("@ServiceDoc", DBNull.Value);
            }

            command.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(record.Remark) ? (object)DBNull.Value : record.Remark);
            command.Parameters.AddWithValue("@ServiceCost", record.ServiceCost);
            command.Parameters.AddWithValue("@ServicedBy", string.IsNullOrWhiteSpace(record.ServicedBy) ? (object)DBNull.Value : record.ServicedBy);
            command.Parameters.AddWithValue("@ApprovedBy", string.IsNullOrWhiteSpace(record.ApprovedBy) ? (object)DBNull.Value : record.ApprovedBy);
        }

        private string SaveFile(HttpPostedFileBase file, string virtualFolder)
        {
            string fileName = Path.GetFileName(file.FileName);
            string folderPath = HttpContext.Current.Server.MapPath(virtualFolder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);
            file.SaveAs(filePath);

            // Return public URL
            string baseUrl = "https://api.urest.in:8096";
            string publicUrl = $"{baseUrl}{virtualFolder.Replace("~", "")}{fileName}";
            return publicUrl;
        }


        // Helper method to safely convert a string to DateTime
        private DateTime? ConvertToDateTime(string dateString)
        {
            if (DateTime.TryParse(dateString, out var result))
            {
                return result;
            }
            return null;
        }
        private byte[] ConvertBase64ToByteArray(string base64String)
        {
            try
            {
                string paddedBase64 = base64String.Replace(" ", "+");
                int mod4 = paddedBase64.Length % 4;
                if (mod4 > 0)
                {
                    paddedBase64 += new string('=', 4 - mod4);
                }
                return Convert.FromBase64String(paddedBase64);
            }
            catch
            {
                return null;
            }
        }


        [HttpGet]
        [Route("api/Asset/GetCountOnAssets")]
        public IHttpActionResult GetCountOnAssets(int propertyId, string dateFrom, string dateTo)
        {
            try
            {

                DateTime? dateFromValue = string.IsNullOrEmpty(dateFrom) ? (DateTime?)null : Convert.ToDateTime(dateFrom).Date;
                DateTime? dateToValue = string.IsNullOrEmpty(dateTo) ? (DateTime?)null : Convert.ToDateTime(dateTo).Date;


                var result = new
                {
                    TotalAsset = 0,
                    ServiceOverdueAssets = 0,
                    UpcomingServices = 0,
                    RentedOutAsset = 0,
                    CheckedOutAssets = 0,
                };

                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand("GetCountOnAssets", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        command.Parameters.AddWithValue("@propertyId", propertyId);
                        command.Parameters.AddWithValue("@dateFrom", dateFromValue ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@dateTo", dateToValue ?? (object)DBNull.Value);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {

                            if (reader.HasRows && reader.Read())
                            {
                                result = new
                                {
                                    TotalAsset = reader.GetInt32(0),
                                    ServiceOverdueAssets = reader.GetInt32(1),
                                    UpcomingServices = reader.GetInt32(2),
                                    RentedOutAsset = reader.GetInt32(3),
                                    CheckedOutAssets = reader.GetInt32(4)
                                };
                            }
                        }
                    }
                }

                // Return the aggregated result
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log the exception and return an error response
                return InternalServerError(ex);
            }
        }

        // GET api/Asset/GetAssetDetailsByQRCode?qrCode=3688
        [HttpGet]
        [Route("api/Asset/GetAssetDetailsByQRCode")]
        public async Task<IHttpActionResult> GetAssetDetailsByQRCode(string qrCode)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return BadRequest("QR Code is required and cannot be empty.");
            }

            try
            {
                var assetDetails = new List<AssetDetails>();

                using (var con = new SqlConnection(constr))
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT 
                            Id, 
                            Name, 
                            Description, 
                            AssetType, 
                            Manufacturer, 
                            AssetModel, 
                            LastServiceDate, 
                            NextServiceDate, 
                            AssetImage, 
                            AMCdoc, 
                            Status,
                            Category,
                            Location
                        FROM 
                            AssetMaster 
                        WHERE 
                            QRCode = @QRCode";

                    using (var command = new SqlCommand(query, con))
                    {
                        // Add QR code as a parameter to prevent SQL injection
                        command.Parameters.AddWithValue("@QRCode", qrCode);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (await reader.ReadAsync())
                                {
                                    assetDetails.Add(new AssetDetails
                                    {
                                        Id = Convert.ToInt32(reader["Id"]),
                                        Name = reader["Name"]?.ToString(),
                                        Description = reader["Description"]?.ToString(),
                                        AssetType = reader["AssetType"]?.ToString(),
                                        Manufacturer = reader["Manufacturer"]?.ToString(),
                                        AssetModel = reader["AssetModel"]?.ToString(),
                                        Status = reader["Status"]?.ToString(),
                                        Category = reader["Category"]?.ToString(),
                                        Location = reader["Location"]?.ToString(),
                                        LastServiceDate = reader["LastServiceDate"] != DBNull.Value
                                            ? Convert.ToDateTime(reader["LastServiceDate"]).ToString("yyyy-MM-dd")
                                            : null,
                                        NextServiceDate = reader["NextServiceDate"] != DBNull.Value
                                            ? Convert.ToDateTime(reader["NextServiceDate"]).ToString("yyyy-MM-dd")
                                            : null,
                                        AssetImage = reader["AssetImage"] != DBNull.Value ? Convert.ToBase64String((byte[])reader["AssetImage"]) : null,
                                        AMCdoc = reader["AMCdoc"] != DBNull.Value ? Convert.ToBase64String((byte[])reader["AMCdoc"]) : null,

                                    });
                                }

                                return Ok(assetDetails);
                            }
                            else
                            {
                                // Return a custom NotFound message
                                return Content(HttpStatusCode.NotFound, "No records found for this QR.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (add logging mechanism as needed)
                return InternalServerError(new Exception("An error occurred while fetching the asset details. Please try again later.", ex));
            }
        }

        [Route("api/Asset/GetAssetCheckOutData")]
        [HttpGet]
        public IHttpActionResult GetAssetCheckOutData(int PropId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = con;
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@PropertyId", PropId);
                    command.CommandText = @"
                                SELECT 
                                    am.ID, 
                                    am.Name, 
                                    CASE 
                                        WHEN at.ReturnDate IS NULL AND at.CheckoutDateTime IS NOT NULL THEN NULL
                                        WHEN at.AssetID IS NULL THEN '1900-01-01'
                                        ELSE at.ReturnDate
                                    END AS ReturnDate
                                FROM 
                                    [dbo].[AssetMaster] am
                                LEFT JOIN (
                                    SELECT
                                        AssetID,
                                        ReturnDate,
                                        CheckoutDateTime
                                    FROM 
                                        [dbo].[AssetTransaction]
                                    WHERE 
                                        Id IN (
                                            SELECT 
                                                MAX(Id) 
                                            FROM 
                                                [dbo].[AssetTransaction] 
                                            GROUP BY 
                                                AssetID
                                        )
                                ) at ON am.ID = at.AssetID
                                WHERE 
                                    (am.isdeleted = 0 OR am.isdeleted IS NULL)
                                    AND am.PropertyId=@PropertyId
                                ORDER BY 
                                    am.Name";

                    SqlDataReader reader = command.ExecuteReader();

                    List<AssetTransaction> eList = new List<AssetTransaction>();

                    while (reader.Read())
                    {
                        eList.Add(new AssetTransaction
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ReturnDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2)
                        });
                    }

                    reader.Close();
                    con.Close();

                    return Ok(eList);
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("api/Asset/GetRentalAssetData")]
        [HttpGet]
        public IHttpActionResult GetRentalAssetData(int PropId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = con;
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@PropertyId", PropId);
                    command.CommandText = @"SELECT 
                                                am.ID, 
                                                am.Name, 
                                                ra.ReturnDate, 
                                                ra.RentedOutDate
                                            FROM 
                                                AssetMaster am
                                            LEFT JOIN (
                                                SELECT 
                                                    AssetID, 
                                                    ReturnDate, 
                                                    RentedOutDate, 
                                                    ROW_NUMBER() OVER (PARTITION BY AssetID ORDER BY RentedOutDate DESC) AS RowNum
                                                FROM 
                                                    RentalAssets
                                            ) ra ON am.ID = ra.AssetID AND ra.RowNum = 1
                                            WHERE 
                                                am.IsRentable = 1 
                                                AND (am.isdeleted = 0 OR am.isdeleted IS NULL)
                                                AND am.PropertyId=@PropertyId
                                            ORDER BY 
                                                am.Name;";

                    SqlDataReader reader = command.ExecuteReader();

                    List<AssetTransaction> eList = new List<AssetTransaction>();

                    while (reader.Read())
                    {
                        eList.Add(new AssetTransaction
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            ReturnDate = reader.IsDBNull(reader.GetOrdinal("ReturnDate"))
    ? (DateTime?)null
    : reader.GetDateTime(reader.GetOrdinal("ReturnDate")),
                            RentedOutDate = reader.IsDBNull(reader.GetOrdinal("RentedOutDate"))
    ? (DateTime?)null
    : reader.GetDateTime(reader.GetOrdinal("RentedOutDate"))

                        });
                    }

                    reader.Close();
                    con.Close();

                    return Ok(eList);
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

    }
}
