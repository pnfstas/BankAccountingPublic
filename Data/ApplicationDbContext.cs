using BankAccountingApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace BankAccountingApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<BankApiUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //optionsBuilder.UseSqlServer(System.Configuration.ConfigurationManager.ConnectionStrings[1].ConnectionString);
        }
    }
}
