using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AutoRent.ViewModels
{
    public class RegisterVm
    {
        [Required(ErrorMessage="Nhập họ tên"), StringLength(100, MinimumLength=2)] public string FullName { get; set; }
        [Required, EmailAddress, StringLength(150)] public string Email { get; set; }
        [Required, MinLength(6), StringLength(100)] public string Password { get; set; }
        [Required, Compare("Password", ErrorMessage="Mật khẩu nhập lại không khớp")] public string ConfirmPassword { get; set; }
    }
    public class LoginVm
    {
        [Required, EmailAddress] public string Email { get; set; }
        [Required] public string Password { get; set; }
    }
    public class ProfileVm
    {
        [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; }
        [Required, EmailAddress] public string Email { get; set; }
        [RegularExpression(@"^[0-9+() .-]{9,20}$", ErrorMessage="Số điện thoại không hợp lệ")]
        public string Phone { get; set; }
    }
    public class SiteHeaderVm
    {
        public List<AutoRent.Models.Category> Categories { get; set; } = new List<AutoRent.Models.Category>();
        public string FullName { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSignedIn { get; set; }
        public int CartCount { get; set; }
    }
    public class AddToCartVm
    {
        [Range(1, int.MaxValue)] public int VehicleId { get; set; }
        [Required] public DateTime? StartDate { get; set; }
        [Required] public DateTime? EndDate { get; set; }
        [Range(1, 100, ErrorMessage="Số lượng thuê từ 1 đến 100")]
        public int Quantity { get; set; } = 1;
    }
    public class CartLine
    {
        public int VehicleId { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Quantity { get; set; } = 1;
        public int Days { get { return (EndDate - StartDate).Days + 1; } }
        public decimal DailyPrice { get; set; }
        public decimal Deposit { get; set; } // tiền cọc / 1 chiếc xe
        public decimal DepositAmount { get { return Deposit * Quantity; } }
        public decimal RentAmount { get { return Days * DailyPrice * Quantity; } }
    }
    public class CartVm
    {
        public List<CartLine> Items { get; set; } = new List<CartLine>();
        public int TotalItems { get { return Items.Sum(x => x.Quantity); } }
        public decimal TotalRent { get { return Items.Sum(x => x.RentAmount); } }
        public decimal TotalDeposit { get { return Items.Sum(x => x.DepositAmount); } }
    }
    public class CheckoutVm
    {
        public string FullName { get; set; }
        [Required, RegularExpression(@"^[0-9+() .-]{9,20}$", ErrorMessage="Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }
        [StringLength(500)] public string Note { get; set; }
    }
    public class CategoryFormVm
    {
        public int CategoryId { get; set; }
        [Required(ErrorMessage="Chưa nhập tên danh mục"), StringLength(80, MinimumLength=2)] public string Name { get; set; }
        [StringLength(255)] public string Description { get; set; }
    }
    public class CategoryRowVm
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int VehicleCount { get; set; }
    }
    public class CategoryDetailVm
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<VehicleListVm> Vehicles { get; set; } = new List<VehicleListVm>();
    }
    public class VehicleFormVm
    {
        public int VehicleId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage="Chọn danh mục")]
        public int CategoryId { get; set; }
        [Required, StringLength(120, MinimumLength=2)] public string Name { get; set; }
        [StringLength(80)] public string Brand { get; set; }
        [StringLength(1000)] public string Description { get; set; }
        [Range(1, 100000000, ErrorMessage="Giá thuê phải lớn hơn 0")]
        public decimal DailyPrice { get; set; }
        [Range(0, 100000000)] public decimal Deposit { get; set; }
        [Range(0, 10000, ErrorMessage="Số xe không được âm")]
        public int Quantity { get; set; } = 1;
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
    public class VehicleListVm
    {
        public int VehicleId { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        public decimal DailyPrice { get; set; }
        public decimal Deposit { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int Quantity { get; set; }
        public int ReservedToday { get; set; }
        public int AvailableToday { get { return IsAvailable ? Math.Max(0, Quantity-ReservedToday) : 0; } }
        public int TotalBooked { get; set; } // số lượt xe đã được đặt, không phải xe bán
    }
    public class VehiclePageVm
    {
        public List<AutoRent.Models.Vehicle> Vehicles { get; set; } = new List<AutoRent.Models.Vehicle>();
        public List<AutoRent.Models.Category> Categories { get; set; } = new List<AutoRent.Models.Category>();
        public string Q { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MaxPrice { get; set; }
        public string Sort { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; } = 6;
        public int TotalCount { get; set; }
        public int TotalPages { get { return Math.Max(1,(TotalCount+PageSize-1)/PageSize); } }
    }
    public class VehicleDetailVm
    {
        public AutoRent.Models.Vehicle Vehicle { get; set; }
        public string CategoryName { get; set; }
        public List<AutoRent.Models.Vehicle> Related { get; set; } = new List<AutoRent.Models.Vehicle>();
        public bool AdminView { get; set; }
    }
    public class AdminUserFormVm
    {
        public int UserId { get; set; }
        [Required, StringLength(100, MinimumLength=2)] public string FullName { get; set; }
        [Required, EmailAddress, StringLength(150)] public string Email { get; set; }
        [StringLength(100, MinimumLength=6, ErrorMessage="Mật khẩu tối thiểu 6 ký tự")]
        public string Password { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class CustomerDetailVm
    {
        public AutoRent.Models.User Customer { get; set; }
        public List<OrderRowVm> Orders { get; set; } = new List<OrderRowVm>();
    }
    public class OrderRowVm
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalRent { get; set; }
        public decimal TotalDeposit { get; set; }
        public string CustomerName { get; set; }
    }
    public class OrderLineVm
    {
        public string VehicleName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public int Quantity { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal Deposit { get; set; } // tiền cọc cho cả dòng
        public decimal RentAmount { get; set; }
    }
    public class OrderDetailVm
    {
        public OrderRowVm Order { get; set; }
        public string CustomerPhone { get; set; }
        public string Note { get; set; }
        public List<OrderLineVm> Items { get; set; } = new List<OrderLineVm>();
    }
}
