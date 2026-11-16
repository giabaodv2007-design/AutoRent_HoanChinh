using System.Linq;
using System.Web.Mvc;
using AutoRent.Models;
using AutoRent.ViewModels;

namespace AutoRent.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AutoRentEntities db = new AutoRentEntities();
        public ActionResult Index()
        {
            var email = User.Identity.Name;
            var rows = (from o in db.Orders
                        join u in db.Users on o.UserId equals u.UserId
                        where u.Email == email
                        orderby o.CreatedAt descending
                        select new OrderRowVm {
                            OrderId = o.OrderId, CreatedAt = o.CreatedAt, Status = o.Status,
                            TotalRent = o.TotalRent, TotalDeposit = o.TotalDeposit,
                            CustomerName = u.FullName
                        }).ToList();
            return View(rows);
        }
        public ActionResult Details(int id)
        {
            var email = User.Identity.Name;
            var order = (from o in db.Orders
                         join u in db.Users on o.UserId equals u.UserId
                         where o.OrderId == id && u.Email == email
                         select new OrderDetailVm {
                             Order = new OrderRowVm {
                                 OrderId = o.OrderId, CreatedAt = o.CreatedAt, Status = o.Status,
                                 TotalRent = o.TotalRent, TotalDeposit = o.TotalDeposit,
                                 CustomerName = u.FullName
                             },
                             CustomerPhone = o.CustomerPhone, Note = o.Note
                         }).FirstOrDefault();
            if (order == null) return HttpNotFound();
            order.Items = (from i in db.OrderItems
                           join v in db.Vehicles on i.VehicleId equals v.VehicleId
                           where i.OrderId == id
                           select new OrderLineVm {
                               VehicleName = v.Name, StartDate = i.StartDate, EndDate = i.EndDate,
                               Days = i.Days, Quantity=i.Quantity, PricePerDay = i.PricePerDay,
                               Deposit = i.Deposit, RentAmount = i.RentAmount
                           }).ToList();
            return View(order);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
