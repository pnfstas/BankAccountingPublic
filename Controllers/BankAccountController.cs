using Microsoft.AspNetCore.Mvc;

namespace BankAccountingApi.Controllers
{
    public class BankAccountController : Controller
    {
        public IActionResult Pay()
        {
            return View();
        }
        public IActionResult Transfer()
        {
            return View();
        }
        public IActionResult Balance()
        {
            return View();
        }
        public IActionResult OperationsHistory()
        {
            return View();
        }
    }
}
