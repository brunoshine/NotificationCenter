using Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace NotificationCenterWebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            await NotificationCenter.GetInstance().PostNotificationAsync("NOTIFICATION", new SimpleNotification() { Type = "INFO", Message = "This is info message" });
            await NotificationCenter.GetInstance().PostNotificationAsync("NOTIFICATION", new SimpleNotification() { Type = "SUCCESS", Message = "This is success message" });
            await NotificationCenter.GetInstance().PostNotificationAsync("NOTIFICATION", new SimpleNotification() { Type = "WARNING", Message = "This is warning message" });
            await NotificationCenter.GetInstance().PostNotificationAsync("NOTIFICATION", new SimpleNotification() { Type = "ERROR", Message = "This is error message" });
            ViewBag.Message = StaticDemo.MESSAGE;
            return View();
        }

        public ActionResult About()
        {
            StaticDemo.SetMessage(ViewBag.Message = "Your application description page.");

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}