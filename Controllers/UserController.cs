using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    public class UserController : ApiController
    {
        string constr = string.Empty;
        Integrations integrations = new Integrations();
        public UserController()
        {
            constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        }

        [HttpGet]
        [Route("GetAllUserTypes")]
        public IHttpActionResult GetAllUserTypes()
        {
            string query = @"
SELECT UserTypeId, UserTypeName, Description, UpdateOn, CreatedOn, UpdatedBy, CreatedBy 
FROM [Identity].UserType";

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<UserType> userTypes = new List<UserType>();
                    while (reader.Read())
                    {
                        userTypes.Add(new UserType
                        {
                            UserTypeId = reader.GetInt32(0),
                            UserTypeName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            UpdateOn = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                            CreateOn = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4), // Adjusted column name
                            UpdatedBy = reader.GetInt32(0),
                            CreateBy = reader.GetInt32(0),
                        });
                    }
                    return Ok(userTypes);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetAllCity")]
        public IHttpActionResult GetAllCity()
        {
            List<City> cities = new List<City>();

            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    string query = "SELECT CityId, CityName, StateId FROM Master.City"; // Adjusted query
                    SqlCommand command = new SqlCommand(query, connection);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cities.Add(new City
                            {
                                CityId = reader.GetInt32(0),
                                CityName = reader.IsDBNull(1) ? null : reader.GetString(1),
                                StateId = reader.GetInt32(2)
                            });
                        }
                    }
                }

                if (!cities.Any())
                {
                    return NotFound(); // Return 404 if no cities found
                }

                return Ok(cities); // Return 200 with the list of cities
            }
            catch (Exception ex)
            {
                return InternalServerError(ex); // Return 500 with error details
            }
        }

        [HttpGet]
        [Route("GetAllUserRole")]
        public IHttpActionResult GetAllUserRole()
        {
            string query = "SELECT UserRoleId, RoleName FROM [Identity].UserRole"; // Adjust the query to match actual columns

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand(query, con);
                    SqlDataReader reader = cmd.ExecuteReader();

                    List<UserRole> userRoles = new List<UserRole>();
                    while (reader.Read())
                    {
                        userRoles.Add(new UserRole
                        {
                            UserRoleId = reader.GetInt32(reader.GetOrdinal("UserRoleId")),
                            UserRoleName = reader["RoleName"]?.ToString()
                        });
                    }

                    if (!userRoles.Any())
                    {
                        return NotFound(); // Return 404 if no records
                    }

                    return Ok(userRoles); // Return 200 with data
                }
            }
            catch (SqlException sqlEx)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, sqlEx.Message));
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("GetAllBranch")]
        public IHttpActionResult GetAllBranch()
        {
            List<Branch> branches = new List<Branch>();

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    // Define the SQL query to get branches where IsDeleted is not equal to 1
                    string query = "SELECT BranchId, BranchName, IsDeleted FROM Master.Branch WHERE IsDeleted != 1";

                    SqlCommand command = new SqlCommand(query, con);

                    con.Open(); // Open the connection
                    SqlDataReader reader = command.ExecuteReader(); // Execute the query and get the data

                    while (reader.Read())
                    {
                        branches.Add(new Branch
                        {
                            BranchId = reader.GetInt32(reader.GetOrdinal("BranchId")),
                            BranchName = reader.GetString(reader.GetOrdinal("BranchName")),
                        });
                    }
                }

                // Check if no branches were found
                if (branches.Count == 0)
                {
                    return NotFound(); // Return 404 if no branches are found
                }

                // Return the list of branches as a JSON response
                return Ok(branches);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex); // Return an error response if something goes wrong
            }
        }


        [HttpPost]
        [Route("CreateNewUser")]
        public IHttpActionResult CreateNewUser(UserData User)
        {
            if (User == null)
                return BadRequest("Invalid user data.");

            if (string.IsNullOrEmpty(User.EmployeeId) || string.IsNullOrEmpty(User.FirstName) || string.IsNullOrEmpty(User.Email))
            {
                return BadRequest("EmployeeId, FirstName, and Email are required fields.");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    if (User.RoleId == 6 || User.RoleId == 9) // Check for supervisor
                    {
                        string designation = string.Empty;

                        var fetchRoleQuery = @"SELECT RoleName FROM [UfirmApp_Production].[Identity].[UserRole] 
                                            WHERE UserRoleId = @RoleId";

                        using (var fetchRoleCommand = new SqlCommand(fetchRoleQuery, con))
                        {
                            // Map the RoleId parameter
                            fetchRoleCommand.Parameters.AddWithValue("@RoleId", User.RoleId);

                            // Execute the query and retrieve the RoleName
                            var result = fetchRoleCommand.ExecuteScalar();
                            if (result != null)
                            {
                                designation = result.ToString().ToUpper(); // Convert the RoleName to uppercase
                            }
                            else
                            {
                                return BadRequest("Invalid RoleId. No matching RoleName found.");
                            }
                        }


                        var createSupervisorQuery = @"INSERT INTO [dbo].[EmployeeList](EmployeeName, FatherName, Designation, MobileNo, IsDeleted, Approved)
                                                    VALUES(@EmployeeName, @FatherName, @Designation, @MobileNo, @IsDeleted, @Approved)";

                        using (var command = new SqlCommand(createSupervisorQuery, con))
                        {
                            // Map parameters to the SQL query
                            command.Parameters.AddWithValue("@EmployeeName", User.FirstName + " " + User.LastName);
                            command.Parameters.AddWithValue("@FatherName", DBNull.Value);
                            command.Parameters.AddWithValue("@Designation", designation); // Use the uppercase designation
                            command.Parameters.AddWithValue("@MobileNo", User.PhoneNumber);
                            command.Parameters.AddWithValue("@IsDeleted", 0);
                            command.Parameters.AddWithValue("@Approved", 1);

                            // Execute the query to insert the employee data
                            int result = command.ExecuteNonQuery();

                            // Return success message
                            if (result > 0)
                            {
                                return Ok(new { Message = "Supervisor ID created successfully." });
                            }
                            else
                            {
                                return BadRequest("Failed to create Supervisor ID.");
                            }
                        }
                    }
                                      
                    var query = @"
                    DECLARE @NewUserId INT;
                    INSERT INTO [UfirmApp_Production].[Identity].[Users]
                    (
                        [EmpCode],[FirstName], [LastName], [EmailAddress], [ContactNumber], [CityId], [UserTypeId], [PropertyId], [ProfileImageUrl],[Accesskey], [IsActive], [IsDeleted], [CreatedOn],[CreatedBy],[BranchId],[UpdatedOn],
                        [UpdatedBy],[LastLoginDateTime], [VendorId],[DepartmentId], [IsFirstLogin])
                    VALUES
                    (
                        @EmpCode,@FirstName, @LastName, @EmailAddress, @ContactNumber,  @CityId,  @UserTypeId,@PropertyId,
                        @ProfileImageUrl,@Accesskey, @IsActive, @IsDeleted,GETDATE(), @CreatedBy,  @BranchId, GETDATE(),
                        @UpdatedBy,  NULL,@VendorId,@DepartmentId, @IsFirstLogin);
                        SET @NewUserId = SCOPE_IDENTITY();
                        SELECT @NewUserId AS UserId;";

                    using (var command = new SqlCommand(query, con))
                    {
                        // Map parameters
                        command.Parameters.AddWithValue("@EmpCode", User.EmployeeId);
                        command.Parameters.AddWithValue("@FirstName", User.FirstName);
                        command.Parameters.AddWithValue("@LastName", User.LastName);
                        command.Parameters.AddWithValue("@EmailAddress", User.Email);
                        command.Parameters.AddWithValue("@ContactNumber", User.PhoneNumber);
                        command.Parameters.AddWithValue("@CityId", DBNull.Value); // Replace with actual CityId lookup
                        command.Parameters.AddWithValue("@UserTypeId", User.UserTypeId); // Replace with actual UserTypeId lookup
                        command.Parameters.AddWithValue("@PropertyId", DBNull.Value); // Replace with actual PropertyId lookup
                        command.Parameters.AddWithValue("@ProfileImageUrl", "/uploads/" + User.ProfileImage?.ToString());
                        command.Parameters.AddWithValue("@Accesskey", "6Cjlgd9KqH0cHpZXkmhgdg=="); // Generate if required
                        command.Parameters.AddWithValue("@IsActive", 1);
                        command.Parameters.AddWithValue("@IsDeleted", 0);
                        command.Parameters.AddWithValue("@CreatedBy", 1); // Replace with actual user
                        command.Parameters.AddWithValue("@BranchId", DBNull.Value); // Replace with actual BranchId lookup
                        command.Parameters.AddWithValue("@UpdatedBy", 1); // Replace with actual user
                        command.Parameters.AddWithValue("@VendorId", DBNull.Value); // Replace with actual VendorId
                        command.Parameters.AddWithValue("@DepartmentId", DBNull.Value); // Replace with actual DepartmentId
                        command.Parameters.AddWithValue("@IsFirstLogin", 1);

                        var userId = command.ExecuteScalar();

                        // Insert UserRoleMapping using the returned UserId
                        var roleMappingQuery = @"
                        INSERT INTO [UfirmApp_Production].[Identity].[UserRoleMapping]
                        (
                            [UserId], [RoleId], [CreatedOn], [CreatedBy], [Status], [UpdatedBy], [UpdatedOn]
                        )
                        VALUES
                        (
                            @UserId, @RoleId, GETDATE(), @CreatedBy, 1, @UpdatedBy, GETDATE()
                        );
                    ";

                        using (var roleMappingCommand = new SqlCommand(roleMappingQuery, con))
                        {
                            // Add parameters for UserRoleMapping
                            roleMappingCommand.Parameters.AddWithValue("@UserId", userId); // Using the UserId from the previous query
                            roleMappingCommand.Parameters.AddWithValue("@RoleId", User.RoleId); // Assuming you have a RoleId in the User object
                            roleMappingCommand.Parameters.AddWithValue("@CreatedBy", 1); // Replace with actual user
                            roleMappingCommand.Parameters.AddWithValue("@UpdatedBy", 1); // Replace with actual user

                            roleMappingCommand.ExecuteNonQuery();
                        }

                        if (User.RoleId != 3 && User.RoleId != 4)
                        {
                            var userPropertyAssignmentQuery = @"INSERT INTO [UfirmApp_Production].[App].[UserPropertyAssignment]
                                ([PropertyId], [UserId], [CreatedOn], [CreatedBy], [Status], [UpdatedBy], [UpdatedOn])
                                VALUES (@PropertyId, @UserId, GETDATE(), @CreatedBy, 1, @UpdatedBy, GETDATE());";

                            using (var userPropertyAssignmentCommand = new SqlCommand(userPropertyAssignmentQuery, con))
                            {
                                // Add parameters for UserPropertyAssignment insert
                                userPropertyAssignmentCommand.Parameters.AddWithValue("@UserId", userId); // Using the UserId from the previous query
                                userPropertyAssignmentCommand.Parameters.AddWithValue("@PropertyId", User.PropertyId); // Assuming PropertyId is provided in User object
                                userPropertyAssignmentCommand.Parameters.AddWithValue("@CreatedBy", 1); // Replace with actual user
                                userPropertyAssignmentCommand.Parameters.AddWithValue("@UpdatedBy", 1); // Replace with actual user

                                userPropertyAssignmentCommand.ExecuteNonQuery();
                            }

                            // Return response with the UserId
                            return Ok(new { Message = "User, role mapping, property assignment created successfully.", UserId = userId });
                        }

                        return Ok(new { Message = "User and role mapping created successfully.", UserId = userId });
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
