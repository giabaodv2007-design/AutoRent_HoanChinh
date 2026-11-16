using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using AutoRent.Models;
using AutoRent.ViewModels;
using AutoRent.Services;

namespace AutoRent.Controllers
{
    public class CartController : Controller
    {
        private readonly AutoRentEntities db=new AutoRentEntities();
        private List<CartLine> GetCart()
        {
            var cart=Session["CART"] as List<CartLine>;
            if(cart==null) { cart=new List<CartLine>(); Session["CART"]=cart; }
            return cart;
        }
        public ActionResult Index() { return View(new CartVm { Items=GetCart() }); }
        private bool ValidDates(DateTime start,DateTime end)
        {
            return start.Date>=DateTime.Today && end.Date>=start.Date && (end.Date-start.Date).Days<30;
        }
        private bool CanBook(Vehicle vehicle,DateTime start,DateTime end,int quantity,int excludeIndex=-1)
        {
            var cart=GetCart();
            return InventoryService.HasCapacity(db,vehicle,start,end,quantity,day=>cart
                .Where((x,i)=>i!=excludeIndex && x.VehicleId==vehicle.VehicleId && x.StartDate<=day && x.EndDate>=day)
                .Sum(x=>x.Quantity));
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Add(AddToCartVm model)
        {
            if(!ModelState.IsValid || !model.StartDate.HasValue || !model.EndDate.HasValue ||
               !ValidDates(model.StartDate.Value,model.EndDate.Value))
            {
                TempData["Error"]="Chọn ngày hợp lệ (tối đa 30 ngày) và số lượng từ 1 đến 100.";
                return RedirectToAction("Details","Home",new{id=model.VehicleId});
            }
            var v=db.Vehicles.Find(model.VehicleId);
            if(v==null) return HttpNotFound();
            DateTime start=model.StartDate.Value.Date,end=model.EndDate.Value.Date;
            if(!CanBook(v,start,end,model.Quantity))
            {
                TempData["Error"]="Không còn đủ số xe trống trong thời gian này. Vui lòng giảm số lượng hoặc đổi ngày.";
                return RedirectToAction("Details","Home",new{id=v.VehicleId});
            }
            var cart=GetCart();
            var existing=cart.FirstOrDefault(x=>x.VehicleId==v.VehicleId && x.StartDate==start && x.EndDate==end);
            if(existing!=null) existing.Quantity+=model.Quantity;
            else cart.Add(new CartLine { VehicleId=v.VehicleId,Name=v.Name,ImageUrl=v.ImageUrl,
                    StartDate=start,EndDate=end,Quantity=model.Quantity,DailyPrice=v.DailyPrice,Deposit=v.Deposit });
            TempData["Success"]="Đã thêm xe vào giỏ hàng.";
            return RedirectToAction("Index");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Update(int index,int quantity)
        {
            var cart=GetCart();
            if(index<0 || index>=cart.Count) return new HttpStatusCodeResult(400);
            var line=cart[index];
            var v=db.Vehicles.Find(line.VehicleId);
            if(quantity<1 || quantity>100 || v==null || !ValidDates(line.StartDate,line.EndDate) ||
                !CanBook(v,line.StartDate,line.EndDate,quantity,index))
            {
                TempData["Error"]="Số lượng không hợp lệ hoặc không còn đủ xe trong những ngày đã chọn.";
                return RedirectToAction("Index");
            }
            line.Quantity=quantity;
            line.DailyPrice=v.DailyPrice;line.Deposit=v.Deposit;
            TempData["Success"]="Đã cập nhật giỏ hàng.";
            return RedirectToAction("Index");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult Remove(int index)
        {
            var cart=GetCart();
            if(index>=0 && index<cart.Count) cart.RemoveAt(index);
            return RedirectToAction("Index");
        }
        [Authorize]
        public ActionResult Checkout()
        {
            if(!GetCart().Any()) return RedirectToAction("Index");
            var u=db.Users.FirstOrDefault(x=>x.Email==User.Identity.Name && x.IsActive);
            if(u==null) return RedirectToAction("Login","Account");
            return View(new CheckoutVm{FullName=u.FullName,CustomerPhone=u.Phone});
        }
        [Authorize,HttpPost,ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutVm model)
        {
            var cart=GetCart();
            if(!cart.Any()) return RedirectToAction("Index");
            var user=db.Users.FirstOrDefault(x=>x.Email==User.Identity.Name && x.IsActive);
            if(user==null) return RedirectToAction("Login","Account");
            model.FullName=user.FullName;
            if(!ModelState.IsValid) return View(model);
            // Giữ mức cô lập Serializable để tránh đặt vượt số xe khi 2 khách cùng thanh toán.
            using(var transaction=db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                foreach(var item in cart)
                {
                    var v=db.Vehicles.Find(item.VehicleId);
                    if(!ValidDates(item.StartDate,item.EndDate) || item.Quantity<1 || item.Quantity>100 ||
                        !InventoryService.HasCapacity(db,v,item.StartDate,item.EndDate,item.Quantity,
                           d=>cart.Where(x=>!object.ReferenceEquals(x,item) && x.VehicleId==item.VehicleId &&
                               x.StartDate<=d && x.EndDate>=d).Sum(x=>x.Quantity)))
                    {
                        ModelState.AddModelError("","Một xe đã hết chỗ hoặc lịch thuê không hợp lệ. Hãy cập nhật giỏ hàng.");
                        return View(model);
                    }
                    item.DailyPrice=v.DailyPrice;item.Deposit=v.Deposit;
                }
                var order=new Order {UserId=user.UserId,CreatedAt=DateTime.Now,Status="Chờ duyệt",
                    TotalRent=cart.Sum(x=>x.RentAmount),TotalDeposit=cart.Sum(x=>x.DepositAmount),
                    CustomerPhone=model.CustomerPhone.Trim(),Note=string.IsNullOrWhiteSpace(model.Note)?null:model.Note.Trim()};
                db.Orders.Add(order);db.SaveChanges();
                foreach(var item in cart)
                    db.OrderItems.Add(new OrderItem {OrderId=order.OrderId,VehicleId=item.VehicleId,
                        Quantity=item.Quantity,StartDate=item.StartDate,EndDate=item.EndDate,Days=item.Days,
                        PricePerDay=item.DailyPrice,Deposit=item.DepositAmount,RentAmount=item.RentAmount});
                if(string.IsNullOrWhiteSpace(user.Phone)) user.Phone=model.CustomerPhone.Trim();
                db.SaveChanges();transaction.Commit();
                Session.Remove("CART");
                TempData["Success"]="Đặt xe thành công! Đơn đang chờ duyệt.";
                return RedirectToAction("Details","Orders",new{id=order.OrderId});
            }
        }
        protected override void Dispose(bool disposing) {if(disposing)db.Dispose();base.Dispose(disposing);}
    }
}
