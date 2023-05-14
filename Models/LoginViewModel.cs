using BankAccountingApi.Extensions;
using BankAccountingApi.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace BankAccountingApi.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Login type must be selected")]
        [Display(Name = "Login by")]
        public LoginType LoginType { get; set; }
        public bool TwoFactorEnabled { get => LoginType != LoginType.LoginByUserName; }
        [TwoFactorAuth(ErrorMessage = "User name must be specified")]
        [Display(Name = "User name")]
        public string? UserName { get; set; }
        [TwoFactorAuth(ErrorMessage = "E-Mail must be specified")]
        [EmailAddress]
        public string? Email { get; set; }
        [TwoFactorAuth(ErrorMessage = "Phone number must be specified")]
        [Phone]
        [Display(Name = "Phone number")]
        public string? DislayedPhoneNumber { get; set; }
        public string? PhoneNumber
        {
            get
            {
                return !string.IsNullOrWhiteSpace(DislayedPhoneNumber) ?
                    string.Concat(Regex.Matches(DislayedPhoneNumber, "[0-9]+")?.Select<Match, string>(curMatch => curMatch?.Value)) : null;
            }
        }
        [Required(ErrorMessage = "Password must be specified")]
        [DataType(DataType.Password)]
        [StringLength(8)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
        public bool Submitted { get; set; }
        public static Dictionary<string, string> GetErrorMessages() => ModelHelper.GetErrorMessages(typeof(LoginViewModel));
        public LoginViewModel()
        {
        }
        public LoginViewModel(BankApiUser? user)
        {
            this.CopyPropertiesFrom(user);
        }
        public LoginViewModel(RegisterViewModel? model)
        {
            this.CopyPropertiesFrom(model);
        }
    }
}
