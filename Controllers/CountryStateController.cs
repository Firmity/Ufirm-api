using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/location")]
    public class LocationController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // GET: api/location/countries
        [HttpGet]
        [Route("countries")]
        public IHttpActionResult GetAllCountries()
        {
            List<Country> countries = new List<Country>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT CountryId, CountryName FROM App.CountryMaster ORDER BY CountryName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            countries.Add(new Country
                            {
                                CountryId = Convert.ToInt32(reader["CountryId"]),
                                Name = reader["CountryName"].ToString()
                            });
                        }
                    }
                }
            }

            if (countries.Count == 0)
                return NotFound();

            return Ok(countries);
        }

        // GET: api/location/states/{countryId}
        [HttpGet]
        [Route("states/{countryId:int}")]
        public IHttpActionResult GetStatesByCountryId(int countryId)
        {
            if (countryId <= 0)
                return BadRequest("Invalid CountryId");

            List<State> states = new List<State>();
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT StateId, CountryId, StateName FROM App.StateMaster WHERE CountryId = @CountryId ORDER BY StateName";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CountryId", countryId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            states.Add(new State
                            {
                                StateId = Convert.ToInt32(reader["StateId"]),
                                CountryId = Convert.ToInt32(reader["CountryId"]),
                                StateName = reader["StateName"].ToString()
                            });
                        }
                    }
                }
            }

            if (states.Count == 0)
                return NotFound();

            return Ok(states);
        }

        // GET: api/location/state/{stateId}
        [HttpGet]
        [Route("state/{stateId:int}")]
        public IHttpActionResult GetStateById(int stateId)
        {
            if (stateId <= 0)
                return BadRequest("Invalid StateId");

            State state = null;
            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "SELECT StateId, CountryId, StateName FROM App.StateMaster WHERE StateId = @StateId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StateId", stateId);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            state = new State
                            {
                                StateId = Convert.ToInt32(reader["StateId"]),
                                CountryId = Convert.ToInt32(reader["CountryId"]),
                                StateName = reader["StateName"].ToString()
                            };
                        }
                    }
                }
            }

            if (state == null)
                return NotFound();

            return Ok(state);
        }

        // Optional: Add Country
        [HttpPost]
        [Route("country/add")]
        public IHttpActionResult AddCountry([FromBody] Country country)
        {
            if (country == null || string.IsNullOrEmpty(country.Name))
                return BadRequest("Invalid country data");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "INSERT INTO App.CountryMaster (CountryName) VALUES (@Name); SELECT SCOPE_IDENTITY();";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", country.Name);
                    conn.Open();
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                    return Ok(new { message = "Country added successfully", CountryId = newId });
                }
            }
        }

        // Optional: Add State
        [HttpPost]
        [Route("state/add")]
        public IHttpActionResult AddState([FromBody] State state)
        {
            if (state == null || string.IsNullOrEmpty(state.StateName) || state.CountryId <= 0)
                return BadRequest("Invalid state data");

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "INSERT INTO App.StateMaster (CountryId, StateName) VALUES (@CountryId, @StateName); SELECT SCOPE_IDENTITY();";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CountryId", state.CountryId);
                    cmd.Parameters.AddWithValue("@StateName", state.StateName);
                    conn.Open();
                    int newId = Convert.ToInt32(cmd.ExecuteScalar());
                    return Ok(new { message = "State added successfully", StateId = newId });
                }
            }
        }
    }
}
