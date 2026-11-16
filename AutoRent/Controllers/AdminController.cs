using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoRent.Filters;
using AutoRent.Helpers;
using AutoRent.Models;
using AutoRent.Services;
using AutoRent.ViewModels;

namespace AutoRent.Controllers
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly AutoRentEntities db=new AutoRentEntities();
        public ActionResult Index()
        {
            ViewBag.CategoryCount=db.Categories.Count();
            ViewBag.VehicleCount=db.Vehicles.Count();
            ViewBag.OrderCount=db.Orders.Count();
            ViewBag.UserCount=db.Users.Count(x=>x.Role=="User");
            ViewBag.StockCount=db.Vehicles.Select(x=>(int?)x.Quantity).Sum() ?? 0;
            return View();
        }
        public ActionResult Categories()
        {
            return View(db.Categories.OrderBy(x=>x.Name).Select(c=>new CategoryRowVm {
                CategoryId=c.CategoryId,Name=c.Name,Description=c.Description,
                VehicleCount=db.Vehicles.Count(v=>v.CategoryId==c.CategoryId)
            }).ToList());
        }
        public ActionResult CategoryDetail(int id)
        {
            var c=db.Categories.Find(id);if(c==null)return HttpNotFound();
            return View(new CategoryDetailVm { Name=c.Name, Description=c.Description,
                Vehicles=GetVehicleRows().Where(x=>x.CategoryName==c.Name).ToList() });
        }
        public ActionResult CategoryCreate() {ViewBag.IsEdit=false;return View("CategoryForm",new CategoryFormVm());}
        private void ValidateCategoryName(CategoryFormVm vm)
        {
            string name=vm.Name==null?"":vm.Name.Trim();
            if(db.Categories.Any(x=>x.Name==name && x.CategoryId!=vm.CategoryId))
                ModelState.AddModelError("Name","Tên danh mục đã tồn tại.");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult CategoryCreate(CategoryFormVm vm)
        {
            if(ModelState.IsValid) ValidateCategoryName(vm);
            if(!ModelState.IsValid){ViewBag.IsEdit=false;return View("CategoryForm",vm);}
            db.Categories.Add(new Category{Name=vm.Name.Trim(),Description=vm.Description});db.SaveChanges();
            TempData["Success"]="Đã thêm danh mục.";return RedirectToAction("Categories");
        }
        public ActionResult CategoryEdit(int id)
        {
            var c=db.Categories.Find(id);if(c==null)return HttpNotFound();
            ViewBag.IsEdit=true;
            return View("CategoryForm",new CategoryFormVm{CategoryId=c.CategoryId,Name=c.Name,Description=c.Description});
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult CategoryEdit(CategoryFormVm vm)
        {
            var c=db.Categories.Find(vm.CategoryId);if(c==null)return HttpNotFound();
            if(ModelState.IsValid) ValidateCategoryName(vm);
            if(!ModelState.IsValid){ViewBag.IsEdit=true;return View("CategoryForm",vm);}
            c.Name=vm.Name.Trim();c.Description=vm.Description;db.SaveChanges();
            TempData["Success"]="Đã sửa danh mục.";return RedirectToAction("Categories");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult CategoryDelete(int id)
        {
            if(db.Vehicles.Any(x=>x.CategoryId==id))
                TempData["Error"]="Danh mục còn xe: không thể xóa vì có khóa ngoại.";
            else
            {
                var c=db.Categories.Find(id);
                if(c!=null){db.Categories.Remove(c);db.SaveChanges();TempData["Success"]="Đã xóa danh mục.";}
            }
            return RedirectToAction("Categories");
        }
        private System.Collections.Generic.List<VehicleListVm> GetVehicleRows()
        {
            var items=(from v in db.Vehicles join c in db.Categories on v.CategoryId equals c.CategoryId
                orderby v.VehicleId descending
                select new VehicleListVm {
                    VehicleId=v.VehicleId,Name=v.Name,CategoryName=c.Name,DailyPrice=v.DailyPrice,
                    Deposit=v.Deposit,ImageUrl=v.ImageUrl,IsAvailable=v.IsAvailable,Quantity=v.Quantity
                }).ToList();
            DateTime today=DateTime.Today;
            var reserved=db.OrderItems.Where(x=>x.StartDate<=today && x.EndDate>=today)
                .GroupBy(x=>x.VehicleId).Select(g=>new {VehicleId=g.Key,Quantity=g.Sum(x=>x.Quantity)}).ToList();
            var booked=db.OrderItems.GroupBy(x=>x.VehicleId).Select(g=>new {VehicleId=g.Key,Quantity=g.Sum(x=>x.Quantity)}).ToList();
            foreach(var v in items)
            {
                v.ReservedToday=reserved.Where(x=>x.VehicleId==v.VehicleId).Select(x=>x.Quantity).FirstOrDefault();
                v.TotalBooked=booked.Where(x=>x.VehicleId==v.VehicleId).Select(x=>x.Quantity).FirstOrDefault();
            }
            return items;
        }
        public ActionResult Vehicles() {return View(GetVehicleRows());}
        public ActionResult Inventory() {return View(GetVehicleRows());}
        public ActionResult VehicleDetail(int id)
        {
            var v=db.Vehicles.Find(id);if(v==null)return HttpNotFound();
            return View(new VehicleDetailVm {Vehicle=v,AdminView=true,
                CategoryName=db.Categories.Where(c=>c.CategoryId==v.CategoryId).Select(c=>c.Name).FirstOrDefault()});
        }
        private void FillCategories()
        {
            ViewBag.Categories=new SelectList(db.Categories.OrderBy(x=>x.Name).ToList(),"CategoryId","Name");
        }
        private void ValidateVehicle(VehicleFormVm vm)
        {
            if(!db.Categories.Any(x=>x.CategoryId==vm.CategoryId))
                ModelState.AddModelError("CategoryId","Chọn danh mục hợp lệ.");
        }
        private string SaveImage(HttpPostedFileBase file)
        {
            if(file==null || file.ContentLength==0)return null;
            string ext=Path.GetExtension(file.FileName).ToLowerInvariant();
            if((ext!=".jpg" && ext!=".jpeg" && ext!=".png" && ext!=".gif") || file.ContentLength>3*1024*1024)
            {
                ModelState.AddModelError("","Chỉ cho ảnh JPG/PNG/GIF, tối đa 3MB.");return null;
            }
            try
            {
                using(var image=System.Drawing.Image.FromStream(file.InputStream))
                    if(image.Width<1 || image.Height<1)throw new InvalidDataException();
                if(file.InputStream.CanSeek)file.InputStream.Position=0;
                string name=Guid.NewGuid().ToString("N")+ext;
                string folder=Server.MapPath("~/Content/Uploads/");
                Directory.CreateDirectory(folder);file.SaveAs(Path.Combine(folder,name));
                return "~/Content/Uploads/"+name;
            }
            catch(Exception) {ModelState.AddModelError("","Ảnh không hợp lệ.");return null;}
        }
        public ActionResult VehicleCreate()
        {
            FillCategories();ViewBag.IsEdit=false;
            return View("VehicleForm",new VehicleFormVm {IsAvailable=true,Quantity=1});
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult VehicleCreate(VehicleFormVm vm,HttpPostedFileBase image)
        {
            ValidateVehicle(vm);
            if(!ModelState.IsValid){FillCategories();ViewBag.IsEdit=false;return View("VehicleForm",vm);}
            var path=SaveImage(image);
            if(!ModelState.IsValid){FillCategories();ViewBag.IsEdit=false;return View("VehicleForm",vm);}
            db.Vehicles.Add(new Vehicle {CategoryId=vm.CategoryId,Name=vm.Name.Trim(),Brand=vm.Brand,
                Description=vm.Description,DailyPrice=vm.DailyPrice,Deposit=vm.Deposit,Quantity=vm.Quantity,
                IsAvailable=vm.IsAvailable,ImageUrl=path??"~/Content/images/placeholder.svg"});
            db.SaveChanges();TempData["Success"]="Đã thêm xe.";return RedirectToAction("Vehicles");
        }
        public ActionResult VehicleEdit(int id)
        {
            var v=db.Vehicles.Find(id);if(v==null)return HttpNotFound();
            FillCategories();ViewBag.IsEdit=true;
            return View("VehicleForm",new VehicleFormVm {VehicleId=v.VehicleId,CategoryId=v.CategoryId,
                Name=v.Name,Brand=v.Brand,Description=v.Description,DailyPrice=v.DailyPrice,
                Deposit=v.Deposit,Quantity=v.Quantity,IsAvailable=v.IsAvailable,ImageUrl=v.ImageUrl});
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult VehicleEdit(VehicleFormVm vm,HttpPostedFileBase image)
        {
            var v=db.Vehicles.Find(vm.VehicleId);if(v==null)return HttpNotFound();
            ValidateVehicle(vm);
            // Không được hạ số xe thấp hơn số lượng đã giữ chỗ ở bất kỳ NGÀY TƯƠNG LAI nào.
            var dates=db.OrderItems.Where(x=>x.VehicleId==v.VehicleId && x.EndDate>=DateTime.Today)
                .Select(x=>new {x.StartDate,x.EndDate}).ToList();
            bool insufficient=false;
            foreach(var period in dates)
            {
                for(var day=(period.StartDate>DateTime.Today?period.StartDate.Date:DateTime.Today);day<=period.EndDate.Date;day=day.AddDays(1))
                {
                    if(InventoryService.ReservedOnDay(db,v.VehicleId,day)>vm.Quantity) { insufficient=true;break; }
                }
                if(insufficient)break;
            }
            if(insufficient)
                ModelState.AddModelError("Quantity","Số xe thấp hơn số đã được đặt trong các ngày sắp tới.");
            if(!ModelState.IsValid)
            {FillCategories();ViewBag.IsEdit=true;vm.ImageUrl=v.ImageUrl;return View("VehicleForm",vm);}
            var path=SaveImage(image);
            if(!ModelState.IsValid)
            {FillCategories();ViewBag.IsEdit=true;vm.ImageUrl=v.ImageUrl;return View("VehicleForm",vm);}
            v.CategoryId=vm.CategoryId;v.Name=vm.Name.Trim();v.Brand=vm.Brand;v.Description=vm.Description;
            v.DailyPrice=vm.DailyPrice;v.Deposit=vm.Deposit;v.Quantity=vm.Quantity;v.IsAvailable=vm.IsAvailable;
            if(path!=null)v.ImageUrl=path;
            db.SaveChanges();TempData["Success"]="Đã sửa xe.";return RedirectToAction("Vehicles");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult VehicleDelete(int id)
        {
            if(db.OrderItems.Any(x=>x.VehicleId==id))TempData["Error"]="Xe đã có đơn: hãy ngưng cho thuê, không được xóa lịch sử.";
            else {var v=db.Vehicles.Find(id);if(v!=null){db.Vehicles.Remove(v);db.SaveChanges();TempData["Success"]="Đã xóa xe.";}}
            return RedirectToAction("Vehicles");
        }
        private System.Collections.Generic.List<OrderRowVm> GetOrders(int? userId=null)
        {
            var q=from o in db.Orders join u in db.Users on o.UserId equals u.UserId
                select new {o,u};
            if(userId.HasValue)q=q.Where(x=>x.o.UserId==userId.Value);
            return q.OrderByDescending(x=>x.o.CreatedAt).Select(x=>new OrderRowVm {
                OrderId=x.o.OrderId,CreatedAt=x.o.CreatedAt,Status=x.o.Status,TotalRent=x.o.TotalRent,
                TotalDeposit=x.o.TotalDeposit,CustomerName=x.u.FullName
            }).ToList();
        }
        public ActionResult Orders() {return View(GetOrders());}
        public ActionResult OrderDetail(int id)
        {
            var order=(from o in db.Orders join u in db.Users on o.UserId equals u.UserId
                where o.OrderId==id select new OrderDetailVm {
                Order=new OrderRowVm {OrderId=o.OrderId,CreatedAt=o.CreatedAt,Status=o.Status,
                    TotalRent=o.TotalRent,TotalDeposit=o.TotalDeposit,CustomerName=u.FullName},
                CustomerPhone=o.CustomerPhone,Note=o.Note
            }).FirstOrDefault();
            if(order==null)return HttpNotFound();
            order.Items=(from i in db.OrderItems join v in db.Vehicles on i.VehicleId equals v.VehicleId
                where i.OrderId==id select new OrderLineVm {
                    VehicleName=v.Name,StartDate=i.StartDate,EndDate=i.EndDate,Days=i.Days,
                    Quantity=i.Quantity,PricePerDay=i.PricePerDay,Deposit=i.Deposit,RentAmount=i.RentAmount
            }).ToList();
            return View(order);
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int id,string status)
        {
            if(status!="Chờ duyệt" && status!="Đang giao" && status!="Đã giao")
                return new HttpStatusCodeResult(400);
            var order=db.Orders.Find(id);if(order==null)return HttpNotFound();
            order.Status=status;db.SaveChanges();
            TempData["Success"]="Cập nhật trạng thái thành công.";
            return RedirectToAction("OrderDetail",new{id=id});
        }
        // Checklist: chỉ hiển thị / CRUD quản trị viên trên trang Users.
        public ActionResult Users() {return View(db.Users.Where(x=>x.Role=="Admin").OrderBy(x=>x.UserId).ToList());}
        public ActionResult UserCreate() {return View("UserForm",new AdminUserFormVm());}
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult UserCreate(AdminUserFormVm vm)
        {
            if(string.IsNullOrWhiteSpace(vm.Password))ModelState.AddModelError("Password","Cần nhập mật khẩu quản trị.");
            string email=vm.Email==null?"":vm.Email.Trim().ToLowerInvariant();
            if(db.Users.Any(x=>x.Email==email))ModelState.AddModelError("Email","Email đã tồn tại.");
            if(!ModelState.IsValid)return View("UserForm",vm);
            db.Users.Add(new User {FullName=vm.FullName.Trim(),Email=email,Role="Admin",IsActive=vm.IsActive,
                PasswordHash=PasswordHelper.Hash(vm.Password)});
            db.SaveChanges();TempData["Success"]="Đã thêm tài khoản quản trị.";return RedirectToAction("Users");
        }
        public ActionResult UserEdit(int id)
        {
            var u=db.Users.FirstOrDefault(x=>x.UserId==id && x.Role=="Admin");if(u==null)return HttpNotFound();
            return View("UserForm",new AdminUserFormVm {UserId=u.UserId,FullName=u.FullName,Email=u.Email,IsActive=u.IsActive});
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult UserEdit(AdminUserFormVm vm)
        {
            var u=db.Users.FirstOrDefault(x=>x.UserId==vm.UserId && x.Role=="Admin");if(u==null)return HttpNotFound();
            string email=vm.Email==null?"":vm.Email.Trim().ToLowerInvariant();
            if(email!=u.Email && db.Users.Any(x=>x.Email==email))ModelState.AddModelError("Email","Email đã tồn tại.");
            if(!ModelState.IsValid)return View("UserForm",vm);
            if(u.Email==User.Identity.Name && (email!=u.Email || !vm.IsActive))
            {ModelState.AddModelError("","Không được đổi email hoặc khóa chính tài khoản đang đăng nhập.");return View("UserForm",vm);}
            u.FullName=vm.FullName.Trim();u.Email=email;u.IsActive=vm.IsActive;
            if(!string.IsNullOrWhiteSpace(vm.Password))u.PasswordHash=PasswordHelper.Hash(vm.Password);
            db.SaveChanges();TempData["Success"]="Đã sửa quản trị viên.";return RedirectToAction("Users");
        }
        [HttpPost,ValidateAntiForgeryToken]
        public ActionResult UserDelete(int id)
        {
            var u=db.Users.FirstOrDefault(x=>x.UserId==id && x.Role=="Admin");
            if(u==null)return HttpNotFound();
            if(u.Email==User.Identity.Name || db.Orders.Any(x=>x.UserId==id))
                TempData["Error"]="Không thể xóa chính mình hoặc người đã có đơn hàng.";
            else {db.Users.Remove(u);db.SaveChanges();TempData["Success"]="Đã xóa quản trị viên.";}
            return RedirectToAction("Users");
        }
        // Checklist: Customer READ ONLY, chi tiết kèm lịch sử thuê.
        public ActionResult Customers() {return View(db.Users.Where(x=>x.Role=="User").OrderBy(x=>x.FullName).ToList());}
        public ActionResult CustomerDetail(int id)
        {
            var customer=db.Users.FirstOrDefault(x=>x.UserId==id && x.Role=="User");
            if(customer==null)return HttpNotFound();
            return View(new CustomerDetailVm{Customer=customer,Orders=GetOrders(id)});
        }
        protected override void Dispose(bool disposing) {if(disposing)db.Dispose();base.Dispose(disposing);}
    }
}
