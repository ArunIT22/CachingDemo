using CachingDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.Json;

namespace CachingDemo.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;

        public ProductsController(ApplicationDbContext context, IMemoryCache memoryCache, IDistributedCache distributedCache)
        {
            _context = context;
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
        }

        //Memory Cache Example
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

        public IActionResult GetProducts()
        {
            string cacheKey = "ProductList";
            Console.WriteLine("Fetching Product Details");
            var cacheProduct = _distributedCache.Get(cacheKey);
            if (cacheProduct == null)
            {
                Console.WriteLine("Distributed Cache - Product List not found in cache. Fetching from Database");
                var products = _context.Products.ToList();
                var productJson = JsonSerializer.Serialize(products);
                _distributedCache.SetString(cacheKey, productJson, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(30),
                    AbsoluteExpiration = DateTimeOffset.Now.AddHours(1)
                });
                return View("Index", products);
            }
            Console.WriteLine("Distributed Cache - Product Details found in Cache");
            var cachedProduct = JsonSerializer.Deserialize<IEnumerable<Product>>(cacheProduct);
            return View("Index", cachedProduct);
        }
    }
}
