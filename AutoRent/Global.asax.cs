using System.Web;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;

namespace AutoRent
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
        protected void Application_PostAuthenticateRequest()
        {
            // Mỗi request kiểm tra lại người dùng bị khóa để không giữ phiên cũ.
            if (Context.User == null || !Context.User.Identity.IsAuthenticated) return;
            string email = Context.User.Identity.Name;
            using (var db = new Models.AutoRentEntities())
            {
                if (!db.Users.Any(u => u.Email == email && u.IsActive))
                    System.Web.Security.FormsAuthentication.SignOut();
            }
        }
    }
}
