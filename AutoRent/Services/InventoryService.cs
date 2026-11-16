using System;
using System.Linq;
using AutoRent.Models;

namespace AutoRent.Services
{
    // Website thuê xe: số xe còn trống phụ thuộc NGÀY THUÊ, không trừ vĩnh viễn.
    // Mỗi OrderItem giữ Quantity; các đơn đặt xe đều được tính giữ chỗ đến hết kỳ thuê.
    public static class InventoryService
    {
        public static int ReservedOnDay(AutoRentEntities db, int vehicleId, DateTime day)
        {
            DateTime d = day.Date;
            return db.OrderItems.Where(x => x.VehicleId == vehicleId && x.StartDate <= d && x.EndDate >= d)
                .Select(x => (int?)x.Quantity).Sum() ?? 0;
        }
        public static bool HasCapacity(AutoRentEntities db, Vehicle vehicle, DateTime start, DateTime end, int quantity, Func<DateTime,int> inCart)
        {
            if (vehicle == null || !vehicle.IsAvailable || quantity < 1 || vehicle.Quantity < quantity || end < start)
                return false;
            // Duyệt từng ngày: không cộng sai các đơn chỉ chạm một phần khoảng thời gian.
            for (DateTime date=start.Date; date<=end.Date; date=date.AddDays(1))
            {
                int reserved = ReservedOnDay(db, vehicle.VehicleId, date);
                int cart = inCart == null ? 0 : inCart(date);
                if (reserved + cart + quantity > vehicle.Quantity) return false;
            }
            return true;
        }
    }
}
