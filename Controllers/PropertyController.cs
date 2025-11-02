using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    public class PropertyController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpPost]
        [Route("api/Property/ManagePropertyDetails")]
        public async Task<IHttpActionResult> ManagePropertyDetails([FromBody] PropertyDetails request)
        {
            // Validate input
            if (request == null || string.IsNullOrWhiteSpace(request.CmdType))
            {
                return BadRequest("Invalid request data.");
            }

            try
            {
                using (var con = new SqlConnection(constr))
                {
                    await con.OpenAsync();

                    using (var command = new SqlCommand("App.ManagePropertyDetails", con))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@PropertyDetailsId", request.PropertyDetailsId);
                        command.Parameters.AddWithValue("@PropertyId", request.PropertyId);
                        command.Parameters.AddWithValue("@PropertyTowerId", request.PropertyTowerId);
                        command.Parameters.AddWithValue("@Floor", request.Floor);
                        command.Parameters.AddWithValue("@Flat", request.Flat);
                        command.Parameters.AddWithValue("@ContactNumber", request.ContactNumber ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@UserId", request.UserId);
                        command.Parameters.AddWithValue("@PropertyDetailTypeId", request.PropertyDetailTypeId);
                        command.Parameters.AddWithValue("@TotalArea", request.TotalArea);
                        command.Parameters.AddWithValue("@BuiltupArea", request.BuiltupArea);
                        command.Parameters.AddWithValue("@CarpetArea", request.CarpetArea);
                        command.Parameters.AddWithValue("@SuperBuilUpArea", request.SuperBuilUpArea);
                        command.Parameters.AddWithValue("@MeasurementUnitsId", request.MeasurementUnitsId);
                        command.Parameters.AddWithValue("@UniteConfiguration", request.UniteConfiguration);
                        command.Parameters.AddWithValue("@cmdType", request.CmdType);

                        // If the command type is 'R', read the results
                        if (request.CmdType == "R")
                        {
                            var propertyDetailsList = new List<PropertyDetails>();

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var propertyDetails = new PropertyDetails
                                    {
                                        PropertyDetailsId = reader.GetInt32(reader.GetOrdinal("PropertyDetailsId")),
                                        PropertyId = reader.GetInt32(reader.GetOrdinal("PropertyId")),
                                        PropertyTowerId = reader.GetInt32(reader.GetOrdinal("PropertyTowerId")),
                                        Floor = reader.GetInt32(reader.GetOrdinal("Floor")),
                                        Flat = reader["Flat"] as string,
                                        ContactNumber = reader["ContactNumber"] as string,
                                        PropertyDetailTypeId = reader.GetInt32(reader.GetOrdinal("PropertyDetailTypeId")),
                                        TotalArea = reader["TotalArea"] as string,
                                        BuiltupArea = reader["BuiltupArea"] as string,
                                        CarpetArea = reader["CarpetArea"] as string,
                                        SuperBuilUpArea = reader["SuperBuilUpArea"] as string,
                                        MeasurementUnitsId = reader["MeasurementUnitsId"] as string,
                                        UniteConfiguration = reader["UniteConfiguration"] as string,
                                        TowerName = reader["Towername"] as string
                                        // Add any other properties you need to map
                                    };

                                    propertyDetailsList.Add(propertyDetails);
                                }
                            }

                            return Ok(propertyDetailsList);
                        }
                        else
                        {
                            // Execute the command for Create, Update, or Delete
                            await command.ExecuteNonQueryAsync();
                            return Ok("Operation completed successfully.");
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return InternalServerError(new Exception("Database error occurred: " + sqlEx.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An error occurred while processing your request. Please try again later.", ex));
            }
        }

        [HttpGet]
        [Route("api/Property/GetPropertyDetailsByQRCode")]
        public async Task<IHttpActionResult> GetPropertyDetailsByQRCode(string qrCode)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return BadRequest("QR Code is required and cannot be empty.");
            }

            try
            {
                var propertyDetails = new List<PropertyDetails>();

                using (var con = new SqlConnection(constr))
                {
                    await con.OpenAsync();

                    string query = @"
                        SELECT pd.PropertyDetailsId, pd.PropertyId, pd.PropertyTowerId, pm.Name AS PropertyName, pt.TowerName, pd.Flat 
                  FROM app.PropertyDetails pd
                  INNER JOIN app.PropertyMaster pm ON pd.PropertyId = pm.PropertyId
                  INNER JOIN app.PropertyTowers pt ON pd.PropertyTowerId = pt.PropertyTowerId
                  WHERE pd.QRCode = @QRCode";

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
                                    propertyDetails.Add(new PropertyDetails
                                    {
                                        PropertyDetailsId = reader.GetInt32(reader.GetOrdinal("PropertyDetailsId")),
                                        PropertyId = reader.GetInt32(reader.GetOrdinal("PropertyId")),
                                        PropertyTowerId = reader.GetInt32(reader.GetOrdinal("PropertyTowerId")),
                                        PropertyName = reader["PropertyName"] as string,
                                        TowerName = reader["TowerName"] as string,
                                        Flat = reader["Flat"] as string

                                    });
                                }

                                return Ok(propertyDetails);
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
                return InternalServerError(new Exception("An error occurred while fetching the property details. Please try again later.", ex));
            }
        }

        [HttpGet]
        [Route("api/property/{propertyId}")]
        public IHttpActionResult GetPropertyById(int propertyId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    PropertyId,
                    PropertyTypeId,
                    Name,
                    AddressLine1,
                    AddressLine12,
                    CityId,
                    ContactNumber,
                    LanguageId,
                    ProjectArea,
                    TotalTowers,
                    Totalunits,
                    TotalCommercialUnits,
                    Landmark,
                    Pincode,
                    IsActive,
                    CreatedBy,
                    CreatedOn,
                    UpdateOn,
                    updatedby,
                    IsDeleted,
                    Latitude,
                    Longitude
                FROM App.PropertyMaster
                WHERE PropertyId = @PropertyId AND IsActive = 1";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var property = new
                        {
                            PropertyId = reader["PropertyId"],
                            PropertyTypeId = reader["PropertyTypeId"],
                            Name = reader["Name"],
                            AddressLine1 = reader["AddressLine1"],
                            AddressLine12 = reader["AddressLine12"],
                            CityId = reader["CityId"],
                            ContactNumber = reader["ContactNumber"],
                            LanguageId = reader["LanguageId"],
                            ProjectArea = reader["ProjectArea"],
                            TotalTowers = reader["TotalTowers"],
                            Totalunits = reader["Totalunits"],
                            TotalCommercialUnits = reader["TotalCommercialUnits"],
                            Landmark = reader["Landmark"],
                            Pincode = reader["Pincode"],
                            IsActive = reader["IsActive"],
                            CreatedBy = reader["CreatedBy"],
                            CreatedOn = reader["CreatedOn"],
                            UpdateOn = reader["UpdateOn"],
                            updatedby = reader["updatedby"],
                            IsDeleted = reader["IsDeleted"],
                            Latitude = reader["Latitude"],
                            Longitude = reader["Longitude"]
                        };

                        return Ok(property);
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
    }
}
