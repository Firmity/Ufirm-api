using ExcelDataReader;
using Microsoft.Ajax.Utilities;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.UI.WebControls;
using System.Web.Util;
using System.Windows.Input;
using System.Xml.Linq;
using UrestComplaintWebApi.Models;
using EventTask = UrestComplaintWebApi.Models.EventTask;
using File = System.IO.File;

namespace UrestComplaintWebApi.Controllers
{
    //[EnableCors(origins: "*", headers: "*", methods: "*")]
    public class UrestController : ApiController
    {
        string constr = string.Empty;
        Integrations integrations = new Integrations();
        public UrestController()
        {
            constr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;

        }


        [HttpPut]
        [Route("api/facilitymember/reset-password")]
        public IHttpActionResult ResetFacilityMemberPassword([FromBody] ResetPasswordRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                return BadRequest("MobileNumber is required.");
            }

            string connStr = ConfigurationManager.ConnectionStrings["adoConnectionstring"].ConnectionString;
            string newPassword = "123456";

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    using (SqlCommand cmd = new SqlCommand("UPDATE App.FacilityMember SET Password = @Password WHERE MobileNumber = @MobileNumber", conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                        cmd.Parameters.AddWithValue("@MobileNumber", request.MobileNumber);

                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok(new
                            {
                                Success = true,
                                Message = "Password reset to default successfully",
                                MobileNumber = request.MobileNumber,
                                NewPassword = newPassword
                            });
                        }
                        else
                        {
                            return Content(HttpStatusCode.NotFound, new
                            {
                                Success = false,
                                Message = "No facility member found with the provided mobile number"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error resetting password: " + ex.Message));
            }
        }

        // POST: api/MemberComplaintRegistrationsSignup
        [Route("Signup")]
        [HttpPost]
        public IHttpActionResult Signup(MemberComplaintRegistration e)
        {

            string errorMsg = "", succMsg = "Registration successful !";

            if (e.Mobile.Length != 10 || !Utilities.IsNumeric(e.Mobile.ToString()))
            { return Ok("Invalid Mobile No. !"); }

            if (e.Name.Length == 0 || e.Name == "string")
            { return Ok("Invalid User Name !"); }

            if (e.Email == "string")
            { return Ok("Invalid email id !"); }

            if (e.Password.Length == 0 || e.Password == "string" || e.ConfirmPassword.Length == 0 || e.ConfirmPassword == "string")
            { return Ok("Invalid Password !"); }

            if (e.Password != e.ConfirmPassword)
            { return Ok("Password and Confirm Password not matching !"); }

            try
            {
                MemberComplaintRegistration er = new MemberComplaintRegistration();
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    using (SqlCommand cmd0 = new SqlCommand())
                    {
                        cmd0.Connection = con;
                        cmd0.CommandType = CommandType.Text;
                        cmd0.CommandText = "select count(Mobile) from [app].[MemberComplaintRegistration] where mobile=@Mobile";
                        cmd0.Parameters.AddWithValue("@Mobile", e.Mobile);
                        var resp = cmd0.ExecuteScalar();
                        if (Convert.ToInt32(resp) > 0)
                        { errorMsg = "Mobile No. " + e.Mobile + "  Already Exists !"; }
                        else
                        {
                            using (SqlCommand cmd = new SqlCommand())
                            {
                                cmd.Connection = con;
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.CommandText = "SP_MemberComplaintRegistration";
                                cmd.Parameters.AddWithValue("@Mobile", e.Mobile);
                                cmd.Parameters.AddWithValue("@Name", e.Name);
                                cmd.Parameters.AddWithValue("@Email", e.Email);
                                cmd.Parameters.AddWithValue("@Password", e.Password);
                                cmd.Parameters.AddWithValue("@ConfirmPassword", e.ConfirmPassword);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    con.Close();
                }
                if (errorMsg.Length > 0)
                {
                    return Ok(errorMsg);
                }
                else
                {
                    return Ok(succMsg);
                }
            }
            catch (Exception ex)
            { return Ok(ex.Message); }
        }

        // POST: api/MemberComplaintRegistrationsSignin
        [Route("Signin")]
        [HttpPost]
        public IHttpActionResult Signin(Signin e)
        {
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                string Query = "select Mobile,Password from [App].[MemberComplaintRegistration] where Mobile=@Mobile and Password=@Password";
                SqlCommand cmd = new SqlCommand(Query, con);
                cmd.Parameters.AddWithValue("@Mobile", e.Mobile);
                cmd.Parameters.AddWithValue("@Password", e.Password);
                var mob = e.Mobile;

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
                con.Close();
            }

            var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Signin
            {
                Mobile = dataRow.Field<string>("Mobile"),
                Password = dataRow.Field<string>("Password")
            })).ToList();

            return Ok(eList);
        }

        [Route("SendOTPSMS")]
        [HttpPost]
        public async Task<string> SendOTPSMS(string ToSendMobileNo)
        {
            return await integrations.SendOTP(ToSendMobileNo, 4);
        }

        [Route("SendCOMPPSMS")]
        [HttpPost]
        public async Task<IHttpActionResult> SendCOMPPSMS(string ToSendMobileNo, string TicketId)
        {
            string Sender = "URSTCP";
            string message = $"Your ticket {TicketId} has been successfully logged and would be attended shorlty.\n-Team Urest\nUFIRM TECHNOLOGIES PVT LTD";
            try
            {
                bool isSent = await integrations.SendSMSAsync(ToSendMobileNo, message,Sender);

                if (isSent)
                {
                    return Ok(new { success = true, message = "SMS sent successfully!" });
                }
                else
                {
                    return BadRequest("Failed to send SMS.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Internal Server Error: " + ex.Message));
            }
        }

        [Route ("Complaint")]
        [HttpGet]
        public IHttpActionResult Complaint(string Mobile)
        {

            string connetionString = null;
            SqlConnection connection;
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();
            DataSet dss = new DataSet();
            DataSet dsss = new DataSet();


            int i = 0;

            //connetionString = "Data Source=WIN-HBSI1RRBVE0;Initial Catalog=UfirmApp_Production;integrated security=true";
            connection = new SqlConnection(constr);

            connection.Open();
            command.Connection = connection;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "[App].[GetComplaints]";
            param = new SqlParameter("@Mobile", Mobile);
            param.Direction = ParameterDirection.Input;
            param.DbType = DbType.String;
            command.Parameters.Add(param);

            adapter = new SqlDataAdapter(command);
            adapter.Fill(ds);


            //selecting Name

            command.CommandType = CommandType.Text;
            command.CommandText = "select Name from [App].[PropertyMember] where ContactNumber = @Mobiles";
            param = new SqlParameter("@Mobiles", Mobile);
            param.Direction = ParameterDirection.Input;
            param.DbType = DbType.String;
            command.Parameters.Add(param);
            //adapter = new SqlDataAdapter(command);
            //adapter.Fill(dss);



            //Selecting Flatname
            command.CommandType = CommandType.Text;
            command.CommandText = "select Flat from [App].[PropertyDetails] where PropertyDetailsId = @PropertyDetaildId";
            param = new SqlParameter("@PropertyDetaildId", 7);
            param.Direction = ParameterDirection.Input;
            param.DbType = DbType.String;
            command.Parameters.Add(param);
            adapter = new SqlDataAdapter(command);
            adapter.Fill(dsss);
            //var listes = dss.Tables[0].AsEnumerable().Select(dataRow => new PropertyMember { Name = dataRow.Field<string>("Name") }).ToList();

            var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Ticket
            {
                TicketNumber = dataRow.Field<string>("TicketNumber"),
                Description = dataRow.Field<string>("Description"),
                TicketOrigin = dataRow.Field<string>("TicketOrigin"),
                Title = dataRow.Field<string>("Title"),
                Visibility = dataRow.Field<string>("Visibility"),
                TicketId = dataRow.Field<int>("TicketId"),
                PropertyDetaildId = dataRow.Field<int>("PropertyDetaildId"),
                StatusTypeId = dataRow.Field<int>("StatusTypeId")
            })).ToList();






            connection.Close();


            return Ok(eList);

        }

