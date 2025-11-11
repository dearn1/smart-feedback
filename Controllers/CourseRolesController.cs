using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;

namespace smart_feedback.Controllers
{
    public class CourseRolesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseRolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CourseRoles
        public async Task<IActionResult> Index()
        {
            return View(await _context.CourseRoles.ToListAsync());
        }

        // GET: CourseRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseRoles = await _context.CourseRoles
                .FirstOrDefaultAsync(m => m.CourseRolesId == id);
            if (courseRoles == null)
            {
                return NotFound();
            }

            return View(courseRoles);
        }

        // GET: CourseRoles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CourseRoles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseRolesId,CourseCode,CourseName,TermName,Programme,Institution,RoleLecturer,RoleModerator")] CourseRoles courseRoles)
        {
            if (ModelState.IsValid)
            {
                _context.Add(courseRoles);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(courseRoles);
        }

        // GET: CourseRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseRoles = await _context.CourseRoles.FindAsync(id);
            if (courseRoles == null)
            {
                return NotFound();
            }
            return View(courseRoles);
        }

        // POST: CourseRoles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CourseRolesId,CourseCode,CourseName,TermName,Programme,Institution,RoleLecturer,RoleModerator")] CourseRoles courseRoles)
        {
            if (id != courseRoles.CourseRolesId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseRoles);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseRolesExists(courseRoles.CourseRolesId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(courseRoles);
        }

        // GET: CourseRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseRoles = await _context.CourseRoles
                .FirstOrDefaultAsync(m => m.CourseRolesId == id);
            if (courseRoles == null)
            {
                return NotFound();
            }

            return View(courseRoles);
        }

        // POST: CourseRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var courseRoles = await _context.CourseRoles.FindAsync(id);
            if (courseRoles != null)
            {
                _context.CourseRoles.Remove(courseRoles);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CourseRolesExists(int id)
        {
            return _context.CourseRoles.Any(e => e.CourseRolesId == id);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                ViewBag.Message = "Please select a CSV file.";
                return View();
            }

            using (var stream = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8))
            using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    // Skip the first column (CourseRolesId) as it's an identity column
                    var courseRole = new CourseRoles
                    {
                        // Don't set CourseRolesId - let it auto-increment
                        CourseCode = csv.GetField<string>("CourseCode"),
                        CourseName = csv.GetField<string>("CourseName"),
                        TermName = csv.GetField<string>("TermName"),
                        Programme = csv.GetField<string>("Programme"),
                        Institution = csv.GetField<string>("Institution"),
                        RoleLecturer = csv.GetField<string>("RoleLecturer"),
                        RoleModerator = csv.GetField<string>("RoleModerator")
                    };

                    _context.CourseRoles.Add(courseRole);
                }

                await _context.SaveChangesAsync();
            }

            ViewBag.Message = "CSV uploaded successfully.";
            return View();
        }
    }
}
