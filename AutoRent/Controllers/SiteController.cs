using System.Linq;
using System.Web.Mvc;
using AutoRent.Models;
using AutoRent.ViewModels;

namespace AutoRent.Controllers
{
    public class SiteController : Controller
    {
        [ChildActionOnly]
        public ActionResult Header()
        {
            var m = new SiteHeaderVm();
            using (var db = new AutoRentEntities())
            {
                m.Categories = db.Categories.OrderBy(x => x.Name).ToList();
                if (User.Identity.IsAuthenticated)
                {
                    var user = db.Users.FirstOrDefault(x => x.Email == User.Identity.Name && x.IsActive);
                    if (user != null) { m.IsSignedIn = true; m.IsAdmin = user.Role == "Admin"; m.FullName = user.FullName; }
                }
            }
            var cart = Session["CART"] as System.Collections.Generic.List<CartLine>;
            m.CartCount = cart == null ? 0 : cart.Sum(x => x.Quantity);
            return PartialView("~/Views/Shared/_SiteHeader.cshtml",m);
        }
    }
}
