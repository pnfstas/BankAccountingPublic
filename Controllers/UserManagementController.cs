using BankAccountingApi.Data;
using BankAccountingApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json.Linq;

namespace BankAccountingApi.Controllers
{
    public class UserManagementController : Controller
    {
        public ApplicationDbContext DbContext { get; }
        public UserManager<BankApiUser> UserManager { get; }
        public SignInManager<BankApiUser> SignInManager { get; }
        public UserManagementController(ApplicationDbContext dbcontext, UserManager<BankApiUser> userManager, SignInManager<BankApiUser> signInManager)
        {
            DbContext = dbcontext;
            UserManager = userManager;
            SignInManager = signInManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult StartConfirmEmail(RegisterViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        public IActionResult ProcessConfirmEmail([FromBody]RegisterViewModel model)
        {
            return View(model);
        }
        [HttpGet]
        public IActionResult StartConfirmPhone(RegisterViewModel model)
        {
            return View(model);
        }
        [HttpPost]
        public IActionResult ProcessConfirmPhone(RegisterViewModel model)
        {
            return View(model);
        }
    }
}
