using BankAccountingApi.Data;
using BankAccountingApi.Models;
using BankAccountingApi.Services;
using MailKit.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BankAccountingApi.Startup
{
    public class Startup
    {
        public static string TwoFactorTokenProviderName = "TwoFactorTokenProvider";
        public static IConfiguration AppConfiguration { get; set; }
        public static IWebHostEnvironment WebHostEnvironment { get; set; }
        public static BankApiUserOptions UserOptions { get; set; }
        public static SmtpServiceOptions SmtpOptions { get; set; }
        public static SmsServiceOptions SmsOptions { get; set; }
        public static UserManager<BankApiUser> UserManager { get; set; }
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            AppConfiguration = configuration;
            WebHostEnvironment = env;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(AppConfiguration["ConnectionStrings:DefaultConnection"]));
            services.AddIdentity<BankApiUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<BankApiUserTwoFactorTokenProvider>(TwoFactorTokenProviderName);
            services.AddControllersWithViews();
            services.AddSingleton<ITokenStoreService, TokenStoreService>();
            services.Configure<IdentityOptions>(options =>
            {
                // sign-in
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 7;
                //options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.LoginPath = "/UserAccount/Login";
                options.AccessDeniedPath = "/UserAccount/AccessDenied";
                options.SlidingExpiration = true;
            });
            services.AddOptions();
            services.Configure<BankApiUserOptions>(AppConfiguration?.GetSection(BankApiUserOptions.SectionName));
            services.Configure<SmtpServiceOptions>(options =>
            {
                IConfigurationSection section = AppConfiguration?.GetSection(SmtpServiceOptions.SectionName);
                section?.Bind(options);
                SecureSocketOptions secureSocketOptions = SecureSocketOptions.None;
                if(Enum.TryParse<SecureSocketOptions>(section["SecureSocketOptions"], out secureSocketOptions))
                {
                    options.SecureSocketOptions = secureSocketOptions;
                }
            });
            services.Configure<SmsServiceOptions>(options => AppConfiguration?.GetSection(SmsServiceOptions.SectionName)?.Bind(options));
        }
        public void Configure(IApplicationBuilder app, IOptions<BankApiUserOptions> userOptions, IOptions<SmtpServiceOptions> smtpOptions,
            IOptions<SmsServiceOptions> smsOptions, UserManager<BankApiUser> userManager)
        {
            UserOptions = userOptions?.Value;
            SmtpOptions = smtpOptions?.Value;
            SmsOptions = smsOptions?.Value;
            UserManager = userManager;
            app.UseMigrationsEndPoint();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseDefaultFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
