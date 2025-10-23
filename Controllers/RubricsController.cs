using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NPOI.XWPF.UserModel;
using smart_feedback.Data;
using smart_feedback.Models;
using Microsoft.VisualStudio.Web.CodeGeneration.Design;
using smart_feedback.Data.Migrations;

namespace smart_feedback.Controllers
{
    public class RubricsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public RubricsController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
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

            // Get the related RubricTasks
            var rubricTasks = await _context.RubricTask
                .Where(rt => rt.RubricsId == id)
                .ToListAsync();

            // Create the ViewModel
            var viewModel = new RubricDetailsViewModel
            {
                Rubric = rubrics,
                RubricTasks = rubricTasks
            };

            return View(viewModel);
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

        // GET: Rubrics/Task/CreateTask
        public IActionResult CreateTask()
        {
            return View();
        }

        // GET: Rubrics/CreateTask/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTask(int id)
        {
            var rubricTask = new RubricTask
            {
                RubricsId = id
            };
            return View(rubricTask);
        }

        // GET: UploadRubrics
        public IActionResult Upload(int id)
        {
            return View();
        }

        // POST: UploadRubrics
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile rubricsFile)
        {
            if (rubricsFile == null || rubricsFile.Length == 0)
            {
                ModelState.AddModelError("rubricsFile", "Please upload your Rubrics File.");
                return RedirectToAction("Upload", "Rubrics");
            }

            // Check file extension
            var extension = Path.GetExtension(rubricsFile.FileName).ToLower();
            if (extension != ".doc" && extension != ".docx")
            {
                ModelState.AddModelError("rubricsFile", "Only Word documents are allowed.");
                return RedirectToAction("Upload", "Rubrics");
            }

            // Check file size (limit to 10MB)
            if (rubricsFile.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("rubricsFile", "File size must be less than 10MB.");
                return RedirectToAction("Upload", "Rubrics");
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

                var rubric = new Rubrics();
                List<string> rubricsParagraphs = new List<string>();
                List<RubricTask> rubricTasks = new List<RubricTask>();
                if (extension == ".docx")
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        XWPFDocument docx = new XWPFDocument(stream);

                        // Extract rubrics paragraphs
                        foreach (var para in docx.Paragraphs)
                        {
                            if (!string.IsNullOrWhiteSpace(para.ParagraphText))
                            {
                                rubricsParagraphs.Add(para.ParagraphText.Trim());
                                if (rubricsParagraphs.Count >= 4)
                                    break;
                            }
                        }

                        // Extract rubric criteria from tables
                        int tableIndex = 0;
                        foreach (var table in docx.Tables)
                        {
                            
                            // Extract rubric criteria from ONLY the first table
                            if (docx.Tables.Count > 0)
                            {                                
                                if (tableIndex == 0)
                                {
                                    var firstTable = docx.Tables[tableIndex]; // Get only the first table
                                    var rows = firstTable.Rows;

                                    // Skip header row (index 0) and process data rows
                                    for (int i = 1; i < rows.Count; i++)
                                    {
                                        var row = rows[i];
                                        var cells = row.GetTableCells();

                                        if (cells.Count >= 3) // Ensure we have at least 3 columns
                                        {
                                            var task = new RubricTask
                                            {
                                                TaskTitle = GetCellText(cells[0]),
                                                TaskDescription = GetCellText(cells[1]),
                                                MaxMarks = ParseMaxMarks(GetCellText(cells[2]))
                                            };

                                            // Only add if we have meaningful data
                                            if (!string.IsNullOrWhiteSpace(task.TaskTitle))
                                            {
                                                rubricTasks.Add(task);
                                            }
                                        }
                                    }
                                }
                                tableIndex++;
                            }
                            
                        }
                        string fullText = rubricsParagraphs.Count > 1 ? rubricsParagraphs[1] : "";
                        int firstSpaceIndex = fullText.IndexOf(' ');

                        rubric.Institution = "Auckland Institute of Studies";
                        rubric.Programme = rubricsParagraphs[0];
                        rubric.CourseCode = firstSpaceIndex > 0 ? fullText.Substring(0, firstSpaceIndex) : fullText;
                        rubric.CourseName = firstSpaceIndex > 0 ? fullText.Substring(firstSpaceIndex + 1).Trim() : "";
                        rubric.RubricName = rubricsParagraphs.Count > 2 ? rubricsParagraphs[2] : ""; 
                        rubric.TotalMarks = rubricTasks.Sum(t => t.MaxMarks);
                        rubric.SourceFile = filePath;
                    }
                }

                _context.Add(rubric);
                await _context.SaveChangesAsync();

                // Save rubric tasks with the rubric ID
                foreach (var task in rubricTasks)
                {
                    task.RubricsId = rubric.RubricsId;
                    _context.RubricTask.Add(task);
                }
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Your rubrics has been submitted successfully! {rubricTasks.Count} tasks extracted.";

                return RedirectToAction("Details", "Rubrics", new { id = rubric.RubricsId });
            }
            catch (Exception ex)
            
            {
                ModelState.AddModelError("", $"An error occurred while uploading your Rubrics: {ex.Message}");
                return View();
            }
        }

        // Helper method to extract text from table cell
        private string GetCellText(XWPFTableCell cell)
        {
            if (cell == null) return "";

            var text = cell.GetText().Trim();
            // Remove extra whitespace and line breaks
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        }

        // Helper method to parse marks from text
        private int ParseMaxMarks(string marksText)
        {
            if (string.IsNullOrWhiteSpace(marksText)) return 0;

            // Try to extract numbers from the text
            var numbers = System.Text.RegularExpressions.Regex.Matches(marksText, @"\d+");
            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int marks))
                {
                    return marks;
                }
            }

            return 0;
        }
    }
}
