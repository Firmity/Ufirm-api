using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using UrestComplaintWebApi.Models;

namespace UrestComplaintWebApi.Controllers
{
    public class EmployeeController : ApiController
    {
        string constr = string.Empty;
        Integrations integrations = new Integrations();

        public EmployeeController()
        {
            constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;
        }

        [HttpPost]
        [Route("api/employee/create")]
        public IHttpActionResult CreateEmployee([FromBody] EmployeeRequest request)
        {
            if (request == null || request.Profile == null)
                return BadRequest("Invalid request payload.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        int employeeId;

                        // 1️⃣ Insert into Emp_Profile_Info
                        using (SqlCommand cmd = new SqlCommand(@"
                    INSERT INTO App.Emp_Profile_Info
                    (OfficeId, EmployeeCode, EmployeeName, EmploymentType, CreatedOn, UpdatedOn, IsActive, Email, PhoneNumber, Designation, Department, Gender, DateOfBirth, PanCard, AadharCard, AddressLine1, AddressLine2, City, State)
                    VALUES (@OfficeId, @EmployeeCode, @EmployeeName, @EmploymentType, @CreatedOn, @UpdatedOn, @IsActive, @Email, @PhoneNumber, @Designation, @Department, @Gender, @DateOfBirth, @PanCard, @AadharCard, @AddressLine1, @AddressLine2, @City, @State);
                    SELECT CAST(SCOPE_IDENTITY() as int);", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OfficeId", (object)request.Profile.OfficeId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EmployeeCode", (object)request.Profile.EmployeeCode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EmployeeName", (object)request.Profile.EmployeeName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EmploymentType", (object)request.Profile.EmploymentType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@CreatedOn", (object)request.Profile.CreatedOn ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@UpdatedOn", (object)request.Profile.UpdatedOn ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@IsActive", request.Profile.IsActive);
                            cmd.Parameters.AddWithValue("@Email", (object)request.Profile.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PhoneNumber", (object)request.Profile.PhoneNumber ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Designation", (object)request.Profile.Designation ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Department", (object)request.Profile.Department ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Gender", (object)request.Profile.Gender ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DateOfBirth", (object)request.Profile.DateOfBirth ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PanCard", (object)request.Profile.PanCard ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AadharCard", (object)request.Profile.AadharCard ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AddressLine1", (object)request.Profile.AddressLine1 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@AddressLine2", (object)request.Profile.AddressLine2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@City", (object)request.Profile.City ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@State", (object)request.Profile.State ?? DBNull.Value);

                            employeeId = (int)cmd.ExecuteScalar();
                        }

                        // 2️⃣ Insert multiple Work History records
                        if (request.WorkHistories != null && request.WorkHistories.Any())
                        {
                            var sorted = request.WorkHistories.OrderBy(x => x.StartDate).ToList();
                            DateTime? dateOfJoining = sorted.FirstOrDefault()?.StartDate;
                            DateTime? relievingDate = sorted.LastOrDefault()?.EndDate;

                            foreach (var work in sorted)
                            {
                                using (SqlCommand cmd = new SqlCommand(@"
            INSERT INTO App.Emp_Work_History
            (EmployeeID, CompanyName, Role, StartDate, EndDate, DateOfJoining, RelievingDate, ThirdPartyVerification, UploadResume, CreatedOn, UpdatedOn, IsActive)
            VALUES (@EmployeeID, @CompanyName, @Role, @StartDate, @EndDate, @DateOfJoining, @RelievingDate, @ThirdPartyVerification, @UploadResume, @CreatedOn, @UpdatedOn, @IsActive);", conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                                    cmd.Parameters.AddWithValue("@CompanyName", (object)work.CompanyName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Role", (object)work.Role ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@StartDate", (object)work.StartDate ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EndDate", (object)work.EndDate ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DateOfJoining", (object)dateOfJoining ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@RelievingDate", (object)relievingDate ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ThirdPartyVerification", work.ThirdPartyVerification);
                                    cmd.Parameters.AddWithValue("@UploadResume", (object)work.UploadResume ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@CreatedOn", (object)work.CreatedOn ?? DateTime.Now);
                                    cmd.Parameters.AddWithValue("@UpdatedOn", (object)work.UpdatedOn ?? DateTime.Now);
                                    cmd.Parameters.AddWithValue("@IsActive", work.IsActive);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }


                        // 3️⃣ Insert Financial Info
                        if (request.FinancialInfo != null)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO App.Emp_Financial_Info
                        (EmployeeID, BankAccountNumber, BankIFSCCode, BankName, UANNumber, PANNumber, CreatedOn, UpdatedOn, IsActive, PF_number, ESINumber)
                        VALUES (@EmployeeID, @BankAccountNumber, @BankIFSCCode, @BankName, @UANNumber, @PANNumber, @CreatedOn, @UpdatedOn, @IsActive, @PF_number, @ESINumber);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                                cmd.Parameters.AddWithValue("@BankAccountNumber", (object)request.FinancialInfo.BankAccountNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BankIFSCCode", (object)request.FinancialInfo.BankIFSCCode ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BankName", (object)request.FinancialInfo.BankName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@UANNumber", (object)request.FinancialInfo.UANNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@PANNumber", (object)request.FinancialInfo.PANNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CreatedOn", (object)request.FinancialInfo.CreatedOn ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@UpdatedOn", (object)request.FinancialInfo.UpdatedOn ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@IsActive", request.FinancialInfo.IsActive);
                                cmd.Parameters.AddWithValue("@PF_number", (object)request.FinancialInfo.PFNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ESINumber", (object)request.FinancialInfo.ESINumber ?? DBNull.Value);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 4️⃣ Insert into FacilityMember
                        if (request.FacilityMember != null)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO App.FacilityMember
                        (PropertyId, Name, Gender, MobileNumber, Address, FacilityMasterId, ProfileImageUrl, IsBlocked, AccessCode, IsApproved, ApprovedOn, ApprovedBy, IsActive, IsDeleted, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, oldID, Password, SG_Link_ID, tax_amount)
                        VALUES (@PropertyId, @Name, @Gender, @MobileNumber, @Address, @FacilityMasterId, @ProfileImageUrl, @IsBlocked, @AccessCode, @IsApproved, @ApprovedOn, @ApprovedBy, @IsActive, @IsDeleted, @CreatedBy, @CreatedOn, @UpdatedBy, @UpdatedOn, @oldID, @Password, @SG_Link_ID, @tax_amount);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PropertyId", (object)request.FacilityMember.PropertyId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Name", (object)request.Profile.EmployeeName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Gender", (object)request.Profile.Gender ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@MobileNumber", (object)request.Profile.PhoneNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Address", (object)request.FacilityMember.Address ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@FacilityMasterId", (object)request.FacilityMember.FacilityMasterId ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ProfileImageUrl", (object)request.FacilityMember.ProfileImageUrl ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsBlocked", request.FacilityMember.IsBlocked);
                                cmd.Parameters.AddWithValue("@AccessCode", (object)request.FacilityMember.AccessCode ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsApproved", request.FacilityMember.IsApproved);
                                cmd.Parameters.AddWithValue("@ApprovedOn", (object)request.FacilityMember.ApprovedOn ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ApprovedBy", (object)request.FacilityMember.ApprovedBy ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsActive", request.FacilityMember.IsActive);
                                cmd.Parameters.AddWithValue("@IsDeleted", request.FacilityMember.IsDeleted);
                                cmd.Parameters.AddWithValue("@CreatedBy", (object)request.FacilityMember.CreatedBy ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CreatedOn", (object)request.FacilityMember.CreatedOn ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@UpdatedBy", (object)request.FacilityMember.UpdatedBy ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@UpdatedOn", (object)request.FacilityMember.UpdatedOn ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@oldID", (object)request.FacilityMember.oldID ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Password", "123456"); // default password
                                cmd.Parameters.AddWithValue("@SG_Link_ID", (object)request.FacilityMember.SG_Link_ID ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@tax_amount", (object)request.FacilityMember.tax_amount ?? DBNull.Value);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 5️⃣ Insert into EmployeeList
                        if (request.EmployeeList != null)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO dbo.EmployeeList
                        (EmployeeName, FatherName, Designation, MobileNo, IsDeleted, Approved)
                        VALUES (@EmployeeName, @FatherName, @Designation, @MobileNo, @IsDeleted, @Approved);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeName", (object)request.Profile.EmployeeName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@FatherName", (object)request.EmployeeList.FatherName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Designation", (object)request.Profile.Designation ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@MobileNo", (object)request.Profile.PhoneNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsDeleted", request.EmployeeList.IsDeleted);
                                cmd.Parameters.AddWithValue("@Approved", request.EmployeeList.Approved);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // ✅ Commit everything
                        transaction.Commit();

                        return Ok(new
                        {
                            Success = true,
                            Message = "Employee data inserted successfully",
                            EmployeeID = employeeId
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error inserting employee data: " + ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Connection failed: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("api/employee/upload-resume")]
        public IHttpActionResult UploadResume()
        {
            var httpRequest = HttpContext.Current.Request;

            if (httpRequest.Files.Count == 0)
                return BadRequest("No file uploaded.");

            var postedFile = httpRequest.Files[0];
            string folderPath = HttpContext.Current.Server.MapPath("~/Uploads/Resumes/");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = Path.GetFileName(postedFile.FileName);
            string filePath = Path.Combine(folderPath, fileName);
            postedFile.SaveAs(filePath);

            // 🔹 Use your custom base URL here
            string baseUrl = "https://api.urest.in:8096";  // <-- Replace with your actual domain
            string fileUrl = $"{baseUrl}/Uploads/Resumes/{fileName}";

            // Return only the full URL string
            return Ok(fileUrl);
        }


        [HttpGet]
        [Route("api/employee/getByOffice/{officeId}")]
        public IHttpActionResult GetEmployeesByOffice(int officeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    var responseList = new List<EmployeeRequest>();

                    string query = @"
SELECT 
    -- FacilityMember
    fm.FacilityMemberId, fm.PropertyId, fm.Name AS FacilityName, fm.Gender AS FacilityGender, 
    fm.MobileNumber AS FacilityMobile, fm.Address AS FacilityAddress, fm.FacilityMasterId,
    fm.ProfileImageUrl, fm.IsBlocked, fm.AccessCode, fm.IsApproved, fm.ApprovedOn, fm.ApprovedBy,
    fm.IsActive AS FacilityActive, fm.IsDeleted AS FacilityDeleted, fm.CreatedBy AS FacilityCreatedBy,
    fm.CreatedOn AS FacilityCreatedOn, fm.UpdatedBy AS FacilityUpdatedBy, fm.UpdatedOn AS FacilityUpdatedOn,
    fm.oldID, fm.Password, fm.SG_Link_ID, fm.tax_amount,

    -- EmployeeList
    el.Id AS EmpListId, el.EmployeeName AS EmpListName, el.FatherName, 
    el.Designation AS EmpListDesignation, el.MobileNo, el.IsDeleted AS EmpListDeleted, el.Approved AS EmpListApproved,

    -- Profile
    p.EmployeeID, p.OfficeId, p.EmployeeCode, p.EmployeeName, p.EmploymentType, 
    p.CreatedOn, p.UpdatedOn, p.IsActive, p.Email, p.PhoneNumber, 
    p.Designation, p.Department, p.Gender, p.DateOfBirth, 
    p.PanCard, p.AadharCard, p.AddressLine1, p.AddressLine2, p.City, p.State,

    -- WorkHistory (latest only)
    wh.CompanyName, wh.Role, wh.StartDate, wh.EndDate, wh.DateOfJoining, wh.RelievingDate,
    wh.ThirdPartyVerification, wh.UploadResume, wh.CreatedOn AS WHCreatedOn, 
    wh.UpdatedOn AS WHUpdatedOn, wh.IsActive AS WHIsActive,

    -- Financial Info (latest only)
    fi.BankAccountNumber, fi.BankIFSCCode, fi.BankName, fi.UANNumber, fi.PANNumber,
    fi.CreatedOn AS FICreatedOn, fi.UpdatedOn AS FIUpdatedOn, fi.IsActive AS FIIsActive, fi.PF_number, fi.ESINumber

FROM App.FacilityMember fm
LEFT JOIN dbo.EmployeeList el ON fm.MobileNumber = el.MobileNo AND el.IsDeleted = 0
LEFT JOIN App.Emp_Profile_Info p ON fm.MobileNumber = p.PhoneNumber AND p.IsActive = 1
LEFT JOIN (
    SELECT EmployeeID, CompanyName, Role, StartDate, EndDate, DateOfJoining, RelievingDate,
           ThirdPartyVerification, UploadResume, CreatedOn, UpdatedOn, IsActive
    FROM (
        SELECT *, ROW_NUMBER() OVER(PARTITION BY EmployeeID ORDER BY CreatedOn DESC) rn
        FROM App.Emp_Work_History
    ) t WHERE rn = 1
) wh ON p.EmployeeID = wh.EmployeeID
LEFT JOIN (
    SELECT EmployeeID, BankAccountNumber, BankIFSCCode, BankName, UANNumber, PANNumber, 
           CreatedOn, UpdatedOn, IsActive, PF_number, ESINumber
    FROM (
        SELECT *, ROW_NUMBER() OVER(PARTITION BY EmployeeID ORDER BY CreatedOn DESC) rn
        FROM App.Emp_Financial_Info
    ) t WHERE rn = 1
) fi ON p.EmployeeID = fi.EmployeeID
WHERE fm.PropertyId = @OfficeId AND fm.IsActive = 1 AND fm.IsDeleted = 0;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OfficeId", officeId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var employee = new EmployeeRequest
                                {
                                    FacilityMember = reader["FacilityMemberId"] == DBNull.Value ? null : new FacilityMember
                                    {
                                        FacilityMemberId= GetInt(reader, "FacilityMemberId"),
                                        PropertyId = GetInt(reader, "PropertyId"),
                                        Address = GetString(reader, "FacilityAddress"),
                                        FacilityMasterId = GetInt(reader, "FacilityMasterId"),
                                        ProfileImageUrl = GetString(reader, "ProfileImageUrl"),
                                        IsBlocked = GetBool(reader, "IsBlocked"),
                                        AccessCode = GetString(reader, "AccessCode"),
                                        IsApproved = GetBool(reader, "IsApproved"),
                                        ApprovedOn = GetDate(reader, "ApprovedOn"),
                                        ApprovedBy = GetInt(reader, "ApprovedBy"),
                                        IsActive = GetBool(reader, "FacilityActive"),
                                        IsDeleted = GetBool(reader, "FacilityDeleted"),
                                        CreatedBy = GetInt(reader, "FacilityCreatedBy"),
                                        CreatedOn = GetDate(reader, "FacilityCreatedOn"),
                                        UpdatedBy = GetInt(reader, "FacilityUpdatedBy"),
                                        UpdatedOn = GetDate(reader, "FacilityUpdatedOn"),
                                        oldID = GetString(reader, "oldID"),
                                        SG_Link_ID = GetString(reader, "SG_Link_ID"),
                                        tax_amount = GetDecimal(reader, "tax_amount")
                                    },

                                    EmployeeList = reader["EmpListId"] == DBNull.Value ? null : new EmployeeList
                                    {
                                        Designation = GetString(reader, "EmpListDesignation"),
                                        FatherName = GetString(reader, "FatherName"),
                                        IsDeleted = GetBool(reader, "EmpListDeleted"),
                                        Approved = GetBool(reader, "EmpListApproved")
                                    },

                                    Profile = reader["EmployeeID"] == DBNull.Value ? null : new EmpProfile
                                    {
                                        OfficeId = GetInt(reader, "OfficeId"),
                                        EmployeeCode = GetString(reader, "EmployeeCode"),
                                        EmployeeName = GetString(reader, "EmployeeName"),
                                        EmploymentType = GetString(reader, "EmploymentType"),
                                        CreatedOn = GetDate(reader, "CreatedOn"),
                                        UpdatedOn = GetDate(reader, "UpdatedOn"),
                                        IsActive = GetBool(reader, "IsActive"),
                                        Email = GetString(reader, "Email"),
                                        PhoneNumber = GetString(reader, "PhoneNumber"),
                                        Designation = GetString(reader, "Designation"),
                                        Department = GetString(reader, "Department"),
                                        Gender = GetString(reader, "Gender"),
                                        DateOfBirth = GetDate(reader, "DateOfBirth"),
                                        PanCard = GetString(reader, "PanCard"),
                                        AadharCard = GetString(reader, "AadharCard"),
                                        AddressLine1 = GetString(reader, "AddressLine1"),
                                        AddressLine2 = GetString(reader, "AddressLine2"),
                                        City = GetString(reader, "City"),
                                        State = GetString(reader, "State")
                                    },

                                    WorkHistories = reader["CompanyName"] == DBNull.Value ? null : new List<EmpWorkHistory>
                            {
                                new EmpWorkHistory
                                {
                                    CompanyName = GetString(reader, "CompanyName"),
                                    Role = GetString(reader, "Role"),
                                    StartDate = GetDate(reader, "StartDate"),
                                    EndDate = GetDate(reader, "EndDate"),
                                    ThirdPartyVerification = GetBool(reader, "ThirdPartyVerification"),
                                    UploadResume = GetString(reader, "UploadResume"),
                                    CreatedOn = GetDate(reader, "WHCreatedOn"),
                                    UpdatedOn = GetDate(reader, "WHUpdatedOn"),
                                    IsActive = GetBool(reader, "WHIsActive")
                                }
                            },

                                    FinancialInfo = reader["BankAccountNumber"] == DBNull.Value ? null : new EmpFinancialInfo
                                    {
                                        BankAccountNumber = GetString(reader, "BankAccountNumber"),
                                        BankIFSCCode = GetString(reader, "BankIFSCCode"),
                                        BankName = GetString(reader, "BankName"),
                                        UANNumber = GetString(reader, "UANNumber"),
                                        PANNumber = GetString(reader, "PANNumber"),
                                        CreatedOn = GetDate(reader, "FICreatedOn"),
                                        UpdatedOn = GetDate(reader, "FIUpdatedOn"),
                                        IsActive = GetBool(reader, "FIIsActive"),
                                        PFNumber = GetString(reader, "PF_number"),
                                        ESINumber = GetString(reader, "ESINumber")
                                    }
                                };

                                responseList.Add(employee);
                            }
                        }
                    }

                    return Ok(responseList);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching employee data by OfficeId: " + ex.Message));
            }
        }


        [HttpPut]
        [Route("api/employee/update/{facilityMemberId}")]
        public IHttpActionResult UpdateEmployee(int facilityMemberId, [FromBody] EmployeeRequest request)
        {
            if (request == null || request.Profile == null)
                return BadRequest("Invalid request payload.");

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // --- Update FacilityMember ---
                        using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE App.FacilityMember
                    SET Address=@Address, IsBlocked=@IsBlocked, AccessCode=@AccessCode,
                        IsApproved=@IsApproved, UpdatedBy=@UpdatedBy, UpdatedOn=@UpdatedOn,
                        IsActive=@IsActive, IsDeleted=@IsDeleted
                    WHERE FacilityMemberID=@FacilityMemberID;", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacilityMemberID", facilityMemberId);
                            cmd.Parameters.AddWithValue("@Address", request.FacilityMember?.Address ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsBlocked", request.FacilityMember?.IsBlocked ?? false);
                            cmd.Parameters.AddWithValue("@AccessCode", request.FacilityMember?.AccessCode ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsApproved", request.FacilityMember?.IsApproved ?? false);
                            cmd.Parameters.AddWithValue("@UpdatedBy", request.FacilityMember?.UpdatedBy ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UpdatedOn", request.FacilityMember?.UpdatedOn ?? DateTime.Now);
                            cmd.Parameters.AddWithValue("@IsActive", request.FacilityMember?.IsActive ?? true);
                            cmd.Parameters.AddWithValue("@IsDeleted", request.FacilityMember?.IsDeleted ?? false);
                            cmd.ExecuteNonQuery();
                        }

                        // --- Get EmployeeID by FacilityMemberID ---
                        int employeeId = 0;
                        using (SqlCommand cmd = new SqlCommand(@"
                    SELECT EmployeeID FROM App.Emp_Profile_Info 
                    WHERE PhoneNumber IN (
                        SELECT MobileNumber FROM App.FacilityMember WHERE FacilityMemberID=@FacilityMemberID
                    );", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacilityMemberID", facilityMemberId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                                employeeId = Convert.ToInt32(result);
                        }

                        // --- If Employee not found → Insert new record ---
                        if (employeeId == 0)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO App.Emp_Profile_Info
                        (OfficeId, EmployeeCode, EmployeeName, EmploymentType, CreatedOn, UpdatedOn,
                         IsActive, Email, PhoneNumber, Designation, Department, Gender, DateOfBirth,
                         PanCard, AadharCard, AddressLine1, AddressLine2, City, State)
                        OUTPUT INSERTED.EmployeeID
                        VALUES
                        (@OfficeId, @EmployeeCode, @EmployeeName, @EmploymentType, @CreatedOn, @UpdatedOn,
                         @IsActive, @Email, @PhoneNumber, @Designation, @Department, @Gender, @DateOfBirth,
                         @PanCard, @AadharCard, @AddressLine1, @AddressLine2, @City, @State);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OfficeId", request.Profile.OfficeId);
                                cmd.Parameters.AddWithValue("@EmployeeCode", request.Profile.EmployeeCode);
                                cmd.Parameters.AddWithValue("@EmployeeName", request.Profile.EmployeeName);
                                cmd.Parameters.AddWithValue("@EmploymentType", request.Profile.EmploymentType);
                                cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
                                cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.Now);
                                cmd.Parameters.AddWithValue("@IsActive", request.Profile.IsActive);
                                cmd.Parameters.AddWithValue("@Email", request.Profile.Email ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PhoneNumber", request.Profile.PhoneNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Designation", request.Profile.Designation ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Department", request.Profile.Department ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Gender", request.Profile.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DateOfBirth", request.Profile.DateOfBirth);
                                cmd.Parameters.AddWithValue("@PanCard", request.Profile.PanCard ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AadharCard", request.Profile.AadharCard ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine1", request.Profile.AddressLine1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine2", request.Profile.AddressLine2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@City", request.Profile.City ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@State", request.Profile.State ?? (object)DBNull.Value);

                                employeeId = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        else
                        {
                            // --- Update existing Profile ---
                            using (SqlCommand cmd = new SqlCommand(@"
                        UPDATE App.Emp_Profile_Info
                        SET OfficeId=@OfficeId, EmployeeCode=@EmployeeCode, EmployeeName=@EmployeeName,
                            EmploymentType=@EmploymentType, UpdatedOn=@UpdatedOn, IsActive=@IsActive,
                            Email=@Email, PhoneNumber=@PhoneNumber, Designation=@Designation,
                            Department=@Department, Gender=@Gender, DateOfBirth=@DateOfBirth,
                            PanCard=@PanCard, AadharCard=@AadharCard, AddressLine1=@AddressLine1,
                            AddressLine2=@AddressLine2, City=@City, State=@State
                        WHERE EmployeeID=@EmployeeID;", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                                cmd.Parameters.AddWithValue("@OfficeId", request.Profile.OfficeId);
                                cmd.Parameters.AddWithValue("@EmployeeCode", request.Profile.EmployeeCode);
                                cmd.Parameters.AddWithValue("@EmployeeName", request.Profile.EmployeeName);
                                cmd.Parameters.AddWithValue("@EmploymentType", request.Profile.EmploymentType);
                                cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.Now);
                                cmd.Parameters.AddWithValue("@IsActive", request.Profile.IsActive);
                                cmd.Parameters.AddWithValue("@Email", request.Profile.Email ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PhoneNumber", request.Profile.PhoneNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Designation", request.Profile.Designation ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Department", request.Profile.Department ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Gender", request.Profile.Gender ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DateOfBirth", request.Profile.DateOfBirth);
                                cmd.Parameters.AddWithValue("@PanCard", request.Profile.PanCard ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AadharCard", request.Profile.AadharCard ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine1", request.Profile.AddressLine1 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@AddressLine2", request.Profile.AddressLine2 ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@City", request.Profile.City ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@State", request.Profile.State ?? (object)DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // --- Work History (Insert or Update) ---
                        if (request.WorkHistory != null)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
        IF EXISTS (SELECT 1 FROM App.Emp_Work_History WHERE EmployeeID=@EmployeeID)
            UPDATE App.Emp_Work_History
            SET CompanyName=@CompanyName, Role=@Role, StartDate=@StartDate,
                EndDate=@EndDate, DateOfJoining=@DateOfJoining, RelievingDate=@RelievingDate,
                ThirdPartyVerification=@ThirdPartyVerification, UploadResume=@UploadResume,
                UpdatedOn=@UpdatedOn, IsActive=@IsActive
            WHERE EmployeeID=@EmployeeID
        ELSE
            INSERT INTO App.Emp_Work_History
            (EmployeeID, CompanyName, Role, StartDate, EndDate, DateOfJoining, RelievingDate,
             ThirdPartyVerification, UploadResume, CreatedOn, UpdatedOn, IsActive)
            VALUES
            (@EmployeeID, @CompanyName, @Role, @StartDate, @EndDate, @DateOfJoining, @RelievingDate,
             @ThirdPartyVerification, @UploadResume, @UpdatedOn, @UpdatedOn, @IsActive);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                                cmd.Parameters.AddWithValue("@CompanyName", request.WorkHistory.CompanyName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Role", request.WorkHistory.Role ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@StartDate", (object)request.WorkHistory.StartDate ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EndDate", (object)request.WorkHistory.EndDate ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ThirdPartyVerification", request.WorkHistory.ThirdPartyVerification);
                                cmd.Parameters.AddWithValue("@UploadResume", request.WorkHistory.UploadResume ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.Now);
                                cmd.Parameters.AddWithValue("@IsActive", request.WorkHistory.IsActive);
                                cmd.ExecuteNonQuery();
                            }
                        }


                        // --- Financial Info (Insert or Update) ---
                        if (request.FinancialInfo != null)
                        {
                            using (SqlCommand cmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM App.Emp_Financial_Info WHERE EmployeeID=@EmployeeID)
                            UPDATE App.Emp_Financial_Info
                            SET BankAccountNumber=@BankAccountNumber, BankIFSCCode=@BankIFSCCode,
                                BankName=@BankName, UANNumber=@UANNumber, PANNumber=@PANNumber,
                                UpdatedOn=@UpdatedOn, IsActive=@IsActive, PF_number=@PF_number, ESINumber=@ESINumber
                            WHERE EmployeeID=@EmployeeID
                        ELSE
                            INSERT INTO App.Emp_Financial_Info
                            (EmployeeID, BankAccountNumber, BankIFSCCode, BankName, UANNumber, PANNumber, CreatedOn, UpdatedOn, IsActive,PF_number,ESINumber)
                            VALUES
                            (@EmployeeID, @BankAccountNumber, @BankIFSCCode, @BankName, @UANNumber, @PANNumber, @UpdatedOn, @UpdatedOn, @IsActive,@PF_number,@ESINumber);", conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@EmployeeID", employeeId);
                                cmd.Parameters.AddWithValue("@BankAccountNumber", request.FinancialInfo.BankAccountNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BankIFSCCode", request.FinancialInfo.BankIFSCCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BankName", request.FinancialInfo.BankName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UANNumber", request.FinancialInfo.UANNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@PANNumber", request.FinancialInfo.PANNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UpdatedOn", DateTime.Now);
                                cmd.Parameters.AddWithValue("@IsActive", request.FinancialInfo.IsActive);
                                cmd.Parameters.AddWithValue("@PF_number", request.FinancialInfo.PFNumber);
                                cmd.Parameters.AddWithValue("@ESINumber", request.FinancialInfo.ESINumber);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();

                        return Ok(new
                        {
                            Success = true,
                            Message = employeeId == 0
                                ? "New employee created successfully and linked with FacilityMemberID."
                                : "Employee data updated successfully.",
                            FacilityMemberID = facilityMemberId,
                            EmployeeID = employeeId
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error updating employee data: " + ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Connection failed: " + ex.Message));
            }
        }


        [HttpDelete]
        [Route("api/employee/delete/{facilityMemberId}")]
        public IHttpActionResult DeleteEmployee(int facilityMemberId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // --- Step 1: Soft delete from FacilityMember ---
                        using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE App.FacilityMember
                    SET IsActive = 0, IsDeleted = 1
                    WHERE FacilityMemberID = @FacilityMemberID;", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacilityMemberID", facilityMemberId);
                            cmd.ExecuteNonQuery();
                        }

                        // --- Step 2: Soft delete from EmployeeList ---
                        using (SqlCommand cmd = new SqlCommand(@"
                    UPDATE dbo.EmployeeList
                    SET IsDeleted = 1
                    WHERE MobileNo IN (
                        SELECT MobileNumber FROM App.FacilityMember WHERE FacilityMemberID = @FacilityMemberID
                    );", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacilityMemberID", facilityMemberId);
                            cmd.ExecuteNonQuery();
                        }

                        // --- Step 3: Check if Emp_Profile_Info exists ---
                        int employeeId = 0;
                        using (SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1 EmployeeID 
                    FROM App.Emp_Profile_Info 
                    WHERE PhoneNumber IN (
                        SELECT MobileNumber FROM App.FacilityMember WHERE FacilityMemberID = @FacilityMemberID
                    );", conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@FacilityMemberID", facilityMemberId);
                            var result = cmd.ExecuteScalar();
                            if (result != null)
                            {
                                employeeId = Convert.ToInt32(result);

                                // --- Step 4: Soft delete from Emp_Profile_Info ---
                                using (SqlCommand cmdDelete = new SqlCommand(@"
                            UPDATE App.Emp_Profile_Info 
                            SET IsActive = 0 
                            WHERE EmployeeID = @EmployeeID;", conn, transaction))
                                {
                                    cmdDelete.Parameters.AddWithValue("@EmployeeID", employeeId);
                                    cmdDelete.ExecuteNonQuery();
                                }

                                // --- Step 5: Soft delete from Emp_Work_History ---
                                using (SqlCommand cmdDelete = new SqlCommand(@"
                            UPDATE App.Emp_Work_History 
                            SET IsActive = 0 
                            WHERE EmployeeID = @EmployeeID;", conn, transaction))
                                {
                                    cmdDelete.Parameters.AddWithValue("@EmployeeID", employeeId);
                                    cmdDelete.ExecuteNonQuery();
                                }

                                // --- Step 6: Soft delete from Emp_Financial_Info ---
                                using (SqlCommand cmdDelete = new SqlCommand(@"
                            UPDATE App.Emp_Financial_Info 
                            SET IsActive = 0 
                            WHERE EmployeeID = @EmployeeID;", conn, transaction))
                                {
                                    cmdDelete.Parameters.AddWithValue("@EmployeeID", employeeId);
                                    cmdDelete.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return Ok(new
                        {
                            Success = true,
                            Message = "Employee deleted (soft delete) successfully by FacilityMemberID"
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return InternalServerError(new Exception("Error deleting employee: " + ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Connection failed: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/employee/generatedSalary/{officeId}")]
        public IHttpActionResult GetEmployeeSalaryByOffice(int officeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    var responseList = new List<EmployeeGeneratedSalary>();

                    string query = @"Select * from App.GeneratedSalary where Office_id=@OfficeId AND is_active=1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OfficeId", officeId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var employee = new EmployeeGeneratedSalary
                                {
                                    EmployeeId = reader.GetInt32(reader.GetOrdinal("Emp_ID")),
                                    EmployeeName = reader.GetString(reader.GetOrdinal("Employee_Name")),
                                    OfficeId = reader.GetInt32(reader.GetOrdinal("Office_id")),
                                    CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn")),
                                    Month = reader.GetString(reader.GetOrdinal("Month")),
                                    Year = reader.GetInt32(reader.GetOrdinal("Year")),
                                    is_active = reader.GetBoolean(reader.GetOrdinal("is_active"))
                                };

                                responseList.Add(employee);
                            }
                        }
                    }

                    return Ok(responseList);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching employee data by OfficeId: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("api/employee/generatedSalary")]
        public IHttpActionResult AddEmployeeGeneratedSalary([FromBody] EmployeeGeneratedSalary employee)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    string query = @"INSERT INTO App.GeneratedSalary (Emp_ID, Employee_Name, Office_id, CreatedOn, Month, Year,is_active) 
                             VALUES (@Emp_ID, @Employee_Name, @Office_id, @CreatedOn, @Month, @Year,1)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Emp_ID", employee.EmployeeId);
                        cmd.Parameters.AddWithValue("@Employee_Name", employee.EmployeeName);
                        cmd.Parameters.AddWithValue("@Office_id", employee.OfficeId);
                        cmd.Parameters.AddWithValue("@CreatedOn", employee.CreatedOn);
                        cmd.Parameters.AddWithValue("@Month", employee.Month);
                        cmd.Parameters.AddWithValue("@Year", employee.Year);
                        cmd.Parameters.AddWithValue("@is_active", 1);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return Ok("Employee salary record added successfully.");
                        else
                            return BadRequest("Failed to insert record.");
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error inserting employee salary data: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("api/employee/regeneratedSalary")]
        public IHttpActionResult RegenerateEmployeeSalary([FromBody] EmployeeGeneratedSalary employee)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    // Step 1: Delete existing record (if any)
                    string deleteQuery = @"DELETE FROM App.GeneratedSalary 
                                   WHERE Emp_ID = @Emp_ID AND Month = @Month AND Year = @Year";

                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, conn))
                    {
                        deleteCmd.Parameters.AddWithValue("@Emp_ID", employee.EmployeeId);
                        deleteCmd.Parameters.AddWithValue("@Month", employee.Month);
                        deleteCmd.Parameters.AddWithValue("@Year", employee.Year);

                        deleteCmd.ExecuteNonQuery(); // delete existing record
                    }

                    // Step 2: Insert the new record
                    string insertQuery = @"INSERT INTO App.GeneratedSalary 
                                   (Emp_ID, Employee_Name, Office_id, CreatedOn, Month, Year, is_active)
                                   VALUES (@Emp_ID, @Employee_Name, @Office_id, @CreatedOn, @Month, @Year, 1)";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Emp_ID", employee.EmployeeId);
                        insertCmd.Parameters.AddWithValue("@Employee_Name", employee.EmployeeName);
                        insertCmd.Parameters.AddWithValue("@Office_id", employee.OfficeId);
                        insertCmd.Parameters.AddWithValue("@CreatedOn",
                            employee.CreatedOn == default(DateTime) ? DateTime.Now : employee.CreatedOn);
                        insertCmd.Parameters.AddWithValue("@Month", employee.Month);
                        insertCmd.Parameters.AddWithValue("@Year", employee.Year);
                        insertCmd.Parameters.AddWithValue("@is_active", 1);

                        int rowsAffected = insertCmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return Ok("Employee salary record regenerated successfully.");
                        else
                            return BadRequest("Failed to insert new salary record.");
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error regenerating employee salary data: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("api/employee/generatedSalary/{employeeId}")]
        public IHttpActionResult DeleteEmployeeGeneratedSalary(int employeeId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string query = @"DELETE From App.GeneratedSalary WHERE Emp_ID = @Emp_ID";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Emp_ID", employeeId);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                            return Ok("Employee salary record deleted successfully.");
                        else
                            return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error deleting employee salary data: " + ex.Message));
            }
        }

        [HttpPut]
        [Route("api/employee/generatedSalary/{employeeId}")]
        public IHttpActionResult UpdateEmployeeGeneratedSalary(int employeeId, [FromBody] EmployeeGeneratedSalary employee)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();
                    string query = @"UPDATE App.GeneratedSalary 
                                     SET CreatedOn=@CreatedOn, Month=@Month, Year=@Year 
                                     WHERE Emp_ID = @Emp_ID AND is_acitve=1";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Emp_ID", employeeId);
                        cmd.Parameters.AddWithValue("@Employee_Name", employee.EmployeeName);
                        cmd.Parameters.AddWithValue("@Office_id", employee.OfficeId);
                        cmd.Parameters.AddWithValue("@CreatedOn", employee.CreatedOn == default(DateTime)
                                                                ? DateTime.Now
                                                                : employee.CreatedOn);
                        cmd.Parameters.AddWithValue("@Month", employee.Month);
                        cmd.Parameters.AddWithValue("@Year", employee.Year);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                            return Ok("Employee salary record updated successfully.");
                        else
                            return NotFound();
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error updating employee salary data: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("get-facilitymember-salary-details")]
        public IHttpActionResult GetFacilityMemberSalaryDetails(
    string FacilityMemberIds,  // e.g. "1,2,3"
    string Month,
    int Year)
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            string connStr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

            if (string.IsNullOrWhiteSpace(FacilityMemberIds))
                return BadRequest("FacilityMemberIds cannot be empty.");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("App.GetFacilityMemberSalaryDetails", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Pass the comma-separated IDs as a string
                        cmd.Parameters.AddWithValue("@FacilityMemberIds", FacilityMemberIds);
                        cmd.Parameters.AddWithValue("@Month", (object)Month ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Year", Year);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row.Add(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i));
                                }
                                result.Add(row);
                            }
                        }
                    }
                }

                return Ok(result);
            }
            catch (SqlException sqlEx)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    message = "Database error occurred.",
                    error = sqlEx.Message
                });
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    message = "An unexpected error occurred.",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        [Route("api/employee/updateGeneratedSalaryDoc")]
        public IHttpActionResult UpdateGeneratedSalaryDocument()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                // 🔹 Validate required fields
                if (string.IsNullOrWhiteSpace(httpRequest.Form["Emp_ID"]) ||
                    string.IsNullOrWhiteSpace(httpRequest.Form["Month"]) ||
                    string.IsNullOrWhiteSpace(httpRequest.Form["Year"]))
                {
                    return BadRequest("Emp_ID, Month, and Year are required to update document.");
                }

                int empId = Convert.ToInt32(httpRequest.Form["Emp_ID"]);
                string month = httpRequest.Form["Month"];
                int year = Convert.ToInt32(httpRequest.Form["Year"]);

                // 🔹 Handle uploaded file
                HttpPostedFile docFile = httpRequest.Files["SalaryDoc"];
                if (docFile == null || docFile.ContentLength == 0)
                    return BadRequest("Salary document file is required.");

                // 🔹 Save the new file
                string docUrl = SaveFile(docFile,
                    @"C:\inetpub\wwwroot\SupervisorLogins\uploads\generatedSalary\",
                    "uploads/generatedSalary/");

                // 🔹 Update document in App.GeneratedSalary
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    string query = @"
                UPDATE App.GeneratedSalary
                SET generated_PDF = @SalaryDocUrl
                WHERE Emp_ID = @Emp_ID AND Month = @Month AND Year = @Year;
            ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Emp_ID", empId);
                        cmd.Parameters.AddWithValue("@Month", month);
                        cmd.Parameters.AddWithValue("@Year", year);
                        cmd.Parameters.AddWithValue("@SalaryDocUrl", docUrl);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok(new
                            {
                                message = "✅ Salary document updated successfully.",
                                salaryDocUrl = docUrl
                            });
                        }
                        else
                        {
                            return BadRequest("❌ No matching record found for the given Emp_ID, Month, and Year.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("❌ Error while updating salary document: " + ex.Message));
            }
        }

        private string SaveFile(HttpPostedFile file, string physicalFolder, string virtualFolder)
        {
            try
            {
                if (!Directory.Exists(physicalFolder))
                    Directory.CreateDirectory(physicalFolder);

                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string filePath = Path.Combine(physicalFolder, fileName);
                file.SaveAs(filePath);

                string baseUrl = "https://admin.urest.in:8097/";
                string fileUrl = $"{baseUrl}{virtualFolder}{fileName}".Replace("\\", "/");
                return fileUrl;
            }
            catch (Exception ex)
            {
                throw new Exception("File save error: " + ex.Message);
            }
        }



        [HttpGet]
        [Route("api/employee/ungeneratedSalary/{officeId}/{month}/{year}")]
        public IHttpActionResult GetUngeneratedSalaryByOffice(int officeId, string month, int year)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    var responseList = new List<EmployeeUngeneratedSalary>();

                    // ✅ Convert month name to month number
                    int monthNumber;
                    if (!int.TryParse(month, out monthNumber))
                    {
                        try
                        {
                            DateTime temp = DateTime.ParseExact(month, "MMMM", System.Globalization.CultureInfo.InvariantCulture);
                            monthNumber = temp.Month;
                        }
                        catch
                        {
                            return BadRequest("Invalid month format. Use either full month name (e.g., 'October') or month number (e.g., '10').");
                        }
                    }

                    string query = @"
                SELECT 
                    fm.FacilityMemberId AS Id,
                    fm.Name AS EmployeeName,
                    el.Designation
                FROM app.FacilityMember fm
                INNER JOIN dbo.EmployeeList el 
                    ON fm.Name = el.EmployeeName
                INNER JOIN salarygroupfacilitylinking sgl
                    ON fm.FacilityMemberId = sgl.FacilityMemberId
                WHERE fm.IsActive = 1
                  AND fm.PropertyId = @OfficeId
                  AND sgl.Start_date IS NOT NULL

                  -- ✅ Include only if the employee was active in this month
                  AND (
                        -- started before or during the selected month/year
                        (YEAR(sgl.Start_date) < @Year)
                        OR (YEAR(sgl.Start_date) = @Year AND MONTH(sgl.Start_date) <= @MonthNum)
                      )
                  AND (
                        -- not ended yet or ended after this month/year
                        sgl.End_date IS NULL
                        OR (YEAR(sgl.End_date) > @Year)
                        OR (YEAR(sgl.End_date) = @Year AND MONTH(sgl.End_date) >= @MonthNum)
                      )

                  -- ✅ Exclude already generated salary for this month/year
                  AND NOT EXISTS (
                      SELECT 1
                      FROM app.GeneratedSalary gs
                      WHERE gs.Office_id = fm.PropertyId
                        AND gs.Emp_ID = fm.FacilityMemberId
                        AND gs.Month = DATENAME(MONTH, DATEFROMPARTS(@Year, @MonthNum, 1))
                        AND gs.Year = @Year
                        AND gs.is_active = 1
                  );
            ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@OfficeId", officeId);
                        cmd.Parameters.AddWithValue("@MonthNum", monthNumber);
                        cmd.Parameters.AddWithValue("@Year", year);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var employee = new EmployeeUngeneratedSalary
                                {
                                    EmployeeId = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    EmployeeName = reader["EmployeeName"]?.ToString() ?? string.Empty,
                                    Designation = reader["Designation"]?.ToString() ?? string.Empty
                                };

                                responseList.Add(employee);
                            }
                        }
                    }

                    return Ok(responseList);
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching ungenerated salary employees: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("api/othours/add")]
        public IHttpActionResult AddOTHour([FromBody] OTHoursMaster model)
        {
            if (model == null)
                return BadRequest("Invalid or empty request body.");

            try
            {
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString))
                {
                    string query = @"
                INSERT INTO app.OThoursMaster (Property_id, designation, price)
                VALUES (@Property_id, @designation, @price);
            ";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Property_id", model.Property_id);
                        cmd.Parameters.AddWithValue("@designation", (object)model.designation ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@price", (object)model.price ?? DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { message = "OT Hours added successfully!" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("~/api/othours/get-by-property/{propertyId}")]
        public IHttpActionResult GetOTByProperty(int propertyId)
        {
            List<OTHoursMaster> list = new List<OTHoursMaster>();

            try
            {
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString))
                {
                    using (var cmd = new SqlCommand("SELECT * FROM app.OThoursMaster WHERE Property_id = @Property_id", con))
                    {
                        cmd.Parameters.AddWithValue("@Property_id", propertyId);

                        con.Open();
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            list.Add(new OTHoursMaster
                            {
                                ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                                Property_id = reader["Property_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Property_id"]),
                                designation = reader["designation"] == DBNull.Value ? null : reader["designation"].ToString(),
                                price = reader["price"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["price"])
                            });
                        }
                    }
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        private static string GetString(SqlDataReader reader, string column)
        {
            return reader[column] == DBNull.Value ? null : reader[column].ToString();
        }

        private int GetInt(SqlDataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value ? 0 : Convert.ToInt32(reader[columnName]);
        }

        private static bool GetBool(SqlDataReader reader, string column)
        {
            return reader[column] != DBNull.Value && Convert.ToBoolean(reader[column]);
        }

        private static DateTime? GetDate(SqlDataReader reader, string column)
        {
            return reader[column] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[column]);
        }

        private static decimal? GetDecimal(SqlDataReader reader, string column)
        {
            return reader[column] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader[column]);
        }


        #region Safe Helpers
        private int? GetInt(IDataReader reader, string column) =>
            reader[column] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[column]);

        private string GetString(IDataReader reader, string column) =>
            reader[column] == DBNull.Value ? null : reader[column].ToString();

        private DateTime? GetDate(IDataReader reader, string column) =>
            reader[column] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[column]);

        private bool GetBool(IDataReader reader, string column) =>
            reader[column] != DBNull.Value && Convert.ToBoolean(reader[column]);
        #endregion
    }
}
