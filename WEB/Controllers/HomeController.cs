using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;





namespace SSISDashboard.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrentMonthProv : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
