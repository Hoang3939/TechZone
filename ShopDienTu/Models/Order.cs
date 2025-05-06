using ShopDienTu.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDienTu.Models
{
    public class Order
    {
        public int OrderID { get; set; }

        public int? UserID { get; set; }

        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Tổng tiền")]
        public decimal? TotalAmount { get; set; }

        public int? PaymentMethodID { get; set; }

        [StringLength(50)]
        [Display(Name = "Trạng thái đơn hàng")]
        public string OrderStatus { get; set; } = "Pending";

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime? UpdatedAt { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("PaymentMethodID")]
        public virtual PaymentMethod PaymentMethod { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
