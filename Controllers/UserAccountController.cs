using BankAccountingApi.Data;
using BankAccountingApi.Extensions;
using BankAccountingApi.Helpers;
using BankAccountingApi.Models;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace BankAccountingApi.Controllers
{
    public class UserAccountController : Controller
    {
        public ApplicationDbContext DbContext { get; }
        public UserManager<BankApiUser> UserManager { get; }
        public SignInManager<BankApiUser> SignInManager { get; }
        public UserAccountController(ApplicationDbContext dbcontext, UserManager<BankApiUser> userManager, SignInManager<BankApiUser> signInManager)
        {
            DbContext = dbcontext;
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public override bool TryValidateModel(object model)
        {
            bool fIsModelValid = base.TryValidateModel(model);
            Type typeOfModel = model.GetType();
            if(typeOfModel == typeof(RegisterViewModel) || typeOfModel == typeof(LoginViewModel))
            {
                LoginType loginType = (LoginType)typeOfModel.GetProperty("LoginType")?.GetValue(model);
                string strUserName = typeOfModel.GetProperty("UserName")?.GetValue(model) as string;
                string strEmail = typeOfModel.GetProperty("Email")?.GetValue(model) as string;
                string strPhoneNumber = typeOfModel.GetProperty("PhoneNumber")?.GetValue(model) as string;
                if(loginType == LoginType.LoginByUserName && string.IsNullOrWhiteSpace(strUserName))
                {
                    ModelState.AddModelError("UserName", "User name must be specified");
                    fIsModelValid = false;
                }
                else if(loginType == LoginType.LoginByEmail && string.IsNullOrWhiteSpace(strEmail))
                {
                    ModelState.AddModelError("Email", "E-mail must be specified");
                    fIsModelValid = false;
                }
                else if(loginType == LoginType.LoginByPhoneNumber && string.IsNullOrWhiteSpace(strPhoneNumber))
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number must be specified");
                    fIsModelValid = false;
                }
                if(typeOfModel == typeof(RegisterViewModel))
                {
                    (model as RegisterViewModel).Submitted = ModelState.ErrorCount == 0;
                }
                else
                {
                    (model as LoginViewModel).Submitted = ModelState.ErrorCount == 0;
                }
            }
            return ModelState.ErrorCount == 0;
        }
        [HttpGet]
        public IActionResult Register(RegisterViewModel? model = null)
        {
            return View(model ?? new RegisterViewModel());
        }
        [HttpPost]
        public IActionResult GetModelValidationState()
        {
            IActionResult actionResult = new JsonResult(new
            {
                valid = ModelState.IsValid
            });
            return actionResult;
        }
        [HttpGet]
        public async Task<IActionResult> ProcessRegister(RegisterViewModel model)
        {
            IActionResult actionResult = View("Register", new RegisterViewModel());
            if(model != null)
            {
                if(string.IsNullOrWhiteSpace(model.UserName))
                {
                    model.UserName = UserManager.GenerateNewUserName();
                }
                BankApiUser newUser = new BankApiUser(model);
                IdentityResult identityResult = IdentityResult.Success;
                try
                {
                    if(model.LoginType == LoginType.LoginByEmail)
                    {
                        IEnumerable<BankApiUser> sameEmailUsers = DbContext.Users.Where<BankApiUser>(curuser => curuser != null && string.Equals(curuser.Email, model.Email));
                        if(sameEmailUsers != null)
                        {
                            foreach(BankApiUser curUser in sameEmailUsers)
                            {
                                if((identityResult = await UserManager.DeleteAsync(curUser)) != IdentityResult.Success)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    if(identityResult == IdentityResult.Success)
                    {
                        identityResult = await UserManager.CreateAsync(newUser, model.Password);
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine($"{e}");
                    throw e;
                }
                if(identityResult == IdentityResult.Success)
                {
                    switch(model.LoginType)
                    {
                    case LoginType.LoginByUserName:
                        actionResult = RedirectToAction("Login", model);
                        break;
                    case LoginType.LoginByEmail:
                        await EmailHelper.SendConfirmationLinkMail(HttpContext, model, UserManager);
                        actionResult = RedirectToAction("Register", model);
                        /*
                        model.EmailVerificationCode = await UserManager.GenerateTwoFactorTokenAsync(newUser, Startup.Startup.TwoFactorTokenProviderName);
                        if(!string.IsNullOrWhiteSpace(model.EmailVerificationCode))
                        {
                            model.EmailConfirmationState = ConfirmationState.WaitForConfirmation;
                            actionResult = RedirectToAction("Register", model);
                        }
                        */
                        break;
                    case LoginType.LoginByPhoneNumber:
                        await SmsHelper.SendVerificationCode(model);
                        actionResult = RedirectToAction("Register", model);
                        break;
                    }
                }
            }
            return actionResult;
        }
        [HttpPost]
        public async Task<IActionResult> SendConfirmationLinkMail([FromBody] RegisterViewModel model)
        {
            await EmailHelper.SendConfirmationLinkMail(HttpContext, model, UserManager);
            return RedirectToAction("Register", model);
        }
        [HttpPost]
        public async Task<IActionResult> GetEmailConfirmationState([FromBody] RegisterViewModel model)
        {
            bool isConfirmed = false;
            if(model != null && model.LoginType == LoginType.LoginByEmail)
            {
                BankApiUser user = await UserManager.FindByEmailAsync(model.Email);
                if(user != null)
                {
                    isConfirmed = user.EmailConfirmed;
                }
            }
            return new JsonResult(new
            {
                confirmed = isConfirmed
            });
        }
        [HttpGet]
        public async Task<IActionResult> CompleteConfirmEmail(RegisterViewModel model)
        {
            string action = "Register";
            if(!string.IsNullOrWhiteSpace(model?.EmailVerificationCode))
            {
                BankApiUser user = await UserManager.FindByEmailAsync(model?.Email);
                if(user != null && await UserManager.VerifyTwoFactorTokenAsync(user, Startup.Startup.TwoFactorTokenProviderName, model.EmailVerificationCode))
                {
                    model.EmailConfirmationState = ConfirmationState.Complete;
                    model.Submitted = false;
                    action = "Login";
                }
            }
            if(model == null)
            {
                model = new RegisterViewModel();
            }
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                {"url", $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/UserAccount/{action}/?{model.ToQueryString()}" },
                {"target", "BankApi.Register" }
            };
            return PartialView("RedirectPartial", parameters);
        }
        [HttpPost]
        public async Task<IActionResult> SendNewVerificationCode([FromBody] RegisterViewModel model)
        {
            await SmsHelper.SendVerificationCode(model);
            return RedirectToAction("Register", model);
        }
        [HttpPost]
        public IActionResult CompleteConfirmPhone([FromBody] RegisterViewModel model)
        {
            model.PhoneConfirmationState = ConfirmationState.Complete;
            BankApiUser user = UserManager.FindByPhoneNumber(model.PhoneNumber);
            return RedirectToAction("Login", user);
        }
        [HttpGet]
        public IActionResult Login(RegisterViewModel? regModel = null)
        {
            LoginViewModel model = new LoginViewModel(regModel);
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            IActionResult actionResult = View(model);
            if(ModelState.IsValid)
            {
                BankApiUser userForLogin = null;
                switch(model.LoginType)
                {
                case LoginType.LoginByUserName:
                    userForLogin = await UserManager.FindByNameAsync(model.UserName);
                    break;
                case LoginType.LoginByEmail:
                    userForLogin = await UserManager.FindByEmailAsync(model.Email);
                    break;
                case LoginType.LoginByPhoneNumber:
                    userForLogin = UserManager.FindByPhoneNumber(model.PhoneNumber);
                    break;
                }
                if(userForLogin != null)
                {
                    try
                    {
                        bool fLockOutOnFailure = userForLogin.AccessFailedCount > BankAccountingApi.Startup.Startup.UserOptions.MaxAccessFailedCount - 2;
                        Microsoft.AspNetCore.Identity.SignInResult signInResult = await SignInManager.PasswordSignInAsync(userForLogin, model.Password, model.RememberMe, fLockOutOnFailure);
                        if(signInResult?.Succeeded == true)
                        {
                            actionResult = RedirectToAction("Index", "Home");
                        }
                        else
                        {
                            if(fLockOutOnFailure)
                            {
                                userForLogin.LockoutEnd = DateTime.Now.AddSeconds(BankAccountingApi.Startup.Startup.UserOptions.LoginLockoutInterval);
                                userForLogin.AccessFailedCount = 0;
                            }
                            else
                            {
                                userForLogin.AccessFailedCount++;
                            }
                            IdentityResult identityResult = await UserManager.UpdateAsync(userForLogin);
                            actionResult = RedirectToAction("AccessDenied");
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine($"{e}");
                        throw e;
                    }
                }
            }
            return actionResult;
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return RedirectToAction("Index", "Home", "AccessDenied");
        }
        [HttpGet]
        public IActionResult EditProfile()
        {
            return View();
        }
        [HttpPost]
        public IActionResult EditProfile(UserProfileViewModel model)
        {
            return View();
        }
        [HttpGet]
        public IActionResult LogOut()
        {
            return View();
        }
    }
}
