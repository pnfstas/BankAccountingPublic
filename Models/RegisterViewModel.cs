using BankAccountingApi.Extensions;
using BankAccountingApi.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace BankAccountingApi.Models
{
    public enum LoginType : int
    {
        LoginByUserName = 0,    
        LoginByEmail,
        LoginByPhoneNumber
    }

    public enum ConfirmationState : int
    {
        [Description("Not confirmed")]
        NotStarted = 0,
        [Description("Verification info sent")]
        VerificationSent,
        [Description("Wait for confirmation")]
        WaitForConfirmation,
        [Description("Confirmed")]
        Complete
    }

    public class TwoFactorAuthAttribute : ValidationAttribute
    {
        public static string DefaultErrorMessage { get; set; } = "User name must be specified";
        public new string? ErrorMessage { get => base.ErrorMessage ?? DefaultErrorMessage; set => base.ErrorMessage = value; }
        public TwoFactorAuthAttribute(string? errorMessage = null) : base(errorMessage ?? DefaultErrorMessage)
        {
        }
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            ValidationResult result = null;
            RegisterViewModel model = validationContext?.ObjectInstance as RegisterViewModel;
            if(model != null)
            {
                result = model.LoginType == LoginType.LoginByUserName && string.IsNullOrWhiteSpace(model.UserName)
                    || model.LoginType == LoginType.LoginByEmail && string.IsNullOrWhiteSpace(model.Email)
                    || model.LoginType == LoginType.LoginByPhoneNumber && string.IsNullOrWhiteSpace(model.PhoneNumber) ?
                    new ValidationResult(ErrorMessage) : ValidationResult.Success;
            }
            return result;
        }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Login type must be selected")]
        [Display(Name = "Login by")]
        public LoginType LoginType { get; set; }
        public bool TwoFactorEnabled { get => LoginType != LoginType.LoginByUserName; }
        [TwoFactorAuth(ErrorMessage = "User name must be specified")]
        [Display(Name = "User name")]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Password must be specified")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Password is not confirmed")]
        [Compare("Password", ErrorMessage = "Password confirmation don't match password")]
        [DataType(DataType.Password)]
        [StringLength(20)]
        [Display(Name = "Confirm password")]
        public string PasswordConfirmation { get; set; }
        [Required(ErrorMessage = "First name must be specified")]
        [Display(Name = "First name")]
        public string FirstName { get; set; }
        [Display(Name = "Last name")]
        public string? LastName { get; set; }
        [TwoFactorAuth(ErrorMessage = "E-Mail must be specified")]
        [EmailAddress]
        [Display(Name = "E-Mail")]
        public string? Email { get; set; }
        [Display(Name = "E-Mail verification code")]
        public string? EmailVerificationCode { get; set; }
        [ValidateNever]
        public ConfirmationState EmailConfirmationState { get; set; }
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
        [Display(Name = "Phone verification code")]
        public string? PhoneVerificationCode { get; set; }
        //public string? ConfirmPhoneVerificationCode { get; set; }
        [ValidateNever]
        public ConfirmationState PhoneConfirmationState { get; set; }
        [Display(Name = "Country")]
        public string? Country { get; set; }
        [Display(Name = "City")]
        public string? City { get; set; }
        public string? UserFullName
        {
            get => !string.IsNullOrWhiteSpace(FirstName) ? FirstName + (!string.IsNullOrWhiteSpace(LastName) ?
                $" {LastName}" : "") : LoginType != LoginType.LoginByUserName && !string.IsNullOrWhiteSpace(Email) ?
                Email : LoginType != LoginType.LoginByUserName && !string.IsNullOrWhiteSpace(PhoneNumber) ? PhoneNumber : UserName;
        }
        [ValidateNever]
        public bool Submitted { get; set; }
		public RegisterViewModel()
        {
        }
        public RegisterViewModel(BankApiUser? user)
        {
            this.CopyPropertiesFrom(user);
        }
        public static Dictionary<string, string> GetErrorMessages() => ModelHelper.GetErrorMessages(typeof(RegisterViewModel));
    }
}
