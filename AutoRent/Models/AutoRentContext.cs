using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace AutoRent.Models
{
    // Context Database First khớp với metadata nhúng từ AutoRentModel.edmx.
    // Không chạy Migrations/Code First lên DB đã tạo bằng script SQL.
    public partial class AutoRentEntities : DbContext
    {
        public AutoRentEntities() : base("name=AutoRentEntities") { }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Vehicle> Vehicles { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
    }
}
