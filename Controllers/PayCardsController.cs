using Microsoft.AspNetCore.Mvc;

namespace BankAccountingApi.Controllers
{
    public class PayCardsController : Controller
    {
        public IActionResult Open()
        {
            return View();
        }
        public IActionResult Block()
        {
            return View();
        }
        public IActionResult Close()
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
