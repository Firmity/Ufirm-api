using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace UrestComplaintWebApi
{
    public class Integrations
    {
        //private string smsUrl = ConfigurationManager.AppSettings.Keys[4].  //"https://api.textlocal.in/send/?"; //Startup.Configuration.GetSection("SMSInfo").GetSection("Url").Value.ToString();
        //private string Key = ConfigurationManager.AppSettings.Keys[5].ToString();  //"NGY0ODY4NDQ3MDRlMzU0ZjU4NDg0MTY4NTQ0NTc0NjM="; //Startup.Configuration.GetSection("SMSInfo").GetSection("Key").Value.ToString();
        //private string Sender = ConfigurationManager.AppSettings.Keys[6].ToString();  //"URSTIN";// Startup.Configuration.GetSection("SMSInfo").GetSection("Sender").Value.ToString();

        private string Url = "https://api.textlocal.in/send/?"; //Startup.Configuration.GetSection("SMSInfo").GetSection("Url").Value.ToString();
        private string Key = "NGY0ODY4NDQ3MDRlMzU0ZjU4NDg0MTY4NTQ0NTc0NjM="; //Startup.Configuration.GetSection("SMSInfo").GetSection("Key").Value.ToString();
        private string Sender = "URSTCP";
        private string Key1 = "MzU3ODQ4Nzg3OTQ1NTMzNjYyNjU2OTVhNjE2NTU1NmI=";
       
        private readonly string templateId = "1207175852624032420";

        public async Task<string> SendOTP(string ComplainBy, int Length)
        {
            string response = "Success";
            try
            {
                string otp = GenerateOTP(Length);
                string clientSMS = "Your OTP to access Urest facility management dashboard is " + otp;
                bool smsst = await SendSMS(ComplainBy, clientSMS);

                response = otp;
            }
            catch (Exception ex)
            {
                response = ex.Message;
            }
            return response;
        }

        private async Task<bool> SendSMS(string mobileNo, string message)
        {
            bool st = false;
            string finalUrl = "";

            await Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(mobileNo) && !string.IsNullOrEmpty(Url) && !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Sender))
                {
                    finalUrl = $"{Url}apikey={Key}&sender={Sender}&numbers=91{mobileNo}&message={message}";
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string res = reader.ReadToEnd();
                dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(res);
                if (stuff != null)
                {
                    if (stuff.status == "success")
                    {
                        st = true;
                    }
                }

            });

            
            return st;
        }

        private string GenerateOTP(int Length)
        {
            string[] saAllowedCharacters = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

            return GenerateRandomOTP(Length, saAllowedCharacters);
        }

        private string GenerateRandomOTP(int iOTPLength, string[] saAllowedCharacters)
        {
            string sOTP = String.Empty;
            string sTempChars = String.Empty;

            Random rand = new Random();
            for (int i = 0; i < iOTPLength; i++)
            {
                int p = rand.Next(0, saAllowedCharacters.Length);
                sTempChars = saAllowedCharacters[rand.Next(0, saAllowedCharacters.Length)];
                sOTP += sTempChars;
            }

            return sOTP;
        }
        public async Task<bool> SendTemplateSMSAsync(object payload)
        {
            bool isSuccess = false;

            try
            {
                if (payload == null || string.IsNullOrEmpty(Url))
                    throw new ArgumentException("Payload or API URL is missing.");

                // Convert payload to JSON
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                // Create HTTP request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = 15000; // 15 seconds

                using (var streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await streamWriter.WriteAsync(jsonPayload);
                    await streamWriter.FlushAsync();
                }

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string result = await reader.ReadToEndAsync();
                    dynamic responseJson = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

                    // TextLocal/Jio may return status, error, or message depending on API
                    if (responseJson != null && responseJson.status == "success")
                    {
                        isSuccess = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DLT SMS API returned error: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendTemplateSMSAsync: {ex.Message}");
            }

            return isSuccess;
        }


        public async Task<bool> SendSMSAsync(string mobileNo, string message, string sender)
        {
            bool isSuccess = false;

            try
            {
                if (string.IsNullOrEmpty(mobileNo) || string.IsNullOrEmpty(message) ||
                    string.IsNullOrEmpty(sender) || string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Key))
                {
                    throw new ArgumentException("Missing required SMS parameters.");
                }
                string apiKeyToUse = sender.Equals("VISMGT", StringComparison.OrdinalIgnoreCase)
                   ? Key1
                   : Key;

                if (string.IsNullOrEmpty(apiKeyToUse))
                {
                    throw new ArgumentException("API key not configured for the given sender.");
                }

                // ✅ URL-encode the message for safety (spaces, special chars, links)
                string encodedMessage = Uri.EscapeDataString(message);

                // ✅ Build final URL
                string finalUrl = $"{Url}apiKey={apiKeyToUse}&sender={sender}&numbers={mobileNo}&message={encodedMessage}";

                // ✅ Send the HTTP request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalUrl);
                request.Method = "GET";
                request.Timeout = 15000; // 15 seconds

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    string result = await reader.ReadToEndAsync();

                    // ✅ Deserialize JSON response safely
                    dynamic responseJson = Newtonsoft.Json.JsonConvert.DeserializeObject(result);

                    if (responseJson != null && responseJson.status == "success")
                    {
                        isSuccess = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"SMS API returned error: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SendSMSAsync: {ex.Message}");
            }

            return isSuccess;
        }

    }
}