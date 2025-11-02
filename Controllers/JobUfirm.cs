using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/jobs")]
    public class JobsController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // GET: api/jobs?search=...
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetJobs(string search = "")
        {
            var list = new List<Job>();
            string query = "SELECT * FROM app.JobMaster WHERE is_active = 1";

            if (!string.IsNullOrEmpty(search))
                query += " AND title LIKE '%' + @search + '%'";

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@search", search);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(MapReaderToJob(reader));
                        }
                    }
                }
            }

            return Ok(list);
        }

        // POST: api/jobs
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateJob([FromBody] Job model)
        {
            if (model == null)
                return BadRequest("Invalid job data.");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"
                    INSERT INTO app.JobMaster
                    (title, type, education, ctc, company, department, designation, image_url, posted, is_active, created_by, created_on)
                    VALUES
                    (@title, @type, @education, @ctc, @company, @department, @designation, @image_url, @posted, 1, @created_by, GETDATE());
                    SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParameters(cmd, model);

                    int insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    model.Id = insertedId;
                    model.IsActive = true;
                    model.CreatedOn = DateTime.Now;
                }
            }

            return Ok(new { message = "Job posted successfully", data = model });
        }

        // PUT: api/jobs/{id}
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> UpdateJob(int id, [FromBody] Job model)
        {
            if (model == null)
                return BadRequest("Invalid job data.");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"
                    UPDATE app.JobMaster
                    SET title=@title, type=@type, education=@education, ctc=@ctc, company=@company,
                        department=@department, designation=@designation, image_url=@image_url, posted=@posted,
                        updated_by=@updated_by, updated_on=GETDATE()
                    WHERE id=@id AND is_active=1";

                using (var cmd = new SqlCommand(query, conn))
                {
                    AddParameters(cmd, model);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@updated_by", model.UpdatedBy ?? (object)DBNull.Value);

                    int rows = await cmd.ExecuteNonQueryAsync();
                    if (rows == 0)
                        return NotFound();
                }
            }

            return Ok(new { message = "Job updated successfully", data = model });
        }

        // DELETE: api/jobs/{id}
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> DeleteJob(int id)
        {
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = "UPDATE app.JobMaster SET is_active=0 WHERE id=@id";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows == 0)
                        return NotFound();
                }
            }

            return Ok(new { message = "Job soft-deleted successfully" });
        }

        // Map SQL reader to Job object
        private Job MapReaderToJob(SqlDataReader reader)
        {
            return new Job
            {
                Id = Convert.ToInt32(reader["id"]),
                Title = reader["title"].ToString(),
                Type = reader["type"].ToString(),
                Education = reader["education"].ToString(),
                CTC = reader["ctc"].ToString(),
                Company = reader["company"].ToString(),
                Department = reader["department"].ToString(),
                Designation = reader["designation"].ToString(),
                ImageUrl = reader["image_url"] == DBNull.Value ? null : reader["image_url"].ToString(), // Base64
                Posted = reader["posted"] == DBNull.Value ? null : reader["posted"].ToString(),
                IsActive = Convert.ToBoolean(reader["is_active"]),
                CreatedBy = Convert.ToInt32(reader["created_by"]),
                CreatedOn = Convert.ToDateTime(reader["created_on"]),
                UpdatedBy = reader["updated_by"] == DBNull.Value ? null : (int?)reader["updated_by"],
                UpdatedOn = reader["updated_on"] == DBNull.Value ? null : (DateTime?)reader["updated_on"]
            };
        }

        // Add parameters for insert/update
        private void AddParameters(SqlCommand cmd, Job model)
        {
            cmd.Parameters.AddWithValue("@title", model.Title ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@type", model.Type ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@education", model.Education ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@ctc", model.CTC ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@company", model.Company ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@department", model.Department ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@designation", model.Designation ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@image_url", model.ImageUrl ?? (object)DBNull.Value); // Base64
            cmd.Parameters.AddWithValue("@posted", model.Posted ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", model.CreatedBy);
        }
    }

}