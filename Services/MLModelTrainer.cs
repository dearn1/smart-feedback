using Microsoft.ML;
using smart_feedback.Models.ML;
using smart_feedback.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace smart_feedback.Services
{
    public class MLModelTrainer
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly MLContext _mlContext;

        public MLModelTrainer(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            _mlContext = new MLContext(seed: 0);
        }

        public async Task<bool> PrepareTrainingDataFromDatabase()
        {
            try
            {
                // Extract historical feedback data from your database
                //var historicalFeedback = await _context.StudentAssessmentScores
                //    .Include(s => s.RubricCriteria)
                //        .ThenInclude(rc => rc.RubricTask)
                //    .Include(s => s.RubricCriteriaScoreNavigation)
                //    .Where(s => !string.IsNullOrEmpty(s.CustomComment))
                //    .Select(s => new FeedbackData
                //    {
                //        CriteriaTitle = s.RubricCriteria.CriterionTitle,
                //        Score = s.Score,
                //        MaxScore = s.RubricCriteria.MaxScore,
                //        PercentageScore = (float)s.Score / s.RubricCriteria.MaxScore * 100,
                //        ScoreDescription = s.RubricCriteriaScoreNavigation.ScoreDescription ?? "",
                //        ScoreTitle = s.RubricCriteriaScoreNavigation.ScoreTitle ?? "",
                //        TaskTitle = s.RubricCriteria.RubricTask.TaskTitle ?? "",
                //        CustomComment = s.CustomComment ?? "",
                //        GeneratedFeedback = s.CustomComment ?? "" // Use actual feedback as training label
                //    })
                //    .ToListAsync();

                var historicalFeedback = new List<FeedbackData>(); // Assume no data for demonstration

                if (!historicalFeedback.Any())
                {
                    // Use sample data if no historical data exists
                    historicalFeedback = GenerateSampleTrainingData();
                }

                // Save to CSV for training
                var mlDirectory = Path.Combine(_environment.ContentRootPath, "ML");
                Directory.CreateDirectory(mlDirectory);
                
                var feedbackPath = Path.Combine(mlDirectory, "feedback_training_data.csv");
                await SaveToCsvAsync(feedbackPath, historicalFeedback);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error preparing training data: {ex.Message}");
                return false;
            }
        }

        private List<FeedbackData> GenerateSampleTrainingData()
        {
            return new List<FeedbackData>
            {
                // Excellent Performance (90-100%)
                new() { 
                    CriteriaTitle = "Code Quality", 
                    Score = 4, MaxScore = 4, PercentageScore = 100,
                    ScoreTitle = "Excellent", 
                    ScoreDescription = "Code is well-structured, efficient, and follows best practices",
                    TaskTitle = "Programming Assignment", 
                    GeneratedFeedback = "Outstanding code quality! Your implementation demonstrates excellent understanding of software design principles. The code is clean, well-documented, and efficiently structured. You've shown mastery of best practices including proper naming conventions, modularity, and error handling. Keep up this exceptional standard!"
                },
                new() { 
                    CriteriaTitle = "Problem Solving", 
                    Score = 4, MaxScore = 4, PercentageScore = 100,
                    ScoreTitle = "Excellent", 
                    ScoreDescription = "Demonstrates exceptional analytical and problem-solving skills",
                    TaskTitle = "Analysis Task", 
                    GeneratedFeedback = "Exceptional problem-solving approach! You've demonstrated strong analytical thinking by breaking down complex problems into manageable components. Your solution is both elegant and efficient, showing deep understanding of the underlying concepts. Consider sharing your methodology with peers."
                },

                // Good Performance (75-89%)
                new() { 
                    CriteriaTitle = "Documentation", 
                    Score = 3, MaxScore = 4, PercentageScore = 75,
                    ScoreTitle = "Good", 
                    ScoreDescription = "Documentation is clear but could include more detailed examples",
                    TaskTitle = "Technical Report", 
                    GeneratedFeedback = "Good documentation overall! Your explanations are clear and well-organized. To reach the next level, consider adding more detailed code examples and edge case scenarios. Include visual diagrams where appropriate to enhance understanding. You're on the right track!"
                },
                new() { 
                    CriteriaTitle = "Testing", 
                    Score = 3, MaxScore = 4, PercentageScore = 75,
                    ScoreTitle = "Good", 
                    ScoreDescription = "Good test coverage with room for improvement in edge cases",
                    TaskTitle = "Unit Testing", 
                    GeneratedFeedback = "Good testing approach! You've covered the main functionality well. To improve, focus on testing edge cases, error conditions, and boundary values. Consider implementing integration tests alongside your unit tests. Your foundation is solid."
                },

                // Satisfactory Performance (50-74%)
                new() { 
                    CriteriaTitle = "Algorithm Design", 
                    Score = 2, MaxScore = 4, PercentageScore = 50,
                    ScoreTitle = "Satisfactory", 
                    ScoreDescription = "Algorithm works but lacks efficiency optimization",
                    TaskTitle = "Algorithm Implementation", 
                    GeneratedFeedback = "Your algorithm produces correct results, which is a good foundation. However, there's significant room for optimization. Review time and space complexity concepts, and consider using more efficient data structures. Practice analyzing algorithm performance to identify bottlenecks. Focus on these areas in your next assignment."
                },
                new() { 
                    CriteriaTitle = "User Interface Design", 
                    Score = 2, MaxScore = 4, PercentageScore = 50,
                    ScoreTitle = "Satisfactory", 
                    ScoreDescription = "Basic UI functionality present but lacks polish and usability features",
                    TaskTitle = "UI Development", 
                    GeneratedFeedback = "You've implemented the basic UI requirements successfully. To improve, focus on user experience principles such as intuitive navigation, consistent styling, and responsive design. Consider accessibility standards and add proper error messaging. Review UI/UX best practices and apply them to your next project."
                },

                // Needs Improvement (25-49%)
                new() { 
                    CriteriaTitle = "Error Handling", 
                    Score = 1, MaxScore = 4, PercentageScore = 25,
                    ScoreTitle = "Needs Improvement", 
                    ScoreDescription = "Minimal error handling, application crashes on invalid input",
                    TaskTitle = "Exception Management", 
                    GeneratedFeedback = "Error handling needs significant development. Your application should gracefully handle invalid inputs and unexpected conditions. Study try-catch blocks, input validation, and defensive programming techniques. Implement comprehensive error handling in your next iteration. Consider attending the upcoming workshop on defensive programming."
                },
                new() { 
                    CriteriaTitle = "Code Comments", 
                    Score = 1, MaxScore = 4, PercentageScore = 25,
                    ScoreTitle = "Needs Improvement", 
                    ScoreDescription = "Very few comments, code purpose unclear",
                    TaskTitle = "Code Documentation", 
                    GeneratedFeedback = "Your code lacks sufficient comments and documentation. This makes it difficult for others (and your future self) to understand the logic. Practice adding meaningful comments that explain the 'why' not just the 'what'. Review documentation standards for your programming language. Schedule office hours to review proper commenting techniques."
                },

                // Requires Development (0-24%)
                new() { 
                    CriteriaTitle = "Requirements Implementation", 
                    Score = 0, MaxScore = 4, PercentageScore = 0,
                    ScoreTitle = "Not Demonstrated", 
                    ScoreDescription = "Most requirements not met, significant gaps in implementation",
                    TaskTitle = "Feature Development", 
                    GeneratedFeedback = "This area requires substantial attention. Many core requirements were not implemented. Let's schedule a meeting to discuss the project requirements in detail and create an action plan. Break down the requirements into smaller, manageable tasks. Don't hesitate to ask questions early and often. I'm here to support your learning journey."
                },

                // Additional diverse examples
                new() { 
                    CriteriaTitle = "Database Design", 
                    Score = 4, MaxScore = 4, PercentageScore = 100,
                    ScoreTitle = "Excellent", 
                    ScoreDescription = "Well-normalized database with proper relationships and constraints",
                    TaskTitle = "Database Schema", 
                    GeneratedFeedback = "Excellent database design! Your schema is well-normalized, with appropriate relationships, constraints, and indexes. You've clearly understood relational database principles. The choice of data types is optimal, and you've implemented proper referential integrity. This is professional-level work."
                },
                new() { 
                    CriteriaTitle = "Security Implementation", 
                    Score = 3, MaxScore = 4, PercentageScore = 75,
                    ScoreTitle = "Good", 
                    ScoreDescription = "Basic security measures in place, some advanced features missing",
                    TaskTitle = "Security Features", 
                    GeneratedFeedback = "Good security implementation! You've covered the essential security measures including authentication and basic authorization. To enhance this further, consider implementing input sanitization, CSRF protection, and security headers. Review OWASP Top 10 vulnerabilities and ensure your application addresses each one."
                },
                new() { 
                    CriteriaTitle = "API Design", 
                    Score = 2, MaxScore = 4, PercentageScore = 50,
                    ScoreTitle = "Satisfactory", 
                    ScoreDescription = "API endpoints work but don't follow REST conventions",
                    TaskTitle = "REST API Development", 
                    GeneratedFeedback = "Your API endpoints are functional, which is a good start. However, they don't follow RESTful conventions consistently. Study REST principles including proper HTTP methods, status codes, and resource naming. Refactor your endpoints to align with industry standards. This will make your API more intuitive and maintainable."
                },
                new() { 
                    CriteriaTitle = "Performance Optimization", 
                    Score = 1, MaxScore = 4, PercentageScore = 25,
                    ScoreTitle = "Needs Improvement", 
                    ScoreDescription = "Application is slow, no optimization attempts visible",
                    TaskTitle = "Performance Task", 
                    GeneratedFeedback = "Performance needs significant attention. The application experiences noticeable delays during common operations. Learn about caching strategies, database query optimization, and efficient algorithm selection. Use profiling tools to identify bottlenecks. Start with the most frequently-used features and optimize those first."
                },
                new() { 
                    CriteriaTitle = "Version Control", 
                    Score = 3, MaxScore = 4, PercentageScore = 75,
                    ScoreTitle = "Good", 
                    ScoreDescription = "Regular commits with mostly clear messages",
                    TaskTitle = "Git Usage", 
                    GeneratedFeedback = "Good version control practices! You're making regular commits with generally clear messages. To improve, adopt conventional commit message formats and create feature branches for new work. Consider using pull requests for code review even in solo projects. These habits will serve you well in professional environments."
                }
            };
        }

        private async Task SaveToCsvAsync<T>(string filePath, List<T> data)
        {
            var csv = new StringBuilder();
            var properties = typeof(T).GetProperties();

            // Header
            csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Data - escape commas and quotes
            foreach (var item in data)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? "";
                    // Escape quotes and wrap in quotes if contains comma, quote, or newline
                    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                    {
                        value = "\"" + value.Replace("\"", "\"\"") + "\"";
                    }
                    return value;
                });
                csv.AppendLine(string.Join(",", values));
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        public async Task<bool> TrainFeedbackModelAsync()
        {
            try
            {
                // SKIP FEEDBACK MODEL TRAINING
                // Text generation requires advanced NLP models (transformers, GPT-like models)
                // which are not natively supported in ML.NET
                // The rule-based feedback generator in MLFeedbackGenerationService is more effective
                
                Console.WriteLine("Skipping feedback model training - using rule-based generation instead");
                Console.WriteLine("The system will use intelligent rule-based feedback generation which provides:");
                Console.WriteLine("  - Contextual feedback based on score ranges");
                Console.WriteLine("  - Personalized suggestions");
                Console.WriteLine("  - Consistent quality across all assessments");
                
                return true; // Return true so the process continues
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in feedback model training: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TrainSentimentModelAsync()
        {
            try
            {
                var mlDirectory = Path.Combine(_environment.ContentRootPath, "ML");
                var sentimentDataPath = Path.Combine(mlDirectory, "sentiment_training_data.csv");
                var modelPath = Path.Combine(mlDirectory, "SentimentModel.zip");

                // Create sentiment training data
                var sentimentData = new List<SentimentData>
                {
                    // Positive feedback examples
                    new() { Text = "Excellent work! Outstanding performance demonstrated.", IsPositive = true },
                    new() { Text = "Good job! Nice improvement shown.", IsPositive = true },
                    new() { Text = "Well done! Keep up the great work.", IsPositive = true },
                    new() { Text = "Strong understanding demonstrated.", IsPositive = true },
                    new() { Text = "Impressive solution with great attention to detail.", IsPositive = true },
                    new() { Text = "You've shown mastery of the concepts.", IsPositive = true },
                    new() { Text = "Your approach is both elegant and efficient.", IsPositive = true },
                    
                    // Constructive feedback examples (still positive tone)
                    new() { Text = "Satisfactory work with room for growth and improvement.", IsPositive = true },
                    new() { Text = "Good foundation, focus on strengthening key areas.", IsPositive = true },
                    new() { Text = "You're on the right track, continue practicing.", IsPositive = true },
                    
                    // Needs improvement (but constructive)
                    new() { Text = "This area needs more attention and practice.", IsPositive = false },
                    new() { Text = "Significant gaps found, requires substantial improvement.", IsPositive = false },
                    new() { Text = "Poor implementation, many issues identified.", IsPositive = false },
                    new() { Text = "Fails to meet basic requirements.", IsPositive = false },
                    new() { Text = "Lacks understanding of fundamental concepts.", IsPositive = false }
                };

                await SaveToCsvAsync(sentimentDataPath, sentimentData);

                // Load and train
                var dataView = _mlContext.Data.LoadFromTextFile<SentimentData>(
                    sentimentDataPath,
                    separatorChar: ',',
                    hasHeader: true,
                    allowQuoting: true);

                var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                    .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                        labelColumnName: nameof(SentimentData.IsPositive),
                        featureColumnName: "Features"));

                Console.WriteLine("Training sentiment analysis model...");
                var model = pipeline.Fit(dataView);

                // Save model
                _mlContext.Model.Save(model, dataView.Schema, modelPath);
                Console.WriteLine($"Sentiment model saved to: {modelPath}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error training sentiment model: {ex.Message}");
                return false;
            }
        }
    }
}