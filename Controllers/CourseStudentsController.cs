using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using smart_feedback.Data;
using smart_feedback.Models;
using smart_feedback.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace smart_feedback.Controllers
{
    [Authorize]
    public class CourseStudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseStudentsController> _logger;

        public CourseStudentsController(ApplicationDbContext context, ILogger<CourseStudentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CourseStudents/Manage
        public async Task<IActionResult> Manage(int courseId, string role)
        {
            _logger.LogInformation("Manage students called for courseId: {CourseId}, role: {Role}", courseId, role);

            var course = await _context.CourseRoles.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Course not found with ID: {CourseId}", courseId);
                TempData["ErrorMessage"] = "Course not found.";
                return RedirectToAction("Index", "Home");
            }

            // Get all students in the system
            var allStudents = await _context.Student
                .OrderBy(s => s.StudentId)
                .ToListAsync();

            // Get students enrolled in this course
            var enrolledStudents = await _context.CourseStudent
                .Where(cs => cs.CourseRolesId == courseId)
                .Include(cs => cs.Student)
                .OrderBy(cs => cs.Student.StudentId)
                .Select(cs => new StudentEnrollmentInfo
                {
                    CourseStudentId = cs.CourseStudentId,
                    StudentId = cs.StudentId,
                    StudentIdNumber = cs.Student.StudentId,
                    Name = cs.Student.Name,
                    Email = cs.Student.Email,
                    EnrolledDate = cs.EnrolledDate
                })
                .ToListAsync();

            var enrolledStudentIds = enrolledStudents.Select(es => es.StudentId).ToHashSet();

            // Get available students (not enrolled yet)
            var availableStudents = allStudents
                .Where(s => !enrolledStudentIds.Contains(s.Id))
                .ToList();

            var viewModel = new CourseStudentManagementViewModel
            {
                CourseRolesId = courseId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                TermName = course.TermName,
                Programme = course.Programme,
                Role = role,
                AllStudents = allStudents,
                EnrolledStudents = enrolledStudents,
                AvailableStudents = availableStudents
            };

            _logger.LogInformation("Found {TotalStudents} total students, {EnrolledCount} enrolled, {AvailableCount} available",
                allStudents.Count, enrolledStudents.Count, availableStudents.Count);

            return View(viewModel);
        }

        // POST: CourseStudents/AddStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(int courseId, int studentId, string role)
        {
            try
            {
                _logger.LogInformation("Adding student {StudentId} to course {CourseId}", studentId, courseId);

                // Check if student is already enrolled
                var existingEnrollment = await _context.CourseStudent
                    .FirstOrDefaultAsync(cs => cs.CourseRolesId == courseId && cs.StudentId == studentId);

                if (existingEnrollment != null)
                {
                    _logger.LogWarning("Student {StudentId} already enrolled in course {CourseId}", studentId, courseId);
                    TempData["ErrorMessage"] = "Student is already enrolled in this course.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var courseStudent = new CourseStudent
                {
                    CourseRolesId = courseId,
                    StudentId = studentId,
                    EnrolledDate = DateTime.Now
                };

                _context.CourseStudent.Add(courseStudent);
                await _context.SaveChangesAsync();

                var student = await _context.Student.FindAsync(studentId);
                _logger.LogInformation("Successfully enrolled student {StudentName} (ID: {StudentId}) in course {CourseId}",
                    student?.Name, studentId, courseId);

                TempData["SuccessMessage"] = $"Student {student?.Name} successfully enrolled.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // POST: CourseStudents/AddMultipleStudents
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMultipleStudents(int courseId, string role, string studentIds)
        {
            try
            {
                _logger.LogInformation("Adding students to course {CourseId}, studentIds raw: {StudentIds}", courseId, studentIds);

                if (string.IsNullOrEmpty(studentIds))
                {
                    TempData["ErrorMessage"] = "No students selected.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                // Parse the comma-separated string
                var studentIdList = studentIds.Split(',')
                    .Where(id => int.TryParse(id.Trim(), out _))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();

                if (!studentIdList.Any())
                {
                    TempData["ErrorMessage"] = "Invalid student IDs provided.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                _logger.LogInformation("Parsed {Count} student IDs", studentIdList.Count);

                // Get existing enrollments
                var existingEnrollments = await _context.CourseStudent
                    .Where(cs => cs.CourseRolesId == courseId && studentIdList.Contains(cs.StudentId))
                    .Select(cs => cs.StudentId)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} existing enrollments", existingEnrollments.Count);

                // Filter out already enrolled students
                var newStudentIds = studentIdList.Except(existingEnrollments).ToList();

                if (!newStudentIds.Any())
                {
                    _logger.LogWarning("All selected students already enrolled in course {CourseId}", courseId);
                    TempData["ErrorMessage"] = "All selected students are already enrolled.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var newEnrollments = newStudentIds.Select(studentId => new CourseStudent
                {
                    CourseRolesId = courseId,
                    StudentId = studentId,
                    EnrolledDate = DateTime.Now
                }).ToList();

                _context.CourseStudent.AddRange(newEnrollments);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully enrolled {Count} students in course {CourseId}", newEnrollments.Count, courseId);

                var message = $"Successfully enrolled {newEnrollments.Count} student(s).";
                if (existingEnrollments.Any())
                {
                    message += $" {existingEnrollments.Count} student(s) were already enrolled.";
                }

                TempData["SuccessMessage"] = message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding multiple students to course {CourseId}", courseId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }

        // POST: CourseStudents/RemoveStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int courseStudentId, int courseId, string role)
        {
            try
            {
                _logger.LogInformation("Removing enrollment ID: {CourseStudentId}", courseStudentId);

                var courseStudent = await _context.CourseStudent
                    .Include(cs => cs.Student)
                    .FirstOrDefaultAsync(cs => cs.CourseStudentId == courseStudentId);

                if (courseStudent == null)
                {
                    _logger.LogWarning("Enrollment not found with ID: {CourseStudentId}", courseStudentId);
                    TempData["ErrorMessage"] = "Enrollment not found.";
                    return RedirectToAction("Manage", new { courseId, role });
                }

                var studentName = courseStudent.Student?.Name;

                _context.CourseStudent.Remove(courseStudent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully removed student {StudentName} from course {CourseId}",
                    studentName, courseId);

                TempData["SuccessMessage"] = $"Student {studentName} successfully removed from course.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing enrollment ID: {CourseStudentId}", courseStudentId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Manage", new { courseId, role });
        }
    }
}