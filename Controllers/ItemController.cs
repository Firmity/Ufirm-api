using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/itemmaster")]
    public class ProductController : ApiController
    {
        private readonly string constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        // ✅ GET: api/itemmaster/getItem
        [HttpGet]
        [Route("getItem")]
        public async Task<IHttpActionResult> GetAllProducts()
        {
            var list = new List<Item>();
            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = "SELECT id, name, specification FROM app.itemmaster";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Item
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Name = reader["name"].ToString(),
                            Specification = reader["specification"].ToString()
                        });
                    }
                }
            }
            return Ok(list);
        }

        [HttpGet]
        [Route("getItemByName/{name}")]
        public async Task<IHttpActionResult> GetItemByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Item name is required.");

            var items = new List<Item>();

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"
            SELECT id, name, specification 
            FROM app.itemmaster 
            WHERE LOWER(name) LIKE LOWER('%' + @name + '%')
            ORDER BY name;";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", name);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync()) // <-- ✅ changed from IF to WHILE
                        {
                            items.Add(new Item
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["name"].ToString(),
                                Specification = reader["specification"].ToString()
                            });
                        }
                    }
                }
            }

            if (items.Count == 0)
                return NotFound();

            return Ok(items);
        }

        // ✅ POST: api/itemmaster/postitem
        [HttpPost]
        [Route("postitem")]
        public async Task<IHttpActionResult> AddProduct([FromBody] Item model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest("Invalid product data");

            using (var conn = new SqlConnection(constr))
            {
                await conn.OpenAsync();
                string query = @"INSERT INTO app.itemmaster (name, specification) 
                                 VALUES (@name, @specification);
                                 SELECT SCOPE_IDENTITY();";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@specification", (object)model.Specification ?? DBNull.Value);

                    int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new { message = "Product added successfully", id = newId });
                }
            }
        }
    }
}
