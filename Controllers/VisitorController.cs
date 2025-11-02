using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/attendance")]
    public class VisitorController : ApiController
    {
        private readonly string connStr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        [HttpGet]
        [Route("getVisitorDetails")]
        public IHttpActionResult GetVisitorDetails(int propertyId, int? employeeId = null, int? facilityMemberId = null)
        {
            List<VisitorResponseModel> visitors = new List<VisitorResponseModel>();

            using (SqlConnection con = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        vd.ID,
                        vd.Fname,
                        vd.Lname,
                        vd.MobileNo,
                        vd.CreatedOn AS MeetingStartTime,
                        vd.UpdatedOn AS MeetingEndTime,
                        vd.Address,
                        fm.FacilityMemberId AS ContactPersonFacilityMemberId,
                        fm.Name AS ContactPersonName,
                        el.Id AS ContactPersonEmployeeId,
                        CASE WHEN vd.Status = 1 THEN 'Approved' ELSE 'Rejected' END AS Status,
                        vd.MPurpose,
                        vd.ACarrying,
                        ISNULL(vd.Is_Meeting_Over, 0) AS IsMeetingOver
                    FROM VisitorDetails vd
                    INNER JOIN App.FacilityMember fm ON fm.FacilityMemberId = vd.ContactPersonId
                    LEFT JOIN EmployeeList el ON el.MobileNo = fm.MobileNumber
                    WHERE fm.PropertyId = @PropertyId
                      " + (facilityMemberId != null ? "AND fm.FacilityMemberId = @FacilityMemberId " : "");

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                    if (facilityMemberId != null)
                        cmd.Parameters.AddWithValue("@FacilityMemberId", facilityMemberId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            visitors.Add(new VisitorResponseModel
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Fname = reader["Fname"]?.ToString(),
                                Lname = reader["Lname"]?.ToString(),
                                MobileNo = reader["MobileNo"]?.ToString(),
                                MeetingStartTime = reader["MeetingStartTime"] as DateTime?,
                                MeetingEndTime = reader["MeetingEndTime"] as DateTime?,
                                Address = reader["Address"]?.ToString(),
                                ContactPersonFacilityMemberId = reader["ContactPersonFacilityMemberId"] as int?,
                                ContactPersonName = reader["ContactPersonName"]?.ToString(),
                                ContactPersonEmployeeId = reader["ContactPersonEmployeeId"] as int?,
                                Status = reader["Status"]?.ToString(),
                                MPurpose = reader["MPurpose"]?.ToString(),
                                ACarrying = reader["ACarrying"]?.ToString(),
                                IsMeetingOver = Convert.ToBoolean(reader["IsMeetingOver"])
                            });
                        }
                    }
                }
            }

            return Ok(visitors);
        }
    }
}
