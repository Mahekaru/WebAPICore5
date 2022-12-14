using HPlusSport.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAPICore5.Classes;
using WebAPICore5.Models;

namespace WebAPICore5.Controllers
{
    [ApiVersion("1.0")]
    [Route("/v{v:apiVersion}/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ShopContext _context;

        public ProductsController(ShopContext context)
        {
            _context = context;

            _context.Database.EnsureCreated();
        }

        [HttpGet]
        [Route("/products")]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductQueryParams queryParams)
        {
            IQueryable<Product> products = _context.Products;

            if (queryParams.MinPrice != null && queryParams.MaxPrice != null)
            {
                products = products.Where(
                    p => p.Price >= queryParams.MinPrice.Value &&
                    p.Price <= queryParams.MaxPrice.Value);
            }

            if (!string.IsNullOrEmpty(queryParams.SearchTerm))
            {
                products = products.Where(p => p.Sku.ToLower().Contains(queryParams.SearchTerm.ToLower()) ||
                p.Name.ToLower().Contains(queryParams.SearchTerm.ToLower()));
            }

            if (!string.IsNullOrEmpty(queryParams.Sku))
            {
                products = products.Where(p => p.Sku == queryParams.Sku);
            }

            if (!string.IsNullOrEmpty(queryParams.Name))
            {
                products = products.Where(p => p.Name.ToLower().Contains(queryParams.Name.ToLower()));
            }

            if (!string.IsNullOrEmpty(queryParams.SortBy))
            {
                if (typeof(Product).GetProperty(queryParams.SortBy) != null)
                {
                    products = products.OrderByCustom(queryParams.SortBy, queryParams.SortOrder);
                }
            }

            products = products
                .Skip(queryParams.Size * (queryParams.Page - 1))
                .Take(queryParams.Size);

            return Ok(await products.ToArrayAsync());
        }

        [HttpGet]
        [Route("/products/{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) {
                return NotFound();
            }

            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct([FromBody]Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new {id = product.Id},product);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct([FromRoute] int id, [FromBody] Product product)
        {
            if(id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if(_context.Products.Find(id) == null)
                {
                    return NotFound();
                }

                throw;
            }
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<Product>> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return product;
        }

        [HttpPost]
        [Route("Delete")]
        public async Task<IActionResult> DeleteProducts([FromQuery]IEnumerable<int> ids)
        {
            var products = new List<Product>();

           foreach (int id in ids)
            {
                var product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    return NotFound();
                }

                products.Add(product);


            }

            _context.Products.RemoveRange(products);
            await _context.SaveChangesAsync();

            return Ok(products);
        }
    }
}
