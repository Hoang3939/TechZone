using ShopDienTu.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShopDienTu.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }

        // Navigation properties
        public virtual ICollection<SubCategory> SubCategories { get; set; }
    }
}
