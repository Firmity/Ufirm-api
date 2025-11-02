using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace UrestComplaintWebApi.Helpers
{
    public static class SoapClientHelper
    {
        public static async Task<string> CallSoapService(string url, string soapAction, string soapBody)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("SOAPAction", soapAction);
                request.Content = new StringContent(soapBody, Encoding.UTF8, "text/xml");

                try
                {
                    var response = await httpClient.SendAsync(request);

                    // Read content even if status code is 500
                    var content = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"SOAP Status Code: {response.StatusCode}");
                    Console.WriteLine($"SOAP Response: {content}");

                    return content;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine("HTTP Request Exception: " + ex.Message);
                    throw; // Optional: rethrow for controller logging
                }
            }
        }

        public static string ExtractSoapResult(string soapResponse, string resultNode)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(soapResponse);

                var ns = new XmlNamespaceManager(xmlDoc.NameTable);
                ns.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                ns.AddNamespace("ns", "http://tempuri.org/");

                var node = xmlDoc.SelectSingleNode($"//ns:{resultNode}", ns);
                return node?.InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error extracting SOAP result: " + ex.Message);
                return null;
            }
        }
    }
}
