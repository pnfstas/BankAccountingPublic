using BankAccountingApi.Models;
using BankAccountingApi.Controllers;
using BankAccountingApi.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BankAccountingApi.Middleware
{
    public class MainMenuInitializer
    {
        private readonly RequestDelegate _next;

        public MainMenuInitializer(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext httpContext)
        {
            BankApiUser user = null;
            Stream stmOriginalBody = httpContext.Response.Body;
            MemoryStream stmNewBody = null;
            try
            {
                UserManager<BankApiUser> userManager = httpContext.RequestServices.GetService<UserManager<BankApiUser>>();
                SignInManager<BankApiUser> signInManager = httpContext.RequestServices.GetService<SignInManager<BankApiUser>>();
                if(signInManager?.IsSignedIn(httpContext.User) == true && (user = await userManager?.FindByNameAsync(httpContext.User?.Identity?.Name)) != null)
                {
                    httpContext.Request.EnableBuffering();
                    stmNewBody = new MemoryStream();
                    httpContext.Response.Body = stmNewBody;
                    await _next(httpContext);
                    StreamReader reader = new StreamReader(stmNewBody, Encoding.UTF8, true);
                    string strContent = await reader.ReadToEndAsync();
                    HomeViewModel.InitMainMenu(strContent, user.FullName);
                    stmNewBody.Seek(0, SeekOrigin.Begin);
                    await stmNewBody.CopyToAsync(stmOriginalBody);
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{e}");
                throw e;
            }
            finally
            {
                httpContext.Response.Body = stmOriginalBody;
            }
        }
    }
    public static class MainMenuInitializerExtensions
    {
        public static IApplicationBuilder UseMainMenuInitializer(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MainMenuInitializer>();
        }
    }
}
