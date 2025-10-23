using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;

namespace smart_feedback.Controllers
{
    public class RubricsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public RubricsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rubrics
        public async Task<IActionResult> Index()
        {
            return View(await _context.Rubrics.ToListAsync());
        }

        // GET: Rubrics/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubrics = await _context.Rubrics
                .FirstOrDefaultAsync(m => m.RubricsId == id);
            if (rubrics == null)
            {
                return NotFound();
            }

            return View(rubrics);
        }

        // GET: Rubrics/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rubrics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            rubrics.TotalMarks = 100; // Set default value for TotalMarks
            rubrics.SourceFile = ""; // Set default value for SourceFile
            //if (ModelState.IsValid)
            //{
                _context.Add(rubrics);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            //}
            //return View(rubrics);
        }

        // GET: Rubrics/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubrics = await _context.Rubrics.FindAsync(id);
            if (rubrics == null)
            {
                return NotFound();
            }
            return View(rubrics);
        }

        // POST: Rubrics/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            if (id != rubrics.RubricsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rubrics);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RubricsExists(rubrics.RubricsId))
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
            return View(rubrics);
        }

        // GET: Rubrics/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubrics = await _context.Rubrics
                .FirstOrDefaultAsync(m => m.RubricsId == id);
            if (rubrics == null)
            {
                return NotFound();
            }

            return View(rubrics);
        }

        // POST: Rubrics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rubrics = await _context.Rubrics.FindAsync(id);
            if (rubrics != null)
            {
                _context.Rubrics.Remove(rubrics);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RubricsExists(int id)
        {
            return _context.Rubrics.Any(e => e.RubricsId == id);
        }

        // GET: Rubrics/Upload
        public IActionResult Upload()
        {
            return View();
        }

        // POST: Rubrics/Upload
        [HttpPost, ActionName("Upload")]
        [ValidateAntiForgeryToken]
        public IActionResult Upload(int? id)
        {
            return RedirectToAction(nameof(Upload));
        }

        // GET: Rubrics/Task/CreateTask
        public IActionResult CreateTask()
        {
            return View();
        }

        // POST: Rubrics/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(int id, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            rubricTask.RubricsId = id;
            _context.Add(rubricTask);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id});
        }

        // GET: UploadRubrics
        public IActionResult UploadRubrics(int id)
        {
            //var jobPosting = _context.JobPostings.Find(id);
            //if (jobPosting == null)
            //{
            //    return NotFound();
            //}

            //ViewBag.JobPostingId = id;
            //ViewBag.JobTitle = jobPosting.Title;
            return View();
        }

        // POST: UploadRubrics
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadRubrics(IFormFile rubricsFile)
        {
            if (rubricsFile == null || rubricsFile.Length == 0)
            {
                ModelState.AddModelError("rubricsFile", "Please upload your Rubrics File.");
                //ViewBag.JobPostingId = JobPostingId;
                return View();
            }

            // Check file extension
            var extension = Path.GetExtension(rubricsFile.FileName).ToLower();
            if (extension != ".doc" && extension != ".docx")
            {
                ModelState.AddModelError("rubricsFile", "Only Word documents are allowed.");
                //ViewBag.JobPostingId = JobPostingId;
                return View();
            }

            // Check file size (limit to 10MB)
            if (rubricsFile.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("rubricsFile", "File size must be less than 10MB.");
                //ViewBag.JobPostingId = JobPostingId;
                return View();
            }

            try
            {
                // Create uploads directory if it doesn't exist
                string uploadsFolder = Path.Combine(
                    _hostingEnvironment.WebRootPath, "uploads", "rubrics");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique file name to prevent overwriting
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + rubricsFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await rubricsFile.CopyToAsync(fileStream);
                }

                // Create new rubrics
                var rubric = new Rubrics
                {
                    //JobPostingId = JobPostingId,
                    //UserId = User.Identity.Name,
                    //Status = "APPLIED",
                    //AppliedDate = DateTime.Now,
                    //CVFilePath = filePath,
                    //CVFileName = cvFile.FileName,
                    //CVFileType = extension
                    SourceFile = filePath + rubricsFile.FileName + extension
                };

                _context.Add(rubric);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your rubrics has been submitted successfully!";
                return RedirectToAction("Index", "Rubrics");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while uploading your Rubrics. Please try again.");
                //ViewBag.JobPostingId = JobPostingId;
                return View();
            }
        }
    }


}
