using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDienTu.Data;
using ShopDienTu.Models;
using System.Data.Entity;
[Area("Admin")]
public class SubCategoriesController : Controller
{
    private readonly ApplicationDbContext _context;

    public SubCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }


}