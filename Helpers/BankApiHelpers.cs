using BankAccountingApi.Extensions;
using BankAccountingApi.Models;
using BankAccountingApi.Startup;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
/*
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
*/
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.Mail;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using NetSmtpClient = System.Net.Mail.SmtpClient;
using NetSmtpStatusCode = System.Net.Mail.SmtpStatusCode;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;
using MailKitSmtpStatusCode = MailKit.Net.Smtp.SmtpStatusCode;

namespace BankAccountingApi.Helpers
{
    public class WebDriverHelper
    {
        public static IWebDriver Initialize(string url, bool headless = false)
        {
            IWebDriver driver = null;
            try
            {
                string strPath = @$"{Startup.Startup.WebHostEnvironment.ContentRootPath}\msedgedriver.exe";
                EdgeDriverService service = EdgeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;
                if(headless)
                {
                    EdgeOptions options = new EdgeOptions();
                    options.AddArgument("headless");
                    driver = new EdgeDriver(service, options);
                }
                else
                {
                    driver = new EdgeDriver(service);
                }
                driver.Url = url;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
            return driver;
        }
        public static void ExecuteScript(string url, string strScript, object[] args = null)
        {
            try
            {
                IWebDriver driver = Initialize(url, true);
                driver?.ExecuteJavaScript(strScript, args);
                driver?.Quit();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
        }
        public static void WaitForScriptResult(string url, string strScript, string strResult, Action<IWebDriver> successAction, int timeout = 90)
        {
            try
            {
                IWebDriver driver = Initialize(url, true);
                if(driver != null)
                {
                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                    if(wait.Until<bool>(curdriver => string.Equals((curdriver as IJavaScriptExecutor).ExecuteScript(strScript)?.ToString(), strResult)))
                    {
                        successAction.Invoke(driver);
                    }
                    driver.Quit();
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
        }
    }
    public class EmailHelper
    {
        public enum DeliveryStatus
        {
            [Description("failed")]
            Failed,
            [Description("delayed")]
            Delayed,
            [Description("delivered")]
            Delivered,
            [Description("relayed")]
            Relayed,
            [Description("expanded")]
            Expanded
        }
        public class CustomMailKitSmtpClient : MailKitSmtpClient
        {
            protected override string GetEnvelopeId(MimeMessage message)
            {
                return message.MessageId;
            }
            protected override DeliveryStatusNotification? GetDeliveryStatusNotifications(MimeMessage message, MailboxAddress mailbox)
            {
                return DeliveryStatusNotification.Success | DeliveryStatusNotification.Failure;
            }
        }
        public class MailKitSmtpResponse : SmtpResponse
        {
            public new MailKitSmtpStatusCode StatusCode
            { 
                get => base.StatusCode;
                set
                {
                    typeof(SmtpResponse).GetProperty("StatusCode")?.GetSetMethod(true)?.Invoke(this, new object[] { value });
                }
            }
            public NetSmtpStatusCode NetStatusCode 
            {
                get => Enum.Parse<NetSmtpStatusCode>(Enum.GetName<MailKitSmtpStatusCode>(StatusCode));
            }
            public string EnhancedStatusCode { get; set; }
            public MailKitSmtpResponse(MailKitSmtpStatusCode code, string response, string? enhancedCode = null) : base(code, response)
            {
            }
            public MailKitSmtpResponse(string response) : this(default, response)
            {
                Regex regex = new Regex("(?<StatusCodeEnh>\\d\\.\\d\\.\\d)\\s+(?<StatusCodeMain>\\w+)");
                Match match = regex.Match(response);
                if(match.Success)
                {
                    if(match.Groups["StatusCodeMain"].Success)
                    {
                        string[] arrNames =
                            (from curName in Enum.GetNames<MailKitSmtpStatusCode>()
                             select curName.ToUpper()).ToArray<string>();
                        MailKitSmtpStatusCode[] arrValues = Enum.GetValues<MailKitSmtpStatusCode>();
                        int index = Array.IndexOf<string>(arrNames, match.Groups["StatusCodeMain"].Value.ToUpper());
                        if(index >= 0)
                        {
                            StatusCode = arrValues[index];
                        }
                    }
                    if(match.Groups["StatusCodeEnh"].Success)
                    {
                        EnhancedStatusCode = match.Groups["StatusCodeEnh"].Value;
                    }
                }
            }
        }
        protected static IConfiguration Configuration { get; set; }
        protected static IWebHostEnvironment Environment { get; set; }
        public EmailHelper(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }
		public static string GetConfirmEmailMessageBodyHtml(HttpContext context)
		{
			string body = null;
            string url = $"{context.Request.Scheme}://{context.Request.Host}/ConfirmEmailMessage.html";
            WebDriverHelper.WaitForScriptResult(url, "return document.readyState", "complete", (webdriver) =>
            {
                IReadOnlyList<IWebElement> bodyElements = webdriver.FindElements(By.TagName("body"));
                if(bodyElements?.Count > 0)
                {
                    body = bodyElements[0].GetAttribute("outerHTML");
                }
            }, 90);
            return body;
		}
		public static async Task<bool> SendConfirmationLinkMail(HttpContext context, RegisterViewModel model,  UserManager<BankApiUser> userManager)
        {
            bool result = false;
            try
            {
                string url = context?.Request?.GetEncodedUrl();
                BankApiUserOptions userOptions = Startup.Startup.UserOptions;
                if(!string.IsNullOrWhiteSpace(url) && !string.IsNullOrWhiteSpace(model.Email)
                    && model.EmailConfirmationState == ConfirmationState.NotStarted && userManager != null && userOptions != null)
                {
                    string strPath = Startup.Startup.WebHostEnvironment.WebRootPath + "\\ConfirmEmailMessage.html";
                    if(File.Exists(strPath))
                    {
                        BankApiUser user = await userManager.FindByEmailAsync(model.Email);
                        if(user != null)
                        {
                            model.EmailVerificationCode = await userManager.GenerateTwoFactorTokenAsync(user, Startup.Startup.TwoFactorTokenProviderName);
                            if(!string.IsNullOrWhiteSpace(model.EmailVerificationCode))
                            {
                                string strQuery = model.ToQueryString();
                                //string strQuery = $"email={WebUtility.HtmlEncode(user.Email)}&code={WebUtility.HtmlEncode(model.EmailVerificationCode)}";
                                if(!string.IsNullOrWhiteSpace(strQuery))
                                {
                                    url = $"{context.Request.Scheme}://{context.Request.Host}/UserAccount/CompleteConfirmEmail/?{strQuery}";
                                    string body = GetConfirmEmailMessageBodyHtml(context);
                                    body = body?.Replace("@username", model.UserFullName)?.Replace("@url", url);
                                    if(result = await MailKitSendMessageAsync(model.UserFullName, model.Email, body, "Test e-mail from my API"))
                                    {
                                        model.EmailConfirmationState = ConfirmationState.WaitForConfirmation;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
            return result;
        }
        public static async Task<bool> NetSendMailAsync(string to, string body, string subject = null)
        {
            bool result = false;
            NetSmtpClient smtpClient = new NetSmtpClient();
            SendCompletedEventHandler handler = delegate { };
            try
            {
                SmtpServiceOptions smtpOptions = Startup.Startup.SmtpOptions;
                smtpClient.CopyPropertiesFrom(smtpOptions);
                smtpClient.Credentials = new NetworkCredential(smtpOptions.UserName, smtpOptions.Password);
                MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(smtpOptions.From),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = body,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8,
                    BodyTransferEncoding = TransferEncoding.Base64
                };
                mailMessage.To.Add(new MailAddress(to));
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                handler = async (sender, args) =>
                {
                    smtpClient.SendCompleted -= handler;
                    await Task.Yield();
                    if(args.UserState == tcs)
                    {
                        if(args.Cancelled)
                        {
                            tcs.TrySetCanceled();
                        }
                        else if(args.Error != null)
                        {
                            tcs.TrySetException(args.Error);
                        }
                        else
                        {
                            tcs.TrySetResult(true);
                        }
                    }
                };
                smtpClient.SendCompleted += handler;
                await Task.Run(async () =>
                {
                    smtpClient.SendAsync(mailMessage, tcs);
                    await tcs.Task;
                });
                result = tcs.Task?.Result == true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
            finally
            {
                smtpClient.SendCompleted -= handler;
            }
            return result;
        }
        public static async Task<bool> MailKitSendMessageAsync(string toName, string toAddress, string body, string subject = null)
        {
            bool result = false;
            CustomMailKitSmtpClient smtpClient = new CustomMailKitSmtpClient();
            EventHandler<MessageSentEventArgs> handler = delegate { };
            try
            {
                SmtpServiceOptions smtpOptions = Startup.Startup.SmtpOptions;
                MimeMessage message = new MimeMessage()
                {
                    Subject = subject,
                    Body = new TextPart("html")
                    {
                        Text = body
                    }
                };
                message.From.Add(new MailboxAddress("BankAccountingApi", smtpOptions.From));
                message.To.Add(new MailboxAddress(toName, toAddress));
                smtpClient.CheckCertificateRevocation = false;
                smtpClient.ServerCertificateValidationCallback += (sender, cert, chain, errors) => true;
                smtpClient.MessageSent += handler;
                await smtpClient.ConnectAsync(smtpOptions.Host, smtpOptions.Port, smtpOptions.SecureSocketOptions);
                await smtpClient.AuthenticateAsync(smtpOptions.UserName, smtpOptions.Password);
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                handler = async (sender, args) =>
                {
                    smtpClient.MessageSent -= handler;
                    await Task.Yield();
                    MailKitSmtpResponse smtpResponse = new MailKitSmtpResponse(args.Response);
                    if(smtpResponse.StatusCode == MailKitSmtpStatusCode.Ok)
                    {
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        tcs.TrySetException(new SmtpException(smtpResponse.NetStatusCode, smtpResponse.Response));
                    }
                };
                smtpClient.MessageSent += handler;
                await smtpClient.SendAsync(message);
                await tcs.Task;
                result = tcs.Task?.Result == true;
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw e;
            }
            finally
            {
                smtpClient.MessageSent -= handler;
                if(smtpClient.IsConnected)
                {
                    await smtpClient.DisconnectAsync(true);
                }
            }
            return result;
        }
        public static DeliveryStatus GetMessageDeliveryStatus(MimeMessage message, out string failedRecipient)
        {
            DeliveryStatus status = DeliveryStatus.Failed;
            failedRecipient = null;
            MultipartReport report = new MultipartReport("delivery-status");
            report.Add(message?.Body);
            IEnumerable<MessageDeliveryStatus> deliveryStatuses = report.OfType<MessageDeliveryStatus>()?.Where<MessageDeliveryStatus>(mds => mds?.StatusGroups?.Count > 1);
            if(deliveryStatuses != null)
            {
                List<HeaderList> listStatusGroups = null;
                string strRecipient = null;
                DeliveryStatus action = default;
                foreach(MessageDeliveryStatus deliveryStatus in deliveryStatuses)
                {
                    listStatusGroups = deliveryStatus.StatusGroups.ToList<HeaderList>();
                    if(!string.Equals(listStatusGroups[0]?["Original-Envelope-Id"], message.MessageId))
                    {
                        status = DeliveryStatus.Failed;
                        break;
                    }
                    foreach(HeaderList headers in listStatusGroups.Where<HeaderList>(curheaders => listStatusGroups.IndexOf(curheaders) > 0
                        && curheaders.Contains("Action") && curheaders.Contains("Original-Recipient") && curheaders.Contains("Final-Recipient")))
                    {
                        strRecipient = headers["Original-Recipient"] ?? headers["Final-Recipient"];
                        if(Enum.TryParse<DeliveryStatus>(headers["Action"], out action))
                        {
                            if(action == DeliveryStatus.Failed)
                            {
                                status = DeliveryStatus.Failed;
                                failedRecipient = strRecipient;
                                break;
                            }
                            else if(action != DeliveryStatus.Delayed)
                            {
                                status = DeliveryStatus.Delayed;
                                failedRecipient = strRecipient;
                            }
                            else if(status != DeliveryStatus.Delayed)
                            {
                                status = DeliveryStatus.Delivered;
                            }
                        }
                    }
                    if(status == DeliveryStatus.Failed)
                    {
                        break;
                    }
                }
            }
            return status;
        }
    }
    public class SmsHelper
    {
        public static async Task<bool> SendVerificationCode(RegisterViewModel model)
        {
            bool result = false;
            if(!string.IsNullOrWhiteSpace(model?.PhoneNumber) && model.PhoneConfirmationState == ConfirmationState.NotStarted)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        BankApiUserOptions userOptions = Startup.Startup.UserOptions;
                        SmsServiceOptions smsOptions = Startup.Startup.SmsOptions;
                        model.PhoneVerificationCode = SecurityHelper.GenerateRandomIntLambda(userOptions.VerificationCodeLength);
                        if(!string.IsNullOrWhiteSpace(model.PhoneVerificationCode) && !string.IsNullOrWhiteSpace(smsOptions?.Url) && !string.IsNullOrWhiteSpace(smsOptions.Authorization))
                        {
                            JObject jobjRequestBody = JObject.FromObject(new
                            {
                                submits = new[]
                                {
                                    new
                                    {
                                        msid = model.PhoneNumber.ToString(),
                                        message = $"your BankApi registration verification code is {model.PhoneVerificationCode}"
                                    }
                                },
                                naming = model.UserFullName
                            });
                            if(jobjRequestBody?.HasValues == true)
                            {
                                HttpClient httpClient = new HttpClient();
                                HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, smsOptions.Url);
                                httpRequest.Headers.Add("Authorization", smsOptions.Authorization);
                                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                                httpRequest.Content = new StringContent(jobjRequestBody.ToString(), new MediaTypeHeaderValue("application/json"));
                                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                                /*
                                HttpResponseMessage httpResponse = await httpClient.SendAsync(httpRequest);
                                JObject jobjResponse = JObject.FromObject(httpResponse);
                                if(result = httpResponse?.IsSuccessStatusCode == true)
                                {
                                    model.PhoneConfirmationState = ConfirmationState.WaitForConfirmation;
                                }
                                */
                                model.PhoneConfirmationState = ConfirmationState.WaitForConfirmation;
                                result = true;
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine($"{e}");
                        throw (e);
                    }
                });
            }
            return result;
        }
    }
    public class SecurityHelper
    {
        public static string GenerateRandomString(int length)
        {
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            int count = length * 2 + 2;
            byte[] arrRandomData = RandomNumberGenerator.GetBytes(count);
            return Convert.ToBase64String(arrRandomData);
        }
        public static string GenerateRandomInt(int length)
        {
            int maxValue = (int)Math.Pow(10, length) - 1;
            return maxValue > 0 ? RandomNumberGenerator.GetInt32(maxValue).ToString($"D{length}") : null;
        }
        public static Func<int, string> GenerateRandomIntLambda = (length) =>
        {
            int maxValue = (int)Math.Pow(10, length) - 1;
            return maxValue > 0 ? RandomNumberGenerator.GetInt32(maxValue).ToString($"D{length}") : null;
        };
    }
    public class ModelHelper
    {
        public static Dictionary<string, string> GetErrorMessages(Type typeofModel)
        {
            return new Dictionary<string, string>(
                from curAttributePair in
                    (from curProperty in typeofModel?.GetProperties()
                     select
                        new KeyValuePair<string, string>(curProperty.Name,
                            (from curCustomAttr in curProperty.CustomAttributes
                             where (curCustomAttr.AttributeType == typeof(RequiredAttribute) || curCustomAttr.AttributeType == typeof(TwoFactorAuthAttribute))
                             select curCustomAttr.NamedArguments?.FirstOrDefault<CustomAttributeNamedArgument>(narg => string.Equals(narg.MemberName, "ErrorMessage"))
                                .TypedValue.Value as string).FirstOrDefault()))
                where !string.IsNullOrWhiteSpace(curAttributePair.Value as string)
                select curAttributePair
                );
        }
    }
}
