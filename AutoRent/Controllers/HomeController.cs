using System;
using System.Linq;
using System.Web.Mvc;
using AutoRent.Models;
using AutoRent.ViewModels;

namespace AutoRent.Controllers
{
    public class HomeController : Controller
    {
        private readonly AutoRentEntities db = new AutoRentEntities();
        public ActionResult Index(string q, int? categoryId, decimal? maxPrice, string sort="price-asc", int page=1)
        {
            var query = db.Vehicles.Where(x=>x.IsAvailable && x.Quantity>0);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q=q.Trim();
                query=query.Where(v=>v.Name.Contains(q) || (v.Brand!=null && v.Brand.Contains(q)) ||
                    (v.Description!=null && v.Description.Contains(q)));
            }
            if(categoryId.HasValue) query=query.Where(v=>v.CategoryId==categoryId.Value);
            if(maxPrice.HasValue && maxPrice.Value>=0) query=query.Where(v=>v.DailyPrice<=maxPrice.Value);
            if(sort=="price-desc") query=query.OrderByDescending(v=>v.DailyPrice).ThenBy(v=>v.VehicleId);
            else if(sort=="newest") query=query.OrderByDescending(v=>v.VehicleId);
            else { sort="price-asc"; query=query.OrderBy(v=>v.DailyPrice).ThenBy(v=>v.VehicleId); }
            const int pageSize=6;
            int count=query.Count(); int totalPages=Math.Max(1,(count+pageSize-1)/pageSize);
            page=Math.Max(1,Math.Min(page,totalPages));
            return View(new VehiclePageVm {
                Vehicles=query.Skip((page-1)*pageSize).Take(pageSize).ToList(),
                Categories=db.Categories.OrderBy(c=>c.Name).ToList(),
                Q=q,CategoryId=categoryId,MaxPrice=maxPrice,Sort=sort,
                Page=page,PageSize=pageSize,TotalCount=count
            });
        }
        public ActionResult Details(int id)
        {
            var v=db.Vehicles.Find(id);
            if(v==null) return HttpNotFound();
            var name=db.Categories.Where(c=>c.CategoryId==v.CategoryId).Select(c=>c.Name).FirstOrDefault();
            return View(new VehicleDetailVm { Vehicle=v,CategoryName=name,
                Related=db.Vehicles.Where(x=>x.CategoryId==v.CategoryId && x.VehicleId!=id && x.Quantity>0 && x.IsAvailable)
                   .OrderBy(x=>x.Name).Take(3).ToList() });
        }
        protected override void Dispose(bool disposing) { if(disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
