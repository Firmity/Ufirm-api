using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UrestComplaintWebApi
{
    public class Integrations
    {
        // Replace TextLocal with Fast2SMS API
        private readonly string Fast2SmsUrl = "https://www.fast2sms.com/dev/bulkV2";
        private readonly string Fast2SmsApiKey = "AhtI9lxsgiB1pKHRS2r5qCMbjmdk6WDO8G4zF7fo3uvJEZYULPC3fP1ZYsN264nzlbSoM0jqxFHLypiQ"; // 🔒 Replace with your actual API key
        private readonly string DltSenderId = "URSTOP";             // 🔒 Replace with your DLT Sender ID
        private readonly string DltTemplateId = "201973";        // 🔒 Replace with your approved DLT Template ID

        // -------------------- OTP Function --------------------

        public async Task<string> SendOTP(string mobileNo, int length)
        {
            string response = "Success";
            try
            {
                string otp = GenerateOTP(length);

                // ✅ Match template variables:
                // Template: "Your OTP to {#var#} is {#var#}."
                string variableValues = $"Urest Dashboard|{otp}";

                bool smsSent = await SendDLTSMSAsync(
                    mobileNo,
                    DltTemplateId,   // Template ID
                    DltSenderId,     // Sender ID
                    variableValues   // Template variables separated by '|'
                );

                if (!smsSent)
                    response = "Failed to send SMS";
                else
                    response = otp;
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }
            return response;
        }

        // -------------------- DLT SMS Sender --------------------

        public async Task<bool> SendDLTSMSAsync(
      string mobileNo,
      string templateId,
      string senderId,
      string variableValues,
      int flash = 0)
        {
            bool isSuccess = false;

            try
            {
                if (string.IsNullOrEmpty(mobileNo) ||
                    string.IsNullOrEmpty(templateId) ||
                    string.IsNullOrEmpty(senderId))
                    throw new ArgumentException("Missing required parameters.");

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("authorization", Fast2SmsApiKey);

                    var payload = new
                    {
                        route = "dlt",
                        sender_id = senderId,
                        message = templateId,
                        variables_values = variableValues,
                        flash = flash,
                        numbers = mobileNo
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(Fast2SmsUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("📤 Fast2SMS Request: " + jsonPayload);
                    Console.WriteLine("📥 Fast2SMS Response: " + responseString);

                    dynamic resJson = JsonConvert.DeserializeObject(responseString);
                    if (resJson != null && resJson.@return == true)
                    {
                        isSuccess = true;
                        Console.WriteLine("✅ SMS sent successfully.");
                    }
                    else
                    {
                        Console.WriteLine("❌ Fast2SMS Error: " + (resJson?.message ?? "Unknown error"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Exception in SendDLTSMSAsync: {ex.Message}");
            }

            return isSuccess;
        }




        // -------------------- OTP Generator --------------------

        private string GenerateOTP(int length)
        {
            string[] digits = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            Random rand = new Random();
            string otp = "";

            for (int i = 0; i < length; i++)
            {
                otp += digits[rand.Next(0, digits.Length)];
            }

            return otp;
        }
    }
}
