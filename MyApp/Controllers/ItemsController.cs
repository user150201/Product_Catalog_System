using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class ItemsController : Controller
    {
        private readonly MyAppContext _context;

        public ItemsController(MyAppContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var item = await _context.Items.Include(s=>s.SerialNumber)
                                            .Include(c=>c.Category)
                                            .Include(ic=>ic.ItemClients)
                                            .ThenInclude(c=>c.Client)
                                            .ToListAsync();
            return View(item);
        }

        public IActionResult Create()
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,CategoryId")] Item item, string serialNumberName)
        {
            if (ModelState.IsValid)
            {
                _context.Items.Add(item);
                await _context.SaveChangesAsync();

                // Manually linking SerialNumber if provided
                if (!string.IsNullOrEmpty(serialNumberName))
                {
                    var serialNumber = new SerialNumber
                    {
                        Name = serialNumberName,
                        ItemId = item.Id
                    };

                    _context.SerialNumbers.Add(serialNumber);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index");
            }
            return View(item);
        }


        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Categories"] = new SelectList(_context.Categories, "Id", "Name");

            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);
            return View(item);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,CategoryId")] Item item, string serialNumberName)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Retrieve the existing item from the database
                    var existingItem = await _context.Items
                        .Include(i => i.SerialNumber)
                        .FirstOrDefaultAsync(i => i.Id == id);

                    if (existingItem == null)
                    {
                        return NotFound();
                    }

                    // Update item properties
                    existingItem.Name = item.Name;
                    existingItem.Price = item.Price;
                    existingItem.CategoryId = item.CategoryId;

                    // Check if the item already has a SerialNumber
                    if (existingItem.SerialNumber != null)
                    {
                        // Update existing SerialNumber
                        existingItem.SerialNumber.Name = serialNumberName;
                        _context.SerialNumbers.Update(existingItem.SerialNumber);
                    }
                    else if (!string.IsNullOrEmpty(serialNumberName))
                    {
                        // Create new SerialNumber if it does not exist
                        var newSerialNumber = new SerialNumber
                        {
                            Name = serialNumberName,
                            ItemId = existingItem.Id
                        };

                        _context.SerialNumbers.Add(newSerialNumber);
                        existingItem.SerialNumber = newSerialNumber; // Link the new SerialNumber
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.Id == item.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            return View(item);
        }


        // Helper function to check if item exists
        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }



        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);
            return View(item);
        }

        [HttpPost, ActionName("Delete")]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                

            }
           
                return RedirectToAction("Index");
            
        }
    }
}
