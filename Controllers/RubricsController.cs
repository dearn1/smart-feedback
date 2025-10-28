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
using MathNet.Numerics.RootFinding;

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

            // Get the related RubricCriteria
            List<RubricCriteria> rubricCriteria = new List<RubricCriteria>();
            foreach (RubricTask rt in rubricTasks)
            {
                var rubricCriteriaTemp = await _context.RubricCriteria
                .Where(rct => rct.RubricTaskId == rt.RubricTaskId)
                .ToListAsync();
                rubricCriteria.AddRange(rubricCriteriaTemp);
            }
            
            //Get the related RubricCriteriaScore
            List<RubricCriteriaScore> rubricCriteriaScores = new List<RubricCriteriaScore>();
            foreach (RubricCriteria rc in rubricCriteria)
            {
                var rubricCriteriaScoreTemp = await _context.RubricCriteriaScore
                    .Where(rcst => rcst.RubricCriteriaId == rc.RubricCriteriaId)
                    .ToListAsync();
                rubricCriteriaScores.AddRange(rubricCriteriaScoreTemp);
            }

            // Create the ViewModel
            var viewModel = new RubricDetailsViewModel
            {
                Rubric = rubrics,
                RubricTasks = rubricTasks,
                RubricCriterias = rubricCriteria,
                RubricCriteriaScores = rubricCriteriaScores
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
        public async Task<IActionResult> Create([Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName, TermName,TotalMarks,SourceFile")] Rubrics rubrics)
        {
            rubrics.TotalMarks = 0; // Set default value for TotalMarks
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
        public async Task<IActionResult> Edit(int id, [Bind("RubricsId,RubricName,Institution,Programme,CourseCode,CourseName,TermName,TotalMarks,SourceFile")] Rubrics rubrics)
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

        // GET: Rubrics/EditTask/5
        public async Task<IActionResult> EditTask(int? id, int? rubricId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubricTask = await _context.RubricTask.FindAsync(id);
            if (rubricTask == null)
            {
                return NotFound();
            }

            ViewBag.RubricId = rubricId;

            return View(rubricTask);
        }

        // POST: Rubrics/EditTask/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> EditTask(int id, int rubricId, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            if (id != rubricTask.RubricTaskId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
                try
                {
                    _context.Update(rubricTask);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RubricTaskExists(rubricTask.RubricTaskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Rubrics", new { id = rubricId });
            //}
        }

        // GET: Rubrics/EditTask/5
        public async Task<IActionResult> EditCriteria(int? criteriaId, int? rubricId)
        {
            if (criteriaId == null)
            {
                return NotFound();
            }

            var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
            if (rubricCriteria == null)
            {
                return NotFound();
            }

            ViewBag.RubricId = rubricId;

            return View(rubricCriteria);
        }

        // POST: Rubrics/EditTask/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> EditCriteria(int criteriaId, int rubricId, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            if (criteriaId != rubricCriteria.RubricCriteriaId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            //{
            try
            {
                _context.Update(rubricCriteria);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RubricCriteriaExists(rubricCriteria.RubricCriteriaId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction("Details", "Rubrics", new { id = rubricId });
            //}
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

        // GET: Rubrics/DeleteTask/5
        public async Task<IActionResult> DeleteTask(int? id, int? rubricId)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rubricTask = await _context.RubricTask
                .FirstOrDefaultAsync(m => m.RubricTaskId == id);
            if (rubricTask == null)
            {
                return NotFound();
            }

            // Pass the rubricId to the view
            ViewBag.RubricId = rubricId;

            return View(rubricTask);
        }

        // POST: Rubrics/DeleteTask/5
        [HttpPost, ActionName("DeleteTask")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTaskConfirmed(int id, int rubricId)
        {
            var rubricTask = await _context.RubricTask.FindAsync(id);
            if (rubricTask != null)
            {
                _context.RubricTask.Remove(rubricTask);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Rubrics", new { id = rubricId });
        }

        // GET: Rubrics/DeleteCriteria/5
        public async Task<IActionResult> DeleteCriteria(int? criteriaId, int? rubricId)
        {
            if (criteriaId == null)
            {
                return NotFound();
            }

            var rubrics = await _context.RubricCriteria
                .FirstOrDefaultAsync(m => m.RubricCriteriaId == criteriaId);
            if (rubrics == null)
            {
                return NotFound();
            }

            // Pass the rubricId to the view
            ViewBag.RubricId = rubricId;

            return View(rubrics);
        }

        // POST: Rubrics/DeleteCriteria/5
        [HttpPost, ActionName("DeleteCriteria")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCriteriaConfirmed(int criteriaId, int rubricId)
        {
            var rubricCriteria = await _context.RubricCriteria.FindAsync(criteriaId);
            if (rubricCriteria != null)
            {
                _context.RubricCriteria.Remove(rubricCriteria);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Rubrics", new { id = rubricId });
        }

        private bool RubricsExists(int id)
        {
            return _context.Rubrics.Any(e => e.RubricsId == id);
        }

        private bool RubricTaskExists(int id)
        {
            return _context.RubricTask.Any(e => e.RubricTaskId == id);
        }

        private bool RubricCriteriaExists(int id)
        {
            return _context.RubricCriteria.Any(e => e.RubricCriteriaId == id);
        }

        // GET: Rubrics/Task/CreateTask
        public async Task<IActionResult> CreateTask(int? id)
        {
            ViewBag.RubricId = id;
            return View();
        }

        // Fix for CS1983: Change the return type of the method to Task<IActionResult> to match the requirement for async methods.
        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateTask(int id, [Bind("RubricTaskId,RubricsId,TaskTitle,TaskDescription,MaxMarks")] RubricTask rubricTask)
        {
            rubricTask.RubricsId = id;
            _context.Add(rubricTask);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Rubrics", new { id = rubricTask.RubricsId });
        }

        //CreateTaskCriteria
        // GET: Rubrics/CreateTaskCriteria
        public async Task<IActionResult> CreateCriteria(int? id, int? rubricsId)
        {
            ViewBag.RubricTaskId = id;
            ViewBag.RubricId = rubricsId;
            return View(new RubricCriteria());
        }

        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> CreateCriteria(int id, int rubricsId, [Bind("RubricCriteriaId,RubricTaskId,CriterionTitle,Weight,MaxScore")] RubricCriteria rubricCriteria)
        {
            rubricCriteria.RubricTaskId = id;
            _context.Add(rubricCriteria);
            await _context.SaveChangesAsync();

            //save RubricTaskScores with value from text area ScoreDescription_
            for (int score = rubricCriteria.MaxScore; score >= 0; score--)
            {
                string formFieldName = "ScoreDescription_" + score;
                string scoreDescription = Request.Form[formFieldName];
                var rubricCriteriaScore = new RubricCriteriaScore
                {
                    RubricCriteriaId = rubricCriteria.RubricCriteriaId,
                    CriterionScore = score,
                    ScoreDescription = scoreDescription
                };
                _context.RubricCriteriaScore.Add(rubricCriteriaScore);
                await _context.SaveChangesAsync();
            }


            return RedirectToAction("Details", "Rubrics", new { id = rubricsId });
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
                List<RubricCriteria> rubricCriterias = new List<RubricCriteria>();
                List<RubricCriteriaScore> rubricCriteriaScores = new List<RubricCriteriaScore>();
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
                        int taskIndex = 0;
                        bool isTableRead = false;
                        foreach (var table in docx.Tables)
                        {
                            isTableRead = false;
                            // Extract rubric criteria from ONLY the first table
                            if (docx.Tables.Count > 0)
                            {
                                if (tableIndex == 0)
                                {
                                    //Fetch rubrics tasks from the first table
                                    var rows = table.Rows;

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

                                            isTableRead = true;
                                        }
                                    }
                                }
                                else
                                {
                                    //Fetch rubrics tasks criterias from the remaining table
                                    var rows = table.Rows;

                                    // Skip header row (index 0) and process data rows
                                    for (int i = 1; i < rows.Count; i++)
                                    {
                                        var row = rows[i];
                                        var cells = row.GetTableCells();

                                        if (cells.Count >= 4) // Ensure we have at least 4 columns
                                        {
                                            int maxScore = cells.Count - 3;

                                            var rubricCriteria = new RubricCriteria
                                            {
                                                RubricTaskId = tableIndex - 1,      // Link to the correct RubricTask (temp value=0,1,2,3,...)
                                                CriterionTitle = GetCellText(cells[0]),
                                                Weight = double.TryParse(GetCellText(cells[1]).TrimEnd('%'), out double weight) ? weight : 0,
                                                MaxScore = maxScore
                                            };
                                            rubricCriterias.Add(rubricCriteria);

                                            // Extract scores for this criterion
                                            for (int j = 0; j <= maxScore; j++)
                                            {
                                                var rubricCriteriaScore = new RubricCriteriaScore
                                                {
                                                    RubricCriteriaId = taskIndex,       // Link to the correct RubricCriteria (temp value=0,1,2,3,...)
                                                    CriterionScore = maxScore - j,
                                                    ScoreDescription = GetCellText(cells[j + 2])
                                                };
                                                rubricCriteriaScores.Add(rubricCriteriaScore);
                                            }
                                            taskIndex++;
                                            isTableRead = true;
                                        }                                        
                                    }
                                }
                                string fullText = rubricsParagraphs.Count > 1 ? rubricsParagraphs[1] : "";
                                int firstSpaceIndex = fullText.IndexOf(' ');

                                rubric.Institution = "Auckland Institute of Studies";
                                rubric.Programme = rubricsParagraphs[0];
                                rubric.CourseCode = firstSpaceIndex > 0 ? fullText.Substring(0, firstSpaceIndex) : fullText;
                                rubric.CourseName = firstSpaceIndex > 0 ? fullText.Substring(firstSpaceIndex + 1).Trim() : "";
                                rubric.RubricName = rubricsParagraphs.Count > 2 ? rubricsParagraphs[2] : "";
                                rubric.TermName = rubricsParagraphs[3];
                                rubric.TotalMarks = rubricTasks.Sum(t => t.MaxMarks);
                                rubric.SourceFile = filePath;

                                if (isTableRead)
                                {
                                    tableIndex++;
                                }
                            }
                            
                        }

                        // Save rubric first to get the RubricsId
                        _context.Add(rubric);
                        await _context.SaveChangesAsync();

                        // Save rubric tasks with the rubric ID
                        foreach (var task in rubricTasks)
                        {
                            task.RubricsId = rubric.RubricsId;
                            _context.RubricTask.Add(task);
                        }
                        await _context.SaveChangesAsync();

                        // Save rubric criterias with the correct RubricTaskId
                        foreach (var criteria in rubricCriterias)
                        {
                            var correspondingTask = rubricTasks[criteria.RubricTaskId];
                            criteria.RubricTaskId = correspondingTask.RubricTaskId;
                            _context.RubricCriteria.Add(criteria);
                        }
                        await _context.SaveChangesAsync();

                        // Save rubric criteria scores with the correct RubricCriteriaId
                        foreach (var score in rubricCriteriaScores)
                        {
                            var correspondingCriteria = rubricCriterias[score.RubricCriteriaId];
                            score.RubricCriteriaId = correspondingCriteria.RubricCriteriaId;
                            _context.RubricCriteriaScore.Add(score);
                        }
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Your rubrics has been submitted successfully! {rubricTasks.Count} tasks extracted.";
                    }
                }
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
