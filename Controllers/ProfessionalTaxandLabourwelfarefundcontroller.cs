using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/master")]
    public class ProfessionalTaxandLabourwelfarefundController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // GET all PFTs
        [HttpGet]
        [Route("pft")]
        public IHttpActionResult GetAllPft()
        {
            List<PftMaster> list = new List<PftMaster>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT PftId, StateId, AmountFrom, AmountTo, PftAmount FROM App.PftMaster";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PftMaster
                    {
                        PftId = Convert.ToInt32(reader["PftId"]),
                        StateId = Convert.ToInt32(reader["StateId"]),
                        AmountFrom = Convert.ToDecimal(reader["AmountFrom"]),
                        AmountTo = Convert.ToDecimal(reader["AmountTo"]),
                        PftAmount = Convert.ToDecimal(reader["PftAmount"])
                    });
                }
            }
            return Ok(list);
        }

        // GET PFT by Id
        [HttpGet]
        [Route("pft/{id:int}")]
        public IHttpActionResult GetPftById(int id)
        {
            PftMaster pft = null;
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT PftId, StateId, AmountFrom, AmountTo, PftAmount FROM App.PftMaster WHERE PftId=@PftId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PftId", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    pft = new PftMaster
                    {
                        PftId = Convert.ToInt32(reader["PftId"]),
                        StateId = Convert.ToInt32(reader["StateId"]),
                        AmountFrom = Convert.ToDecimal(reader["AmountFrom"]),
                        AmountTo = Convert.ToDecimal(reader["AmountTo"]),
                        PftAmount = Convert.ToDecimal(reader["PftAmount"])
                    };
                }
            }

            if (pft == null) return NotFound();
            return Ok(pft);
        }

        // POST add PFT
        [HttpPost]
        [Route("pft/add")]
        public IHttpActionResult AddPft([FromBody] PftMaster pft)
        {
            if (pft == null) return BadRequest("Invalid data");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"INSERT INTO App.PftMaster ( StateId, AmountFrom, AmountTo, PftAmount)
                                 VALUES ( @StateId, @AmountFrom, @AmountTo, @PftAmount)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@StateId", pft.StateId);
                cmd.Parameters.AddWithValue("@AmountFrom", pft.AmountFrom);
                cmd.Parameters.AddWithValue("@AmountTo", pft.AmountTo);
                cmd.Parameters.AddWithValue("@PftAmount", pft.PftAmount);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "PFT record added successfully" });
        }

        // PUT update PFT
        [HttpPut]
        [Route("pft/{id:int}")]
        public IHttpActionResult UpdatePft(int id, [FromBody] PftMaster pft)
        {
            if (pft == null) return BadRequest("Invalid data");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"UPDATE App.PftMaster
                                 SET StateId=@StateId, AmountFrom=@AmountFrom, AmountTo=@AmountTo, PftAmount=@PftAmount
                                 WHERE PftId=@PftId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PftId", id);
                cmd.Parameters.AddWithValue("@StateId", pft.StateId);
                cmd.Parameters.AddWithValue("@AmountFrom", pft.AmountFrom);
                cmd.Parameters.AddWithValue("@AmountTo", pft.AmountTo);
                cmd.Parameters.AddWithValue("@PftAmount", pft.PftAmount);

                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0) return NotFound();
            }

            return Ok(new { message = "PFT record updated successfully" });
        }

        // DELETE PFT
        [HttpDelete]
        [Route("pft/{id:int}")]
        public IHttpActionResult DeletePft(int id)
        {
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "DELETE FROM App.PftMaster WHERE PftId=@PftId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PftId", id);
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                if (rows == 0) return NotFound();
            }

            return Ok(new { message = "PFT record deleted successfully" });
        }


        // GET all LWF
        [HttpGet]
        [Route("lwf")]
        public IHttpActionResult GetAllLwf()
        {
            List<LwfMaster> list = new List<LwfMaster>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT LwfId, StateId, LwfAmount, EmployeeAmount, EmployerAmount FROM App.LwfMaster";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new LwfMaster
                    {
                        LwfId = Convert.ToInt32(reader["LwfId"]),
                        StateId = Convert.ToInt32(reader["StateId"]),
                        LwfAmount = Convert.ToDecimal(reader["LwfAmount"]),
                        EmployeeAmount = Convert.ToDecimal(reader["EmployeeAmount"]),
                        EmployerAmount = Convert.ToDecimal(reader["EmployerAmount"])
                    });
                }
            }
            return Ok(list);
        }

        // GET LWF by Id
        [HttpGet]
        [Route("lwf/{id:int}")]
        public IHttpActionResult GetLwfById(int id)
        {
            LwfMaster lwf = null;
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT LwfId, StateId, LwfAmount, EmployeeAmount, EmployerAmount FROM App.LwfMaster WHERE LwfId=@LwfId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@LwfId", id);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    lwf = new LwfMaster
                    {
                        LwfId = Convert.ToInt32(reader["LwfId"]),
                        StateId = Convert.ToInt32(reader["StateId"]),
                        LwfAmount = Convert.ToDecimal(reader["LwfAmount"]),
                        EmployeeAmount = Convert.ToDecimal(reader["EmployeeAmount"]),
                        EmployerAmount = Convert.ToDecimal(reader["EmployerAmount"])
                    };
                }
            }

            if (lwf == null) return NotFound();
            return Ok(lwf);
        }

        // POST add LWF
        [HttpPost]
        [Route("lwf/add")]
        public IHttpActionResult AddLwf([FromBody] LwfMaster lwf)
        {
            if (lwf == null) return BadRequest("Invalid data");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = @"INSERT INTO App.LwfMaster (StateId, LwfAmount, EmployeeAmount, EmployerAmount)
                                 VALUES (@StateId, @LwfAmount, @EmployeeAmount, @EmployerAmount)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@StateId", lwf.StateId);
                cmd.Parameters.AddWithValue("@LwfAmount", lwf.LwfAmount);
                cmd.Parameters.AddWithValue("@EmployeeAmount", lwf.EmployeeAmount);
                cmd.Parameters.AddWithValue("@EmployerAmount", lwf.EmployerAmount);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "LWF record added successfully" });
        }

            // PUT update LWF
            [HttpPut]
            [Route("lwf/{id:int}")]
            public IHttpActionResult UpdateLwf(int id, [FromBody] LwfMaster lwf)
            {
                if (lwf == null) return BadRequest("Invalid data");

                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = @"UPDATE App.LwfMaster
                                 SET StateId=@StateId, LwfAmount=@LwfAmount, EmployeeAmount=@EmployeeAmount, EmployerAmount=@EmployerAmount
                                 WHERE LwfId=@LwfId";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@LwfId", id);
                    cmd.Parameters.AddWithValue("@StateId", lwf.StateId);
                    cmd.Parameters.AddWithValue("@LwfAmount", lwf.LwfAmount);
                    cmd.Parameters.AddWithValue("@EmployeeAmount", lwf.EmployeeAmount);
                    cmd.Parameters.AddWithValue("@EmployerAmount", lwf.EmployerAmount);

                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }

                return Ok(new { message = "LWF record updated successfully" });
            }

            // DELETE LWF
            [HttpDelete]
            [Route("lwf/{id:int}")]
            public IHttpActionResult DeleteLwf(int id)
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = "DELETE FROM App.LwfMaster WHERE LwfId=@LwfId";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@LwfId", id);
                    conn.Open();
                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0) return NotFound();
                }

                return Ok(new { message = "LWF record deleted successfully" });
            }
            
        
    }
}