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
        [Route("api/Asset/SaveServiceRecord")]
        public IHttpActionResult SaveServiceRecord()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // ✅ Validate required fields
                if (string.IsNullOrWhiteSpace(httpRequest.Form["AssetId"]) ||
                    string.IsNullOrWhiteSpace(httpRequest.Form["ServiceDate"]) ||
                    string.IsNullOrWhiteSpace(httpRequest.Form["NextServiceDate"]))
                {
                    return BadRequest("AssetId, ServiceDate, and NextServiceDate are required.");
                }

                int assetId = Convert.ToInt32(httpRequest.Form["AssetId"]);
                DateTime serviceDate = Convert.ToDateTime(httpRequest.Form["ServiceDate"]);
                DateTime nextServiceDate = Convert.ToDateTime(httpRequest.Form["NextServiceDate"]);
                string remark = httpRequest.Form["Remark"];
                string servicedBy = httpRequest.Form["ServicedBy"];
                string approvedBy = httpRequest.Form["ApprovedBy"];

                decimal serviceCost = 0;
                decimal.TryParse(httpRequest.Form["ServiceCost"], out serviceCost);

                // ✅ File handling
                string imageUrl = null;
                string docUrl = null;

                HttpPostedFile imageFile = httpRequest.Files["ServiceImg"];
                HttpPostedFile docFile = httpRequest.Files["ServiceDoc"];

                // 🔹 Save only if file exists
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    imageUrl = SaveFile(imageFile, @"C:\inetpub\wwwroot\URestAPI\Uploads\Serviceimg\", "Uploads/Serviceimg/");
                }

                if (docFile != null && docFile.ContentLength > 0)
                {
                    docUrl = SaveFile(docFile, @"C:\inetpub\wwwroot\URestAPI\Uploads\Servicedoc\", "Uploads/Servicedoc/");
                }

                // ✅ Insert record and update asset
                using (var connection = new SqlConnection(constr))
                {
                    connection.Open();

                    string query = @"
                INSERT INTO AssetServiceRecord 
                    (AssetId, ServiceDate, NextServiceDate, Image, Remark, ServiceDoc, ServiceCost, ServicedBy, ApprovedBy)
                VALUES 
                    (@AssetId, @ServiceDate, @NextServiceDate, @Image, @Remark, @ServiceDoc, @ServiceCost, @ServicedBy, @ApprovedBy);

                UPDATE AssetMaster
                SET LastServiceDate = @ServiceDate,
                    NextServiceDate = @NextServiceDate
                WHERE Id = @AssetId;
            ";

                    using (var cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@AssetId", assetId);
                        cmd.Parameters.AddWithValue("@ServiceDate", serviceDate);
                        cmd.Parameters.AddWithValue("@NextServiceDate", nextServiceDate);
                        cmd.Parameters.AddWithValue("@Image", (object)imageUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remark", string.IsNullOrWhiteSpace(remark) ? (object)DBNull.Value : remark);
                        cmd.Parameters.AddWithValue("@ServiceDoc", (object)docUrl ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ServiceCost", serviceCost);
                        cmd.Parameters.AddWithValue("@ServicedBy", string.IsNullOrWhiteSpace(servicedBy) ? (object)DBNull.Value : servicedBy);
                        cmd.Parameters.AddWithValue("@ApprovedBy", string.IsNullOrWhiteSpace(approvedBy) ? (object)DBNull.Value : approvedBy);

                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            return Ok(new
                            {
                                message = "✅ Service Record added successfully.",
                                imageUrl,
                                docUrl
                            });
                        }
                        else
                        {
                            return InternalServerError(new Exception("Failed to insert service record."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 🔹 Return clean error message
                return InternalServerError(new Exception("❌ Error while saving record: " + ex.Message));
            }
        }

        private string SaveFile(HttpPostedFile file, string physicalFolder, string virtualFolder)
        {
            try
            {
                if (!Directory.Exists(physicalFolder))
                {
                    Directory.CreateDirectory(physicalFolder);
                }

                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(physicalFolder, fileName);

                file.SaveAs(filePath);

                // 🔹 Public URL generation
                string baseUrl = "https://api.urest.in:8096/";
                string fileUrl = $"{baseUrl}{virtualFolder}{fileName}".Replace("\\", "/");
                return fileUrl;
            }
            catch (Exception ex)
            {
                throw new Exception("File save error: " + ex.Message);
            }
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
