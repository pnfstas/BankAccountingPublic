using BankAccountingApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankAccountingApi.Controllers
{
	public class HomeController : Controller
	{
        public UserManager<BankApiUser> UserManager { get; }
        public SignInManager<BankApiUser> SignInManager { get; }
        public HomeController(UserManager<BankApiUser> userManager, SignInManager<BankApiUser> signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }
        public async Task<IActionResult> Index()
		{
            IActionResult result = null;
            BankApiUser user = null;
            if(SignInManager.IsSignedIn(User) && await UserManager.FindByNameAsync(User.Identity.Name) != null)
            {
                result = View();
            }
            else
            {
                result = RedirectToAction("Login", "UserAccount");
            }
            return result;
		}
    }
}
