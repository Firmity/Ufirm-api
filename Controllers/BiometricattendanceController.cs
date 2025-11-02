using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using UrestComplaintWebApi.Helpers;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    [RoutePrefix("api/biometricattendance")]
    public class BiometricattendanceController : ApiController
    {
        private readonly string serviceUrl = "http://ebioservernew.esslsecurity.com:99/webservice.asmx";

        [HttpGet]
        [Route("getall")]
        public async Task<IHttpActionResult> GetAttendanceForAll(string date, string userName, string password)
        {
            try
            {
                // Format date as yyyy-MM-dd
                var formattedDate = DateTime.Parse(date).ToString("yyyy-MM-dd");

                // 1️⃣ Get all employees
                string empSoapBody = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetAllEmployees xmlns=""http://tempuri.org/"">
      <UserName>{userName}</UserName>
      <Password>{password}</Password>
    </GetAllEmployees>
  </soap:Body>
</soap:Envelope>";

                var empResponse = await SoapClientHelper.CallSoapService(serviceUrl, "http://tempuri.org/GetAllEmployees", empSoapBody);
                var empResult = SoapClientHelper.ExtractSoapResult(empResponse, "GetAllEmployeesResult");

                if (string.IsNullOrEmpty(empResult))
                    return BadRequest("No employees returned from server.");

                var employees = empResult.Split(';')
                                         .Where(e => !string.IsNullOrWhiteSpace(e))
                                         .Select(e => new EmployeeDetailsDto
                                         {
                                             EmployeeCode = e.Split(',')[0],
                                             EmployeeName = e.Split(',')[1],
                                             CardNumber = "",
                                             LocationCode = "",
                                             EmployeeRole = "",
                                             VerificationType = ""
                                         })
                                         .ToList();

                // 2️⃣ Fetch punch logs for all employees
                var attendanceTasks = employees.Select(async emp =>
                {
                    string punchSoapBody = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
               xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <GetEmployeePunchLogs xmlns=""http://tempuri.org/"">
      <UserName>{userName}</UserName>
      <Password>{password}</Password>
      <EmployeeCode>{emp.EmployeeCode}</EmployeeCode>
      <AttendanceDate>{formattedDate}</AttendanceDate>
    </GetEmployeePunchLogs>
  </soap:Body>
</soap:Envelope>";

                    var punchResponse = await SoapClientHelper.CallSoapService(serviceUrl, "http://tempuri.org/GetEmployeePunchLogs", punchSoapBody);
                    var punchResult = SoapClientHelper.ExtractSoapResult(punchResponse, "GetEmployeePunchLogsResult");

                    var punches = string.IsNullOrEmpty(punchResult)
                        ? Array.Empty<PunchLogDto>()
                        : punchResult.Split(',')
                                     .Where(p => !string.IsNullOrEmpty(p))
                                     .Select(p =>
                                     {
                                         var parts = p.Split('|');
                                         return new PunchLogDto
                                         {
                                             EmployeeCode = emp.EmployeeCode,
                                             PunchTime = DateTime.Parse(parts[0]),
                                             Direction = parts.Length > 1 ? parts[1] : "",
                                             DeviceName = parts.Length > 2 ? parts[2] : "",
                                             DeviceLocation = parts.Length > 3 ? parts[3] : ""
                                         };
                                     })
                                     .ToArray();

                    return new AttendanceDto
                    {
                        Employee = emp,
                        AttendanceDate = formattedDate,
                        FirstIn = punches.FirstOrDefault(),
                        LastOut = punches.LastOrDefault(),
                        AllPunches = punches
                    };
                });

                var allAttendance = await Task.WhenAll(attendanceTasks);
                return Ok(allAttendance);
            }
            catch (Exception ex)
            {
                // Return full exception details for debugging
                return InternalServerError(ex);
            }
        }
    }
}