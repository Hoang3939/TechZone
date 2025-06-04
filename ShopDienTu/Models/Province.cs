using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ShopDienTu.Models
{
    public class Province
    {
        [Key]
        public int ProvinceID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProvinceName { get; set; }

        public ICollection<District> Districts { get; set; }
    }
}