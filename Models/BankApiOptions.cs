using MailKit.Security;

namespace BankAccountingApi.Models
{
    public class BankApiUserOptions
    {
        public static string SectionName { get; } = "BankApiUserOptions";
        public int DefaultUserNameLength { get; set; }
        public int VerificationCodeLength { get; set; }
        public int MaxRegistrationFailedCount { get; set; }
        public int MaxAccessFailedCount { get; set; }
        public int RegistrationLockoutInterval { get; set; }
        public int LoginLockoutInterval { get; set; }
        public void Fill(IConfiguration configuration)
        {
            configuration?.GetSection(BankApiUserOptions.SectionName)?.Bind(this);
        }
    }

    public class SmtpServiceOptions
    {
        public static string SectionName { get; } = "Smtp-Hotmail";
        public string From { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public SecureSocketOptions SecureSocketOptions { get; set; }
        public string PickupDirectoryLocation { get; set; }
        public string DeliveryMethod { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class SmsServiceOptions
    {
        public static string SectionName { get; } = "Sms";
        public string Url { get; set; }
        public string Authorization { get; set; }
    }
}
