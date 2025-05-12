using ShopDienTu.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDienTu.Models
{
    public class Promotion
    {
        public int PromotionID { get; set; }

        public int? ProductID { get; set; }

        [Required]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5, 2)")]
        [Display(Name = "Phần trăm giảm giá")]
        public decimal DiscountPercentage { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; }

        [StringLength(255)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // Navigation properties
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
    }
}
