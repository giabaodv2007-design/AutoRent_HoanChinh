using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using AutoRent.Helpers;
using AutoRent.Models;
using AutoRent.ViewModels;

namespace AutoRent.Controllers
{
    public class AccountController : Controller
    {
        private readonly AutoRentEntities db = new AutoRentEntities();
        [AllowAnonymous]
        public ActionResult Register() { return View(new RegisterVm()); }
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVm model)
        {
            if (!ModelState.IsValid) return View(model);
            var email = model.Email.Trim().ToLowerInvariant();
            if (db.Users.Any(x => x.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã có tài khoản."); return View(model);
            }
            db.Users.Add(new User { FullName=model.FullName.Trim(), Email=email,
                PasswordHash=PasswordHelper.Hash(model.Password), Role="User", IsActive=true });
            db.SaveChanges();
            // Checklist: đăng ký thành công CHUYỂN VỀ ĐĂNG NHẬP, không tự đăng nhập.
            TempData["Success"] = "Đăng ký thành công! Bạn hãy đăng nhập.";
            return RedirectToAction("Login");
        }
        [AllowAnonymous]
        public ActionResult Login(string returnUrl) { ViewBag.ReturnUrl=returnUrl; return View(new LoginVm()); }
        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public ActionResult Login(LoginVm model,string returnUrl)
        {
            ViewBag.ReturnUrl=returnUrl;
            if (!ModelState.IsValid) return View(model);
            var email=model.Email.Trim().ToLowerInvariant();
            var user=db.Users.FirstOrDefault(x=>x.Email==email && x.IsActive);
            if (user==null || !PasswordHelper.Verify(model.Password,user.PasswordHash))
            {
                ModelState.AddModelError("", "Sai email hoặc mật khẩu, hoặc tài khoản bị khóa.");
                return View(model);
            }
            FormsAuthentication.SetAuthCookie(user.Email,false);
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index",user.Role=="Admin" ? "Admin" : "Home");
        }
        [Authorize]
        public ActionResult Profile()
        {
            var u=db.Users.FirstOrDefault(x=>x.Email==User.Identity.Name && x.IsActive);
            if (u==null) return RedirectToAction("Login");
            return View(new ProfileVm { FullName=u.FullName, Email=u.Email, Phone=u.Phone });
        }
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileVm model)
        {
            if (!ModelState.IsValid) return View(model);
            var user=db.Users.FirstOrDefault(x=>x.Email==User.Identity.Name && x.IsActive);
            if (user==null) return RedirectToAction("Login");
            user.FullName=model.FullName.Trim();
            user.Phone=string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            db.SaveChanges();
            TempData["Success"]="Đã cập nhật hồ sơ.";
            return RedirectToAction("Profile");
        }
        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut(); Session.Clear();
            return RedirectToAction("Index","Home");
        }
        protected override void Dispose(bool disposing) { if(disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
