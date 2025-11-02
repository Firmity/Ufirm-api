using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/master")]
    public class MasterController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ✅ GET ALL FREQUENCIES
        [HttpGet]
        [Route("getAllFrequency")]
        public async Task<IHttpActionResult> GetAllFrequency()
        {
            try
            {
                List<FrequencyModel> frequencies = new List<FrequencyModel>();

                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT frequency_id AS Id, frequency_name AS Name,frequency_value,frequency_unit,f_occurence as Occurence
                                     FROM Master.Frequency 
                                     WHERE is_active = 1;";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                frequencies.Add(new FrequencyModel
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Fvalue = reader.GetInt32(2),
                                    Funit = reader.GetString(3),
                                    Occurence = reader.GetString(4),
                                });
                            }
                        }
                    }
                }

                return Ok(frequencies);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ ADD NEW FREQUENCY
        [HttpPost]
        [Route("addFrequency")]
        public async Task<IHttpActionResult> AddFrequency([FromBody] FrequencyModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Name) || model.Fvalue < 1 || string.IsNullOrEmpty(model.Funit))
                return BadRequest("Invalid data provided.");

            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    // Extract first three characters from the Name
                    string occurrence = model.Name.Length >= 3 ? model.Name.Substring(0, 3).ToUpper() : model.Name.ToUpper();

                    string query = @"INSERT INTO Master.Frequency (frequency_name, frequency_value, frequency_unit, f_occurence, is_active) 
                             VALUES (@Name, @FrequencyValue, @FrequencyUnit, @Occurence, 1);";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Name", model.Name);
                        cmd.Parameters.AddWithValue("@FrequencyValue", model.Fvalue);
                        cmd.Parameters.AddWithValue("@FrequencyUnit", model.Funit);
                        cmd.Parameters.AddWithValue("@Occurence", occurrence); // Auto-generate Occurrence

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return Ok(rowsAffected > 0 ? 1 : 0); // Return 1 for success, 0 for failure
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ UPDATE FREQUENCY
        [HttpPut]
        [Route("updateFrequency/{id}")]
        public async Task<IHttpActionResult> UpdateFrequency(int id, [FromBody] FrequencyModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.Name))
                return BadRequest("Invalid data provided.");

            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    // Generate new occurrence value based on Name
                    string occurrence = model.Name.Length >= 3 ? model.Name.Substring(0, 3).ToUpper() : model.Name.ToUpper();

                    string query = @"UPDATE Master.Frequency 
                             SET frequency_name = @Name, f_occurence = @Occurence 
                             WHERE frequency_id = @Id;";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", model.Name);
                        cmd.Parameters.AddWithValue("@Occurence", occurrence);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return Ok(rowsAffected > 0 ? 1 : 0); // Return 1 for success, 0 for failure
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // ✅ DELETE FREQUENCY (SOFT DELETE)
        [HttpDelete]
        [Route("deleteFrequency/{id}")]
        public async Task<IHttpActionResult> DeleteFrequency(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    string query = @"UPDATE Master.Frequency SET is_active = 0 WHERE frequency_id = @Id;";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return Ok(rowsAffected > 0 ? 1 : 0); // Return 1 for success, 0 for failure
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
