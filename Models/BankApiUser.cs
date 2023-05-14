using BankAccountingApi.Extensions;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace BankAccountingApi.Models
{
    public class BankApiUser : IdentityUser
    {
        [Column(TypeName = "int")]
        public LoginType LoginType { get; set; }
        [ProtectedPersonalData]
        public override string? UserName
        { 
            get => base.UserName;
            set
            {
                base.UserName = value;
                NormalizedUserName = value?.ToUpperInvariant();
            }
        }
        [ProtectedPersonalData]
        public override string? Email
        { 
            get => base.Email;
            set
            {
                base.Email = value;
                NormalizedEmail = value?.ToUpperInvariant();
            }
        }
        [PersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? FirstName { get; set; }
        [PersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? LastName { get; set; }
        [PersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? Country { get; set; }
        [PersonalData]
        [Column(TypeName = "nvarchar(256)")]
        public string? City { get; set; }
        [Column(TypeName = "bit")]
        public bool RememberMe { get; set; }
        [NotMapped]
        public string? FullName
        {
            get => !string.IsNullOrWhiteSpace(FirstName) ? FirstName + (!string.IsNullOrWhiteSpace(LastName) ?
                $" {LastName}" : "") : TwoFactorEnabled && !string.IsNullOrWhiteSpace(Email) ?
                Email : TwoFactorEnabled && !string.IsNullOrWhiteSpace(PhoneNumber) ? PhoneNumber : UserName;
        }
        public BankApiUser() : base()
        {
        }
        public BankApiUser(RegisterViewModel model) : base()
        {
            this.CopyPropertiesFrom(model);
        }
    }
}
