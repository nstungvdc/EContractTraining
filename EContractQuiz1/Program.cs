using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection.PortableExecutable;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using EContractQuiz1.Models;
using System.Reflection.Metadata;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace EContractQuiz1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //1.Login - Get token
            var accessToken = await HttpHelper.LoginClientCredentials("auth-service/oauth/token");

            //2. Tạo hợp đồng
            var contractId = await HttpHelper.CreateContract("esolution-service/contracts/create-draft-from-file-raw", accessToken);

            //3.Gửi hợp đồng
            await HttpHelper.SubmitContract($"esolution-service/contracts/{contractId}/submit-contract", accessToken);

            //4. Upload và cập nhật trạng thái file hợp đồng sau khi ký số/ ký điện tử
            await HttpHelper.DigitalSign($"esolution-service/contracts/{contractId}/digital-sign");
        }
        public static class HttpHelper
        {
            public static readonly string apiBasicUri = "https://apigateway-econtract-staging.vnptit3.vn/​";
            public static async Task<HttpResponseMessage> Post(string url, object input, string token)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.BaseAddress = new Uri(HttpHelper.apiBasicUri);
                        client.DefaultRequestHeaders
                              .Accept
                              .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header
                        if (!string.IsNullOrEmpty(token))
                        {
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        }

                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                        request.Content = new StringContent(JsonConvert.SerializeObject(input),
                                                            Encoding.UTF8,
                                                            "application/json");
                        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        return await client.SendAsync(request);
                    }

                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    return default;
                }
            }
            public static async Task<string> LoginClientCredentials(string url)
            {
                var input =
                new
                {
                    grant_type = "client_credentials",
                    client_id = "test.client@econtract.vnpt.vn",
                    client_secret = "U30nrmdko76057dz5aQvV9ug0mTsqAQy"
                };
                Console.WriteLine($"1. Call - API {nameof(LoginClientCredentials)}");
                Console.WriteLine("Input: ");
                var resultApi = await HttpHelper.Post(url, input, string.Empty);
                var response = await resultApi.Content.ReadAsStringAsync();
                Console.WriteLine("Output: ");
                Console.WriteLine(response);
                return GetValueFromJsonString(response, "access_token", String.Empty);
            }
            public static async Task<string> CreateContract(string url, string token)
            {
                Console.WriteLine($"2. Call - API {nameof(CreateContract)}");
                Console.WriteLine("Output: ");
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(HttpHelper.apiBasicUri);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                ////Load the file and set the file's Content-Type header
                var fileStreamContent = new StreamContent(File.OpenRead(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent + @"\6cafaf21-dc1c-4785-971b-3dccca1d16f5.pdf"));
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                var form = new MultipartFormDataContent();
                form.Add(new StringContent("{ \"email\": \"duyenoi1204@gmail.com\", \"sdt\": \"0946839508\", \"userType\": \"CONSUMER\", \"ten\": \"bd test\", \"noiCap\": \"\", \"tenToChuc\": \"to chuc test ne\", \"mst\": \"09924523234\", \"loaiGtId\": \"1\", \"soDkdn\": \"\", \"ngayCapSoDkdn\": \"2021-12-22\", \"noiCapDkkd\": \"\" }"), "customer");
                form.Add(new StringContent("{ \"autoRenew\": \"true\", \"callbackUrl\": \"test url\", \"contractValue\": \"20000\", \"creationNote\": \"\", \"endDate\": \"2021-11-17\", \"flowTemplateId\": \"b611cb60-e9c0-477e-8a2c-b84a8b4c688d\", \"sequence\": 1, \"signFlow\": [ { \"signType\": \"DRAFT\", \"signForm\": [ \"OTP\", \"EKYC\", \"OTP_EMAIL\", \"NO_AUTHEN\", \"EKYC_EMAIL\", \"USB_TOKEN\", \"SMART_CA\" ], \"userId\": \"589ca117-01af-4565-b3db-cbdcc3f5bf84\", \"sequence\": 1, \"limitDate\": 3 }, { \"signType\": \"APPROVE\", \"signForm\": [ \"OTP\", \"EKYC\", \"OTP_EMAIL\", \"NO_AUTHEN\", \"EKYC_EMAIL\", \"USB_TOKEN\", \"SMART_CA\" ], \"departmentId\": \"\", \"userId\": \"589ca117-01af-4565-b3db-cbdcc3f5bf84\", \"sequence\": 2, \"limitDate\": 3 } ], \"signForm\": [ \"USB_TOKEN\", \"SMART_CA\" ], \"templateId\": \"606196ce5e3f61a09ef8ed55\", \"title\": \"Hợp đồng eContract 25092022\", \"validDate\": \"2021-11-17\", \"verificationType\": \"NONE\", \"fixedPosition\": false }"), "contract");
                form.Add(new StringContent("{}"), "fields");
                form.Add(fileStreamContent, name: "file", fileName: "6cafaf21-dc1c-4785-971b-3dccca1d16f5.pdf");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = form;
                request.Content.Headers.ContentType.MediaType = "multipart/form-data";

                var value = await client.SendAsync(request);
                var jsonString = await value.Content.ReadAsStringAsync();
                Console.WriteLine(jsonString);
                return GetValueFromJsonString(jsonString, "object", "contractId");
            }
            public static async Task SubmitContract(string url, string token)
            {
                Console.WriteLine($"3. Call - API {nameof(SubmitContract)}");
                var resultApi = await HttpHelper.Post(url, null, token);
                Console.WriteLine("Output: ");
                Console.WriteLine(resultApi);
                var respone = resultApi.StatusCode == System.Net.HttpStatusCode.NoContent ? " => Submit contract SUCCESS"
                    : " => Submit contract FAIL";
                Console.WriteLine(respone);
            }
            public static async Task<string> GetTokenLoginByAccount(string url)
            {
                var input =
                new
                {
                    domain = "econtract-staging.vnptit3.vn",
                    username = "trungtmq@gmail.com",
                    password = "HbE6R6N1",
                    grant_type = "password",
                    client_id = "test.client@econtract.vnpt.vn",
                    client_secret = "U30nrmdko76057dz5aQvV9ug0mTsqAQy"
                };
                Console.WriteLine($"4.1. Call - API {nameof(GetTokenLoginByAccount)}");
                Console.WriteLine("Input: ");
                var resultApi = await HttpHelper.Post(url, input, string.Empty);
                var response = await resultApi.Content.ReadAsStringAsync();
                Console.WriteLine("Output: ");
                Console.WriteLine(response);
                return GetValueFromJsonString(response, "access_token", String.Empty);
            }
            public static async Task DigitalSign(string url)
            {
                string accessTokenByAccount = await GetTokenLoginByAccount("auth-service/oauth/token");
                Console.WriteLine($"4. Call - API {nameof(DigitalSign)}");
                Console.WriteLine("Output: ");

                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(HttpHelper.apiBasicUri);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessTokenByAccount}");

                ////Load the file and set the file's Content-Type header
                var fileStreamContent = new StreamContent(File.OpenRead(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent + @"\dacochuky.pdf"));
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

                var form = new MultipartFormDataContent();
                form.Add(fileStreamContent, name: "file", fileName: "dacochuky.pdf");
                form.Add(new StringContent("{ \"SignForm\": \"OTP_EMAIL\", \"name\": \"Phan Ngọc Thơ\", \"taxCode\": \"1231231234\", \"provider\": \"Công Ty VNPT\", \"pathImg\": \"iVBORw0KGgoAAAANSUhEUgAAAZAAAAJYCAYAAABM7LCIAABym\", \"identifierCode\": \"1231231234\", \"phone\": \"0917881199\", \"email\": \"abc@gmail.com\", \"status\": \"VALID\", \"signType\": \"APPROVAL\", \"ekycInfo\": \"APPROVAL\" }"), "data");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = form;

                var value = await client.SendAsync(request);
                var jsonString = await value.Content.ReadAsStringAsync();
                Console.WriteLine(jsonString);
            }
            public static string GetValueFromJsonString(string jsonString, string parentObjectName, string targetObjectName)
            {
                JObject jObject = JObject.Parse(jsonString);

                if (!string.IsNullOrEmpty(targetObjectName))
                {
                    var itemProperties = jObject[parentObjectName].Children<JProperty>();
                    var objectHasNested = itemProperties.FirstOrDefault(n => n.Name == targetObjectName);
                    return objectHasNested?.Value != null ? objectHasNested.Value.ToString() : string.Empty;
                }

                var itemProperty = jObject[parentObjectName];
                return itemProperty != null ? itemProperty.ToString() : string.Empty;

            }
        }
    }
}
