namespace smart_feedback.Models.ViewModels
{
    public class StudentAssessmentViewModel
    {
        public int CourseRolesId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public string Role { get; set; }
        public List<Assessment> Assessments { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<Rubrics> AvailableRubrics { get; set; } = new();
        public Dictionary<int, List<int>> AssessmentMarkedStudents { get; set; } = new Dictionary<int, List<int>>();
        public Dictionary<int, int> AssessmentMarkingProgress { get; set; } = new Dictionary<int, int>();
    }

    public class AssessmentMarkingViewModel
    {
        public Assessment Assessment { get; set; }
        public List<Student> Students { get; set; } = new();
        public List<RubricTask> RubricTasks { get; set; } = new();
        public List<RubricCriteria> RubricCriterias { get; set; } = new();
        public List<RubricCriteriaScore> CriteriaScores { get; set; } = new();
        public Dictionary<int, Dictionary<int, StudentAssessmentScore>> StudentScores { get; set; } = new();
        public string CourseCode { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
        public int CourseRolesId { get; set; }
        public string Role { get; set; }

        // Pagination properties
        public int CurrentStudentIndex { get; set; }
        public int TotalStudents { get; set; }
        public List<Student> AllStudents { get; set; } = new List<Student>();
    }

    public class StudentFeedbackViewModel
    {
        public Student Student { get; set; }
        public Assessment Assessment { get; set; }
        public List<StudentCriteriaResult> CriteriaResults { get; set; } = new();
        public Dictionary<string, TaskScoreSummary> TaskSummaries { get; set; } = new();
        public int TotalScore { get; set; }
        public int MaxPossibleScore { get; set; }
        public double Percentage { get; set; }
        public string OverallFeedback { get; set; }
        
        // NEW: Batch mode properties
        public bool IsBatchMode { get; set; }
        public int CurrentStudentIndex { get; set; }
        public int TotalStudents { get; set; }
        public List<Student> AllStudents { get; set; } = new();
        public List<StudentFeedbackViewModel> AllStudentFeedbacks { get; set; } = new();
        public int CourseRolesId { get; set; }
        public string Role { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int Year { get; set; }
        public int Trimester { get; set; }
    }

    public class TaskScoreSummary
    {
        public string TaskTitle { get; set; }

        public string TaskDescription { get; set; }
        public double TotalWeightedScore { get; set; }
        public double MaxWeightedScore { get; set; }
        public double Percentage { get; set; }
        public int MaxMarks { get; set; }
        public double ActualMarks { get; set; }
    }

    public class StudentCriteriaResult
    {
        public string TaskTitle { get; set; }
        public string CriteriaTitle { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public double Weight { get; set; }
        public double WeightedScore { get; set; }
        public double MaxWeightedScore { get; set; }
        public string ScoreDescription { get; set; }
        public string GeneratedFeedback { get; set; }
        public string CustomComment { get; set; }
    }
}
