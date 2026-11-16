using System;

namespace AutoRent.Models
{
    // Các entity tương ứng 1-1 với bảng SQL, dùng cùng tên thuộc tính trong EDMX.
    // Mô hình Database First: Database/AutoRentDB.sql -> Models/AutoRentModel.edmx.
    public partial class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    public partial class Vehicle
    {
        public int VehicleId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Description { get; set; }
        public decimal DailyPrice { get; set; }
        public decimal Deposit { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int Quantity { get; set; }
    }
    public partial class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string Phone { get; set; }
    }
    public partial class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalRent { get; set; }
        public decimal TotalDeposit { get; set; }
        public string CustomerPhone { get; set; }
        public string Note { get; set; }
    }
    public partial class OrderItem
    {
        public int OrderItemId { get; set; }
        public int Quantity { get; set; }
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public decimal PricePerDay { get; set; }
        public decimal Deposit { get; set; }
        public decimal RentAmount { get; set; }
    }
}
