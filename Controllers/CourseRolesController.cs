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
using Microsoft.Extensions.Options;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.Configuration;

namespace smart_feedback.Controllers
{
    public class CourseRolesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseRolesController> _logger;
        private readonly ApplicationSettings _appSettings;


        public CourseRolesController(ApplicationDbContext context, ILogger<CourseRolesController> logger, IOptions<ApplicationSettings> appSettings)
        {
            _context = context;
            _logger = logger;
            _appSettings = appSettings.Value;
        }

        // GET: CourseRoles
        public async Task<IActionResult> Index(string sortOrder, string termName, string programme)
        {
            try
            {
                _logger.LogInformation("CourseRoles Index called with filters - TermName: {TermName}, Programme: {Programme}, SortOrder: {SortOrder}",
                    termName, programme, sortOrder);

                // Set up ViewData for sorting links
                ViewData["CurrentSort"] = sortOrder;
                ViewData["CourseCodeSortParm"] = string.IsNullOrEmpty(sortOrder) ? "courseCode_desc" : "";
                ViewData["CourseNameSortParm"] = sortOrder == "courseName" ? "courseName_desc" : "courseName";
                ViewData["TermNameSortParm"] = sortOrder == "termName" ? "termName_desc" : "termName";
                ViewData["ProgrammeSortParm"] = sortOrder == "programme" ? "programme_desc" : "programme";

                // Set up ViewData for current filter values
                ViewData["CurrentTermFilter"] = termName;
                ViewData["CurrentProgrammeFilter"] = programme;

                // Prepare programme dropdown options
                ViewBag.ProgrammeOptions = _appSettings.GetProgrammeSelectList(programme);

                // Start with all course roles
                var courseRolesQuery = _context.CourseRoles.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(termName))
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.TermName.Contains(termName));
                    _logger.LogDebug("Applied term name filter: {TermName}", termName);
                }

                if (!string.IsNullOrEmpty(programme))
                {
                    courseRolesQuery = courseRolesQuery.Where(cr => cr.Programme.Equals(programme));
                    _logger.LogDebug("Applied programme filter: {Programme}", programme);
                }

                // Apply sorting
                switch (sortOrder)
                {
                    case "courseCode_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseCode);
                        break;
                    case "courseName":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseName);
                        break;
                    case "courseName_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.CourseName);
                        break;
                    case "termName":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.TermName);
                        break;
                    case "termName_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.TermName);
                        break;
                    case "programme":
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.Programme);
                        break;
                    case "programme_desc":
                        courseRolesQuery = courseRolesQuery.OrderByDescending(cr => cr.Programme);
                        break;
                    default:
                        courseRolesQuery = courseRolesQuery.OrderBy(cr => cr.CourseCode);
                        break;
                }

                var courseRoles = await courseRolesQuery.ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} course roles (filtered: Term={HasTermFilter}, Programme={HasProgrammeFilter})",
                    courseRoles.Count, !string.IsNullOrEmpty(termName), !string.IsNullOrEmpty(programme));

                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course roles with filters - TermName: {TermName}, Programme: {Programme}",
                    termName, programme);
                throw;
            }
        }


        // GET: CourseRoles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role details requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles
                    .FirstOrDefaultAsync(m => m.CourseRolesId == id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role details for ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role details for ID: {Id}", id);
                throw;
            }
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
                try
                {
                    _context.Add(courseRoles);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created course role with ID: {Id}, Course: {CourseCode}, Institution: {Institution}",
                        courseRoles.CourseRolesId, courseRoles.CourseCode, courseRoles.Institution);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while creating course role for course: {CourseCode}", courseRoles?.CourseCode);
                    throw;
                }
            }
            
            _logger.LogWarning("Course role creation failed - ModelState is invalid. Course: {CourseCode}, Errors: {Errors}",
                courseRoles?.CourseCode,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                        
            return View(courseRoles);
        }

        // GET: CourseRoles/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role edit requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles.FindAsync(id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for editing", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role for editing - ID: {Id}, Course: {CourseCode}",
                    id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role for editing - ID: {Id}", id);
                throw;
            }
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
                _logger.LogWarning("Course role edit failed - ID mismatch. URL ID: {UrlId}, Model ID: {ModelId}",
                    id, courseRoles.CourseRolesId);
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(courseRoles);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully updated course role - ID: {Id}, Course: {CourseCode}, Institution: {Institution}",
                        id, courseRoles.CourseCode, courseRoles.Institution);

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    if (!CourseRolesExists(courseRoles.CourseRolesId))
                    {
                        _logger.LogWarning("Course role with ID {Id} no longer exists during update", courseRoles.CourseRolesId);
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError(ex, "Concurrency error occurred while updating course role - ID: {Id}", id);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating course role - ID: {Id}", id);
                    throw;
                }
            }

            _logger.LogWarning("Course role update failed - ModelState is invalid. ID: {Id}, Course: {CourseCode}, Errors: {Errors}",
                id, courseRoles?.CourseCode,
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return View(courseRoles);
        }

        // GET: CourseRoles/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Course role delete requested with null ID");
                return NotFound();
            }

            try
            {
                var courseRoles = await _context.CourseRoles
                    .FirstOrDefaultAsync(m => m.CourseRolesId == id);
                if (courseRoles == null)
                {
                    _logger.LogWarning("Course role with ID {Id} not found for deletion", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully retrieved course role for deletion confirmation - ID: {Id}, Course: {CourseCode}",
                        id, courseRoles.CourseCode);
                return View(courseRoles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving course role for deletion - ID: {Id}", id);
                throw;
            }
        }

        // POST: CourseRoles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string courseId, string role)
        {
            try
            {
                var rubrics = await _context.Rubrics.FindAsync(id);
                if (rubrics != null)
                {
                    // Check if rubric is being used in any assessments
                    var assessmentsUsingRubric = await _context.Assessments
                        .Where(a => a.RubricsId == id)
                        .ToListAsync();

                    if (assessmentsUsingRubric.Any())
                    {
                        _logger.LogWarning("Cannot delete rubric {RubricName} (ID: {Id}) - used in {Count} assessments",
                            rubrics.RubricName, id, assessmentsUsingRubric.Count);
                        
                        TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it is being used in {assessmentsUsingRubric.Count} assessment(s). Please delete or reassign the assessments first.";
                        return RedirectToAction("Index", "Rubrics", new { courseId, role });
                    }

                    // Get all tasks for this rubric
                    var rubricTasks = await _context.RubricTask
                        .Where(rt => rt.RubricsId == id)
                        .ToListAsync();

                    // Get all criteria for these tasks
                    var taskIds = rubricTasks.Select(rt => rt.RubricTaskId).ToList();
                    var rubricCriterias = await _context.RubricCriteria
                        .Where(rc => taskIds.Contains(rc.RubricTaskId))
                        .ToListAsync();

                    // Get all scores for these criteria
                    var criteriaIds = rubricCriterias.Select(rc => rc.RubricCriteriaId).ToList();
                    var rubricCriteriaScores = await _context.RubricCriteriaScore
                        .Where(rcs => criteriaIds.Contains(rcs.RubricCriteriaId))
                        .ToListAsync();

                    // Check if any criteria are being used in student assessments
                    var studentScoresUsingCriteria = await _context.StudentAssessmentScores
                        .Where(sas => criteriaIds.Contains(sas.RubricCriteriaId))
                        .ToListAsync();

                    if (studentScoresUsingCriteria.Any())
                    {
                        _logger.LogWarning("Cannot delete rubric {RubricName} (ID: {Id}) - criteria used in {Count} student assessments",
                            rubrics.RubricName, id, studentScoresUsingCriteria.Count);
                        
                        TempData["ErrorMessage"] = $"Cannot delete rubric '{rubrics.RubricName}' because it contains criteria that are being used in student assessments. Please delete the related assessments first.";
                        return RedirectToAction("Index", "Rubrics", new { courseId, role });
                    }

                    // Delete in the correct order to maintain referential integrity
                    _logger.LogInformation("Beginning cascade deletion for rubric {RubricName} (ID: {Id})", rubrics.RubricName, id);

                    // 1. Delete rubric criteria scores first
                    if (rubricCriteriaScores.Any())
                    {
                        _context.RubricCriteriaScore.RemoveRange(rubricCriteriaScores);
                        _logger.LogInformation("Marked {Count} rubric criteria scores for deletion", rubricCriteriaScores.Count);
                    }

                    // 2. Delete rubric criteria
                    if (rubricCriterias.Any())
                    {
                        _context.RubricCriteria.RemoveRange(rubricCriterias);
                        _logger.LogInformation("Marked {Count} rubric criteria for deletion", rubricCriterias.Count);
                    }

                    // 3. Delete rubric tasks
                    if (rubricTasks.Any())
                    {
                        _context.RubricTask.RemoveRange(rubricTasks);
                        _logger.LogInformation("Marked {Count} rubric tasks for deletion", rubricTasks.Count);
                    }

                    // 4. Finally delete the rubric itself
                    _context.Rubrics.Remove(rubrics);
                    _logger.LogInformation("Marked rubric {RubricName} for deletion", rubrics.RubricName);

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted rubric {RubricName} (ID: {Id}) and all related data",
                        rubrics.RubricName, id);

                    TempData["SuccessMessage"] = $"Rubric '{rubrics.RubricName}' and all its related data have been successfully deleted.";
                }
                else
                {
                    _logger.LogWarning("Rubric with ID {Id} not found for deletion", id);
                    TempData["ErrorMessage"] = "Rubric not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting rubric with ID: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the rubric. Please try again.";
            }

            return RedirectToAction("Index", "Rubrics", new { courseId, role });
        }

        private bool CourseRolesExists(int id)
        {
            try
            {
                var exists = _context.CourseRoles.Any(e => e.CourseRolesId == id);
                _logger.LogDebug("Course role exists check for ID {Id}: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if course role exists for ID: {Id}", id);
                return false;
            }
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
                _logger.LogWarning("CSV upload failed - no file selected or file is empty");
                ViewBag.Message = "Please select a CSV file.";
                return View();
            }

            var recordsProcessed = 0;
            var recordsAdded = 0;
            var errors = new List<string>();

            try
            {
                using (var stream = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8))
                using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                {
                    csv.Read();
                    csv.ReadHeader();
                    _logger.LogInformation("CSV file opened successfully, headers read");

                    while (csv.Read())
                    {
                        recordsProcessed++;
                        try
                        {
                            var courseRole = new CourseRoles
                            {
                                CourseCode = csv.GetField<string>("CourseCode"),
                                CourseName = csv.GetField<string>("CourseName"),
                                TermName = csv.GetField<string>("TermName"),
                                Programme = csv.GetField<string>("Programme"),
                                Institution = csv.GetField<string>("Institution"),
                                RoleLecturer = csv.GetField<string>("RoleLecturer"),
                                RoleModerator = csv.GetField<string>("RoleModerator")
                            };

                            _context.CourseRoles.Add(courseRole);
                            recordsAdded++;

                            _logger.LogDebug("Processed CSV record {RecordNumber}: Course {CourseCode}",
                                recordsProcessed, courseRole.CourseCode);
                        }
                        catch (Exception ex)
                        {
                            var error = $"Error processing record {recordsProcessed}: {ex.Message}";
                            errors.Add(error);
                            _logger.LogWarning(ex, "Error processing CSV record {RecordNumber}", recordsProcessed);
                        }
                    }

                    if (recordsAdded > 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("CSV upload completed successfully. Records processed: {Processed}, Records added: {Added}, Errors: {ErrorCount}",
                            recordsProcessed, recordsAdded, errors.Count);
                    }
                }

                if (errors.Any())
                {
                    ViewBag.Message = $"CSV upload completed with {errors.Count} errors. {recordsAdded} records added successfully.";
                    ViewBag.Errors = errors;
                }
                else
                {
                    ViewBag.Message = $"CSV uploaded successfully. {recordsAdded} records added.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during CSV upload. File: {FileName}", csvFile.FileName);
                ViewBag.Message = "An error occurred while processing the CSV file. Please check the format and try again.";
            }

            return View();
        }
    }
}
