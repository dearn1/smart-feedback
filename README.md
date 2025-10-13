# smart-feedback

## Smart Assessment Feedback System: Automating Student Evaluation
This project aims to develop a smart assessment support system that uses marking rubrics to automatically generate constructive comments for students based on their performance. The system allows educators to upload, create, and modify marking rubrics (in Word format or via a built-in editor), and then use these rubrics to assess students. Based on the selected criteria and scores (on a 0–4 scale), the system generates personalized, constructive feedback for each student and compiles it into a downloadable PDF report.

## Key Features:
**1. Rubric Management**
  - Upload Existing Rubrics: Accept Word (.docx) files containing rubric criteria and descriptions.
  - Create New Rubrics: Use a built-in editor to define criteria, performance levels (0–4), and associated comments.
  - Edit Existing Rubrics: Modify uploaded or previously created rubrics through the interface.
  - Rubric Structure: Each criterion includes:
  - Criterion title
  - Descriptions for each score level (0 to 4)
  - Optional predefined feedback comments
    
**2. Student Assessment and Feedback Generation**
  - Student Data Input: Upload or enter student records (name, ID, etc.).
  - Marking Interface: Select scores (0–4) for each criterion per student.
  - Feedback Engine: Automatically generate constructive comments based on:
  - Selected scores
  - Corresponding rubric descriptions
  - Optional comment templates

**3. PDF Report Generation**
  - Comprehensive Report: Generate a PDF report for each student that includes:
  - Student details (name, ID)
  - Marking breakdown by criterion
  - Total score
  - Overall constructive feedback
  - Batch Export: Option to generate and download reports for all students in bulk.

**4. Data Management**
  - Student Data Upload: Bulk upload of student data via Excel or CSV.
  - Scenario Coverage: System must support:
  - Full marks
  - Partial marks
  - Zero marks
  - Mixed performance across criteria
    
**5. User Authentication and Security**
  - User Roles: Admin, Lecturer, Moderator
  - Authentication: Secure login with ASP.NET Identity
  - Data Protection: Encryption of student data
  - Audit Logs: Track rubric changes, assessments, and feedback generation
    
**6. User Interface**
  - Dashboard: For managing rubrics, students, and assessments
  - Feedback Preview: View generated comments before finalizing

## Technology Stack (C#/.NET):
  - Frontend: ASP.NET Core MVC
  - Backend: .NET 8 (C#)
  - Document Handling:
  - Word, Excel, PDF
  - Feedback Generation: Rule-based engine with optional NLP (ML.NET)
  - Authentication: ASP.NET Identity Framework
  - Database: SQL Server
  - File Storage: Local File System
  - UI Template: AdminLTE3
  - PDF Viewer: Preview PDF reports before download
  - Responsive Design: Accessible on desktop and mobile devices
