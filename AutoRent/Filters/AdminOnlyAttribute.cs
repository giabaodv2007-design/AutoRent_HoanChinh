using System.Web;
using System.Web.Mvc;
using AutoRent.Models;
using System.Linq;

namespace AutoRent.Filters
{
    public class AdminOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase context)
        {
            if (context.User == null || !context.User.Identity.IsAuthenticated) return false;
            string email = context.User.Identity.Name;
            using (var db = new AutoRentEntities())
                return db.Users.Any(u => u.Email == email && u.Role == "Admin" && u.IsActive);
        }
    }
}
