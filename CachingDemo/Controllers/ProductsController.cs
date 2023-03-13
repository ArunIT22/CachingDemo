using CachingDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace CachingDemo.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;

        public ProductsController(ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
            var watch = new Stopwatch();
            Console.WriteLine("Fetching Product Details");
            string cacheKey = "ProductList";

            watch.Reset();
            watch.Start();
            if (_memoryCache.TryGetValue(cacheKey, out IEnumerable<Product> Products))
            {
                watch.Stop();
                Console.WriteLine($"Product Details found in cache. Elapsed Time :{watch.ElapsedMilliseconds}");
                return View(Products);
            }
            else
            {
                watch.Reset();
                watch.Start();
                var prd = _context.Products.ToList();
                watch.Stop();
                _memoryCache.Set(cacheKey, prd, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(30),
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                });
                Console.WriteLine($"Product Details not found in Cache. Fetching from Database. Elapsed Time :{watch.ElapsedMilliseconds}");
                return View(prd);
            }
        }
    }
}