        [Route("GetCategory")]
        [HttpGet]
        public IHttpActionResult GetCategory(int categoryId = 0)
        {
            Category cat = new Category()
            { CategoryId = categoryId };
            if (categoryId == 0)
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    SqlConnection connection;
                    SqlDataAdapter adapter;
                    SqlCommand command = new SqlCommand();
                    SqlParameter param;
                    DataSet ds = new DataSet();
                    DataSet dss = new DataSet();
                    DataSet dsss = new DataSet();


                    int i = 0;

                    connection = new SqlConnection(constr);

                    connection.Open();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "[App].[GetAllCategory]";

                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(ds);

                    var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Category
                    {
                        SubCategoryId = dataRow.Field<int>("SubCategoryId"),
                        CategoryId = dataRow.Field<int>("CategoryId"),
                        SubCategoryName = dataRow.Field<string>("SubCategoryName")

                    })).ToList();
                    connection.Close();
                    return Ok(eList);



                }
            }
            else if (categoryId != null)
            {
                using (SqlConnection con = new SqlConnection(constr))
                {


                    SqlConnection connection;
                    SqlDataAdapter adapter;
                    SqlCommand command = new SqlCommand();
                    SqlParameter param;
                    DataSet ds = new DataSet();
                    DataSet dss = new DataSet();
                    DataSet dsss = new DataSet();


                    int i = 0;

                    //connetionString = "Data Source=WIN-HBSI1RRBVE0;Initial Catalog=UfirmApp_Production;integrated security=true";
                    connection = new SqlConnection(constr);

                    connection.Open();
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "[App].[GetCategoryById]";

                    param = new SqlParameter("@CategoryId", cat.CategoryId);
                    param.Direction = ParameterDirection.Input;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);

                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(ds);
                    var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Category
                    {
                        SubCategoryId = dataRow.Field<int>("SubCategoryId"),
                        CategoryId = dataRow.Field<int>("CategoryId"),
                        SubCategoryName = dataRow.Field<string>("SubCategoryName")

                    })).ToList();
                    connection.Close();

                    return Ok(eList);
                }
            }
            return Ok();
        }

        [Route("QuestionsDetailsOfTask")]
        [HttpGet]
        public IHttpActionResult QuestionsDetailsOfTask(int taskID, string TransactionDate)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;

                bool exits = false;
                //command.CommandText = "select q.QuestionID, q.QuestionName, t.TaskName, tm.Occurence from TaskWiseTransaction t inner join TaskMaster tm on tm.Name=t.TaskName right join TaskWiseQuestionnaire q on tm.id=q.TaskID and t.QuestID=q.QuestionID where q.taskid=@taskID group by q.QuestionID, q.QuestionName, t.TaskName, tm.Occurence"; 
                command.CommandText = "ViewTask";
                command.Parameters.AddWithValue("@TaskId", taskID);
                command.Parameters.AddWithValue("@TransactionDate", TransactionDate);
                //param = new SqlParameter("@taskID", taskID);
                //param.Direction = ParameterDirection.Input;
                //param.DbType = DbType.String;
                //command.Parameters.Add(param);
                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        exits = true;
                    }
                }
                //if (!exits)
                //{
                //    //command.CommandText = "select 0 q.QuestionID, q.QuestionName, t.Name taskName, t.Occurence from TaskWiseQuestionnaire q inner join TaskMaster t on q.taskid = t.id where taskid = @taskID order by id";
                //    adapter = new SqlDataAdapter(command);
                //    adapter.Fill(ds);
                //}
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseQuestionnaire
                {
                    //TransactionID = dataRow["TransactionID"] == DBNull.Value ? 0 : dataRow.Field<int>("TransactionID"),
                    //TransactionID = 0,
                    TaskID = taskID,
                    TaskName = dataRow["taskName"] == DBNull.Value ? "" : dataRow.Field<string>("taskName"),
                    Occurance = dataRow["Occurence"] == DBNull.Value ? "" : dataRow.Field<string>("Occurence"),
                    QuestionName = dataRow.Field<string>("QuestionName"),
                    QuestID = dataRow.Field<int>("QuestionID"),
                    //Action = dataRow["Action"] == DBNull.Value ? "" : dataRow.Field<string>("Action"),
                    Remarks = dataRow["Remarks"] == DBNull.Value ? "" : dataRow.Field<string>("Remarks"),
                    Status = "Actionable"
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }
            return Ok();
        }

        [Route("NoticeBoardSave0")]
        [HttpPost]
        public IHttpActionResult CreateNoticeBoard0(FormDataNoticeBoard model, int userid)
        {
            SqlConnection con = new SqlConnection(constr);
            con.Open();
            int returnvalue = 0;

            try
            {
                //string wwwPath = _hostEnvironment.WebRootPath;
                //string path = Path.Combine(wwwPath, "Notifications");

                if (model.PropertyId != 0 && model.NotificationTypeId != 0 && model.PropertyGroupId != 0 && model.Notify != 0 && model.AlertTypeId != 0 && model.Subject != "")
                {
                    List<NoticeBoardAttachment> finalAttachmentData = new List<NoticeBoardAttachment>();

                    //if (model.NoticeBoardAttachment.Count > 0)
                    //{
                    //    string foldername = DateTime.Now.ToString("ddMMyyyy");
                    //    string notificationDir = path + "\\" + foldername;
                    //    if (!Directory.Exists(notificationDir))
                    //    {
                    //        Directory.CreateDirectory(notificationDir);
                    //    }
                    //    foreach (var item in model.NoticeBoardAttachment)
                    //    {
                    //        NoticeBoardAttachment noticeBoardAttachment = new NoticeBoardAttachment();

                    //        string splitFilename = item.filename.Substring(0, item.filename.LastIndexOf('.'));
                    //        string splitFilenameext = item.filename.Substring(item.filename.LastIndexOf('.') + 1);
                    //        string newFilename = splitFilename + DateTime.Now.ToString("yyyyMMddhhmmss") + "." + splitFilenameext;
                    //        string ffilename = notificationDir + "\\" + newFilename;

                    //        string dbpath = foldername + "\\" + newFilename;

                    //        File.WriteAllBytes(ffilename, Convert.FromBase64String(item.filepath));
                    //        noticeBoardAttachment.filename = newFilename;
                    //        noticeBoardAttachment.filepath = dbpath;

                    //        finalAttachmentData.Add(noticeBoardAttachment);
                    //    }
                    //}

                    DataTable NoticeBoardAttachments = new DataTable();// CommonService.ToDataTable(finalAttachmentData);
                    DateTime? expirydate = null;
                    if (!string.IsNullOrEmpty(model.ExpirtyDate))
                    {
                        expirydate = DateTime.Parse(model.ExpirtyDate.Trim());
                    }

                    System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand();

                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "[App].[ManageNoticeBoard]";

                    command.Parameters.AddWithValue("PropertyId", Convert.ToString(model.PropertyId));
                    command.Parameters.AddWithValue("Subject", Convert.ToString(model.Subject));
                    command.Parameters.AddWithValue("Message", Convert.ToString(model.Message));
                    command.Parameters.AddWithValue("NotificationTypeId", Convert.ToString(model.NotificationTypeId));
                    command.Parameters.AddWithValue("PropertyGroupId", Convert.ToString(model.PropertyGroupId));
                    command.Parameters.AddWithValue("Notify", Convert.ToString(model.Notify));
                    command.Parameters.AddWithValue("AlertTypeId", Convert.ToString(model.AlertTypeId));
                    command.Parameters.AddWithValue("ExpirtyDate", expirydate);
                    command.Parameters.AddWithValue("NoticeBoardAttachment", NoticeBoardAttachments);
                    command.Parameters.AddWithValue("PropertyDetailsId", model.PropertyDetailsId);
                    command.Parameters.AddWithValue("PropertyTowerId", model.PropertyTowerId);
                    //{"RolesIds",  model.PropertyRWAMemberId},
                    command.Parameters.AddWithValue("CmdType", model.StatementType);
                    command.Parameters.AddWithValue("CurrentUserId", userid.ToString());

                    command.Parameters.Add("@ret", SqlDbType.Int);
                    command.Parameters["@ret"].Direction = ParameterDirection.Output;

                    command.Connection = con;
                    int retVal = command.ExecuteNonQuery();

                    // get inserted notification id
                    returnvalue = Convert.ToInt32(command.Parameters["@ret"].Value);
                    //if record not saved into db then delete saved files
                    //if (returnvalue <= 0)
                    //{
                    //    if (NoticeBoardAttachments.Rows.Count > 0)
                    //    {
                    //        foreach (DataRow row in NoticeBoardAttachments.Rows)
                    //        {
                    //            var items = row.ItemArray;
                    //            string filePath = path + "\\" + items[1].ToString();
                    //            File.Delete(filePath);
                    //        }
                    //    }
                    //}
                    //if (returnvalue > 0)
                    //{
                    //    List<NoticeBoardNoticeData> emailData = await GetNoticeBoardsNoticeData("R", returnvalue);

                    //    List<NoticeBoardAttachments> attachments = await GetNoticeBoardsAttachment("NATT", model.PropertyId, returnvalue);
                    //    string body = string.Empty;
                    //    foreach (var item in emailData)
                    //    {
                    //        string fSubject = $"{item.NotificationType} - {item.Subject}";
                    //        string fbody = item.Message;
                    //        if (attachments.Count > 0)
                    //        {
                    //            fbody += "<br>Please Click to Download Below Attachments <br>";
                    //            int i = 1;
                    //            foreach (var att in attachments)
                    //            {
                    //                fbody += $"<br <br> {i}. <a href='{att.FilePath}'> {att.FileName} <a/>";
                    //                i++;
                    //            }
                    //        }

                    //        //BackgroundJob.Schedule(() => emailHelper.SendNoticeBoardMail(fSubject, "rakhmaji.ghule@gmail.com", fbody, item.NoticeboardAlertMaster), TimeSpan.FromSeconds(30));
                    //        if (!string.IsNullOrEmpty(item.EmailAddress))
                    //        {
                    //            BackgroundJob.Schedule(() => emailHelper.SendNoticeBoardMail(fSubject, item.EmailAddress, fbody, item.NoticeboardAlertMaster), TimeSpan.FromSeconds(30));
                    //        }

                    //        //emailHelper.SendNoticeBoardMail(fSubject, "rakhmaji.ghule@gmail.com", fbody, item.NoticeboardAlertMaster);
                    //    }
                    //}
                }
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }

            con.Close();
            return Ok(returnvalue);
        }

        [Route("AmenitiesBookingApprove")]
        [HttpPost]
        public IHttpActionResult AmenitiesBookingApprove(int Id)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();
            string msg = "Approved Successfully !";
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "update [dbo].[AmenitiesBooking] set Approved = 1 where Id=@Id";
                param = new SqlParameter("@Id", Id);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);
                command.ExecuteNonQuery();

                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "Select * from [dbo].[AmenitiesBooking] where Id=@Id";
                param = new SqlParameter("@Id", Id);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);

                SqlDataAdapter adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);

                string MobileNo = "", UserID = ds.Tables[0].Rows[0]["userid"].ToString();
                string sql = "select ContactNumber from [Identity].Users where userid= " + UserID;
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = sql;
                MobileNo = command.ExecuteScalar().ToString();
                string ameName = "";//
                sql = "select AmenitiesName from [master].amenities where AmenitiesId = " + ds.Tables[0].Rows[0]["AmenitiesId"].ToString();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = sql;
                ameName = command.ExecuteScalar().ToString();

                sql = "select top(1) PropertyId from [App].[UserPropertyAssignment] where userid=" + UserID + " order by UserPropertyAssignmentId desc";
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = sql;
                string propertyID = command.ExecuteScalar() == null ? "1" : command.ExecuteScalar().ToString();
                if (command.ExecuteScalar() == null || command.ExecuteScalar() == DBNull.Value)
                {
                    sql = "select top 1 PropertyId from [App].[PropertyDetails] where ContactNumber = '" + MobileNo + "'";
                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = sql;
                    propertyID = command.ExecuteScalar() == null ? "1" : command.ExecuteScalar().ToString();
                }

                string sp = "INSERT INTO app.Notification (PropertyId,Subject,Message,NotificationTypeId,PropertyGroupId,IsActive,IsDeleted,CreatedOn,CreatedBy,ExpirtyDate) VALUES (@PropertyId,@Subject,@Message,@NotificationTypeId,@PropertyGroupId,@IsActive,0,GETDATE(),@CurrentUserId,@ExpirtyDate)";//"App.ManageNoticeBoard_Amen";

                SqlConnection con = new SqlConnection(constr);
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sp;
                cmd.Parameters.AddWithValue("@CmdType", "C");
                cmd.Parameters.AddWithValue("@NotificationId", 1);
                cmd.Parameters.AddWithValue("@PropertyId", propertyID);
                cmd.Parameters.AddWithValue("@Subject", "Amenity Booking #" + ds.Tables[0].Rows[0]["id"].ToString() + " Approved !");
                string message = "<p>Your " + ameName + " booking #" + ds.Tables[0].Rows[0]["id"].ToString() + " for " + ds.Tables[0].Rows[0]["NosOfPersons"].ToString() + " persons on time slot " + Convert.ToDateTime(ds.Tables[0].Rows[0]["TimeSlotFr"]).ToString("hh:mm") + " to " + Convert.ToDateTime(ds.Tables[0].Rows[0]["TimeSlotFr"]).ToString("hh:mm") + " has been approved !</p>";
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@NotificationTypeId", "1");
                cmd.Parameters.AddWithValue("@PropertyGroupId", "3");
                cmd.Parameters.AddWithValue("@ExpirtyDate", DateTime.Now.Date.AddDays(3));
                cmd.Parameters.AddWithValue("@IsActive", 1);
                cmd.Parameters.AddWithValue("@PropertyDetailsId", "");
                cmd.Parameters.AddWithValue("@PropertyTowerId", "");
                cmd.Parameters.AddWithValue("@Notify", "1");
                cmd.Parameters.AddWithValue("@AlertTypeId", "1");
                cmd.Parameters.AddWithValue("@IsSent", 0);
                cmd.Parameters.AddWithValue("@CurrentUserId", UserID);

                con.Open();
                cmd.ExecuteNonQuery();

                con.Close();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            connection.Close();

            return Ok(msg);
        }

        [Route("KYCApprove")]
        [HttpPost]
        public IHttpActionResult KYCApprove(int Id)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();
            string msg = "Approved Successfully !";
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "update [dbo].[kycdetails] set Approved = 1 where Id=@Id";
                param = new SqlParameter("@Id", Id);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            connection.Close();

            return Ok(msg);
        }

        [Route("KYCUpate")]
        [HttpPost]
        public IHttpActionResult KYCUpate(KycDetails kycDetails)
        {
            //string path = HttpContext.Current.Server.MapPath("~");

            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();
            string msg = "Updated Successfully !";
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "update [dbo].[kycdetails] set EmployeeId = '" + kycDetails.EmployeeId + "', EmployeeName = '" + kycDetails.EmployeeName + "', JobProfile = '" + kycDetails.JobProfile + "', Gender ='" + kycDetails.Gender + "', IdDoc = '" + kycDetails.IdDoc + "' where Id=@Id";

                param = new SqlParameter("@Id", kycDetails.Id);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);
                command.ExecuteNonQuery();

                if (kycDetails.Image != null)
                {
                    if (kycDetails.Image.Length > 0)
                    {
                        byte[] image = Convert.FromBase64String(kycDetails.Image);

                        SqlCommand sqlCommand = new SqlCommand();
                        sqlCommand.CommandType = CommandType.Text;
                        sqlCommand.Connection = connection;

                        sqlCommand.CommandText = "delete from KycImages where kycid='" + kycDetails.Id + "'";// and description = '" + "" + "'";
                        sqlCommand.ExecuteScalar();

                        sqlCommand.CommandText = "Insert into KycImages([Title],[Description],[ProfileImage],[Contents],[KycID],[Image]) values('" + kycDetails.EmployeeId + "', 'ProfileImage', @Image, ' ', " + kycDetails.Id + ",' ')";
                        sqlCommand.Parameters.AddWithValue("@Image", image);
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }
            connection.Close();

            return Ok(msg);
        }

        [Route("AmenitiesBookings")]
        [HttpGet]
        public IHttpActionResult AmenitiesBookings(int PropertyID, int UserID = 0, string DateFr = null, string DateTo = null)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select amt.id, amt.UserId, amm.AmenitiesId, amm.AmenitiesName, CONCAT(format(amt.timeslotfr, 'hh:mm'), ' to ', format(amt.timeslotto, 'hh:mm')) timeslot, amt.nosofpersons, amt.timeslotfr, amt.timeslotto, concat(usr.FirstName, ' ', usr.LastName) UserName, amt.Approved, amp.Capacity, usr.contactNumber from [dbo].[AmenitiesBooking] amt inner join [Master].[Amenities] amm on amm.AmenitiesId=amt.AmenitiesId inner join[App].[PropertyAmenities] amp on amp.AminitiesId=amm.AmenitiesId and amp.PropertyId=@PropertyID inner join [Identity].[Users] usr on usr.UserId=amt.UserId where (amt.IsDeleted is null or amt.IsDeleted=0) and amp.PropertyId=@PropertyID";

                param = new SqlParameter("@PropertyID", PropertyID);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);

                //if (DateFr != null)
                //{
                //    param = new SqlParameter("@DateFr", DateFr);// Convert.ToDateTime(DateFr).ToString("yyyy/MM/dd"));
                //    param.Direction = ParameterDirection.Input;
                //    param.DbType = DbType.String;
                //    command.Parameters.Add(param);
                //    command.CommandText += " and format(amt.BookingDate, 'yyyyMMdd') >= @DateFr";

                //    param = new SqlParameter("@DateTo", DateTo);// Convert.ToDateTime(DateTo).ToString("yyyy/MM/dd"));
                //    param.Direction = ParameterDirection.Input;
                //    param.DbType = DbType.String;
                //    command.Parameters.Add(param);
                //    command.CommandText += " and format(amt.BookingDate, 'yyyyMMdd') <= @DateTo";
                //}
                if (UserID > 0)
                {
                    param = new SqlParameter("@UserID", UserID);
                    param.Direction = ParameterDirection.Input;
                    param.DbType = DbType.String;
                    command.Parameters.Add(param);
                    command.CommandText += " and amt.userid = @UserID";
                }

                command.CommandText += " order by amm.AmenitiesName";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new AmenitiesBookings
                {
                    PropertyId = PropertyID,
                    Id = dataRow.Field<int>("Id"),
                    AmenitiesId = dataRow.Field<int>("AmenitiesId"),
                    AmenitiesName = dataRow.Field<string>("AmenitiesName"),
                    NosOfPersons = dataRow.Field<int>("nosofpersons"),
                    TimeSlot = dataRow.Field<string>("timeslot"),
                    TimeSlotFr = dataRow.Field<DateTime>("timeslotfr"),
                    TimeSlotTo = dataRow.Field<DateTime>("timeslotto"),
                    Approved = dataRow["Approved"] == DBNull.Value ? 0 : dataRow.Field<int>("Approved"),
                    ApproveStatus = dataRow["Approved"] == DBNull.Value ? "Not Approved" : "Approved",
                    UserName = dataRow.Field<string>("UserName"),
                    MobileNo = dataRow.Field<string>("contactNumber"),
                    UserId = dataRow.Field<int>("UserId")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        //select att.EmployeeID, kyc.EmployeeName, att.PunchTime, att.PunchType from [dbo].[attendancelogs] att inner join [dbo].kycdetails kyc on att.employeeid=kyc.employeeid order by att.id

        [Route("AttendanceLogs")]
        [HttpGet]
        public IHttpActionResult AttendanceLogs()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select kyc.EmployeeID, kyc.EmployeeName, minAtt.inTime, minAttGate.GateNo minAttGateNo, minAttGate.EmployeeName minAttCreatedBy, maxAtt.outTime, maxAttGate.GateNo maxAttGateNo, maxAttGate.EmployeeName maxAttCreatedBy from [dbo].[KycDetails] kyc inner join (select min(punchtime) inTime, EmployeeId from [dbo].attendancelogs mAtt where cast(punchtime as date) = cast(getdate() as date) group by EmployeeId) minAtt on minAtt.employeeid=kyc.employeeid inner join (select mAtt.GateNo, EmployeeId, PunchTime, grd.EmployeeName from [dbo].attendancelogs mAtt inner join guardmaster grd on mAtt.CreatedBy=grd.id where cast(punchtime as date) = cast(getdate() as date)) minAttGate on minAttGate.employeeid=minAtt.employeeid and minAttGate.PunchTime=minAtt.inTime inner join (select max(punchtime) outTime, EmployeeId from [dbo].attendancelogs mAtt where cast(punchtime as date) =cast(getdate() as date) group by EmployeeId) maxAtt on minAtt.employeeid=kyc.employeeid inner join (select GateNo, EmployeeId, PunchTime, grd.EmployeeName from [dbo].attendancelogs mAtt inner join guardmaster grd on mAtt.CreatedBy=grd.id where cast(punchtime as date) = cast(getdate() as date)) maxAttGate on maxAttGate.employeeid=maxAtt.employeeid and maxAttGate.PunchTime=maxAtt.outTime where kyc.Approved = 1 order by kyc.id";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new AttendanceLogs
                {
                    EmployeeId = dataRow.Field<string>("EmployeeId"),
                    EmployeeName = dataRow.Field<string>("EmployeeName"),
                    GateNo = "",
                    PunchTime = dataRow.Field<DateTime>("InTime").ToString("hh:mm:ss t") + ", Gate No.: " + dataRow.Field<string>("minAttGateNo") + ", Guard: " + dataRow.Field<string>("minAttCreatedBy"),
                    PunchType = dataRow.Field<DateTime>("InTime") == dataRow.Field<DateTime>("OutTime") ? "" : dataRow.Field<DateTime>("OutTime").ToString("hh:mm:ss t") + ", Gate No.: " + dataRow.Field<string>("maxAttGateNo") + ", Guard: " + dataRow.Field<string>("maxAttCreatedBy")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("EmployeeList")]
        [HttpGet]
        public IHttpActionResult EmployeeList()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select Id,EmployeeName,FatherName,Designation,MobileNo,IsDeleted,Approved from [dbo].employeelist where (IsDeleted is null or IsDeleted = 0) and (Approved = 1) order by EmployeeName";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Employee
                {
                    Id = dataRow.Field<int>("Id"),
                    EmployeeName = dataRow.Field<string>("EmployeeName"),
                    FatherName = dataRow.Field<string>("FatherName"),
                    Designation = dataRow.Field<string>("Designation"),
                    MobileNo = dataRow.Field<string>("MobileNo"),
                    IsDeleted = dataRow["IsDeleted"] == DBNull.Value ? 0 : dataRow["IsDeleted"] == null ? 0 : dataRow.Field<int>("IsDeleted"),
                    Approved = dataRow["Approved"] == DBNull.Value ? 0 : dataRow["Approved"] == null ? 0 : dataRow.Field<int>("Approved")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("CreateEmployee")]
        [HttpPost]
        public IHttpActionResult CreateEmployee(Employee employee)
        {
            string res = "1";
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select count(id) from [dbo].employeelist where (IsDeleted is null or IsDeleted = 0) and ((EmployeeName=@EmployeeName and FatherName = @FatherName) or MobileNo=@MobileNo) and not id = @Id";
                command.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                command.Parameters.AddWithValue("@FatherName", employee.FatherName);
                command.Parameters.AddWithValue("@MobileNo", employee.MobileNo);
                command.Parameters.AddWithValue("@Id", employee.Id);

                var resp = command.ExecuteScalar();
                res = resp == DBNull.Value ? "1" : Convert.ToInt32(resp) > 0 ? "Name or mobile number already exists !" : "1";

                if (res == "1")
                {
                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;

                    if (employee.Id > 0)
                        command.CommandText = "update [dbo].employeelist set EmployeeName=@EmployeeName,FatherName=@FatherName,Designation=@Designation,MobileNo=@MobileNo where id = @Id";
                    else
                        command.CommandText = "insert into [dbo].employeelist (EmployeeName,FatherName,Designation,MobileNo,Approved) values(@EmployeeName,@FatherName,@Designation,@MobileNo,@Approved)";


                    command.Parameters.AddWithValue("@Id", employee.Id);
                    command.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                    command.Parameters.AddWithValue("@FatherName", employee.FatherName);
                    command.Parameters.AddWithValue("@Designation", employee.Designation);
                    command.Parameters.AddWithValue("@MobileNo", employee.MobileNo);
                    command.Parameters.AddWithValue("@Approved", "1");

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok(res);
        }

        [Route("GuardList")]
        [HttpGet]
        public IHttpActionResult GuardList()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select Id,EmployeeName,FatherName,Designation,MobileNo,IsDeleted,Approved from [dbo].GuardMaster where (IsDeleted is null or IsDeleted = 0) and (Approved = 1) order by EmployeeName";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new GuardMaster
                {
                    Id = dataRow.Field<int>("Id"),
                    EmployeeName = dataRow.Field<string>("EmployeeName"),
                    FatherName = dataRow.Field<string>("FatherName"),
                    Designation = dataRow.Field<string>("Designation"),
                    MobileNo = dataRow.Field<string>("MobileNo"),
                    IsDeleted = dataRow["IsDeleted"] == DBNull.Value ? 0 : dataRow["IsDeleted"] == null ? 0 : dataRow.Field<int>("IsDeleted"),
                    Approved = dataRow["Approved"] == DBNull.Value ? 0 : dataRow["Approved"] == null ? 0 : dataRow.Field<int>("Approved")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("CreateGuard")]
        [HttpPost]
        public IHttpActionResult CreateGuard(GuardMaster employee)
        {
            string res = "1";
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select count(id) from [dbo].guardmaster where (IsDeleted is null or IsDeleted = 0) and ((EmployeeName=@EmployeeName and FatherName = @FatherName) or MobileNo=@MobileNo) and not id = @Id";
                command.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                command.Parameters.AddWithValue("@FatherName", employee.FatherName);
                command.Parameters.AddWithValue("@MobileNo", employee.MobileNo);
                command.Parameters.AddWithValue("@Id", employee.Id);

                var resp = command.ExecuteScalar();
                res = resp == DBNull.Value ? "1" : Convert.ToInt32(resp) > 0 ? "Name or mobile number already exists !" : "1";

                if (res == "1")
                {
                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;

                    if (employee.Id > 0)
                        command.CommandText = "update [dbo].GuardMaster set EmployeeName=@EmployeeName,FatherName=@FatherName,Designation=@Designation,MobileNo=@MobileNo where id = @Id";
                    else
                        command.CommandText = "insert into [dbo].GuardMaster (EmployeeName,FatherName,Designation,MobileNo,Approved) values(@EmployeeName,@FatherName,@Designation,@MobileNo,@Approved)";


                    command.Parameters.AddWithValue("@Id", employee.Id);
                    command.Parameters.AddWithValue("@EmployeeName", employee.EmployeeName);
                    command.Parameters.AddWithValue("@FatherName", employee.FatherName);
                    command.Parameters.AddWithValue("@Designation", employee.Designation);
                    command.Parameters.AddWithValue("@MobileNo", employee.MobileNo);
                    command.Parameters.AddWithValue("@Approved", "1");

                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok(res);
        }

        [Route("KycDetailsList")]
        [HttpGet]
        public IHttpActionResult KycDetailsList()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select Id,MobileNo,EmployeeId,EmployeeName,Gender,JobProfile,Approved,Image,IdDoc from KYCdetails where (IsDeleted is null or IsDeleted = 0) order by id";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new KycDetails
                {
                    Id = dataRow.Field<int>("Id"),
                    EmployeeId = dataRow.Field<string>("EmployeeId"),
                    EmployeeName = dataRow.Field<string>("EmployeeName"),
                    Gender = dataRow.Field<string>("Gender"),
                    JobProfile = dataRow.Field<string>("JobProfile"),
                    MobileNo = dataRow.Field<string>("MobileNo"),
                    IdDoc = dataRow.Field<string>("IdDoc"),
                    Image = dataRow.Field<string>("Image"),
                    ApproveStatus = dataRow["Approved"] == DBNull.Value ? "Not Approved" : "Approved"
                })).ToList();
                ds.Dispose();
                connection.Close();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                ds.Dispose();
                connection.Close();
                return null;
            }
        }

        [Route("QuestionsOfTask")]
        [HttpGet]
        public IHttpActionResult QuestionsOfTask(int taskID)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            SqlParameter param;
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select 0 TransactionID, q.QuestionID, q.QuestionName, t.Name taskName, t.Occurence, '' Action, '' Remarks from TaskWiseQuestionnaire q inner join TaskMaster t on q.taskid = t.id where taskid = @taskID order by id";
                param = new SqlParameter("@taskID", taskID);
                param.Direction = ParameterDirection.Input;
                param.DbType = DbType.String;
                command.Parameters.Add(param);
                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseQuestionnaire
                {
                    TransactionID = dataRow.Field<int>("TransactionID"),
                    TaskID = taskID,
                    TaskName = dataRow.Field<string>("taskName"),
                    Occurance = dataRow.Field<string>("Occurence"),
                    QuestionName = dataRow.Field<string>("QuestionName"),
                    QuestID = dataRow.Field<int>("QuestionID"),
                    Action = dataRow.Field<string>("Action"),
                    Remarks = dataRow.Field<string>("Remarks")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("AssignToList")]
        [HttpGet]
        public IHttpActionResult AssignToList(int propertyId)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select distinct fm.facilitymemberid, CONCAT(fm.name, ' - ', fm.mobilenumber, ' - ', fmas.FacilityName) name, fm.IsActive, fm.facilitymasterid from app.facilitymember fm inner join [App].[FacilityMaster] fmas on fmas.FacilityMasterId=fm.FacilityMasterId where 1=1 and fm.IsActive=1 and fm.PropertyId = @propertyId order by name";
                command.Parameters.AddWithValue("@propertyId", propertyId);

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new AssignToList
                {
                    Id = dataRow.Field<int>("FacilityMemberId"),
                    Name = dataRow.Field<string>("name")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("DashboardAssignToList")]
        [HttpGet]
        public IHttpActionResult DashboardAssignToList(int propertyId)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select distinct fm.facilitymemberid, CONCAT(fm.name, ' - ', fm.mobilenumber, ' - ', fmas.FacilityName) name, fm.IsActive, fm.facilitymasterid from app.facilitymember fm inner join [App].[FacilityMaster] fmas on fmas.FacilityMasterId=fm.FacilityMasterId where 1=1 and fm.IsActive=1 and fm.PropertyId = @propertyId and fm.facilitymemberid in (select distinct assignto from TaskMaster) order by name";
                command.Parameters.AddWithValue("@propertyId", propertyId);

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new AssignToList
                {
                    Id = dataRow.Field<int>("FacilityMemberId"),
                    Name = dataRow.Field<string>("name")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("TaskDetails")]
        [HttpGet]
        public IHttpActionResult TaskDetails(int catID = 0, int subCatID = 0, string occurrence = "0", int assingedtoID = 0, DateTime? dteFr = null, DateTime? dteTo = null, string taskstatus = null, int propID = 0, int taskPriorityId = 0)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            if (occurrence == "D") { occurrence = "2"; }
            else if (occurrence == "W") { occurrence = "1"; }
            else if (occurrence == "M") { occurrence = "3"; }
            else if (occurrence == "Y") { occurrence = "4"; }
            else if (occurrence == "N") { occurrence = "0"; }

            if (catID > 0 || subCatID > 0 || Convert.ToInt32(occurrence) > 0 || assingedtoID > 0 || (dteFr != null && dteTo != null))
            {
                try
                {
                    connection.Open();
                    command = new SqlCommand();
                    command.Connection = connection;

                    SqlCommand command1 = new SqlCommand();
                    command1.Connection = connection;
                    command1.CommandType = CommandType.StoredProcedure;
                    command1.CommandText = "DateWiseTaskStatusProc";
                    command1.ExecuteNonQuery();

                    command.CommandType = CommandType.Text;

                    //command.CommandText = " select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(stat.Remarks,'') Remarks, t.id taskID, ";
                    //command.CommandText = " select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(twt.Remarks,'') Remarks, t.id taskID, ";
                    command.CommandText = " select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(twt.Remarks,'') Remarks, t.id taskID, ";

                    if (dteFr == null)
                    {
                        command.CommandText += " t.datefrom DateFrom, ";
                    }
                    else
                    {
                        command.CommandText += " iif(t.occurence='D', '" + Convert.ToDateTime(dteFr).ToString("MM/dd/yyyy") + "', t.datefrom) DateFrom, ";
                    }


                    //command.CommandText += " t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(len(rtrim(ltrim(twt.Remarks))) > 0,'Actionable', 'Compleate'),'Compleate')), 'Compleate') as taskStatus, stat.updatedon from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id=stat.taskid left join TaskWiseTransaction twt on twt.TaskName = t.Name inner join App.PropertyMaster pm on fm.PropertyId = pm.PropertyId where 1=1 ";
                    //command.CommandText += " t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, t.AssetsID, t.Location, stat.TaskStatus, twt.updatedon from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.TaskWiseStatus stat on t.Name=stat.TaskName left join TaskWiseTransaction twt on twt.TaskName = t.Name and twt.QuestID = stat.QuestID and cast(twt.createdon as datetime) = cast(stat.CreatedOn as datetime) and twt.Remarks = stat.Remarks inner join App.PropertyMaster pm on fm.PropertyId = pm.PropertyId where 1=1 ";
                    //command.CommandText += " t.dateto, t.timefrom, t.timeto, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, t.AssetsID, t.Location, twt.TaskStatus, tra.TaskPriorityId, twt.updatedon from TaskMaster t inner join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId inner join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId inner join app.facilitymember fm on t.assignto = fm.FacilityMemberId left outer join TaskWiseStatusFinal twt on twt.TaskID = t.Id left join TaskWiseTransaction tra on tra.TaskName = t.Name inner join App.PropertyMaster pm on fm.PropertyId = pm.PropertyId where 1=1 ";
                    command.CommandText += " t.dateto, t.timefrom, t.timeto, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, t.AssetsID, t.Location, twt.TaskStatus, twt.TaskPriority, twt.TaskPriorityId, twt.updatedon from TaskMaster t inner join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId inner join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId inner join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join taskwisedailystatusfinal twt on twt.TaskId = t.Id  inner join App.PropertyMaster pm on fm.PropertyId = pm.PropertyId where 1=1 ";


                    if (dteFr != null && dteTo != null)
                    {
                        command.CommandText += " and cast(twt.updatedon as date) >= '" + Convert.ToDateTime(dteFr).ToString("yyyy/MM/dd") + "' and cast(twt.updatedon as date) <= '" + Convert.ToDateTime(dteTo).ToString("yyyy/MM/dd") + "'";
                    }

                    if (propID > 0)
                    {
                        command.Parameters.AddWithValue("@propID", propID);
                        command.CommandText += " and pm.PropertyId = @propID";
                    }

                    if (catID > 0)
                    {
                        command.Parameters.AddWithValue("@Id", catID);
                        command.CommandText += " and t.categoryId = @Id";
                    }
                    if (subCatID > 0)
                    {
                        command.Parameters.AddWithValue("@SId", subCatID);
                        command.CommandText += " and t.SubcategoryId = @SId";
                    }

                    if (taskPriorityId > 0)
                    {
                        command.Parameters.AddWithValue("@Id", taskPriorityId);
                        command.CommandText += " and twt.TaskPriorityId = @Id";
                    }

                    if (occurrence.ToUpper() == "D" || occurrence.ToUpper() == "W" || occurrence.ToUpper() == "M" || occurrence.ToUpper() == "Y" || occurrence == "")
                    {
                        if (occurrence.ToUpper() == "D")
                        { occurrence = "2"; }
                        else if (occurrence.ToUpper() == "W")
                        { occurrence = "1"; }
                        else if (occurrence.ToUpper() == "M")
                        { occurrence = "3"; }
                        else if (occurrence.ToUpper() == "Y")
                        { occurrence = "4"; }
                    }

                    if (Convert.ToInt32(occurrence) > 0 || occurrence.Length > 0)//0-all, 1-w, 2-d, 3-monthly, 4-year
                    {
                        if (Convert.ToInt32(occurrence) == 1 || occurrence.ToUpper() == "W")//0-all, 1-w, 2-d, 3-monthly, 4-year
                        {
                            command.Parameters.AddWithValue("@Occurance", "W");
                            command.CommandText += " and t.Occurence = @Occurance";// and ([dbo].IsTransDateWeekly(t.DateFrom, cast(getdate() as date)))>0";
                        }
                        else if (Convert.ToInt32(occurrence) == 2 || occurrence.ToUpper() == "D")
                        {
                            command.Parameters.AddWithValue("@Occurance", "D");
                            command.CommandText += " and t.Occurence = @Occurance";
                        }
                        else if (Convert.ToInt32(occurrence) == 3 || occurrence.ToUpper() == "M")
                        {
                            command.Parameters.AddWithValue("@Occurance", "M");
                            command.CommandText += " and t.Occurence = @Occurance";// and datepart(month,t.datefrom) = datepart(month, getdate())";
                        }
                        else if (Convert.ToInt32(occurrence) == 4 || occurrence.ToUpper() == "Y")
                        {
                            command.Parameters.AddWithValue("@Occurance", "Y");
                            command.CommandText += " and t.Occurence = @Occurance";
                        }
                    }
                    if (assingedtoID > 0)
                    {
                        command.Parameters.AddWithValue("@AssignTo", assingedtoID);
                        command.CommandText += " and t.AssignTo = @AssignTo";
                    }

                    // my date comment begin
                    //if (Convert.ToInt32(occurrence) == 2 || occurrence.ToUpper() == "D" || Convert.ToInt32(occurrence) == 1 || occurrence.ToUpper() == "W" || Convert.ToInt32(occurrence) == 3 || occurrence.ToUpper() == "M")
                    //{
                    //}
                    //else
                    //{ 
                    //    if (dteFr != null)
                    //    {
                    //        command.Parameters.AddWithValue("@DateFrom", Convert.ToDateTime(dteFr).ToString("MM/dd/yyyy"));
                    //        command.CommandText += " and t.DateFrom >= @DateFrom";
                    //    }
                    //    if (dteTo != null)
                    //    {
                    //        command.Parameters.AddWithValue("@DateTo", Convert.ToDateTime(dteTo).ToString("MM/dd/yyyy"));
                    //        command.CommandText += " and t.DateTo <= @DateTo";
                    //    }
                    //}
                    // end

                    if (taskstatus != null)
                    {
                        if (taskstatus.ToUpper() == "PENDING")
                        {
                            //command.Parameters.AddWithValue("@AssignTo", assingedtoID);
                            //command.CommandText += " and stat.ComplQuests=0";
                            //command.CommandText += " and stat.TaskStatus='Pending'";
                            command.CommandText += " and twt.TaskStatus='Pending'";
                        }
                        if (taskstatus.ToUpper() == "COMPLETE")
                        {
                            //command.CommandText += " and (stat.TotalQuests>0 and stat.ComplQuests=stat.TotalQuests)";
                            //command.CommandText += " and stat.TaskStatus='Compleate'";
                            command.CommandText += " and twt.TaskStatus='Completed'";
                        }
                        if (taskstatus.ToUpper() == "ACTIONABLE")
                        {
                            //command.CommandText += " and (stat.ComplQuests<stat.TotalQuests and stat.ComplQuests>0)";
                            //command.CommandText += " and stat.TaskStatus='Actionable'";
                            command.CommandText += " and twt.TaskStatus='Actionable'";
                        }
                    }

                    //command.CommandText += " group by  cm.CategoryName, sm.SubCategoryName, t.Name, t.Description, t.Occurence, t.TimeFrom, t.TimeTo, twt.Remarks, t.id, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, t.AssetsID, t.Location, stat.TaskStatus, cast(twt.UpdatedOn as DateTime), cast(twt.createdon as DateTime)";
                    //command.CommandText += " group by  cm.CategoryName, sm.SubCategoryName, t.Name, t.Description, t.Occurence, t.TimeFrom, t.TimeTo, twt.Remarks, t.id, t.datefrom, t.dateto, t.timefrom, t.timeto, fm.name, t.CategoryId, t.SubCategoryId, t.AssignTo, fm.PropertyId, t.qrcode, t.AssetsID, t.Location, twt.TaskStatus, tra.TaskPriorityId, cast(twt.UpdatedOn as DateTime)";
                    command.CommandText += " order by t.id, twt.updatedon desc";

                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(ds);

                    var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskTransactionModel
                    {
                        CategoryName = dataRow.Field<string>("CategoryName"),
                        SubCategoryName = dataRow.Field<string>("SubCategoryName"),
                        Name = dataRow.Field<string>("Name"),
                        Description = dataRow.Field<string>("Description"),
                        Occurence = dataRow.Field<string>("Occurence"),
                        Remarks = dataRow.Field<string>("Remarks"),
                        TaskCategoryId = dataRow.Field<int>("CategoryId"),
                        TaskSubCategoryId = dataRow.Field<int>("SubCategoryId"),
                        TaskId = dataRow.Field<int>("taskID"),
                        DateFrom = dataRow["datefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("datefrom"),
                        DateTo = dataRow["dateto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("dateto"),
                        TimeFrom = dataRow["timefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timefrom"].ToString()),
                        TimeTo = dataRow["timeto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timeto"].ToString()),
                        AssignedTo = dataRow.Field<string>("AassignedTo"),
                        PropertyId = dataRow.Field<int>("PropertyId"),
                        AssignedToId = dataRow.Field<int>("AssignTo"),
                        QRCode = dataRow.Field<string>("QRCode"),
                        AssetId = dataRow.Field<int>("AssetsID"),
                        Location = dataRow.Field<string>("Location"),
                        TaskStatus = dataRow.Field<string>("TaskStatus"),
                        UpdatedOn = dataRow["updatedon"] == DBNull.Value ? "" : dataRow.Field<DateTime>("updatedon").ToString("dd-MM-yyyy"),
                        TaskPriorityId = dataRow["TaskPriorityId"] == DBNull.Value ? 0 : dataRow.Field<int>("TaskPriorityId"),
                        TaskPriority = dataRow["TaskPriority"] == DBNull.Value ? "" : dataRow.Field<string>("TaskPriority")
                    })).ToList();

                    return Ok(eList);
                }
                catch (Exception ex)
                {
                    string error = ex.Message;
                }
                finally
                {
                    ds.Dispose();
                    connection.Close();
                }
            }
            else
            {
                var data = new TaskTransactionModel()
                {
                    CategoryName = "",
                    SubCategoryName = "",
                    Name = "",
                    Description = "",
                    Occurence = "",
                    Remarks = "",
                    TaskCategoryId = 0,
                    TaskSubCategoryId = 0,
                    TaskId = 0,
                    DateFrom = Convert.ToDateTime("1900/01/01"),
                    DateTo = Convert.ToDateTime("1900/01/01"),
                    TimeFrom = Convert.ToDateTime("1900/01/01"),
                    TimeTo = Convert.ToDateTime("1900/01/01"),
                    AssignedTo = "",
                    AssignedToId = 0,
                    QRCode = "",
                    TaskStatus = ""
                };
                List<TaskTransactionModel> eList = new List<TaskTransactionModel>();
                eList.Add(data);
                return Ok(eList);
            }
            return Ok();
        }

        [Route("TaskDetailsWithQuestion")]
        [HttpGet]
        public IHttpActionResult TaskDetailsWithQuestion(int catID = 0, int subCatID = 0, string occurrence = "0", int assingedtoID = 0)
        {
            if (occurrence == "D") { occurrence = "2"; }
            else if (occurrence == "W") { occurrence = "1"; }
            else if (occurrence == "M") { occurrence = "3"; }
            else if (occurrence == "Y") { occurrence = "4"; }

            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                string sql = "select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description, '')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(t.Remarks, '') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests > 0, iif((stat.TotalQuests - stat.ComplQuests) = 0, 'Compleate', iif(stat.ComplQuests > 0, 'Actionable', 'Pending')), 'Pending') as taskStatus,stat.remarks RemarksQ, stat.updatedon from TaskMaster t left join[calendar].Category cm on t.CategoryId = cm.ScheduleCategoryId left join[calendar].[SubCategory] sm on t.SubCategoryId = sm.SubCategoryId left outer join(select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName = tm.Name) tm on t.id = tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id = stat.taskid where 1 = 1 and t.Occurence = 'D' union select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description, '')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(t.Remarks, '') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests > 0, iif((stat.TotalQuests - stat.ComplQuests) = 0, 'Compleate', iif(stat.ComplQuests > 0, 'Actionable', 'Pending')), 'Pending') as taskStatus, stat.remarks RemarksQ, stat.updatedon from TaskMaster t left join[calendar].Category cm on t.CategoryId = cm.ScheduleCategoryId left join[calendar].[SubCategory] sm on t.SubCategoryId = sm.SubCategoryId left outer join(select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName = tm.Name) tm on t.id = tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id = stat.taskid where 1 = 1 and t.Occurence = 'W' and([dbo].IsTransDateWeekly(t.DateFrom, cast(getdate() as date)))> 0 union select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description, '')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(t.Remarks, '') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests > 0, iif((stat.TotalQuests - stat.ComplQuests) = 0, 'Compleate', iif(stat.ComplQuests > 0, 'Actionable', 'Pending')), 'Pending') as taskStatus, stat.remarks RemarksQ, stat.updatedon from TaskMaster t left join[calendar].Category cm on t.CategoryId = cm.ScheduleCategoryId left join[calendar].[SubCategory] sm on t.SubCategoryId = sm.SubCategoryId left outer join(select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName = tm.Name) tm on t.id = tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id = stat.taskid where 1 = 1 and t.Occurence = 'M' and datepart(day, t.datefrom) = datepart(day, getdate()) union select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description, '')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(t.Remarks, '') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests > 0, iif((stat.TotalQuests - stat.ComplQuests) = 0, 'Compleate', iif(stat.ComplQuests > 0, 'Actionable', 'Pending')), 'Pending') as taskStatus, stat.remarks RemarksQ, stat.updatedon from TaskMaster t left join[calendar].Category cm on t.CategoryId = cm.ScheduleCategoryId left join[calendar].[SubCategory] sm on t.SubCategoryId = sm.SubCategoryId left outer join(select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName = tm.Name) tm on t.id = tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id = stat.taskid where 1 = 1 and t.Occurence = 'Y' and t.datefrom = getdate() order by taskStatus desc";


                command.CommandText = sql;

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskTransactionModel
                {
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    SubCategoryName = dataRow.Field<string>("SubCategoryName"),
                    Name = dataRow.Field<string>("Name"),
                    Description = dataRow.Field<string>("Description"),
                    Occurence = dataRow.Field<string>("Occurence"),
                    Remarks = dataRow.Field<string>("Remarks"),
                    TaskCategoryId = dataRow.Field<int>("CategoryId"),
                    TaskSubCategoryId = dataRow.Field<int>("SubCategoryId"),
                    TaskId = dataRow.Field<int>("taskID"),
                    DateFrom = dataRow["datefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("datefrom"),
                    DateTo = dataRow["dateto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("dateto"),
                    TimeFrom = dataRow["timefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timefrom"].ToString()),
                    TimeTo = dataRow["timeto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timeto"].ToString()),
                    AssignedTo = dataRow.Field<string>("AassignedTo"),
                    AssignedToId = dataRow.Field<int>("AssignTo"),
                    QRCode = dataRow.Field<string>("QRCode"),
                    TaskStatus = dataRow.Field<string>("taskStatus"),
                    QuestionName = "",//dataRow.Field<string>("QuestionName"),
                    RemarksQuestion = dataRow.Field<string>("RemarksQ"),
                    UpdatedOn = dataRow["UpdatedOn"] == DBNull.Value ? "" : dataRow.Field<DateTime>("UpdatedOn").ToString("dd-MM-yyyy")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("TaskDetailsEvent")]
        [HttpGet]
        public IHttpActionResult TaskDetailsEvent(int catID = 0, int subCatID = 0, int occurrence = 0, int assingedtoID = 0)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select t.* from (select t0.*, row_number() over (partition by t0.DateFrom order by t0.id desc) as seqnum from( select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(t.Remarks,'') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.id, iif(ttlTask.ttlTasks<=1, 'G', iif(ttlTask.ttlTasks>2,'R', 'O')) as TaskStatus from TaskMaster t  left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId  left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select COUNT(transactionid) ttlQuests, TransactionDate, TaskName from TaskWiseTransaction group by TransactionDate, TaskName) ttlQuest on ttlQuest.TransactionDate=t.datefrom and ttlQuest.TaskName=t.Name left outer join (select count(id) ttlTasks, datefrom from TaskMaster group by DateFrom) ttlTask on ttlTask.DateFrom=t.DateFrom left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t  inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid  left join app.facilitymember fm on t.assignto = fm.FacilityMemberId where 1=1 and t.DateFrom>= dateadd(day,1, EOMONTH ( getdate(), -2 )) and t.DateFrom<= EOMONTH ( getdate(), 1 )";

                if (catID > 0)
                {
                    command.Parameters.AddWithValue("@Id", catID);
                    command.CommandText += " and t.categoryId = @Id";
                }
                if (subCatID > 0)
                {
                    command.Parameters.AddWithValue("@SId", subCatID);
                    command.CommandText += " and t.SubcategoryId = @SId";
                }
                if (occurrence > 0)//0-all, 1-w, 2-d, 3-monthly, 4-year
                {
                    if (occurrence == 1)//0-all, 1-w, 2-d, 3-monthly, 4-year
                    {
                        command.Parameters.AddWithValue("@Occurance", "W");
                        command.CommandText += " and t.Occurence = @Occurance";
                    }
                    else if (occurrence == 2)
                    {
                        command.Parameters.AddWithValue("@Occurance", "D");
                        command.CommandText += " and t.Occurence = @Occurance";
                    }
                    else if (occurrence == 3)
                    {
                        command.Parameters.AddWithValue("@Occurance", "M");
                        command.CommandText += " and t.Occurence = @Occurance";
                    }
                    else if (occurrence == 4)
                    {
                        command.Parameters.AddWithValue("@Occurance", "Y");
                        command.CommandText += " and t.Occurence = @Occurance";
                    }
                }
                if (assingedtoID > 0)
                {
                    command.Parameters.AddWithValue("@AssignTo", assingedtoID);
                    command.CommandText += " and t.AssignTo = @AssignTo";
                }

                command.CommandText += ")t0 where t0.DateFrom>= dateadd(day,1, EOMONTH ( getdate(), -2 )) and t0.DateFrom<= EOMONTH ( getdate(), 1 )) t where seqnum <= 3; ";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskTransactionModel
                {
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    SubCategoryName = dataRow.Field<string>("SubCategoryName"),
                    Name = dataRow.Field<string>("Name"),
                    Description = dataRow.Field<string>("Description"),
                    Occurence = dataRow.Field<string>("Occurence"),
                    Remarks = dataRow.Field<string>("Remarks"),
                    TaskCategoryId = catID,
                    TaskSubCategoryId = subCatID,
                    TaskId = dataRow.Field<int>("taskID"),
                    DateFrom = dataRow["datefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("datefrom"),
                    DateTo = dataRow["dateto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("dateto"),
                    TimeFrom = dataRow["timefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timefrom"].ToString()),
                    TimeTo = dataRow["timeto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timeto"].ToString()),
                    AssignedTo = dataRow.Field<string>("AassignedTo"),
                    TaskStatus = dataRow.Field<string>("TaskStatus")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("CreateQuestionnaire")]
        [HttpPost]
        public IHttpActionResult CreateQuestionnaire(List<TaskWiseQuestions> taskWiseQuestions)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    foreach (var item in taskWiseQuestions)
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "insert into [dbo].[TaskWiseQuestionnaire](TaskID, QuestionName, CreatedBy, CreatedOn)values(@TaskID, @Question, @CreatedBy, getdate())";
                            cmd.Parameters.AddWithValue("@TaskID", item.TaskID);
                            cmd.Parameters.AddWithValue("@Question", item.QuestionName);
                            cmd.Parameters.AddWithValue("@CreatedBy", "1");
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "update [dbo].taskstatus set totalquests = (select count(QuestionID) from[TaskWiseQuestionnaire] tr where tr.taskid = @TaskID) where [dbo].taskstatus.taskid = @TaskID";
                            cmd.Parameters.AddWithValue("@TaskID", item.TaskID);
                            cmd.ExecuteNonQuery();
                        }

                    }

                    con.Close();
                }

                return Ok("Created !");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("UpdateQuestionnaire")]
        [HttpPost]
        public IHttpActionResult UpdateQuestionnaire(TaskWiseQuestions quest)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "update [dbo].[TaskWiseQuestionnaire] set QuestionName = '" + quest.QuestionName + "' where QuestionID = @QuestID";
                        cmd.Parameters.AddWithValue("@QuestID", quest.QuestID);
                        cmd.ExecuteNonQuery();
                    }

                    con.Close();
                }

                return Ok("sucess !");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("NoticeBoardSave")]
        [HttpPost]
        public IHttpActionResult CreateNoticeBoard(FormDataNoticeBoard model)
        {
            string userID = "1";
            var resp = "1";

            //var userID = Convert.ToInt32(HttpContext.User.Claims.First(x => x.Type == "UserIdId").Value); ;

            try
            {
                if (model.PropertyId != 0 && model.NotificationTypeId != 0 && model.PropertyGroupId != 0 && model.Notify != 0 && model.AlertTypeId != 0 && model.Subject != "")
                {
                    List<NoticeBoardAttachment> finalAttachmentData = new List<NoticeBoardAttachment>();

                    string sp = "INSERT INTO app.Notification (PropertyId,Subject,Message,NotificationTypeId,PropertyGroupId,IsActive,IsDeleted,CreatedOn,CreatedBy,ExpirtyDate) VALUES (@PropertyId,@Subject,@Message,@NotificationTypeId,@PropertyGroupId,@IsActive,0,GETDATE(),@CurrentUserId,@ExpirtyDate)";//"App.ManageNoticeBoard_Amen";

                    SqlConnection con = new SqlConnection(constr);
                    SqlCommand cmd = new SqlCommand();

                    cmd.Connection = con;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sp;
                    cmd.Parameters.AddWithValue("@CmdType", model.StatementType);
                    cmd.Parameters.AddWithValue("@NotificationId", userID);
                    cmd.Parameters.AddWithValue("@PropertyId", Convert.ToString(model.PropertyId));
                    cmd.Parameters.AddWithValue("@Subject", Convert.ToString(model.Subject));
                    cmd.Parameters.AddWithValue("@Message", Convert.ToString(model.Message));
                    cmd.Parameters.AddWithValue("@NotificationTypeId", Convert.ToString(model.NotificationTypeId));
                    cmd.Parameters.AddWithValue("@PropertyGroupId", Convert.ToString(model.PropertyGroupId));
                    cmd.Parameters.AddWithValue("@ExpirtyDate", model.ExpirtyDate);
                    cmd.Parameters.AddWithValue("@ExecutionDate", model.ExpirtyDate);
                    cmd.Parameters.AddWithValue("@IsActive", 1);
                    cmd.Parameters.AddWithValue("@PropertyDetailsId", model.PropertyDetailsId);
                    cmd.Parameters.AddWithValue("@PropertyTowerId", model.PropertyTowerId);
                    cmd.Parameters.AddWithValue("@Notify", Convert.ToString(model.Notify));
                    cmd.Parameters.AddWithValue("@AlertTypeId", Convert.ToString(model.AlertTypeId));
                    cmd.Parameters.AddWithValue("@IsSent", 0);
                    cmd.Parameters.AddWithValue("@CurrentUserId", userID);

                    con.Open();
                    cmd.ExecuteNonQuery();

                    con.Close();
                }
            }
            catch (Exception ex)
            {
                resp = ex.Message;

            }
            return Ok(resp);
        }

        [Route("CreateSubCategory")]
        [HttpPost]
        public IHttpActionResult CreateSubCategory(SubCategory subCategory)
        {
            string resp = "";
            SqlConnection con = new SqlConnection(constr);
            con.Open();

            try
            {
                if (subCategory.CategoryId == 0)
                { return Ok("Invalid Category ID !"); }
                if (subCategory.SubCategoryName.Length == 0)
                { return Ok("Invalid Sub Category Name !"); }

                string sp = "INSERT INTO [calendar].[SubCategory] (CategoryId,SubCategoryName) VALUES (@CategoryId,@SubCategoryName)";

                SqlCommand cmd = new SqlCommand();

                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sp;
                cmd.Parameters.AddWithValue("@CategoryId", subCategory.CategoryId);
                cmd.Parameters.AddWithValue("@SubCategoryName", subCategory.SubCategoryName);

                cmd.ExecuteNonQuery();
                resp = "Saved Succefully !";
            }
            catch (Exception ex)
            {
                resp = ex.Message;
            }

            con.Close();
            return Ok(resp);
        }

        [Route("DeleteQuestionnaire")]
        [HttpDelete]
        public IHttpActionResult DeleteQuestionnaire(int questID)
        {
            string taskId = ""; int complQuests = 0;
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "select taskid from [dbo].[TaskWiseQuestionnaire] where QuestionID = @QuestID";
                        cmd.Parameters.AddWithValue("@QuestID", questID);
                        taskId = cmd.ExecuteScalar().ToString();
                    }

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "select ComplQuests from TaskStatus where taskid = @TaskId";
                        cmd.Parameters.AddWithValue("@TaskId", taskId);
                        complQuests = cmd.ExecuteScalar() == DBNull.Value ? 0 : Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    con.Close();
                }
                if (complQuests == 0)
                {
                    using (SqlConnection con = new SqlConnection(constr))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            con.Open();
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "delete from [dbo].[TaskWiseQuestionnaire] where QuestionID = @QuestID";
                            cmd.Parameters.AddWithValue("@QuestID", questID);
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }

                    return Ok("Deleted !");
                }
                else
                {
                    return Ok("Can not delete, this question already responded !");
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("CreateTask")]
        [HttpPost]
        public IHttpActionResult CreateTask(TaskMaster taskMaster)
        {
            if (string.IsNullOrWhiteSpace(taskMaster.Name))
            {
                return Ok("Task name cannot be blank.");
            }
            if (string.IsNullOrWhiteSpace(taskMaster.Occurence))
            {
                return Ok("Task occurrence cannot be blank.");
            }
            if (taskMaster.CategoryId == 0)
            {
                return Ok("Task category cannot be blank.");
            }
            if (taskMaster.SubCategoryId == 0)
            {
                return Ok("Task sub-category cannot be blank.");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    // Insert or update the task
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;

                        if (taskMaster.Id == 0)
                        {
                            cmd.CommandText = "INSERT INTO [dbo].[TaskMaster](CategoryId, SubCategoryId, Name, Description, DateFrom, DateTo, TimeFrom, TimeTo, Remarks, Occurence, createdby, createdon, AssignTo, RemindMe, Location, QRCode, AssetsID,task_type) " +
                                              "VALUES (@CategoryId, @SubCategoryId, @Name, @Description, @DateFrom, @DateTo, @TimeFrom, @TimeTo, @Remarks, @Occurence, @createdby, GETDATE(), @AssignTo, @RemindMe, @Location, @QRCode, @AssetsID,@type)";
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE [dbo].[TaskMaster] SET CategoryId=@CategoryId, SubCategoryId=@SubCategoryId, Name=@Name, Description=@Description, DateFrom=@DateFrom, DateTo=@DateTo, TimeFrom=@TimeFrom, TimeTo=@TimeTo, Remarks=@Remarks, Occurence=@Occurence, AssignTo=@AssignTo, RemindMe=@RemindMe, Location=@Location, QRCode=@QRCode, AssetsID=@AssetsID " +
                                              "WHERE Id = @Id";
                        }

                        cmd.Parameters.AddWithValue("@Id", taskMaster.Id);
                        cmd.Parameters.AddWithValue("@CategoryId", taskMaster.CategoryId);
                        cmd.Parameters.AddWithValue("@SubCategoryId", taskMaster.SubCategoryId);
                        cmd.Parameters.AddWithValue("@Name", taskMaster.Name);
                        cmd.Parameters.AddWithValue("@Description", taskMaster.Description);
                        cmd.Parameters.AddWithValue("@DateFrom", Convert.ToDateTime(taskMaster.DateFrom).ToString("MM/dd/yyyy"));
                        cmd.Parameters.AddWithValue("@DateTo", Convert.ToDateTime(taskMaster.DateTo).ToString("MM/dd/yyyy"));
                        cmd.Parameters.AddWithValue("@TimeFrom", Convert.ToDateTime(taskMaster.TimeFrom).ToString("hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@TimeTo", Convert.ToDateTime(taskMaster.TimeTo).ToString("hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@Remarks", taskMaster.Remarks);
                        cmd.Parameters.AddWithValue("@Occurence", taskMaster.Occurence);
                        cmd.Parameters.AddWithValue("@createdby", taskMaster.CreatedBy);
                        cmd.Parameters.AddWithValue("@AssignTo", taskMaster.AssignTo);
                        cmd.Parameters.AddWithValue("@RemindMe", taskMaster.RemindMe);
                        cmd.Parameters.AddWithValue("@Location", taskMaster.Location);
                        cmd.Parameters.AddWithValue("@QRCode", taskMaster.QRCode);
                        cmd.Parameters.AddWithValue("@AssetsID", taskMaster.AssetsID);
                        cmd.Parameters.AddWithValue("@type", taskMaster.Type);

                        cmd.ExecuteNonQuery();
                    }

                    // Execute the stored procedure after task creation/update
                    using (SqlCommand procCmd = new SqlCommand("[DateWiseTaskStatusDashRecoProc]", con))
                    {
                        procCmd.CommandType = CommandType.StoredProcedure;

                        // Add the current date as a parameter
                        procCmd.Parameters.Add("@Date", SqlDbType.Date).Value = DateTime.Now.Date;

                        procCmd.ExecuteNonQuery();
                    }
                }

                return Ok("Created !");
            }
            catch (Exception ex)
            {
                return Ok($"An error occurred: {ex.Message}");
            }
        }

        [Route("DeleteTask")]
        [HttpDelete]
        public IHttpActionResult DeleteTask(int taskID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        con.Open();
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "delete from [dbo].[TaskMaster] where id = @taskID";
                        cmd.Parameters.AddWithValue("@taskID", taskID);
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }

                return Ok("Deleted !");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("GetAssets")]
        [HttpGet]
        public IHttpActionResult GetAssets(int propertyId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    SqlCommand command = new SqlCommand();
                    SqlDataAdapter adapter;
                    DataSet ds = new DataSet();

                    con.Open();
                    command.Connection = con;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "select ID, Name, Description, QRCode, AssetValue, AssetType, Manufacturer, AssetModel, IsMoveable, AssetImage, AMCdoc, LastServiceDate, NextServiceDate, IsRentable, PropertyId,Status,Category,Location from [dbo].[AssetMaster] where PropertyId = @propertyId and (isdeleted = 0) order by NextServiceDate";


                    SqlParameter param = new SqlParameter("@propertyId", propertyId);
                    command.Parameters.Add(param);

                    adapter = new SqlDataAdapter(command);
                    adapter.Fill(ds);

                    var assets = (ds.Tables[0].AsEnumerable().Select(dataRow => new AssetsMaster
                    {
                        Id = dataRow.Field<int>("id"),
                        Name = dataRow.Field<string>("Name"),
                        Description = dataRow.Field<string>("Description"),
                        QRCode = dataRow.Field<string>("QRCode"),
                        AssetType = dataRow.Field<string>("AssetType"),
                        Manufacturer = dataRow.Field<string>("Manufacturer"),
                        AssetModel = dataRow.Field<string>("AssetModel"),
                        AssetValue = dataRow.Field<int>("AssetValue"),
                        IsMoveable = dataRow.Field<bool>("IsMoveable"),
                        AssetImage = dataRow.Field<byte[]>("AssetImage") != null ? Convert.ToBase64String(dataRow.Field<byte[]>("AssetImage")) : null,
                        AMCdoc = dataRow.Field<byte[]>("AMCdoc") != null ? Convert.ToBase64String(dataRow.Field<byte[]>("AMCdoc")) : null,
                        LastServiceDate = dataRow.IsNull("LastServiceDate") ? null : dataRow.Field<DateTime>("LastServiceDate").ToString("yyyy-MM-dd"),
                        NextServiceDate = dataRow.IsNull("NextServiceDate") ? null : dataRow.Field<DateTime>("NextServiceDate").ToString("yyyy-MM-dd"),
                        IsRentable = dataRow.Field<bool>("IsRentable"),
                        Status = dataRow.Field<bool>("Status"),
                        Location = dataRow.Field<string>("Location"),
                        Category = dataRow.Field<string>("Category"),

                    })).ToList();

                    var passedServiceDates = assets.Where(a => a.NextServiceDate != null && DateTime.ParseExact(a.NextServiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) < DateTime.Now).ToList();
                    var upcomingServiceDates = assets.Where(a => a.NextServiceDate == null || DateTime.ParseExact(a.NextServiceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture) >= DateTime.Now).ToList();

                    var response = new
                    {
                        PassedServiceDates = passedServiceDates,
                        UpcomingServiceDates = upcomingServiceDates
                    };

                    con.Close();
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("ManageAssets")]
        [HttpPost]
        public IHttpActionResult ManageAssets(AssetsMaster assetsMaster)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    SqlConnection connection = new SqlConnection(constr);
                    connection.Open();
                    SqlCommand command = new SqlCommand();

                    // string strSql = "";
                    command = new SqlCommand("ManageAssetMaster", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Connection = connection;
                    command.Parameters.AddWithValue("@Id", assetsMaster.Id);
                    command.Parameters.AddWithValue("@Name", assetsMaster.Name);
                    command.Parameters.AddWithValue("@Description", assetsMaster.Description);
                    command.Parameters.AddWithValue("@QRCode", assetsMaster.QRCode);
                    command.Parameters.AddWithValue("@AssetType", assetsMaster.AssetType);
                    command.Parameters.AddWithValue("@Manufacturer", assetsMaster.Manufacturer);
                    command.Parameters.AddWithValue("@AssetModel", assetsMaster.AssetModel);
                    command.Parameters.AddWithValue("@AssetValue", assetsMaster.AssetValue);
                    command.Parameters.AddWithValue("@IsMoveable", assetsMaster.IsMoveable);
                    command.Parameters.AddWithValue("@LastServiceDate",
                        string.IsNullOrEmpty(assetsMaster.LastServiceDate) ? (object)DBNull.Value : DateTime.Parse(assetsMaster.LastServiceDate));

                    command.Parameters.AddWithValue("@NextServiceDate",
                        string.IsNullOrEmpty(assetsMaster.NextServiceDate) ? (object)DBNull.Value : DateTime.Parse(assetsMaster.NextServiceDate));

                    command.Parameters.AddWithValue("@IsRentable", assetsMaster.IsRentable);
                    command.Parameters.AddWithValue("@PropertyId", assetsMaster.PropertyId);
                    command.Parameters.AddWithValue("@Flag", assetsMaster.Flag);
                    command.Parameters.AddWithValue("@Status", assetsMaster.Status);
                    command.Parameters.AddWithValue("@Location", assetsMaster.Location);
                    command.Parameters.AddWithValue("@Category", assetsMaster.Category);
                    if (!string.IsNullOrEmpty(assetsMaster.AssetImage))
                    {
                        try
                        {
                            string paddedAssetImage = assetsMaster.AssetImage.Replace(" ", "+");
                            int mod4 = paddedAssetImage.Length % 4;
                            if (mod4 > 0)
                            {
                                paddedAssetImage += new string('=', 4 - mod4);
                            }
                            byte[] imageByteArray = Convert.FromBase64String(paddedAssetImage);
                            SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
                            parameter.Value = imageByteArray;
                            command.Parameters.Add(parameter);
                        }
                        catch (FormatException ex)
                        {
                            SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
                            parameter.Value = DBNull.Value;
                            command.Parameters.Add(parameter);
                        }
                    }
                    else
                    {
                        SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
                        parameter.Value = DBNull.Value;
                        command.Parameters.Add(parameter);
                    }

                    // Pass the AMCdoc value as a parameter to the command
                    if (!string.IsNullOrEmpty(assetsMaster.AMCdoc))
                    {
                        try
                        {
                            string paddedAMCdoc = assetsMaster.AMCdoc.Replace(" ", "+");
                            int mod4 = paddedAMCdoc.Length % 4;
                            if (mod4 > 0)
                            {
                                paddedAMCdoc += new string('=', 4 - mod4);
                            }
                            byte[] amcDocByteArray = Convert.FromBase64String(paddedAMCdoc);
                            SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
                            amcDocParameter.Value = amcDocByteArray;
                            command.Parameters.Add(amcDocParameter);
                        }
                        catch (FormatException ex)
                        {
                            SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
                            amcDocParameter.Value = DBNull.Value;
                            command.Parameters.Add(amcDocParameter);
                        }
                    }
                    else
                    {
                        SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
                        amcDocParameter.Value = DBNull.Value;
                        command.Parameters.Add(amcDocParameter);
                    }

                    command.ExecuteNonQuery();

                    connection.Close();
                    return Ok("success");
                }
            }
            catch (Exception ex)
            { return Ok(ex.Message); }
        }



        //[Route("ManageAssets")]
        //[HttpPost]
        //public IHttpActionResult ManageAssets(AssetsMaster assetsMaster)
        //{
        //    try
        //    {
        //        using (SqlConnection con = new SqlConnection(constr))
        //        {
        //            SqlConnection connection = new SqlConnection(constr);
        //            connection.Open();
        //            SqlCommand command = new SqlCommand();

        //            // string strSql = "";
        //            command = new SqlCommand("ManageAssetMaster", connection);
        //            command.CommandType = CommandType.StoredProcedure;

        //            //if (assetsMaster.Flag == "I")
        //            //{ 
        //            //    strSql = "insert into [dbo].[AssetMaster](Name, Description, QRCode, AssetValue, AssetType, Manufacturer,   AssetModel, IsMoveable, AssetImage, AMCdoc, LastServiceDate, NextServiceDate, IsRentable) values(@Name, @Description, @QRCode, @AssetValue, @AssetType, @Manufacturer, @AssetModel, @IsMoveable, @AssetImage, @AMCdoc, @LastServiceDate, @NextServiceDate, @IsRentable)";
        //            //}
        //            //else if (assetsMaster.Flag == "U")
        //            //{
        //            //    strSql = "update [dbo].[AssetMaster] set Name=@Name, Description=@Description, QRCode=@QRCode, AssetValue=@AssetValue, AssetType=@AssetType, Manufacturer=@Manufacturer, AssetModel=@AssetModel, IsMoveable=@IsMoveable, AssetImage = @AssetImage, AMCdoc=@AMCdoc, LastServiceDate = @LastServiceDate, NextServiceDate=@NextServiceDate, IsRentable=@IsRentable where Id=@Id";
        //            //}
        //            //else if (assetsMaster.Flag == "D")
        //            //{
        //            //    strSql = "update [dbo].[AssetMaster] set isdeleted=1 where Id=@Id";
        //            //}


        //            //if (!string.IsNullOrEmpty(assetsMaster.ImageBytes))
        //            //{
        //            //    byte[] ImageBytes = Encoding.ASCII.GetBytes(assetsMaster.ImageBytes);
        //            //}
        //            //if (!string.IsNullOrEmpty(assetsMaster.amcDocBytes))
        //            //{
        //            //    byte[] amcDocBytes = Encoding.ASCII.GetBytes(assetsMaster.amcDocBytes);
        //            //}

        //            //byte[] Image = Encoding.ASCII.GetBytes(assetsMaster.Image);
        //            //byte[] AMCdoc = Encoding.ASCII.GetBytes(assetsMaster.AMCdoc);

        //            //byte[] imageByteArray = Convert.FromBase64String(assetsMaster.Image);
        //            //byte[] amcDocByteArray = Convert.FromBase64String(assetsMaster.AMCdoc);
        //            //string imageBase64 = assetsMaster.Image;
        //            //string amcDocBase64 = assetsMaster.AMCdoc;

        //            command.Connection = connection;
        //            //command.CommandType = CommandType.Text;
        //            //command.CommandText = strSql;
        //            command.Parameters.AddWithValue("@Id", assetsMaster.Id);
        //            command.Parameters.AddWithValue("@Name", assetsMaster.Name);
        //            command.Parameters.AddWithValue("@Description", assetsMaster.Description);
        //            command.Parameters.AddWithValue("@QRCode", assetsMaster.QRCode);
        //            command.Parameters.AddWithValue("@AssetType", assetsMaster.AssetType);
        //            command.Parameters.AddWithValue("@Manufacturer", assetsMaster.Manufacturer);
        //            command.Parameters.AddWithValue("@AssetModel", assetsMaster.AssetModel);
        //            command.Parameters.AddWithValue("@AssetValue", assetsMaster.AssetValue);
        //            command.Parameters.AddWithValue("@IsMoveable", assetsMaster.IsMoveable);
        //            SqlParameter lastServiceDateParameter = new SqlParameter("@LastServiceDate", SqlDbType.DateTime);
        //            lastServiceDateParameter.Value = Convert.ToDateTime(assetsMaster.LastServiceDate);
        //            command.Parameters.Add(lastServiceDateParameter);

        //            SqlParameter nextServiceDateParameter = new SqlParameter("@NextServiceDate", SqlDbType.DateTime);
        //            nextServiceDateParameter.Value = Convert.ToDateTime(assetsMaster.NextServiceDate);
        //            command.Parameters.Add(nextServiceDateParameter);
        //            command.Parameters.AddWithValue("@IsRentable", assetsMaster.IsRentable);
        //            command.Parameters.AddWithValue("@PropertyId", assetsMaster.PropertyId);
        //            command.Parameters.AddWithValue("@Flag", assetsMaster.Flag);

        //            if (!string.IsNullOrEmpty(assetsMaster.AssetImage))
        //            {
        //                try
        //                {
        //                    string paddedAssetImage = assetsMaster.AssetImage.Replace(" ", "+");
        //                    int mod4 = paddedAssetImage.Length % 4;
        //                    if (mod4 > 0)
        //                    {
        //                        paddedAssetImage += new string('=', 4 - mod4);
        //                    }
        //                    byte[] imageByteArray = Convert.FromBase64String(paddedAssetImage);
        //                    SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
        //                    parameter.Value = imageByteArray;
        //                    command.Parameters.Add(parameter);
        //                }
        //                catch (FormatException ex)
        //                {
        //                    SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
        //                    parameter.Value = DBNull.Value;
        //                    command.Parameters.Add(parameter);
        //                }
        //            }
        //            else
        //            {
        //                SqlParameter parameter = new SqlParameter("@AssetImage", SqlDbType.VarBinary);
        //                parameter.Value = DBNull.Value;
        //                command.Parameters.Add(parameter);
        //            }

        //            // Pass the AMCdoc value as a parameter to the command
        //            if (!string.IsNullOrEmpty(assetsMaster.AMCdoc))
        //            {
        //                try
        //                {
        //                    string paddedAMCdoc = assetsMaster.AMCdoc.Replace(" ", "+");
        //                    int mod4 = paddedAMCdoc.Length % 4;
        //                    if (mod4 > 0)
        //                    {
        //                        paddedAMCdoc += new string('=', 4 - mod4);
        //                    }
        //                    byte[] amcDocByteArray = Convert.FromBase64String(paddedAMCdoc);
        //                    SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
        //                    amcDocParameter.Value = amcDocByteArray;
        //                    command.Parameters.Add(amcDocParameter);
        //                }
        //                catch (FormatException ex)
        //                {
        //                    SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
        //                    amcDocParameter.Value = DBNull.Value;
        //                    command.Parameters.Add(amcDocParameter);
        //                }
        //            }
        //            else
        //            {
        //                SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
        //                amcDocParameter.Value = DBNull.Value;
        //                command.Parameters.Add(amcDocParameter);
        //            }


        //            //string amcDocPath = @"C:\Users\Vikas\Downloads\Images\Asset.png";                    
        //            //byte[] amcDocByteArray = File.ReadAllBytes(amcDocPath);                   
        //            //string amcDocBase64 = Convert.ToBase64String(amcDocByteArray);                    
        //            //byte[] amcDocBytes = Convert.FromBase64String(amcDocBase64);                    
        //            //SqlParameter amcDocParameter = new SqlParameter("@AMCdoc", SqlDbType.VarBinary);
        //            //amcDocParameter.Value = amcDocBytes;
        //            //command.Parameters.Add(amcDocParameter);


        //            command.ExecuteNonQuery();

        //            connection.Close();
        //            return Ok("success");
        //        }
        //    }
        //    catch (Exception ex)
        //    { return Ok(ex.Message); }
        //}


        [Route("FacilityMemberSave")]
        [HttpPost]
        public IHttpActionResult FacilityMemberSave(FromDataFacilityMemberModel model)
        {
            var id = 0;
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "select count(FacilityMemberId) from [App].[FacilityMember] where FacilityMemberId = @FacilityMemberId";
                        cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                        id = cmd.ExecuteScalar() == DBNull.Value ? 0 : Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (id > 0)
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;

                            cmd.CommandText = "update [App].[FacilityMember] set PropertyId=@PropertyId,Name=@Name,Gender=@Gender,MobileNumber=@MobileNumber,Address=@Address,FacilityMasterId=@FacilityMasterId,ProfileImageUrl=@ProfileImageUrl,IsBlocked=@IsBlocked,IsApproved=@IsApproved,ApprovedOn=getdate(),ApprovedBy=@ApprovedBy,IsActive=@IsActive,IsDeleted=@IsDeleted where FacilityMemberId = @FacilityMemberId";

                            cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                            cmd.Parameters.AddWithValue("@PropertyId", model.PropertyId);
                            cmd.Parameters.AddWithValue("@Name", model.Name);
                            cmd.Parameters.AddWithValue("@Gender", model.Gender);
                            cmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber);
                            cmd.Parameters.AddWithValue("@Address", model.Address);
                            cmd.Parameters.AddWithValue("@FacilityMasterId", model.FacilityMasterId);
                            cmd.Parameters.AddWithValue("@ProfileImageUrl", "");
                            cmd.Parameters.AddWithValue("@IsBlocked", 0);
                            cmd.Parameters.AddWithValue("@AccessCode", GenerateNewRandom());
                            cmd.Parameters.AddWithValue("@IsApproved", 1);
                            cmd.Parameters.AddWithValue("@ApprovedOn", "");
                            cmd.Parameters.AddWithValue("@ApprovedBy", "1");
                            cmd.Parameters.AddWithValue("@IsActive", "1");
                            cmd.Parameters.AddWithValue("@IsDeleted", "0");
                            cmd.Parameters.AddWithValue("@CreatedBy", "1");
                            cmd.Parameters.AddWithValue("@CreatedOn", "");
                            cmd.Parameters.AddWithValue("@UpdatedBy", "");
                            cmd.Parameters.AddWithValue("@UpdatedOn", "");

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;

                            cmd.CommandText = "insert into [App].[FacilityMember] (PropertyId,Name,Gender,MobileNumber,Address,FacilityMasterId,ProfileImageUrl,IsBlocked,AccessCode,IsApproved,ApprovedOn,ApprovedBy,IsActive,IsDeleted,CreatedBy,CreatedOn) values(@PropertyId,@Name,@Gender,@MobileNumber,@Address,@FacilityMasterId,@ProfileImageUrl,@IsBlocked,@AccessCode,1,getdate(),@ApprovedBy,1,@IsDeleted,@CreatedBy,getdate()); select SCOPE_IDENTITY()";

                            cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                            cmd.Parameters.AddWithValue("@PropertyId", model.PropertyId);
                            cmd.Parameters.AddWithValue("@Name", model.Name);
                            cmd.Parameters.AddWithValue("@Gender", model.Gender);
                            cmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber);
                            cmd.Parameters.AddWithValue("@Address", model.Address);
                            cmd.Parameters.AddWithValue("@FacilityMasterId", model.FacilityMasterId);
                            cmd.Parameters.AddWithValue("@ProfileImageUrl", "");
                            cmd.Parameters.AddWithValue("@IsBlocked", 0);
                            cmd.Parameters.AddWithValue("@AccessCode", GenerateNewRandom());
                            cmd.Parameters.AddWithValue("@IsApproved", 1);
                            cmd.Parameters.AddWithValue("@ApprovedOn", "");
                            cmd.Parameters.AddWithValue("@ApprovedBy", "1");
                            cmd.Parameters.AddWithValue("@IsActive", "1");
                            cmd.Parameters.AddWithValue("@IsDeleted", "0");
                            cmd.Parameters.AddWithValue("@CreatedBy", "1");
                            cmd.Parameters.AddWithValue("@CreatedOn", "");
                            cmd.Parameters.AddWithValue("@UpdatedBy", "");
                            cmd.Parameters.AddWithValue("@UpdatedOn", "");

                            cmd.ExecuteNonQuery();
                        }
                    }

                    //Uploading Profile Image ...
                    string serverPath = HttpContext.Current.Server.MapPath("~");
                    //serverPath = "C:\\inetpub\\wwwroot\\admin-uat-api\\wwwroot";
                    //serverPath = "D:\\GSV\\Ufirm\\Dashboard-API-New\\UFirm\\Ufirm.Service\\Ufirm.Service.Common\\wwwroot";
                    var imagePath = Path.Combine(serverPath, "FacilityMemberImages", model.ImageFileName);
                    using (var writer = new BinaryWriter(File.OpenWrite(imagePath)))
                    {
                        writer.Write(model.ImageFile);
                    }
                    //End Uploading Profile Image ...

                    if (model.Document != null)
                    {
                        if (model.Document.Length > 0)
                        {
                            using (SqlCommand cmd = new SqlCommand())
                            {
                                cmd.Connection = con;
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = "delete from [App].[FacilityMemberDocument] where FacilityMemberId=@FacilityMemberId";
                                cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                                cmd.ExecuteNonQuery();
                            }

                            List<DocumentModelNew> documentList = model.Document != null ? GetDocumentModel(model.Document) : new List<DocumentModelNew>();
                            foreach (var item in documentList)
                            {
                                var file = item.DocumentURL;
                                string imageName = item.DocumentName;
                                imagePath = Path.Combine(serverPath, "FacilityMemberDocuments", imageName);

                                using (var writer = new BinaryWriter(File.OpenWrite(imagePath)))
                                {
                                    writer.Write(file);
                                }

                                using (SqlCommand cmd = new SqlCommand())
                                {
                                    cmd.Connection = con;
                                    cmd.CommandType = CommandType.Text;
                                    cmd.CommandText = "Insert into [App].[FacilityMemberDocument] (FacilityMemberId,DocumentTypeId,DocumentName,DocumentUrl,CreatedBy,CreatedOn,IsDeleted) values(@FacilityMemberId,@DocumentTypeId,@DocumentName,@DocumentUrl,@CreatedBy,@CreatedOn,@IsDeleted)";
                                    cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", Convert.ToInt32(item.DocumentTypeId));
                                    cmd.Parameters.AddWithValue("@DocumentName", item.DocumentName);
                                    cmd.Parameters.AddWithValue("@DocumentUrl", imageName);
                                    cmd.Parameters.AddWithValue("@CreatedBy", "1");
                                    cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@IsDeleted", false);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    con.Close();
                }

                return Ok("success");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("FacilityMemberKYCUpload")]
        [HttpPost]
        public IHttpActionResult FacilityMemberKYCSave(FromDataFacilityMemberModel model)
        {
            var id = 0;
            string err = "";
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    List<DocumentModelNew> documents = model.Document != null ? GetDocumentModel(model.Document) : new List<DocumentModelNew>();

                    //Setting Server to Upload Documents ...
                    string serverPath = HttpContext.Current.Server.MapPath("~");
                    serverPath = HttpContext.Current.Server.MapPath("").Substring(0, HttpContext.Current.Server.MapPath("").LastIndexOf("\\"));
                    //End Setting Server to Upload Documents ...

                    if (model.Document != null)
                    {
                        if (model.Document.Length > 0)
                        {
                            //using (SqlCommand cmd = new SqlCommand())
                            //{
                            //    cmd.Connection = con;
                            //    cmd.CommandType = CommandType.Text;
                            //    cmd.CommandText = "delete from [App].[FacilityMemberDocument] where FacilityMemberId=@FacilityMemberId";
                            //    cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                            //    cmd.ExecuteNonQuery();
                            //}

                            List<DocumentModelNew> documentList = model.Document != null ? GetDocumentModel(model.Document) : new List<DocumentModelNew>();
                            foreach (var item in documentList)
                            {
                                var file = item.DocumentURL;
                                var imagePath = Path.Combine(serverPath, "admin-uat-api\\wwwroot\\FacilityMemberDocuments", item.DocumentName);

                                //serverPath = HttpContext.Current.Server.MapPath("~");
                                //imagePath = Path.Combine(serverPath, "FacilityMemberImages", item.DocumentName);

                                using (var writer = new BinaryWriter(File.OpenWrite(imagePath)))
                                {
                                    writer.Write(file);
                                }

                                using (SqlCommand cmd = new SqlCommand())
                                {
                                    cmd.Connection = con;
                                    cmd.CommandType = CommandType.Text;

                                    cmd.Parameters.AddWithValue("@FacilityMemberId", model.FacilityMemberId);
                                    cmd.Parameters.AddWithValue("@facilityMemberDocumentId", item.facilityMemberDocumentId);
                                    cmd.Parameters.AddWithValue("@DocumentTypeId", Convert.ToInt32(item.DocumentTypeId));
                                    cmd.Parameters.AddWithValue("@DocumentName", item.DocumentName);
                                    cmd.Parameters.AddWithValue("@DocumentUrl", item.DocumentName);
                                    cmd.Parameters.AddWithValue("@CreatedBy", "1");
                                    cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@IsDeleted", false);

                                    if (item.facilityMemberDocumentId > 0)
                                    {
                                        cmd.CommandText = "update [App].[FacilityMemberDocument] set FacilityMemberId=@FacilityMemberId,DocumentTypeId=@DocumentTypeId,DocumentName=@DocumentName,DocumentUrl=@DocumentUrl where FacilityMemberDocumentId = @facilityMemberDocumentId";
                                    }
                                    else
                                    {
                                        cmd.CommandText = "Insert into [App].[FacilityMemberDocument] (FacilityMemberId,DocumentTypeId,DocumentName,DocumentUrl,CreatedBy,CreatedOn,IsDeleted) values(@FacilityMemberId,@DocumentTypeId,@DocumentName,@DocumentUrl,@CreatedBy,@CreatedOn,@IsDeleted)";
                                    }

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    con.Close();
                }

                return Ok("success");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        private List<DocumentModelNew> GetDocumentModel(string str)
        {
            var objects = JArray.Parse(str);
            List<DocumentModelNew> docModelList = new List<DocumentModelNew>();
            foreach (JObject root in objects)
            {
                DocumentModelNew docModel = new DocumentModelNew();
                foreach (KeyValuePair<String, JToken> app in root)
                {
                    switch (app.Key)
                    {
                        case "documentTypeName":
                            docModel.DocumentTypeName = app.Value.ToObject<string>().ToString();
                            break;
                        case "documentTypeId":
                            docModel.DocumentTypeId = app.Value.ToObject<string>().ToString();
                            break;
                        case "documentNumber":
                            docModel.DocumentNumber = app.Value.ToObject<string>().ToString();
                            break;
                        case "documentName":
                            docModel.DocumentName = app.Value.ToObject<string>().ToString();
                            break;
                        case "documentUrl":
                            docModel.DocumentURL = app.Value.ToObject<byte[]>();
                            break;
                        case "documentFileName":
                            docModel.DocumentFileName = app.Value.ToObject<string>().ToString();
                            break;
                        case "documentExt":
                            docModel.DocumentExt = app.Value.ToObject<string>().ToString();
                            break;
                        case "facilityMemberDocumentId":
                            docModel.facilityMemberDocumentId = app.Value.ToObject<int>();
                            break;
                        default:
                            break;
                    }
                }
                docModelList.Add(docModel);
            }
            return docModelList;
        }

        private string GenerateNewRandom()
        {
            Random generator = new Random();
            String r = generator.Next(0, 1000000).ToString("D6");
            if (r.Distinct().Count() == 1)
            {
                r = GenerateNewRandom();
            }
            //int facilityMasterId = _ufirmUnitOfWork.GetRepository<FacilityMember>().GetAll().Where(o => o.AccessCode == r).Select(o => o.FacilityMasterId).FirstOrDefault();
            //if (facilityMasterId != 0)
            //{
            //    r = GenerateNewRandom();
            //}
            return r;
        }



        //select bkg.id, eve.Title, tsk.Name, mem.Name, bkg.isapprove from [dbo].[EventTasksResiBooking] bkg
        //inner join[calendar].[Event] eve on eve.EventId= bkg.eventid
        //inner join [dbo].TaskMaster tsk on tsk.Id= bkg.taskid
        //inner join [App].[PropertyMember] mem on mem.PropertyMemberId= bkg.bookedby

        [Route("EventTaskBooking")]
        [HttpGet]
        public IHttpActionResult EventTaskBooking()
        {
            var id = 0;
            string err = "";
            DataSet ds = new DataSet();
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;

                        cmd.CommandText = "select bkg.id, eve.Title EventName, tsk.Name TaskName, mem.Name BookedByName, bkg.isapprove from [dbo].[EventTasksResiBooking] bkg inner join[calendar].[Event] eve on eve.EventId= bkg.eventid inner join [dbo].TaskMaster tsk on tsk.Id= bkg.taskid inner join [App].[PropertyMember] mem on mem.PropertyMemberId= bkg.bookedby order by bkg.id";

                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(ds);
                    }
                    con.Close();
                }

                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EventTask
                {
                    Id = dataRow.Field<int>("Id"),
                    EventName = dataRow.Field<string>("EventName"),
                    TaskName = dataRow.Field<string>("TaskName"),
                    BookedByName = dataRow.Field<string>("BookedByName"),
                    IsApproved = dataRow["isapprove"] == DBNull.Value ? 0 : dataRow.Field<int>("isapprove")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("ApproveEventTaskBooking")]
        [HttpPost]
        public IHttpActionResult ApproveEventTaskBooking(int BookingId)
        {
            var id = 0;
            string err = "";
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.Text;

                        cmd.Parameters.AddWithValue("@BookingId", BookingId);

                        cmd.CommandText = "update [dbo].[EventTasksResiBooking] set isapprove = 1 where id=@BookingId";

                        cmd.ExecuteNonQuery();
                    }

                    con.Close();
                }

                return Ok("success");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("SaveEventTask")]
        [HttpPost]
        public IHttpActionResult SaveEventTask(List<EventTask> model)
        {
            var id = 0;
            string err = "";
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    foreach (var item in model)
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = con;
                            cmd.CommandType = CommandType.Text;

                            cmd.Parameters.AddWithValue("@EventId", item.EventID);
                            cmd.Parameters.AddWithValue("@TaskId", item.TaskID);
                            cmd.Parameters.AddWithValue("@CreatedBy", "1");
                            cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now);
                            cmd.Parameters.AddWithValue("@IsActive", 1);

                            if (item.Id > 0)
                            {
                                //cmd.CommandText = "update [App].[FacilityMemberDocument] set FacilityMemberId=@FacilityMemberId,DocumentTypeId=@DocumentTypeId,DocumentName=@DocumentName,DocumentUrl=@DocumentUrl where FacilityMemberDocumentId = @facilityMemberDocumentId";
                            }
                            else
                            {
                                cmd.CommandText = "insert into [dbo].EventTasks(EventId, TaskId, CreatedBy, CreatedOn, IsActive)values(@EventId, @TaskId, @CreatedBy, @CreatedOn, @IsActive)";
                            }

                            cmd.ExecuteNonQuery();
                        }
                    }

                    con.Close();
                }

                return Ok("success");
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }

        [Route("ProductList")]
        [HttpGet]
        public IHttpActionResult ProductList(int catID = 0, int subCatID = 0, string occurrence = "0", int assingedtoID = 0, DateTime? dteFr = null, DateTime? dteTo = null, string taskstatus = null)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select top 10 cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(stat.Remarks,'') Remarks, t.id taskID, t.datefrom, t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(stat.ComplQuests=stat.TotalQuests,'Compleate', 'Actionable'),'Pending')), 'Pending') as taskStatus, stat.updatedon from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id=stat.taskid where 1=1";


                command.CommandText += " order by t.id desc";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);

                //var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskTransactionModel
                //{
                //    CategoryName = dataRow.Field<string>("CategoryName"),
                //    SubCategoryName = dataRow.Field<string>("SubCategoryName"),
                //    Name = dataRow.Field<string>("Name"),
                //    Description = dataRow.Field<string>("Description"),
                //    Occurence = dataRow.Field<string>("Occurence"),
                //    Remarks = dataRow.Field<string>("Remarks"),
                //    TaskCategoryId = dataRow.Field<int>("CategoryId"),
                //    TaskSubCategoryId = dataRow.Field<int>("SubCategoryId"),
                //    TaskId = dataRow.Field<int>("taskID"),
                //    DateFrom = dataRow["datefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("datefrom"),
                //    DateTo = dataRow["dateto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("dateto"),
                //    TimeFrom = dataRow["timefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timefrom"].ToString()),
                //    TimeTo = dataRow["timeto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timeto"].ToString()),
                //    AssignedTo = dataRow.Field<string>("AassignedTo"),
                //    AssignedToId = dataRow.Field<int>("AssignTo"),
                //    QRCode = dataRow.Field<string>("QRCode"),
                //    TaskStatus = dataRow.Field<string>("taskStatus"),
                //    UpdatedOn = dataRow["updatedon"] == DBNull.Value ? "" : dataRow.Field<DateTime>("updatedon").ToString("dd-MM-yyyy")
                //})).ToList();


                var productList = (ds.Tables[0].AsEnumerable().Select(dataRow => new Product
                {

                    ProductID = dataRow.Field<int>("taskID"),
                    ProductName = dataRow.Field<string>("Name"),
                    UnitPrice = 1200,// product.UnitPrice,
                    UnitsInStock = 1000,// product.UnitsInStock,
                    QuantityPerUnit = "1000",// product.QuantityPerUnit,
                    TotalSales = 36879,
                    Discontinued = true,//rand.Next(1, 3) % 2 == 0 ? true : false,
                    UnitsOnOrder = 1000,// product.UnitsOnOrder,
                    CategoryID = dataRow.Field<int>("CategoryId"),//product.CategoryID,
                    Country = new CountryViewModel() { CountryNameLong = "", CountryNameShort = "" },//countries[rand.Next(0, 7)],
                    CustomerRating = 2,//rand.Next(0, 6),
                    TargetSales = 3156,//rand.Next(7, 101),
                    CountryID = 0,
                    Category = new CategoryViewModel()
                    {
                        CategoryID = dataRow.Field<int>("CategoryId"),
                        CategoryName = dataRow.Field<string>("CategoryName")
                    },

                    LastSupply = Convert.ToDateTime("2023-01-12")//dataRow["updatedon"] == DBNull.Value ? Convert.ToDateTime("1900-01-01") : dataRow.Field<DateTime>("updatedon")
                })).ToList();

                return Ok(productList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }


        [Route("SpotVisitDetails")]
        [HttpGet]
        public IHttpActionResult SpotVisitDetails(int guardId, DateTime? visitDate = null)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                command.CommandText = "select visit.Id, EmployeeName, MobileNo, createdon as VisitDate, Cast(createdon as date) VisitTime, Lat Latitude, Longi Longitude from SpotVisitDetails visit inner join GuardMaster guard on visit.createdby=guard.id where guard.Id = @guardId";

                command.Parameters.AddWithValue("@guardId", guardId);

                if (visitDate != null)
                {
                    command.CommandText += " and cast(createdon as date) = cast(@visitdate as date)";
                    command.Parameters.AddWithValue("@visitdate", visitDate);
                }

                command.CommandText += " order by visit.Id";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);

                var spotVisitDetails = (ds.Tables[0].AsEnumerable().Select(dataRow => new SpotVisitDetail
                {
                    Id = dataRow.Field<int>("Id"),
                    MobileNo = dataRow.Field<string>("MobileNo"),
                    Latitude = dataRow.Field<decimal>("Latitude"),
                    Longitude = dataRow.Field<decimal>("Longitude"),
                    VisitDate = dataRow.Field<DateTime>("VisitDate")
                })).ToList();

                return Ok(spotVisitDetails);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("AssetTracking")]
        [HttpGet]
        public IHttpActionResult AssetTracking(int assetId = 0, DateTime? dteFr = null, DateTime? dteTo = null, string taskstatus = null)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;

                //command.CommandText = " select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(stat.Remarks,'') Remarks, t.id taskID, ";


                //if (dteFr == null)
                //{
                //    command.CommandText += " t.datefrom DateFrom, ";
                //}
                //else
                //{
                //    command.CommandText += " iif(t.occurence='D', '" + Convert.ToDateTime(dteFr).ToString("MM/dd/yyyy") + "', t.datefrom) DateFrom, ";
                //}

                //command.CommandText += " t.dateto, t.timefrom, t.timeto, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(stat.ComplQuests=stat.TotalQuests,'Compleate', 'Actionable'),'Pending')), 'Pending') as taskStatus, stat.updatedon, assets.Name AssetName, assets.id AssetId from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id=stat.taskid inner join AssetMaster assets on t.AssetsId=assets.Id where 1=1 and assets.id = " + assetId;

                //if (taskstatus != null)
                //{
                //    if (taskstatus.ToUpper() == "PENDING")
                //    {
                //        //command.Parameters.AddWithValue("@AssignTo", assingedtoID);
                //        command.CommandText += " and stat.ComplQuests=0";
                //    }
                //    if (taskstatus.ToUpper() == "COMPLETE")
                //    {
                //        command.CommandText += " and (stat.TotalQuests>0 and stat.ComplQuests=stat.TotalQuests)";
                //    }
                //    if (taskstatus.ToUpper() == "ACTIONABLE")
                //    {
                //        command.CommandText += " and (stat.ComplQuests<stat.TotalQuests and stat.ComplQuests>0)";
                //    }
                //}


                //command.CommandText = " select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, isnull(trans.Remarks,'') Remarks, t.id taskID,  t.datefrom DateFrom,  t.dateto, trans.UpdatedOn TimeFrom, t.timeto, datediff(minute, cast(t.timefrom as Time), cast(t.timeto as Time)) as Duration, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(stat.ComplQuests=stat.TotalQuests,'Compleate', 'Actionable'),'Pending')), 'Pending') as taskStatus, stat.updatedon, assets.Name AssetName, assets.id AssetId from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id=stat.taskid inner join TaskWiseTransaction trans on trans.TaskName=t.Name inner join AssetMaster assets on t.AssetsId=assets.Id where 1=1 and assets.id = " + assetId; // + " order by t.id desc";

                command.CommandText = "select cm.CategoryName CategoryName, sm.SubCategoryName SubCategoryName, t.Name Name, isnull(t.Description,'')  Description, (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')') Occurence, '' Remarks, t.id taskID,  cast(trans.createdon as date) DateFrom,  cast(trans.UpdatedOn as date) dateto, left(cast(trans.createdon as time),5) TimeFrom, left(cast(trans.UpdatedOn as time),5) timeto, datediff(minute, cast(trans.createdon as DateTime), cast(trans.UpdatedOn as DateTime)) as Duration, tm.modify, fm.name AassignedTo, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(stat.ComplQuests=stat.TotalQuests,'Compleate', 'Actionable'),'Pending')), 'Pending') as taskStatus, stat.updatedon, assets.Name AssetName, assets.id AssetId from TaskMaster t left join [calendar].Category cm on t.CategoryId=cm.ScheduleCategoryId left join [calendar].[SubCategory] sm on t.SubCategoryId=sm.SubCategoryId left outer join (select distinct tm.id taskid, 1 as modify from TaskWiseTransaction t inner join taskmaster tm on t.TaskName=tm.Name) tm on t.id=tm.taskid left join app.facilitymember fm on t.assignto = fm.FacilityMemberId left join dbo.taskstatus stat on t.id=stat.taskid inner join TaskWiseTransaction trans on trans.TaskName=t.Name inner join AssetMaster assets on t.AssetsId=assets.Id where 1=1 and assets.id = " + assetId;

                command.CommandText += " group by cm.CategoryName, sm.SubCategoryName, t.Name, isnull(t.Description,'') , (trim(t.Occurence) + ' (' + cast(t.TimeFrom as varchar(8)) + ' - ' + cast(t.TimeTo as varchar(8)) + ')'), t.id,  cast(trans.createdon as date),  cast(trans.UpdatedOn as date), left(cast(trans.createdon as time),5), left(cast(trans.UpdatedOn as time),5), tm.modify, datediff(minute, cast(trans.createdon as DateTime), cast(trans.UpdatedOn as DateTime)), fm.name, t.CategoryId, t.SubCategoryId, t.AssignTo, t.qrcode, iif(stat.TotalQuests>0,iif((stat.TotalQuests-stat.ComplQuests)=0,'Compleate', iif(stat.ComplQuests>0,iif(stat.ComplQuests=stat.TotalQuests,'Compleate', 'Actionable'),'Pending')), 'Pending'), stat.updatedon, assets.Name, assets.id";

                command.CommandText += " order by t.id desc";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);

                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskTransactionModel
                {
                    Duration = dataRow.Field<int>("Duration"),
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    SubCategoryName = dataRow.Field<string>("SubCategoryName"),
                    Name = dataRow.Field<string>("Name"),
                    Description = dataRow.Field<string>("Description"),
                    Occurence = dataRow.Field<string>("Occurence"),
                    Remarks = dataRow.Field<string>("Remarks"),
                    TaskCategoryId = dataRow.Field<int>("CategoryId"),
                    TaskSubCategoryId = dataRow.Field<int>("SubCategoryId"),
                    TaskId = dataRow.Field<int>("taskID"),
                    DateFrom = dataRow["datefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("datefrom"),
                    DateTo = dataRow["dateto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : dataRow.Field<DateTime>("dateto"),
                    TimeFrom = dataRow["timefrom"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timefrom"].ToString()),
                    TimeTo = dataRow["timeto"] == DBNull.Value ? Convert.ToDateTime("1900/01/01") : Convert.ToDateTime(dataRow["timeto"].ToString()),
                    AssignedTo = dataRow.Field<string>("AassignedTo"),
                    AssignedToId = dataRow.Field<int>("AssignTo"),
                    QRCode = dataRow.Field<string>("QRCode"),
                    TaskStatus = dataRow.Field<string>("taskStatus"),
                    UpdatedOn = dataRow["updatedon"] == DBNull.Value ? "" : dataRow.Field<DateTime>("updatedon").ToString("dd-MM-yyyy"),
                    AssetName = dataRow.Field<string>("AssetName"),
                    AssetId = dataRow.Field<int>("AssetId")
                })).ToList();

                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            var data = new TaskTransactionModel()
            {
                CategoryName = "",
                SubCategoryName = "",
                Name = "",
                Description = "",
                Occurence = "",
                Remarks = "",
                TaskCategoryId = 0,
                TaskSubCategoryId = 0,
                TaskId = 0,
                DateFrom = Convert.ToDateTime("1900/01/01"),
                DateTo = Convert.ToDateTime("1900/01/01"),
                TimeFrom = Convert.ToDateTime("1900/01/01"),
                TimeTo = Convert.ToDateTime("1900/01/01"),
                AssignedTo = "",
                AssignedToId = 0,
                QRCode = "",
                TaskStatus = "",
                AssetName = "",
                AssetId = 0
            };
            List<TaskTransactionModel> eListB = new List<TaskTransactionModel>();
            eListB.Add(data);
            return Ok(eListB);
        }

        [Route("EmployeeDesignationCount")]
        [HttpGet]
        public IHttpActionResult EmployeeDesignationCount(int propId = 0)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select count(distinct fm.facilitymemberid) Count, fmas.FacilityName as Designation from app.facilitymember fm inner join [App].[FacilityMaster] fmas on fmas.FacilityMasterId=fm.FacilityMasterId where 1=1 and fm.IsActive=1";

                if (propId != 0)
                {
                    command.CommandText += " and fm.PropertyId = " + propId;
                }

                command.CommandText += " group by fmas.FacilityName";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EmployeeDesignationCountModel
                {
                    Designation = dataRow.Field<string>("Designation"),
                    Count = dataRow.Field<int>("Count"),
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("EmployeeAttendanceSummaryCount")]
        [HttpGet]
        public IHttpActionResult EmployeeAttendanceSummaryCount()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT Leave, COUNT(*) AS Count FROM EmployeeAttendanceSummary GROUP BY Leave";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EmployeeAttendanceSummaryCountModel
                {
                    Leave = dataRow.Field<string>("Leave"),
                    Count = dataRow.Field<int>("Count"),
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllEmployeeWiseTaskSummary")]
        [HttpGet]
        public IHttpActionResult GetAllEmployeeWiseTaskSummary(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Employeetasksummary";
                command.Parameters.AddWithValue("@propId", propId);
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EmployeeWiseTaskSummaryModel
                {
                    Id = dataRow.Field<int>("Id"),
                    EmployeeName = dataRow.Field<string>("EmployeeName"),
                    Designation = dataRow.Field<string>("Designation"),
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    OverdueTasks = dataRow.Field<int>("OverdueTasks"),
                    CompletionPercentage = dataRow.Field<decimal>("CompletionPercentage"),
                    Attendance = dataRow.Field<string>("Attendance"),
                    CreatedOn = dataRow.Field<DateTime>("CreatedOn"),
                    AssignTo = dataRow.Field<int>("AssignTo"),
                    ActionItem = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllEmployeeWiseTaskSummaryChartData")]
        [HttpGet]
        public IHttpActionResult GetAllEmployeeWiseTaskSummaryChartData(int PropId, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Employeetasksummarychart";
                command.Parameters.AddWithValue("@PropId", PropId);
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EmployeeWiseTaskSummaryChartModel
                {
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    CompletionPercentage = dataRow.Field<double>("CompletionPercentage"),
                    CreatedOn = dataRow.Field<DateTime>("CreatedOn"),
                    ActionItem = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("CreateEmployeeAttendanceSummary")]
        [HttpPost]
        public IHttpActionResult CreateEmployeeAttendanceSummary(EmployeeAttendanceSummaryModel oEmployee)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();
            string res = "";
            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "INSERT INTO EmployeeAttendanceSummary(FacilityMemberId, Date, Leave, CreatedOn) VALUES (@FacilityMemberId, @Date, @Leave, @CreatedOn); SELECT SCOPE_IDENTITY();";
                command.Parameters.AddWithValue("@FacilityMemberId", oEmployee.FacilityMemberId);
                command.Parameters.AddWithValue("@Date", oEmployee.Date);
                command.Parameters.AddWithValue("@Leave", oEmployee.Leave);
                command.Parameters.AddWithValue("@CreatedOn", oEmployee.CreatedOn);
                var resp = command.ExecuteScalar();

                if (Convert.ToInt32(resp) <= 0)
                {
                    res = "Something went wrong!";
                }
                else
                {
                    res = "Data inserted successfully!";
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok(res);
        }

        [Route("GetAllEmployeeAttendanceSummary")]
        [HttpGet]
        public IHttpActionResult GetAllEmployeeAttendanceSummary(DateTime date)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT a.Id, a.FacilityMemberId, b.Name as FacilityMemberName, a.Date, a.Leave, a.CreatedOn FROM EmployeeAttendanceSummary a LEFT JOIN App.FacilityMember b on b.FacilityMemberId = a.FacilityMemberId WHERE cast(a.CreatedOn as DATE) = cast(@date as DATE)";
                command.Parameters.AddWithValue("@date", date);

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new EmployeeAttendanceSummaryModel
                {
                    Id = dataRow.Field<int>("Id"),
                    FacilityMemberId = dataRow.Field<int>("FacilityMemberId"),
                    FacilityMemberName = dataRow.Field<string>("FacilityMemberName"),
                    Date = dataRow.Field<DateTime>("Date"),
                    Leave = dataRow.Field<string>("Leave"),
                    CreatedOn = dataRow.Field<DateTime>("CreatedOn")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("DeleteEmployeeAttendanceSummary")]
        [HttpDelete]
        public IHttpActionResult DeleteEmployeeAttendanceSummary(int id)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();
            string res = "";
            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT COUNT(1) FROM EmployeeAttendanceSummary WHERE Id = @Id";
                command.Parameters.AddWithValue("@Id", id);
                var isExist = command.ExecuteScalar();

                if (Convert.ToBoolean(isExist))
                {
                    command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = "DELETE FROM EmployeeAttendanceSummary WHERE Id = @Id";
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                    res = "Data deleted successfully!";
                }
                else
                {
                    res = "Data does not exist";
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok(res);
        }

        [Route("GetAllCategoryWiseTasks")]
        [HttpGet]
        public IHttpActionResult GetAllCategoryWiseTasks(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select count(tm.CategoryId) Count, cm.CategoryName as Category from TaskMaster tm inner join calendar.Category cm on cm.ScheduleCategoryId = tm.CategoryId inner join dbo.taskwisedailystatusfinaldash twds on tm.Id = twds.TaskID";

                if (propId != 0)
                {
                    command.CommandText += " inner join App.FacilityMember fm on fm.FacilityMemberId = tm.AssignTo where fm.PropertyId = " + propId;
                }

                if (categoryId != 0)
                {
                    command.CommandText += " and tm.CategoryId = " + categoryId;
                }

                if (subCategoryId != 0)
                {
                    command.CommandText += " and tm.SubCategoryId = " + subCategoryId;
                }

                if (occurance.Length > 0)
                {
                    command.CommandText += $" and twds.Occurence = '{occurance}'";
                }

                if (status.Length > 0)
                {
                    command.CommandText += $" and twds.TaskStatus = '{status}'";
                }

                if (priorityId != 0)
                {
                    command.CommandText += " and twds.TaskPriorityId = " + priorityId;
                }

                if (dateFrom.Length > 0)
                {
                    command.CommandText += $" and cast(twds.updatedon as date) >= cast('{dateFrom}' as date) and cast(twds.updatedon as date) <= cast('{dateTo}' as date)";
                }

                command.CommandText += " group by cm.CategoryName";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new CategoryWiseTasksModel
                {
                    Category = dataRow.Field<string>("Category"),
                    Count = dataRow.Field<int>("Count"),
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }
            return Ok();
        }

        [Route("GetAllCategoryWiseTaskSummaryChart")]
        [HttpGet]
        public IHttpActionResult GetAllCategoryWiseTaskSummaryChart(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "CategoryWiseTasksSummary";
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.Parameters.AddWithValue("@propId", propId);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new CategoryWiseTasksSummaryModel
                {
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    OverdueTasks = dataRow.Field<int>("OverdueTasks"),
                    ActionableTasks = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllTaskWiseSummary")]
        [HttpGet]
        public IHttpActionResult GetAllTaskWiseSummary(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "Taskwisesummary";
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.Parameters.AddWithValue("@propId", propId);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseSummaryModel
                {
                    Id = dataRow.Field<int>("Id"),
                    CategoryId = dataRow.Field<int>("CategoryId"),
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    TaskName = dataRow.Field<string>("TaskName"),
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    OverdueTasks = dataRow.Field<int>("OverdueTasks"),
                    CompletionPercentage = dataRow.Field<decimal>("CompletionPercentage"),
                    ActionItem = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllTaskWiseSummaryChart")]
        [HttpGet]
        public IHttpActionResult GetAllTaskWiseSummaryChart(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "TaskWiseSummaryChart";
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.Parameters.AddWithValue("@propId", propId);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseSummaryChartModel
                {
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    CompletionPercentage = dataRow.Field<double>("CompletionPercentage"),
                    ActionItem = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("CreateTaskWiseFmStatusData")]
        [HttpPost]
        public IHttpActionResult CreateTaskWiseFmStatusData(TaskWiseFmStatusModel oData)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();
            string res = "";
            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "INSERT INTO TaskWiseFmStatus(TaskId, QuestId, Date, Remarks) VALUES (@TaskId, @QuestId, @Date, @Remarks); SELECT SCOPE_IDENTITY();";
                command.Parameters.AddWithValue("@TaskId", oData.TaskId);
                command.Parameters.AddWithValue("@QuestId", oData.QuestId);
                command.Parameters.AddWithValue("@Date", oData.Date);
                command.Parameters.AddWithValue("@Remarks", oData.Remarks);
                var resp = command.ExecuteScalar();

                if (Convert.ToInt32(resp) <= 0)
                {
                    res = "Something went wrong!";
                }
                else
                {
                    res = "Data inserted successfully!";
                }
            }
            catch (Exception ex)
            {
                res = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok(res);
        }

        [Route("GetAllTaskWiseSummaryWithCategory")]
        [HttpGet]
        public IHttpActionResult GetAllTaskWiseSummaryWithCategory(string categoryName)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from CategoryWiseTaskSummary where CategoryName = @categoryName";
                command.Parameters.AddWithValue("@categoryName", categoryName);

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseSummaryModel
                {
                    Id = dataRow.Field<int>("Id"),
                    CategoryName = dataRow.Field<string>("CategoryName"),
                    TaskName = dataRow.Field<string>("TaskName"),
                    TotalTasks = dataRow.Field<int>("TotalTasks"),
                    CompletedTasks = dataRow.Field<int>("CompletedTasks"),
                    OverdueTasks = dataRow.Field<int>("OverdueTasks"),
                    CompletionPercentage = dataRow.Field<decimal>("CompletionPercentage"),
                    ActionItem = dataRow.Field<int>("ActionItem")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllProperties")]
        [HttpGet]
        public IHttpActionResult GetAllProperties()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select * from App.PropertyMaster";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new PropertyModel
                {
                    PropertyId = dataRow.Field<int>("PropertyId"),
                    PropertyTypeId = dataRow.Field<int>("PropertyTypeId"),
                    Name = dataRow.Field<string>("Name"),
                    AddressLine1 = dataRow.Field<string>("AddressLine1"),
                    AddressLine12 = dataRow.Field<string>("AddressLine12"),
                    CityId = dataRow.Field<int>("CityId"),
                    ContactNumber = dataRow.Field<string>("ContactNumber"),
                    LanguageId = dataRow.Field<int?>("LanguageId"),
                    ProjectArea = dataRow.Field<decimal?>("ProjectArea"),
                    TotalTowers = dataRow.Field<int?>("TotalTowers"),
                    Totalunits = dataRow.Field<int?>("Totalunits"),
                    TotalCommercialUnits = dataRow.Field<int?>("TotalCommercialUnits"),
                    Landmark = dataRow.Field<string>("Landmark"),
                    Pincode = dataRow.Field<string>("Pincode"),
                    IsActive = dataRow.Field<bool>("IsActive"),
                    CreatedBy = dataRow.Field<int>("CreatedBy"),
                    CreatedOn = dataRow.Field<DateTime>("CreatedOn"),
                    UpdateOn = dataRow.Field<DateTime?>("UpdateOn"),
                    updatedby = dataRow.Field<int?>("updatedby"),
                    IsDeleted = dataRow.Field<bool?>("IsDeleted")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllFacilityMembers")]
        [HttpGet]
        public IHttpActionResult GetAllFacilityMembers()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select FacilityMemberId, PropertyId, Name, Gender, MobileNumber, Address, FacilityMasterId, AccessCode from App.FacilityMember where IsActive = 1";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new FacilityMemberModel
                {
                    FacilityMemberId = dataRow.Field<int>("FacilityMemberId"),
                    PropertyId = dataRow.Field<int>("PropertyId"),
                    Name = dataRow.Field<string>("Name"),
                    Gender = dataRow.Field<string>("Gender"),
                    MobileNumber = dataRow.Field<string>("MobileNumber"),
                    Address = dataRow.Field<string>("Address"),
                    FacilityMasterId = dataRow.Field<int>("FacilityMasterId"),
                    AccessCode = dataRow.Field<string>("AccessCode")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllTaskPriorities")]
        [HttpGet]
        public IHttpActionResult GetAllTaskPriorities()
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select Id, Name from dbo.TaskPriority";

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskPriorityModel
                {
                    Id = dataRow.Field<int>("Id"),
                    Name = dataRow.Field<string>("Name")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetUserPropertyById")]
        [HttpGet]
        public IHttpActionResult GetUserPropertyById(string email)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlCommand command = new SqlCommand();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select PropertyId from [Identity].[Users] where EmailAddress = @email";
                command.Parameters.AddWithValue("@email", email);
                var propId = command.ExecuteScalar();
                return Ok(propId);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                connection.Close();
            }

            return Ok();
        }

        [Route("Login")]
        [HttpPost]
        public IHttpActionResult Login(Signin e)
        {
            DataSet ds = new DataSet();
            using (SqlConnection con = new SqlConnection(constr))
            {
                con.Open();
                string Query = "select Name, MobileNumber, PropertyId from dbo.aoa_users where Mobile = @Mobile and convert(varchar(50), DECRYPTBYPASSPHRASE('8', [Password]))=@Password";
                SqlCommand cmd = new SqlCommand(Query, con);
                cmd.Parameters.AddWithValue("@Mobile", e.Mobile);
                cmd.Parameters.AddWithValue("@Password", e.Password);
                var mob = e.Mobile;

                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(ds);
                con.Close();
            }

            var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new AoaLoginResp
            {
                Name = dataRow.Field<string>("Name"),
                MobileNumber = dataRow.Field<string>("MobileNumber"),
                PropertyId = dataRow.Field<int>("PropertyId")
            })).ToList();

            return Ok(eList);
        }

        [Route("GetAllTaskWiseStatusFinalDash")]
        [HttpGet]
        public IHttpActionResult GetAllTaskWiseStatusFinalDash(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetModelDataDash";
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.Parameters.AddWithValue("@propId", propId);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseDailyStatusFinalDashModel
                {
                    TaskID = dataRow.Field<int>("TaskID"),
                    TaskName = dataRow.Field<string>("TaskName"),
                    QuestID = dataRow.Field<int>("QuestID"),
                    Remarks = dataRow.Field<string>("Remarks"),
                    Updatedon = dataRow.Field<DateTime>("Updatedon"),
                    TaskStatus = dataRow.Field<string>("TaskStatus"),
                    TaskPriority = dataRow.Field<string>("TaskPriority"),
                    TaskPriorityId = dataRow["TaskPriorityId"] == DBNull.Value ? 0 : dataRow.Field<int>("TaskPriorityId"),
                    CategoryId = dataRow.Field<int>("CategoryId"),
                    SubCategoryId = dataRow.Field<int>("SubCategoryId"),
                    AssignTo = dataRow.Field<int>("AssignTo"),
                    Occurence = dataRow.Field<string>("Occurence"),
                    AssignToName = dataRow.Field<string>("Name")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllTaskWiseStatusFinalCountDash")]
        [HttpGet]
        public IHttpActionResult GetAllTaskWiseStatusFinalCountDash(int propId = 0, int categoryId = 0, int subCategoryId = 0, string occurance = "", string status = "", int priorityId = 0, string dateFrom = "", string dateTo = "")
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "GetModelDataCountDash";
                command.Parameters.AddWithValue("@categoryId", categoryId);
                command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                command.Parameters.AddWithValue("@occurance", occurance);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@priorityId", priorityId);
                command.Parameters.AddWithValue("@dateFrom", dateFrom);
                command.Parameters.AddWithValue("@dateTo", dateTo);
                command.Parameters.AddWithValue("@propId", propId);
                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskWiseDailyStatusFinalCountDashModel
                {
                    TaskStatus = dataRow.Field<string>("TaskStatus"),
                    Count = dataRow.Field<int>("Count")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [HttpGet]
        [Route("GetAllTaskStatusBySubCat")]
        public IHttpActionResult GetAllTaskStatusSummary(int propId, string dateFrom, string dateTo)
        {
            var resultDict = new Dictionary<int, Dictionary<string, int>>();
            var allStatuses = new[] { "actionable", "pending", "completed" };

            using (SqlConnection conn = new SqlConnection(constr))
            {
                using (SqlCommand cmd = new SqlCommand("[GetAllTaskWiseStatusFinalCountAllSubCat]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@propId", propId);
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom);
                    cmd.Parameters.AddWithValue("@dateTo", dateTo);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int subCatId = reader.GetInt32(2);
                            string statusKey = reader.GetString(3).Trim().ToLowerInvariant();
                            int count = reader.GetInt32(4);

                            if (!resultDict.ContainsKey(subCatId))
                            {
                                resultDict[subCatId] = allStatuses.ToDictionary(s => s, s => 0);
                            }

                            if (resultDict[subCatId].ContainsKey(statusKey))
                            {
                                resultDict[subCatId][statusKey] = count;
                            }
                            else
                            {
                                // Optional: handle unexpected statuses
                                resultDict[subCatId][statusKey] = count;
                            }
                        }
                    }
                }
            }

            // Convert keys back to Title Case for output
            var finalResult = resultDict.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToDictionary(
                    inner => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inner.Key),
                    inner => inner.Value
                )
            );

            return Ok(finalResult);
        }


        [Route("GetTaskPriorityCountDash")]
        [HttpGet]
        public IHttpActionResult GetTaskPriorityCountDash(
    int propId = 0,
    int categoryId = 0,
    int subCategoryId = 0,
    string occurance = "",
    string status = "",
    int priorityId = 0,
    string dateFrom = "",
    string dateTo = "")
        {
            var result = new List<PriorityCountDashModel>();

            try
            {
                using (var connection = new SqlConnection(constr))
                using (var command = new SqlCommand("GetPriorityDataCountDash", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@categoryId", categoryId);
                    command.Parameters.AddWithValue("@subCategoryId", subCategoryId);
                    command.Parameters.AddWithValue("@occurance", occurance ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@status", status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@priorityId", priorityId);
                    command.Parameters.AddWithValue("@dateFrom", string.IsNullOrEmpty(dateFrom) ? (object)DBNull.Value : dateFrom);
                    command.Parameters.AddWithValue("@dateTo", string.IsNullOrEmpty(dateTo) ? (object)DBNull.Value : dateTo);
                    command.Parameters.AddWithValue("@propId", propId);

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        var ds = new DataSet();
                        adapter.Fill(ds);

                        if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                        {
                            result = ds.Tables[0].AsEnumerable().Select(row => new PriorityCountDashModel
                            {
                                TaskPriority = row.Field<string>("TaskPriority"),
                                Count = row.Field<int>("Count")
                            }).ToList();
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("GetTaskQuestionImage")]
        [HttpGet]
        public IHttpActionResult GetTaskQuestionImage(int TaskId, int QuestId, string UpdatedOn)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();

            try
            {
                connection.Open();
                command.CommandText =
                                         "SELECT ISNULL(TaskQuestionImageId, 0) " +
                                         "FROM TaskWiseTransaction twt " +
                                         "WHERE TaskId = @TaskId " +
                                         "AND QuestID = @QuestId " +
                                         "AND CAST(UpdatedOn AS DATE) = @UpdatedOn";

                command.Connection = connection;
                command.Parameters.AddWithValue("@TaskId", TaskId);
                command.Parameters.AddWithValue("@QuestId", QuestId);
                command.Parameters.AddWithValue("@UpdatedOn", UpdatedOn);

                var TaskQuestionImageId = Convert.ToInt32(command.ExecuteScalar());

                if (TaskQuestionImageId <= 0)
                {
                    connection.Close();
                    return BadRequest("No image exists for the specified TaskQuestionImageId");
                }
                command.CommandText = $"select Id,[Image] from TaskQuestionImage where Id={TaskQuestionImageId} order by CreatedOn desc;";


                command.ExecuteNonQuery();

                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new
                {
                    Image = dataRow.Field<string>("Image"),
                    Id = dataRow.Field<int>("Id")
                })).ToList();

                connection.Close();

                return Ok(eList.FirstOrDefault());
            }
            catch (Exception ex)
            {
                connection.Close();
                return BadRequest();
            }
            finally
            {
                connection.Close();
            }
            return Ok();
        }

        [Route("GetAllFacilityMembersbyPropID")]
        [HttpGet]
        public IHttpActionResult GetAllFacilityMembersbyPropID(int Propid = 0)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select FacilityMemberId, PropertyId, Name, Gender, MobileNumber, Address, FacilityMasterId, AccessCode from App.FacilityMember where IsActive = 1";
                if (Propid > 0)
                {
                    command.Parameters.AddWithValue("@Id", Propid);
                    command.CommandText += " and PropertyId = @Id";
                }
                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new FacilityMemberModel
                {
                    FacilityMemberId = dataRow.Field<int>("FacilityMemberId"),
                    PropertyId = dataRow.Field<int>("PropertyId"),
                    Name = dataRow.Field<string>("Name"),
                    Gender = dataRow.Field<string>("Gender"),
                    MobileNumber = dataRow.Field<string>("MobileNumber"),
                    Address = dataRow.Field<string>("Address"),
                    FacilityMasterId = dataRow.Field<int>("FacilityMasterId"),
                    AccessCode = dataRow.Field<string>("AccessCode")
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }

        [Route("GetAllTaskDetailsbyPropID")]
        [HttpGet]
        public IHttpActionResult GetAllTaskDetailsbyPropID(int Propid = 0)
        {
            SqlConnection connection = new SqlConnection(constr);
            SqlDataAdapter adapter;
            SqlCommand command = new SqlCommand();
            DataSet ds = new DataSet();
            connection.Open();

            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.Text;
                command.CommandText = "select PropertyId, Daily, Weekly, Monthly from Tasksummary";
                if (Propid > 0)
                {
                    command.Parameters.AddWithValue("@Id", Propid);
                    command.CommandText += " where PropertyId = @Id";
                }
                adapter = new SqlDataAdapter(command);
                adapter.Fill(ds);
                var eList = (ds.Tables[0].AsEnumerable().Select(dataRow => new TaskSummary
                {

                    Propid = dataRow.Field<int>("PropertyId"),
                    Daily = dataRow.Field<int>("Daily"),
                    Monthly = dataRow.Field<int>("Monthly"),
                    Weekly = dataRow.Field<int>("Weekly"),
                })).ToList();
                return Ok(eList);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
            }
            finally
            {
                ds.Dispose();
                connection.Close();
            }

            return Ok();
        }


        [Route("ManageCheckOut")]
        [HttpPost]
        public IHttpActionResult ManageCheckOut(CheckOutModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();
                    //SpareField firstSpareField = model.SpareFields[0];

                    // Determine if SpareFields should be handled
                    if (model.SpareFields[0].SpareName.Length != 0)
                    {
                        // Handle SpareFields insert
                        string insertSpareFieldQuery = @"
                        INSERT INTO AssetSpare (AssetId, AssetName, SpareName, AssigneeName, Purpose, CheckoutDateTime, OutFrom, SentTo, TentativeReturnDate, ImageOut, ApprovedBy)
                         VALUES (@AssetId, @AssetName, @SpareName, @AssigneeName, @Purpose, 
                            @CheckoutDateTime, @OutFrom, @SentTo, @TentativeReturnDate, CONVERT(varbinary(max), @ImageOut), @ApprovedBy)";

                        using (SqlCommand command = new SqlCommand(insertSpareFieldQuery, connection))
                        {
                            foreach (var spareField in model.SpareFields)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@SpareName", spareField.SpareName);
                                command.Parameters.AddWithValue("@TentativeReturnDate", spareField.TentativeReturnDate);
                                command.Parameters.AddWithValue("@AssetId", model.AssetId);
                                command.Parameters.AddWithValue("@AssetName", model.AssetName);
                                command.Parameters.AddWithValue("@AssigneeName", model.AssigneeName);
                                command.Parameters.AddWithValue("@Purpose", model.Purpose);
                                command.Parameters.AddWithValue("@CheckOutDateTime", model.CheckOutDateTime.HasValue ? (object)model.CheckOutDateTime.Value : DBNull.Value);
                                command.Parameters.AddWithValue("@OutFrom", model.OutFrom);
                                command.Parameters.AddWithValue("@SentTo", model.SentTo);
                                command.Parameters.AddWithValue("@ApprovedBy", (object)model.ApprovedBy ?? DBNull.Value);

                                if (!string.IsNullOrEmpty(model.ImageOut))
                                {
                                    command.Parameters.AddWithValue("@ImageOut", Convert.FromBase64String(model.ImageOut));
                                }
                                else
                                {
                                    command.Parameters.AddWithValue("@ImageOut", DBNull.Value);
                                }

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {

                        // Insert into AssetTransaction
                        string insertAssetTransactionQuery = @"
        INSERT INTO AssetTransaction (AssetId, AssetName, AssigneeName, Purpose, CheckOutDateTime, OutFrom, SentTo, TentativeReturnDate, ImageOut, ApprovedBy)
        VALUES (@AssetId, @AssetName, @AssigneeName, @Purpose, @CheckOutDateTime, @OutFrom, @SentTo, @TentativeReturnDate, CONVERT(varbinary(max), @ImageOut), @ApprovedBy)";

                        using (SqlCommand command = new SqlCommand(insertAssetTransactionQuery, connection))
                        {
                            command.Parameters.AddWithValue("@AssetId", model.AssetId);
                            command.Parameters.AddWithValue("@AssetName", model.AssetName);
                            command.Parameters.AddWithValue("@AssigneeName", model.AssigneeName);
                            command.Parameters.AddWithValue("@Purpose", model.Purpose);
                            command.Parameters.AddWithValue("@CheckOutDateTime", model.CheckOutDateTime.HasValue ? (object)model.CheckOutDateTime.Value : DBNull.Value);
                            command.Parameters.AddWithValue("@OutFrom", model.OutFrom);
                            command.Parameters.AddWithValue("@SentTo", model.SentTo);
                            command.Parameters.AddWithValue("@TentativeReturnDate", model.TentativeReturnDate.HasValue ? (object)model.TentativeReturnDate.Value : DBNull.Value);
                            command.Parameters.AddWithValue("@ApprovedBy", (object)model.ApprovedBy ?? DBNull.Value);

                            if (!string.IsNullOrEmpty(model.ImageOut))
                            {
                                command.Parameters.AddWithValue("@ImageOut", Convert.FromBase64String(model.ImageOut));
                            }
                            else
                            {
                                command.Parameters.AddWithValue("@ImageOut", DBNull.Value);
                            }
                            command.ExecuteNonQuery();
                        }
                    }

                    connection.Close();
                    return Ok("Record inserted successfully.");
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, "Error occurred: " + ex.Message);
            }
        }


        [Route("ManageCheckIn")]
        [HttpPut]
        public IHttpActionResult ManageCheckIn(CheckInModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();
                    //SpareField firstSpareField = model.SpareFields[0];

                    // Determine if SpareFields should be handled
                    if (model.SpareFields[0].SpareName.Length != 0)
                    {
                        // Handle SpareFields insert
                        string insertSpareFieldQuery = @"UPDATE AssetSpare SET ReturnedBy = @ReturnedBy, ReturnDate = @ReturnDateTime,
                        ImageIn = @ImageIn WHERE AssetId = @AssetId AND AssetName = @AssetName";

                        using (SqlCommand command = new SqlCommand(insertSpareFieldQuery, connection))
                        {
                            foreach (var spareField in model.SpareFields)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@SpareName", spareField.SpareName);
                                command.Parameters.AddWithValue("@ReturnDateTime", spareField.ReturnDateTime);
                                command.Parameters.AddWithValue("@AssetId", model.AssetId);
                                command.Parameters.AddWithValue("@AssetName", model.AssetName);
                                command.Parameters.AddWithValue("@ReturnedBy", model.ReturnedBy);

                                if (!string.IsNullOrEmpty(model.ImageIn))
                                {
                                    command.Parameters.AddWithValue("@ImageIn", Convert.FromBase64String(model.ImageIn));
                                }
                                else
                                {
                                    command.Parameters.AddWithValue("@ImageIn", DBNull.Value);
                                }

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {

                        // Insert into AssetTransaction
                        string insertAssetTransactionQuery = @"UPDATE at
SET ReturnedBy = @ReturnedBy, ReturnDate = @ReturnDateTime,
    ImageIn = CONVERT(varbinary(max), @ImageIn, 2)
FROM [dbo].[AssetTransaction] at
WHERE at.Id = (
    SELECT MAX(Id)
    FROM [dbo].[AssetTransaction]
    WHERE AssetId = @AssetId AND AssetName = @AssetName
)";

                        using (SqlCommand command = new SqlCommand(insertAssetTransactionQuery, connection))
                        {
                            command.Parameters.AddWithValue("@AssetId", model.AssetId);
                            command.Parameters.AddWithValue("@AssetName", model.AssetName);
                            command.Parameters.AddWithValue("@ReturnedBy", model.ReturnedBy);
                            command.Parameters.AddWithValue("@ReturnDateTime", model.ReturnDateTime.HasValue ? (object)model.ReturnDateTime.Value : DBNull.Value);

                            if (!string.IsNullOrEmpty(model.ImageIn))
                            {
                                command.Parameters.AddWithValue("@ImageIn", Convert.FromBase64String(model.ImageIn));
                            }
                            else
                            {
                                command.Parameters.AddWithValue("@ImageIn", DBNull.Value);
                            }
                            command.ExecuteNonQuery();
                        }
                    }

                    connection.Close();
                    return Ok("Record inserted successfully.");
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, "Error occurred: " + ex.Message);
            }
        }

        [Route("ManageRentOutAsset")]
        [HttpPost]
        public IHttpActionResult ManageRentOutAsset(RentalAsset model)
        {
            try
            {
                using (var connection = new SqlConnection(constr))
                {
                    connection.Open();

                    // Check if AssetId exists in AssetMaster
                    var getAssetQuery = "SELECT Id FROM AssetMaster WHERE Id = @AssetId";
                    using (var command = new SqlCommand(getAssetQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AssetId", model.AssetId);
                        var result = command.ExecuteScalar();

                        if (result == null)
                        {
                            return Content(HttpStatusCode.NotFound, $"AssetId {model.AssetId} does not exist in AssetMaster.");
                        }
                    }

                    // Ensure required fields are provided
                    if (string.IsNullOrEmpty(model.RentedTo))
                    {
                        return Content(HttpStatusCode.BadRequest, "RentedTo is a required field.");
                    }

                    //// Check if AssetId is already rented out
                    //var checkDuplicateQuery = "SELECT COUNT(*) FROM RentalAssets WHERE AssetId = @AssetId";
                    //using (var command = new SqlCommand(checkDuplicateQuery, connection))
                    //{
                    //    command.Parameters.AddWithValue("@AssetId", model.AssetId);
                    //    int count = (int)command.ExecuteScalar();
                    //    if (count > 0)
                    //    {
                    //        return Content(HttpStatusCode.Conflict, $"AssetId {model.AssetId} is already rented out.");
                    //    }
                    //}

                    // Construct the insert query
                    var insertRentalAssetsQuery = @"INSERT INTO RentalAssets(AssetId, AssetName, AssigneeName, RentedOutDate, TentativeDate, RentedTo, OutFrom, MonthlyRent, ImageOut, ApprovedBy) 
                    VALUES(@AssetId, @AssetName, @AssigneeName, @rentOutDateTime, @tentativeReturnDate, @RentedTo, @OutFrom, @rentalCharges, CONVERT(varbinary(max), @ImageOut), @ApprovedBy)";

                    using (var command = new SqlCommand(insertRentalAssetsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@AssetId", model.AssetId);
                        command.Parameters.AddWithValue("@AssetName", model.AssetName ?? string.Empty);
                        command.Parameters.AddWithValue("@AssigneeName", model.AssigneeName ?? string.Empty);
                        command.Parameters.AddWithValue("@rentOutDateTime", model.RentOutDateTime ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@tentativeReturnDate", model.TentativeReturnDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RentedTo", model.RentedTo ?? string.Empty);
                        command.Parameters.AddWithValue("@OutFrom", model.OutFrom ?? string.Empty);
                        command.Parameters.AddWithValue("@rentalCharges", model.RentalCharges);
                        command.Parameters.AddWithValue("@ApprovedBy", model.ApprovedBy ?? (object)DBNull.Value);

                        if (!string.IsNullOrEmpty(model.ImageOut))
                        {
                            byte[] imageBytes = Encoding.ASCII.GetBytes(model.ImageOut);
                            command.Parameters.AddWithValue("@ImageOut", imageBytes);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@ImageOut", DBNull.Value);
                        }

                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                    return Ok("Record inserted successfully.");
                }
            }
            catch (SqlException ex)
            {
                return Content(HttpStatusCode.InternalServerError, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, $"Error: {ex.Message}");
            }
        }

        [Route("ManageRentInAsset")]
        [HttpPut]
        public IHttpActionResult ManageRentInAsset(ReturnRentalAssetModel model)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();

                    // Check if AssetId exists in AssetMaster
                    string getAssetQuery = "SELECT Id FROM AssetMaster WHERE Id = @AssetId";
                    using (SqlCommand getAssetCommand = new SqlCommand(getAssetQuery, connection))
                    {
                        getAssetCommand.Parameters.AddWithValue("@AssetId", model.AssetId);
                        var result = getAssetCommand.ExecuteScalar();

                        if (result == null)
                        {
                            return NotFound();  // Return 404 Not Found if AssetId does not exist
                        }
                    }

                    // Ensure required fields are provided
                    if (string.IsNullOrEmpty(model.ReturnedBy))
                    {
                        return BadRequest("ReturnedBy is a required field.");  // Return 400 Bad Request if ReturnedBy is missing
                    }

                    // Check if the asset is currently rented out
                    string checkRentedOutQuery = "SELECT COUNT(*) FROM RentalAssets WHERE AssetId = @AssetId AND AssetName = @AssetName";
                    using (SqlCommand checkRentedOutCommand = new SqlCommand(checkRentedOutQuery, connection))
                    {
                        checkRentedOutCommand.Parameters.AddWithValue("@AssetId", model.AssetId);
                        checkRentedOutCommand.Parameters.AddWithValue("@AssetName", model.AssetName);
                        int count = (int)checkRentedOutCommand.ExecuteScalar();
                        if (count == 0)
                        {
                            return NotFound();  // Return 404 Not Found if AssetId and AssetName are not found in RentalAssets
                        }
                    }

                    // Construct the update query for check-in
                    string updateRentInAssetQuery = @"UPDATE ra SET ReturnedBy = @ReturnedBy,ReturnDate = @ReturnDateTime,ReturnFrom = @ReturnedFrom, 
                    ImageIn = @ImageIn  FROM [dbo].[RentalAssets] ra WHERE ra.Id=( SELECT MAX(Id) FROM [dbo].[RentalAssets] WHERE AssetId = @AssetId AND AssetName = @AssetName)";

                    using (SqlCommand updateCommand = new SqlCommand(updateRentInAssetQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@AssetId", model.AssetId);
                        updateCommand.Parameters.AddWithValue("@AssetName", model.AssetName);
                        updateCommand.Parameters.AddWithValue("@ReturnedBy", model.ReturnedBy);
                        updateCommand.Parameters.AddWithValue("@ReturnDateTime", (object)model.ReturnDateTime ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@ReturnedFrom", (object)model.ReturnedFrom ?? DBNull.Value);

                        if (!string.IsNullOrEmpty(model.ImageIn))
                        {
                            try
                            {
                                // Ensure the Base64 string is correctly padded
                                string paddedBase64String = model.ImageIn.Trim().PadRight(model.ImageIn.Length + (4 - model.ImageIn.Length % 4) % 4, '=');

                                // Convert Base64 string to byte array
                                byte[] imageBytes = Convert.FromBase64String(paddedBase64String);
                                updateCommand.Parameters.AddWithValue("@ImageIn", imageBytes);
                            }
                            catch (FormatException ex)
                            {
                                return BadRequest("Invalid Base64 string for ImageIn."); // Return 400 Bad Request if Base64 string is invalid
                            }
                        }
                        else
                        {
                            updateCommand.Parameters.AddWithValue("@ImageIn", DBNull.Value);
                        }

                        updateCommand.ExecuteNonQuery();
                    }

                    connection.Close();
                    return Ok("Asset returned in successfully.");  // Return 200 OK on successful returned
                }
            }
            catch (Exception ex)
            {
                // Log the exception details for further analysis
                return InternalServerError(ex);  // Return 500 Internal Server Error on exception
            }
        }

        [Route("NotifyActionableTasksToFM")]
        [HttpGet]
        public IHttpActionResult NotifyActionableTasksToFM()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    // Step 1: Get the difference count
                    SqlCommand command = new SqlCommand();
                    command.Connection = con;
                    command.CommandType = CommandType.Text;
                    command.CommandText = @"SELECT COUNT(TransactionID) - (SELECT TaskTransactionCount FROM CountStore) AS Difference 
                                    FROM TaskWiseTransaction";

                    int difference;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            difference = (int)reader["Difference"];
                        }
                        else
                        {
                            return NotFound(); // Return a 404 response if no rows are found
                        }
                    }

                    // Return 0 if the difference is 0
                    if (difference == 0)
                    {
                        return Ok(0); // Explicitly return 0
                    }

                    // Step 2: Fetch the latest entries if the difference is greater than 0
                    command.CommandText = @"
                SELECT * 
                FROM (
                    SELECT TOP (@Difference) * 
                    FROM TaskWiseTransaction 
                    ORDER BY TransactionID DESC
                ) AS RT 
                WHERE RT.Remarks IS NOT NULL AND RT.Remarks <> ''";

                    // Set the parameter for the difference
                    command.Parameters.AddWithValue("@Difference", difference);

                    var recentTasks = new List<TaskMaster>(); // Replace TaskMaster with your actual model class
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Map the reader to your model
                            var task = new TaskMaster
                            {
                                Id = (int)reader["TaskId"], // Adjust based on your table schema
                                Remarks = reader["Remarks"].ToString(),
                                // Add other properties as needed
                            };
                            recentTasks.Add(task);
                        }
                    }

                    // Check if no tasks were found and handle that scenario
                    if (recentTasks.Count == 0)
                    {
                        return Ok(0); // Return 0 if no tasks are found
                    }

                    // Step 3: Update the TaskTransactionCount in CountStore
                    command.CommandText = @"UPDATE CountStore 
                                    SET TaskTransactionCount = (select count(TransactionId) from TaskWiseTransaction)"; // Replace YourCondition with the appropriate condition for your update

                    // Execute the update command
                    command.Parameters.Clear(); // Clear previous parameter
                    command.ExecuteNonQuery(); // Execute the update

                    return Ok(recentTasks); // Return the list of recent tasks as part of the HTTP response
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); // Return a 400 response with the exception message
            }
        }

        [Route("FMTaskNotification")]
        [HttpGet]
        public IHttpActionResult FMTaskNotification(int PropertyId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(constr))
                {
                    con.Open();

                    SqlCommand command = new SqlCommand(@"SELECT tn.TaskId, tn.QuestionId, tn.TaskName, tn.SUPdateTime, tn.SupRemark, tn.TaskDate, fm.Name, fm.PropertyId
FROM TaskNotification AS tn
LEFT JOIN app.FacilityMember AS fm ON tn.SupId = fm.FacilityMemberId
WHERE tn.CurrentStatus = 'Actionable' and PropertyId=@PropertyId order by SUPdateTime desc", con);

                  
                    command.Parameters.AddWithValue("@PropertyId", PropertyId);

                    var recentTasks = new List<TaskNotification>();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var task = new TaskNotification
                            {
                                TaskId = reader.GetInt32(0), // TaskId
                                QuestionId = reader.GetInt32(1), //  QuestionId
                                TaskName = reader.GetString(2), // TaskName
                                SUPdateTime = reader.GetDateTime(3), // SUPdateTime
                                SupRemark = reader.IsDBNull(4) ? null : reader.GetString(4),
                                TaskDate = reader.GetDateTime(5),
                                SupName = reader.IsDBNull(6) ? null : reader.GetString(6),// Nullable SupName
                                PropertyId = reader.GetInt32(7),
                                
                            };
                            recentTasks.Add(task); // Add to the list
                        }
                    }
                    // Return the recent tasks as an HTTP response
                    return Ok(recentTasks);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message); // Return a 400 response with the error message
            }
        }

        [HttpPost]
        [Route("SupResponse")]
        public IHttpActionResult SupResponse(TaskNotification newTask)
        {
            using (SqlConnection connection = new SqlConnection(constr))
            {
                try
                {
                    connection.Open();

                    string insertQuery = "INSERT INTO TaskNotification (TaskId, QuestionId, TaskName, CurrentStatus, SUPdateTime, SupRemark, SupId) " +
                                         "VALUES (@TaskId, @QuestionId, @TaskName, @CurrentStatus, @SUPdateTime, @SupRemark,@SupId);";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        // Adding parameters to the command
                        command.Parameters.AddWithValue("@TaskId", newTask.TaskId);
                        command.Parameters.AddWithValue("@QuestionId", newTask.QuestionId);
                        command.Parameters.AddWithValue("@TaskName", newTask.TaskName);
                        command.Parameters.AddWithValue("@CurrentStatus", newTask.CurrentStatus);
                        command.Parameters.AddWithValue("@SUPdateTime", (object)newTask.SUPdateTime ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SupRemark", (object)newTask.SupRemark ?? DBNull.Value);
                        command.Parameters.AddWithValue("@SupId", newTask.SupId);

                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();

                        // Return success response with the number of rows affected
                        return Ok(new { Message = "Task inserted successfully.", RowsAffected = rowsAffected });
                    }
                }
                catch (SqlException ex)
                {
                    // Return a detailed error message to the client
                    return InternalServerError(new Exception("An SQL error occurred: " + ex.Message));
                }
                catch (Exception ex)
                {
                    // Handle other types of exceptions
                    return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
                }
            }
        }

        [HttpGet]
        [Route("SUPTaskNotification")]
        public IHttpActionResult SUPTaskNotification()
        {
            using (SqlConnection connection = new SqlConnection(constr))
            {
                connection.Open();

                var query = @"
        SELECT 
            B.TaskId,
            B.QuestionId,
            B.TaskName,
            B.CurrentStatus,
            A.SUPdateTime,
            B.FMdateTime,
            A.SupRemark,
            B.FmRemark,
            B.FmId
 
        FROM 
            TaskNotification A
        INNER JOIN 
            TaskNotificationFM B 
        ON 
            A.TaskId = B.TaskId 
            AND A.QuestionId = B.QuestionId
        WHERE 
            A.SUPdateTime < B.FMdateTime 
            AND B.CurrentStatus = 'Completed'
    ";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var results = new List<TaskNotification>();

                        while (reader.Read())
                        {
                            results.Add(new TaskNotification
                            {

                                TaskId = reader["TaskId"] != DBNull.Value ? Convert.ToInt32(reader["TaskId"]) : 0,
                                QuestionId = reader["QuestionId"] != DBNull.Value ? Convert.ToInt32(reader["QuestionId"]) : 0,
                                TaskName = reader["TaskName"].ToString(),
                                CurrentStatus = reader["CurrentStatus"].ToString(),
                                SUPdateTime = reader["SUPdateTime"] != DBNull.Value ? Convert.ToDateTime(reader["SUPdateTime"]) : DateTime.MinValue,
                                FMdateTime = reader["FMdateTime"] != DBNull.Value ? Convert.ToDateTime(reader["FMdateTime"]) : DateTime.MinValue,
                                SupRemark = reader["SupRemark"].ToString(),
                                FmRemark = reader["FmRemark"].ToString(),
                                FmId = reader["FmId"] != DBNull.Value ? Convert.ToInt32(reader["FmId"]) : 0,

                            });
                        }

                        return Ok(results);
                    }
                }
            }
        }

        [Route("FMResponse")]
        [HttpPost]
        public IHttpActionResult FMResponse(TaskNotification newTask)
        {
            using (SqlConnection connection = new SqlConnection(constr))
            {
                try
                {
                    connection.Open();

                    // Insert TaskNotificationFM
                    string insertQuery = "INSERT INTO TaskNotificationFM (TaskId, QuestionId, TaskName, CurrentStatus, FmId, FMdateTime, FmRemark,TaskDate,TransactionDateTime) " +
                                         "VALUES (@TaskId, @QuestionId, @TaskName, @CurrentStatus, @FmId, @FMdateTime, @FmRemark, @TaskDate,@SUPdateTime);";

                    using (SqlCommand command = new SqlCommand(insertQuery, connection))
                    {
                        // Adding parameters to the command
                        command.Parameters.AddWithValue("@TaskId", newTask.TaskId);
                        command.Parameters.AddWithValue("@QuestionId", newTask.QuestionId);
                        command.Parameters.AddWithValue("@TaskName", newTask.TaskName);
                        command.Parameters.AddWithValue("@CurrentStatus", newTask.CurrentStatus);
                        command.Parameters.AddWithValue("@FmId", newTask.FmId);
                        command.Parameters.AddWithValue("@FMdateTime",newTask.FMdateTime);
                        command.Parameters.AddWithValue("@FmRemark", newTask.FmRemark);
                        command.Parameters.AddWithValue("@TaskDate", newTask.TaskDate);
                        command.Parameters.AddWithValue("@SUPdateTime", newTask.SUPdateTime);


                        // Execute the command
                        int rowsAffected = command.ExecuteNonQuery();

                        // Check if CurrentStatus is Completed
                        if (newTask.CurrentStatus == "Completed")
                        {
                            // Execute additional update query
                            string updateQuery = "UPDATE TaskNotification  SET CurrentStatus = @CurrentStatus WHERE SUPdateTime <= @SUPdateTime and TaskId=@TaskId and QuestionId=@QuestionId;";

                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                // Adding parameters for the update command
                                updateCommand.Parameters.AddWithValue("@CurrentStatus", newTask.CurrentStatus);
                                updateCommand.Parameters.AddWithValue("@SUPdateTime", newTask.SUPdateTime);
                                updateCommand.Parameters.AddWithValue("@TaskId", newTask.TaskId);
                                updateCommand.Parameters.AddWithValue("@QuestionId", newTask.QuestionId);


                                // Execute the update command
                                int updateRowsAffected = updateCommand.ExecuteNonQuery();
                            }
                        }

                        // Return success response with the number of rows affected
                        return Ok(new { Message = "Task inserted successfully.", RowsAffected = rowsAffected });
                    }
                }
                catch (SqlException ex)
                {
                    // Return a detailed error message to the client
                    return InternalServerError(new Exception("An SQL error occurred: " + ex.Message));
                }
                catch (Exception ex)
                {
                    // Handle other types of exceptions
                    return InternalServerError(new Exception("An unexpected error occurred: " + ex.Message));
                }
            }
        }

        [Route("FMComplaintResponse")]
        [HttpPost]
        public IHttpActionResult FMComplaintResponse(ComplaintNotificationFM newComplaint)
        {
            using (SqlConnection connection = new SqlConnection(constr))
            {
                try
                {
                    connection.Open();

                    // Insert ComplaintNotificationFM
                    string checkQuery = "SELECT COUNT(1) FROM ComplaintNotificationFM WHERE TicketId = @TicketId";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@TicketId", newComplaint.TicketId);
                        int exists = (int)checkCommand.ExecuteScalar();

                        if (exists > 0)
                        {
                            // Update existing record
                            string updateQuery = @"UPDATE ComplaintNotificationFM
                               SET LocationName=@LocationName, CurrentStatus=@CurrentStatus, 
                                   FMdateTime=@FMdateTime, FMRemark=@FMRemark, 
                                   TicketDate=@TicketDate, TransactionDate=@TransactionDate
                               WHERE TicketId=@TicketId";

                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@TicketId", newComplaint.TicketId);
                                updateCommand.Parameters.AddWithValue("@LocationName", newComplaint.LocationName ?? (object)DBNull.Value);
                                updateCommand.Parameters.AddWithValue("@CurrentStatus", newComplaint.CurrentStatus ?? (object)DBNull.Value);
                                updateCommand.Parameters.AddWithValue("@FMdateTime", newComplaint.FMdateTime);
                                updateCommand.Parameters.AddWithValue("@FMRemark", newComplaint.FMRemark ?? (object)DBNull.Value);
                                updateCommand.Parameters.AddWithValue("@TicketDate", newComplaint.TicketDate);
                                updateCommand.Parameters.AddWithValue("@TransactionDate", newComplaint.TransactionDate);

                                updateCommand.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Insert new record
                            string insertQuery = @"INSERT INTO ComplaintNotificationFM 
                                (TicketId, LocationName, CurrentStatus, FMdateTime, FMRemark, TicketDate, TransactionDate) 
                                VALUES 
                                (@TicketId, @LocationName, @CurrentStatus, @FMdateTime, @FMRemark, @TicketDate, @TransactionDate);";

                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@TicketId", newComplaint.TicketId);
                                insertCommand.Parameters.AddWithValue("@LocationName", newComplaint.LocationName ?? (object)DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@CurrentStatus", newComplaint.CurrentStatus ?? (object)DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@FMdateTime", newComplaint.FMdateTime);
                                insertCommand.Parameters.AddWithValue("@FMRemark", newComplaint.FMRemark ?? (object)DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@TicketDate", newComplaint.TicketDate);
                                insertCommand.Parameters.AddWithValue("@TransactionDate", newComplaint.TransactionDate);

                                insertCommand.ExecuteNonQuery();
                            }
                        }

                        // If CurrentStatus = Completed, update original Ticket & Task tables
                        if (newComplaint.CurrentStatus == "IN PROGRESS"
                            || newComplaint.CurrentStatus == "RESOLVED"
                            || newComplaint.CurrentStatus == "CLOSED")
                        {
                            // 1. Update TaskNotification
                            string updateTaskQuery = @"UPDATE TaskNotification
                               SET CurrentStatus = @CurrentStatus
                               WHERE TicketId = @TicketId 
                               AND TaskDate <= @TransactionDate;";

                            using (SqlCommand updateTaskCommand = new SqlCommand(updateTaskQuery, connection))
                            {
                                updateTaskCommand.Parameters.AddWithValue("@CurrentStatus", newComplaint.CurrentStatus);
                                updateTaskCommand.Parameters.AddWithValue("@TicketId", newComplaint.TicketId);
                                updateTaskCommand.Parameters.AddWithValue("@TransactionDate", newComplaint.TransactionDate);

                                updateTaskCommand.ExecuteNonQuery();
                            }

                            // 2. Map status string -> StatusTypeId
                            int statusTypeId = 1; // Default OPEN
                            switch (newComplaint.CurrentStatus.ToUpper())
                            {
                                case "IN PROGRESS":
                                    statusTypeId = 2;
                                    break;
                                case "RESOLVED":
                                    statusTypeId = 3;
                                    break;
                                case "CLOSED":
                                    statusTypeId = 4;
                                    break;
                            }

                            // 3. Update Ticket table
                            string updateTicketQuery = @"UPDATE app.Ticket
                                 SET StatusTypeId = @StatusTypeId
                                 WHERE TicketId = @TicketId;";

                            using (SqlCommand updateTicketCommand = new SqlCommand(updateTicketQuery, connection))
                            {
                                updateTicketCommand.Parameters.AddWithValue("@StatusTypeId", statusTypeId);
                                updateTicketCommand.Parameters.AddWithValue("@TicketId", newComplaint.TicketId);

                                updateTicketCommand.ExecuteNonQuery();
                            }
                        }

                        return Ok(new { Message = "Complaint FM inserted successfully." });
                    }
                }
                catch (SqlException ex)
                {
                    return InternalServerError(new Exception("SQL error: " + ex.Message));
                }
                catch (Exception ex)
                {
                    return InternalServerError(new Exception("Unexpected error: " + ex.Message));
                }
            }
        }


        [HttpGet]
        [Route("ChatNotification")]
        public IHttpActionResult ChatNotification(int taskId, int questionId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();


                    using (SqlCommand command = new SqlCommand(@"
         SELECT TaskId, QuestionId, CurrentStatus, FmRemark AS Remark, FMDateTime AS DateTime, 'FM' AS Source 
         FROM TaskNotificationFM 
         WHERE TaskId = @TaskId AND QuestionId = @QuestionId
         UNION ALL 
         SELECT TaskId, QuestionId, CurrentStatus, SupRemark AS Remark, SUPdateTime AS DateTime, 'SUP' AS Source 
         FROM TaskNotification 
         WHERE TaskId = @TaskId AND QuestionId = @QuestionId", connection))
                    {
                        command.Parameters.AddWithValue("@TaskId", taskId);
                        command.Parameters.AddWithValue("@QuestionId", questionId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<TaskNotification> taskNotifications = new List<TaskNotification>();

                            while (reader.Read())
                            {
                                TaskNotification taskNotification = new TaskNotification
                                {
                                    TaskId = Convert.ToInt32(reader["TaskId"]),
                                    QuestionId = Convert.ToInt32(reader["QuestionId"]),
                                    CurrentStatus = reader["CurrentStatus"].ToString(),
                                    Remark = reader["Remark"].ToString(),
                                    DateTime = reader["DateTime"] != DBNull.Value ? Convert.ToDateTime(reader["DateTime"]) : DateTime.MinValue,
                                    Source = reader["Source"].ToString()
                                };

                                taskNotifications.Add(taskNotification);
                            }

                            return Json(taskNotifications);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("ComplaintChatNotification")]
        public IHttpActionResult ComplaintChatNotification(int ticketId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(constr))
                {
                    connection.Open();

                    using (SqlCommand command = new SqlCommand(@"
                SELECT TicketId, CurrentStatus, FMRemark AS Remark, FMdateTime AS DateTime, 'FM' AS Source
                FROM ComplaintNotificationFM
                WHERE TicketId = @TicketId

                UNION ALL

                SELECT TicketId, CurrentStatus, SupRemark AS Remark, SUPdateTime AS DateTime, 'SUP' AS Source
                FROM TaskNotification
                WHERE TicketId = @TicketId

                ORDER BY DateTime ASC;", connection))   // 👈 Ensures chat-like chronological order
                    {
                        command.Parameters.AddWithValue("@TicketId", ticketId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<ComplaintChatNotification> notifications = new List<ComplaintChatNotification>();

                            while (reader.Read())
                            {
                                ComplaintChatNotification notification = new ComplaintChatNotification
                                {
                                    TicketId = Convert.ToInt32(reader["TicketId"]),
                                    CurrentStatus = reader["CurrentStatus"].ToString(),
                                    Remark = reader["Remark"] != DBNull.Value ? reader["Remark"].ToString() : null,
                                    DateTime = reader["DateTime"] != DBNull.Value ? Convert.ToDateTime(reader["DateTime"]) : DateTime.MinValue,
                                    Source = reader["Source"].ToString()
                                };

                                notifications.Add(notification);
                            }

                            return Json(notifications);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }


        [HttpPost]
        [Route("TaskUploader")]
        public async Task<IHttpActionResult> PostTestInputData(TestInputData testInputData)
        {
            if (testInputData == null)
            {
                return BadRequest("Test input data is null.");
            }

            // Set CreatedOn and CreatedBy if needed
            testInputData.CreatedOn = DateTime.UtcNow;

            // SQL command to insert into TestInputData and get the new ID
            var sql = @"
        DECLARE @NewId INT;

        INSERT INTO TestInputData 
        (Name, Location, DateFrom, DateTo, Remarks, Occurence, CreatedBy, CreatedOn, AssignTo, RemindMe, AssetsID, QRCode, Description) 
        VALUES 
        (@Name, @Location, @DateFrom, @DateTo, @Remarks, @Occurence, @CreatedBy, @CreatedOn, @AssignTo, @RemindMe, @AssetsID, @QRCode, @Description);

        SET @NewId = SCOPE_IDENTITY(); -- Get the last inserted ID

        SELECT @NewId;"; // Return the new ID 

            // Using ADO.NET to execute the command
            using (var connection = new SqlConnection(constr))
            {
                await connection.OpenAsync();

                // First, insert the task and get the TaskId
                int taskId;
                using (var command = new SqlCommand(sql, connection))
                {
                    // Add parameters
                    command.Parameters.Add(new SqlParameter("@Name", testInputData.Name));
                    command.Parameters.Add(new SqlParameter("@Location", testInputData.Location));
                    command.Parameters.Add(new SqlParameter("@DateFrom", testInputData.DateFrom));
                    command.Parameters.Add(new SqlParameter("@DateTo", testInputData.DateTo));
                    command.Parameters.Add(new SqlParameter("@Remarks", testInputData.Remarks));
                    command.Parameters.Add(new SqlParameter("@Occurence", testInputData.Occurence));
                    command.Parameters.Add(new SqlParameter("@CreatedBy", testInputData.CreatedBy));
                    command.Parameters.Add(new SqlParameter("@CreatedOn", testInputData.CreatedOn));
                    command.Parameters.Add(new SqlParameter("@AssignTo", testInputData.AssignTo));
                    command.Parameters.Add(new SqlParameter("@RemindMe", testInputData.RemindMe));
                    command.Parameters.Add(new SqlParameter("@AssetsID", testInputData.AssetsID));
                    command.Parameters.Add(new SqlParameter("@QRCode", testInputData.QRCode));
                    command.Parameters.Add(new SqlParameter("@Description", testInputData.Description));

                    // Execute the command and retrieve the new ID
                    taskId = (int)await command.ExecuteScalarAsync();
                }

                // Now insert the questions
                if (testInputData.Questions != null)
                {
                    foreach (var question in testInputData.Questions)
                    {
                        var insertQuestionSql = @"
                                    INSERT INTO Questions (TaskId,QuestionID, QuestionName, CreatedBy, CreatedOn) 
                                    VALUES (@TaskId,@QuestionID, @QuestionName, @CreatedBy, @CreatedOn)";

                        using (var questionCommand = new SqlCommand(insertQuestionSql, connection))
                        {
                            questionCommand.Parameters.Add(new SqlParameter("@TaskId", taskId));
                            questionCommand.Parameters.Add(new SqlParameter("@QuestionID", question.QuestionId));
                            questionCommand.Parameters.Add(new SqlParameter("@QuestionName", question.QuestionName));
                            questionCommand.Parameters.Add(new SqlParameter("@CreatedBy", question.CreatedBy));
                            questionCommand.Parameters.Add(new SqlParameter("@CreatedOn", DateTime.UtcNow));

                            await questionCommand.ExecuteNonQueryAsync();
                        }
                    }
                }
                return Ok(new { message = "Record inserted successfully.", taskId });

            }
        }

        [HttpPost]
        [Route("Uploadtask")]
        public async Task<IHttpActionResult> PostTestInputData()
        {
            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var file = httpRequest.Files[0];
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (fileExtension != ".xls" && fileExtension != ".xlsx" && fileExtension != ".csv")
            {
                return BadRequest("Please upload a valid Excel or CSV file.");
            }

            List<TestInputData> testInputDataList = new List<TestInputData>();

            // Reading the file
            using (var stream = new MemoryStream())
            {
                await file.InputStream.CopyToAsync(stream);
                stream.Position = 0;

                if (fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    // Handle Excel files
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true
                            }
                        });

                        var dataTable = result.Tables[0];
                        foreach (DataRow row in dataTable.Rows)
                        {
                            testInputDataList.Add(new TestInputData
                            {
                                Name = row["Name"].ToString(),
                                Location = row["Location"].ToString(),
                                DateFrom = DateTime.UtcNow,
                                DateTo = DateTime.UtcNow,
                                Occurence = row["Occurence"].ToString(),
                                CreatedBy = Convert.ToInt32(3),
                                CreatedOn = DateTime.UtcNow,
                                AssignTo = Convert.ToInt32(row["AssignTo"]),
                                Questions = new List<QuestionModel>()
                            });
                        }
                    }
                }
                else if (fileExtension == ".csv")
                {
                    // Handle CSV files
                    using (var reader = new StreamReader(stream))
                    using (var csv = new CsvHelper.CsvReader(reader, System.Globalization.CultureInfo.InvariantCulture))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        while (csv.Read())
                        {
                            testInputDataList.Add(new TestInputData
                            {
                                Name = csv.GetField<string>("Name"),
                                Location = csv.GetField<string>("Location"),
                                DateFrom = DateTime.Parse(csv.GetField<string>("DateFrom")),
                                DateTo = DateTime.Parse(csv.GetField<string>("DateTo")),
                                Remarks = csv.GetField<string>("Remarks"),
                                Occurence = csv.GetField<string>("Occurence"),
                                CreatedBy = csv.GetField<int>("CreatedBy"),
                                CreatedOn = DateTime.UtcNow,
                                AssignTo = csv.GetField<int>("AssignTo"),
                                Questions = new List<QuestionModel>()
                            });
                        }
                    }
                }

                // Insert Data into the Database
                using (var connection = new SqlConnection(constr))
                {
                    await connection.OpenAsync();

                    foreach (var testInputData in testInputDataList)
                    {
                        var sql = @"
DECLARE @NewId INT;
INSERT INTO TestInputData 
(Name, Location, DateFrom, DateTo, Remarks, Occurence, CreatedBy, CreatedOn, AssignTo, RemindMe, AssetsID, QRCode, Description) 
VALUES 
(@Name, @Location, @DateFrom, @DateTo, @Remarks, @Occurence, @CreatedBy, @CreatedOn, @AssignTo, @RemindMe, @AssetsID, @QRCode, @Description);
SET @NewId = SCOPE_IDENTITY();
SELECT @NewId;";

                        int taskId;
                        using (var command = new SqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Name", testInputData.Name);
                            command.Parameters.AddWithValue("@Location", testInputData.Location);
                            command.Parameters.AddWithValue("@DateFrom", testInputData.DateFrom);
                            command.Parameters.AddWithValue("@DateTo", testInputData.DateTo);
                            command.Parameters.AddWithValue("@Remarks", testInputData.Remarks ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Occurence", testInputData.Occurence);
                            command.Parameters.AddWithValue("@CreatedBy", testInputData.CreatedBy);
                            command.Parameters.AddWithValue("@CreatedOn", testInputData.CreatedOn);
                            command.Parameters.AddWithValue("@AssignTo", testInputData.AssignTo);
                            command.Parameters.AddWithValue("@RemindMe", testInputData.RemindMe ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@AssetsID", testInputData.AssetsID);
                            command.Parameters.AddWithValue("@QRCode", testInputData.QRCode ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Description", testInputData.Description ?? (object)DBNull.Value);

                            taskId = (int)await command.ExecuteScalarAsync();
                        }

                        // Insert Questions (if any)
                        if (testInputData.Questions != null)
                        {
                            foreach (var question in testInputData.Questions)
                            {
                                var insertQuestionSql = @"
        INSERT INTO Questions (TaskId, QuestionID, QuestionName, CreatedBy, CreatedOn) 
        VALUES (@TaskId, @QuestionID, @QuestionName, @CreatedBy, @CreatedOn);";

                                using (var questionCommand = new SqlCommand(insertQuestionSql, connection))
                                {
                                    questionCommand.Parameters.Add(new SqlParameter("@TaskId", taskId));
                                    questionCommand.Parameters.Add(new SqlParameter("@QuestionID", question.QuestionId));
                                    questionCommand.Parameters.Add(new SqlParameter("@QuestionName", question.QuestionName));
                                    questionCommand.Parameters.Add(new SqlParameter("@CreatedBy", question.CreatedBy));
                                    questionCommand.Parameters.Add(new SqlParameter("@CreatedOn", DateTime.UtcNow));

                                    await questionCommand.ExecuteNonQueryAsync();
                                }
                            }
                        }
                    }
                }

                return Ok(new { message = "Records inserted successfully." });
            }
        }
        [HttpGet]
        [Route("GetFacilityMemberByImage/{imageName}")]
        public IHttpActionResult GetFacilityMemberByImage(string imageName)
        {
            using (SqlConnection connection = new SqlConnection(constr))
            {
                connection.Open();

                var query = @"
           SELECT 
    fm.FacilityMemberId,
    fm.PropertyId,
    fm.Name,
    fm.Gender,
    fm.MobileNumber,
    fm.Address,
    fm.FacilityMasterId,
    fm.ProfileImageUrl,
    fm.IsBlocked,
    fm.AccessCode,
    fm.IsApproved,
    fm.ApprovedOn,
    fm.ApprovedBy,
    fm.IsActive,
    fm.IsDeleted,
    fm.CreatedBy,
    fm.CreatedOn,
    fm.UpdatedBy,
    fm.UpdatedOn,
    fm.oldID,
    fm.Password,
    p.Name AS PropertyName,
    p.Latitude,
    p.Longitude
FROM App.FacilityMember fm
LEFT JOIN App.PropertyMaster p 
    ON fm.PropertyId = p.PropertyId
LEFT JOIN App.FacilityMaster f 
    ON fm.FacilityMasterId = f.FacilityMasterId
WHERE fm.ProfileImageUrl = @imageName
  AND (fm.IsDeleted = 0 OR fm.IsDeleted IS NULL)
  AND (fm.IsActive = 1 OR fm.IsActive IS NULL)
        ";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@imageName", imageName);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string baseUrl = "http://194.238.18.39:8000/images/";
                            string profileImageName = reader["ProfileImageUrl"] != DBNull.Value
                                ? reader["ProfileImageUrl"].ToString()
                                : null;
                            string profileImageFullUrl = profileImageName != null
                                ? baseUrl + profileImageName
                                : null;

                            var member = new FacilityMemberDto
                            {
                                FacilityMemberId = reader["FacilityMemberId"] != DBNull.Value ? Convert.ToInt32(reader["FacilityMemberId"]) : 0,
                                PropertyId = reader["PropertyId"] != DBNull.Value ? Convert.ToInt32(reader["PropertyId"]) : (int?)null,
                                Name = reader["Name"]?.ToString(),
                                Gender = reader["Gender"]?.ToString(),
                                MobileNumber = reader["MobileNumber"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                FacilityMasterId = reader["FacilityMasterId"] != DBNull.Value ? Convert.ToInt32(reader["FacilityMasterId"]) : (int?)null,
                                ProfileImageUrl = profileImageFullUrl,
                                IsBlocked = reader["IsBlocked"] != DBNull.Value ? Convert.ToBoolean(reader["IsBlocked"]) : (bool?)null,
                                AccessCode = reader["AccessCode"]?.ToString(),
                                IsApproved = reader["IsApproved"] != DBNull.Value ? Convert.ToBoolean(reader["IsApproved"]) : false,
                                ApprovedOn = reader["ApprovedOn"] != DBNull.Value ? Convert.ToDateTime(reader["ApprovedOn"]) : (DateTime?)null,
                                ApprovedBy = reader["ApprovedBy"] != DBNull.Value ? Convert.ToInt32(reader["ApprovedBy"]) : (int?)null,
                                IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : (bool?)null,
                                IsDeleted = reader["IsDeleted"] != DBNull.Value ? Convert.ToBoolean(reader["IsDeleted"]) : (bool?)null,
                                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : 0,
                                CreatedOn = reader["CreatedOn"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedOn"]) : DateTime.MinValue,
                                UpdatedBy = reader["UpdatedBy"] != DBNull.Value ? Convert.ToInt32(reader["UpdatedBy"]) : (int?)null,
                                UpdatedOn = reader["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedOn"]) : (DateTime?)null,
                                OldID = reader["oldID"] != DBNull.Value ? Convert.ToInt32(reader["oldID"]) : (int?)null,
                                Password = reader["Password"]?.ToString(),
                                PropertyName = reader["PropertyName"]?.ToString(),
                                Latitude= reader["Latitude"] != DBNull.Value ? Convert.ToDouble(reader["Latitude"]) : (double?)null,
                                Longitude = reader["Longitude"] != DBNull.Value ? Convert.ToDouble(reader["Longitude"]) : (double?)null
                            };

                            return Ok(member);
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
        }

        [HttpPost]
        [Route("api/UfirmEmployee/add-Member")]
        public IHttpActionResult AddUfirmEmployeeLocation([FromBody] UfirmEmployeeLocation request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            if (request.FacilityMemberId <= 0)
            {
                return BadRequest("FacilityMemberId is required.");
            }

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                return BadRequest("MobileNumber is required.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string sql = @"
                INSERT INTO App.Ufirm_Employee_Locations
                (FacilityMemberId, MobileNumber, Latitude, Longitude, IsActive, CreatedOn, LocationName, Type)
                VALUES (@FacilityMemberId, @MobileNumber, @Latitude, @Longitude, @IsActive, @CreatedOn, @LocationName, @Type)";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FacilityMemberId", request.FacilityMemberId);
                        cmd.Parameters.AddWithValue("@MobileNumber", request.MobileNumber);
                        cmd.Parameters.AddWithValue("@Latitude", request.Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", request.Longitude);
                        cmd.Parameters.AddWithValue("@IsActive", request.IsActive);
                        cmd.Parameters.AddWithValue("@CreatedOn", DateTime.Now); // server-set timestamp
                        cmd.Parameters.AddWithValue("@LocationName", (object)request.LocationName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Type", (object)request.Type ?? DBNull.Value);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new
                            {
                                Success = true,
                                Message = "Location data inserted successfully."
                            });
                        }
                        else
                        {
                            return Content(HttpStatusCode.InternalServerError, new
                            {
                                Success = false,
                                Message = "Failed to insert location data."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error inserting location data: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/UfirmEmployee/get-Members")]
        public IHttpActionResult GetUfirmEmployeeLocations(int? propertyId = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string sql = @"
                SELECT loc.FacilityMemberId,
                       loc.MobileNumber,
                       loc.Latitude,
                       loc.Longitude,
                       loc.IsActive,
                       loc.CreatedOn,
                       loc.LocationName,
                       fm.PropertyId,
                       loc.Type
                FROM App.Ufirm_Employee_Locations loc
                INNER JOIN App.FacilityMember fm ON loc.FacilityMemberId = fm.FacilityMemberId";

                    if (propertyId.HasValue)
                    {
                        sql += " WHERE fm.PropertyId = @PropertyId";
                    }

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        if (propertyId.HasValue)
                        {
                            cmd.Parameters.AddWithValue("@PropertyId", propertyId.Value);
                        }

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var result = new List<object>();
                            while (reader.Read())
                            {
                                result.Add(new
                                {
                                    FacilityMemberId = reader["FacilityMemberId"],
                                    MobileNumber = reader["MobileNumber"],
                                    Latitude = reader["Latitude"],
                                    Longitude = reader["Longitude"],
                                    IsActive = reader["IsActive"],
                                    CreatedOn = reader["CreatedOn"],
                                    LocationName = reader["LocationName"],
                                    PropertyId = reader["PropertyId"],
                                    Type = reader["Type"] != DBNull.Value ? reader["Type"].ToString() : null
                                });
                            }

                            return Ok(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving location data: " + ex.Message));
            }
        }


        [HttpDelete]
        [Route("api/UfirmEmployee/delete-Member/{facilityMemberId}")]
        public IHttpActionResult DeleteUfirmEmployeeLocation(int facilityMemberId)
        {
            if (facilityMemberId <= 0)
            {
                return BadRequest("Invalid FacilityMemberId.");
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string sql = "DELETE FROM App.Ufirm_Employee_Locations WHERE FacilityMemberId = @FacilityMemberId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@FacilityMemberId", facilityMemberId);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            return Ok(new
                            {
                                Success = true,
                                Message = "Location data deleted successfully."
                            });
                        }
                        else
                        {
                            return Content(HttpStatusCode.NotFound, new
                            {
                                Success = false,
                                Message = "Location data not found."
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error deleting location data: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/UfirmEmployee/get-FacilityMembers")]
        public IHttpActionResult GetFacilityMembersByPropertyId(int propertyId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string sql = @"
                SELECT 
                    FacilityMemberId,
                    PropertyId,
                    Name,
                    Gender,
                    MobileNumber,
                    Address,
                    FacilityMasterId,
                    ProfileImageUrl,
                    IsBlocked,
                    AccessCode,
                    IsApproved,
                    ApprovedOn
                FROM App.FacilityMember
                WHERE PropertyId = @PropertyId";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@PropertyId", propertyId);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var members = new List<object>();
                            while (reader.Read())
                            {
                                members.Add(new
                                {
                                    FacilityMemberId = reader["FacilityMemberId"],
                                    PropertyId = reader["PropertyId"],
                                    Name = reader["Name"],
                                    Gender = reader["Gender"],
                                    MobileNumber = reader["MobileNumber"],
                                    Address = reader["Address"],
                                    FacilityMasterId = reader["FacilityMasterId"],
                                    ProfileImageUrl = reader["ProfileImageUrl"],
                                    IsBlocked = reader["IsBlocked"],
                                    AccessCode = reader["AccessCode"],
                                    IsApproved = reader["IsApproved"],
                                    ApprovedOn = reader["ApprovedOn"]
                                });
                            }

                            return Ok(members);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving facility members: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("api/get-AllPropertyNames")]
        public IHttpActionResult GetAllPropertyAndLocationNames()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    conn.Open();

                    var allNames = new List<string>();

                    // 1. Get Property Names
                    string sqlProperties = @"SELECT Name FROM app.PropertyMaster WHERE Name IS NOT NULL";
                    using (SqlCommand cmd = new SqlCommand(sqlProperties, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allNames.Add(reader["Name"].ToString());
                        }
                    }

                    // 2. Get Location Names
                    string sqlLocations = @"SELECT LocationName FROM app.Ufirm_Employee_Locations WHERE LocationName IS NOT NULL";
                    using (SqlCommand cmd = new SqlCommand(sqlLocations, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allNames.Add(reader["LocationName"].ToString());
                        }
                    }

                    return Ok(new { Names = allNames });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error retrieving names: " + ex.Message));
            }
        }
    }
}