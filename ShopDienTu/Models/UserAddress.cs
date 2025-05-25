using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDienTu.Models
{
    public class UserAddress
    {
        [Key]
        public int UserAddressID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string Address { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }
}
