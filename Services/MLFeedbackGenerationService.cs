using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using smart_feedback.Models;
using smart_feedback.Models.ML;
using smart_feedback.Models.ViewModels;
using System.Text;

namespace smart_feedback.Services
{
    public class MLFeedbackGenerationService : IFeedbackGenerationService
    {
        private readonly MLContext _mlContext;
        private readonly IWebHostEnvironment _environment;
        private ITransformer _feedbackModel;
        private ITransformer _sentimentModel;
        private readonly string _modelPath;
        private readonly string _sentimentModelPath;

        public MLFeedbackGenerationService(IWebHostEnvironment environment)
        {
            _mlContext = new MLContext(seed: 0);
            _environment = environment;
            _modelPath = Path.Combine(_environment.ContentRootPath, "ML", "FeedbackModel.zip");
            _sentimentModelPath = Path.Combine(_environment.ContentRootPath, "ML", "SentimentModel.zip");

            // Ensure ML directory exists
            Directory.CreateDirectory(Path.Combine(_environment.ContentRootPath, "ML"));

            LoadOrCreateModels();
        }

        public async Task<string> GenerateFeedbackAsync(RubricCriteria criteria, int score, string scoreDescription,
            string scoreTitle, string taskTitle, string customComment = null)
        {
            try
            {
                var feedback = new StringBuilder();

                // Base feedback generation using ML model if available, otherwise fallback to rule-based
                if (_feedbackModel != null)
                {
                    var mlFeedback = await GenerateMLFeedbackAsync(criteria, score, scoreDescription, scoreTitle, taskTitle);
                    feedback.AppendLine(mlFeedback);
                }
                else
                {
                    // Fallback to enhanced rule-based system
                    feedback.AppendLine(GenerateRuleBasedFeedback(criteria, score, scoreDescription, scoreTitle, taskTitle));
                }

                // Add custom comment if provided
                if (!string.IsNullOrEmpty(customComment))
                {
                    feedback.AppendLine($"\nAdditional Comments: {customComment}");
                }

                // Enhance with contextual suggestions
                var enhancedFeedback = await EnhanceFeedbackWithSuggestionsAsync(feedback.ToString(), score, scoreDescription);

                return enhancedFeedback;
            }
            catch (Exception)
            {
                // Fallback to rule-based if ML fails
                return GenerateRuleBasedFeedback(criteria, score, scoreDescription, scoreTitle, taskTitle);
            }
        }

        private async Task<string> GenerateMLFeedbackAsync(RubricCriteria criteria, int score,
            string scoreDescription, string scoreTitle, string taskTitle)
        {
            if (_feedbackModel == null)
                return GenerateRuleBasedFeedback(criteria, score, scoreDescription, scoreTitle, taskTitle);

            var inputData = new FeedbackData
            {
                CriteriaTitle = criteria.CriterionTitle,
                Score = score,
                MaxScore = criteria.MaxScore,
                PercentageScore = (float)score / criteria.MaxScore * 100,
                ScoreDescription = scoreDescription ?? "",
                ScoreTitle = scoreTitle ?? "",
                TaskTitle = taskTitle ?? ""
            };

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<FeedbackData, FeedbackPrediction>(_feedbackModel);
            var prediction = predictionEngine.Predict(inputData);

            return prediction.GeneratedFeedback ?? GenerateRuleBasedFeedback(criteria, score, scoreDescription, scoreTitle, taskTitle);
        }

        private string GenerateRuleBasedFeedback(RubricCriteria criteria, int score,
            string scoreDescription, string scoreTitle, string taskTitle)
        {
            var feedback = new StringBuilder();
            var percentage = (double)score / criteria.MaxScore * 100;

            feedback.AppendLine($"**{criteria.CriterionTitle}** ({taskTitle})");

            // Enhanced feedback based on performance level
            switch (percentage)
            {
                case >= 90:
                    feedback.AppendLine($"🌟 **Exceptional Performance** - {scoreTitle}");
                    feedback.AppendLine($"You have demonstrated mastery in {criteria.CriterionTitle.ToLower()}. {scoreDescription}");
                    feedback.AppendLine("This level of excellence sets a benchmark for others. Keep up the outstanding work!");
                    break;
                case >= 75:
                    feedback.AppendLine($"✅ **Strong Performance** - {scoreTitle}");
                    feedback.AppendLine($"Good work on {criteria.CriterionTitle.ToLower()}. {scoreDescription}");
                    feedback.AppendLine("You're demonstrating solid understanding. Consider refining your approach to reach the next level.");
                    break;
                case >= 50:
                    feedback.AppendLine($"⚠️ **Developing Performance** - {scoreTitle}");
                    feedback.AppendLine($"Your work on {criteria.CriterionTitle.ToLower()} shows progress. {scoreDescription}");
                    feedback.AppendLine("Focus on strengthening this area through additional practice and review of key concepts.");
                    break;
                case >= 25:
                    feedback.AppendLine($"📚 **Needs Attention** - {scoreTitle}");
                    feedback.AppendLine($"This area requires significant improvement. {scoreDescription}");
                    feedback.AppendLine("Consider seeking additional support, reviewing relevant materials, and practicing more extensively.");
                    break;
                default:
                    feedback.AppendLine($"🔄 **Requires Development** - {scoreTitle}");
                    feedback.AppendLine($"This criterion needs substantial work. {scoreDescription}");
                    feedback.AppendLine("Please consult with your instructor for additional guidance and resources.");
                    break;
            }

            return feedback.ToString();
        }

        public async Task<string> GenerateOverallFeedbackAsync(double percentage, List<StudentCriteriaResult> criteriaResults)
        {
            var feedback = new StringBuilder();

            // Overall performance assessment
            feedback.AppendLine("## 📊 Overall Assessment Summary");
            feedback.AppendLine();

            if (percentage >= 85)
            {
                feedback.AppendLine("🎉 **Outstanding Achievement!**");
                feedback.AppendLine("You have demonstrated exceptional understanding and skill across multiple criteria. Your work shows mastery of key concepts and excellent attention to detail.");
            }
            else if (percentage >= 70)
            {
                feedback.AppendLine("👍 **Strong Performance!**");
                feedback.AppendLine("Good overall performance with solid understanding demonstrated in most areas. You're well on your way to mastering these concepts.");
            }
            else if (percentage >= 50)
            {
                feedback.AppendLine("📈 **Satisfactory Progress**");
                feedback.AppendLine("You have met basic requirements and show understanding of fundamental concepts. There's clear potential for improvement with focused effort.");
            }
            else
            {
                feedback.AppendLine("🎯 **Development Opportunity**");
                feedback.AppendLine("This assessment indicates areas that need significant attention. Don't be discouraged - this is valuable feedback for your learning journey.");
            }

            // Analyze patterns using ML-enhanced insights
            await AddMLInsightsAsync(feedback, criteriaResults);

            // Identify strengths and improvement areas
            var strengths = criteriaResults.Where(cr => cr.Score >= (cr.MaxScore * 0.75)).ToList();
            var improvements = criteriaResults.Where(cr => cr.Score <= (cr.MaxScore * 0.5)).ToList();

            if (strengths.Any())
            {
                feedback.AppendLine();
                feedback.AppendLine("### 💪 **Your Strengths:**");
                foreach (var strength in strengths)
                {
                    feedback.AppendLine($"- **{strength.CriteriaTitle}**: Demonstrating solid competency");
                }
            }

            if (improvements.Any())
            {
                feedback.AppendLine();
                feedback.AppendLine("### 🎯 **Focus Areas for Improvement:**");
                foreach (var area in improvements)
                {
                    feedback.AppendLine($"- **{area.CriteriaTitle}**: Requires additional attention and practice");
                }
            }

            // Add personalized recommendations
            feedback.AppendLine();
            feedback.AppendLine("### 📝 **Recommendations:**");
            feedback.AppendLine(await GeneratePersonalizedRecommendationsAsync(percentage, criteriaResults));

            return feedback.ToString();
        }

        private async Task AddMLInsightsAsync(StringBuilder feedback, List<StudentCriteriaResult> criteriaResults)
        {
            // Analyze performance patterns
            var scores = criteriaResults.Select(cr => (double)cr.Score / cr.MaxScore * 100).ToList();
            var variance = CalculateVariance(scores);

            feedback.AppendLine();

            if (variance < 100) // Low variance - consistent performance
            {
                feedback.AppendLine("📋 **Pattern Analysis**: Your performance shows consistency across different criteria, indicating a well-rounded approach to the assessment.");
            }
            else if (variance > 400) // High variance - inconsistent performance
            {
                feedback.AppendLine("📋 **Pattern Analysis**: Your performance varies significantly across criteria. Consider focusing on areas where you scored lower while maintaining your strengths.");
            }
            else
            {
                feedback.AppendLine("📋 **Pattern Analysis**: Your performance shows a normal variation across criteria, with clear areas of strength and opportunity.");
            }
        }

        private async Task<string> GeneratePersonalizedRecommendationsAsync(double percentage, List<StudentCriteriaResult> criteriaResults)
        {
            var recommendations = new StringBuilder();

            if (percentage >= 85)
            {
                recommendations.AppendLine("- Continue maintaining this high standard of work");
                recommendations.AppendLine("- Consider mentoring other students or taking on advanced challenges");
                recommendations.AppendLine("- Reflect on your successful strategies and document them for future reference");
            }
            else if (percentage >= 70)
            {
                recommendations.AppendLine("- Review areas where you scored below 75% to identify specific improvement opportunities");
                recommendations.AppendLine("- Build on your strong foundation by tackling more complex applications");
                recommendations.AppendLine("- Consider forming study groups to discuss challenging concepts");
            }
            else if (percentage >= 50)
            {
                recommendations.AppendLine("- Focus on fundamental concepts in lower-scoring areas");
                recommendations.AppendLine("- Seek additional resources such as textbooks, online tutorials, or office hours");
                recommendations.AppendLine("- Practice regularly and seek feedback on your progress");
            }
            else
            {
                recommendations.AppendLine("- Schedule a meeting with your instructor to develop a focused improvement plan");
                recommendations.AppendLine("- Consider utilizing tutoring services or study support programs");
                recommendations.AppendLine("- Break down complex topics into smaller, manageable components");
                recommendations.AppendLine("- Establish a regular study schedule and track your progress");
            }

            return recommendations.ToString();
        }

        public async Task<string> ImproveFeedbackAsync(string originalFeedback, string context)
        {
            // Use ML to enhance feedback quality
            if (_sentimentModel != null)
            {
                var isPositive = await AnalyzeFeedbackSentimentAsync(originalFeedback);
                if (!isPositive && !originalFeedback.Contains("However") && !originalFeedback.Contains("Consider"))
                {
                    // Add constructive elements to negative feedback
                    return originalFeedback + "\n\nRemember, every challenge is an opportunity to learn and grow. Focus on the specific areas mentioned above for improvement.";
                }
            }

            return originalFeedback;
        }

        public async Task<bool> AnalyzeFeedbackSentimentAsync(string feedback)
        {
            if (_sentimentModel == null)
                return true; // Default to positive

            var inputData = new SentimentData { Text = feedback };
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_sentimentModel);
            var prediction = predictionEngine.Predict(inputData);

            return prediction.IsPositive;
        }

        public async Task TrainModelAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Create sample training data
                    CreateSampleTrainingData();

                    // Train feedback generation model
                    TrainFeedbackModel();

                    // Train sentiment analysis model
                    TrainSentimentModel();
                }
                catch (Exception)
                {
                    // Training failed - will fall back to rule-based system
                }
            });
        }

        private void LoadOrCreateModels()
        {
            try
            {
                if (File.Exists(_modelPath))
                {
                    _feedbackModel = _mlContext.Model.Load(_modelPath, out _);
                }

                if (File.Exists(_sentimentModelPath))
                {
                    _sentimentModel = _mlContext.Model.Load(_sentimentModelPath, out _);
                }

                // If models don't exist, create them asynchronously
                if (_feedbackModel == null || _sentimentModel == null)
                {
                    _ = Task.Run(() => TrainModelAsync());
                }
            }
            catch
            {
                // Model loading failed - will use rule-based system
            }
        }

        private void CreateSampleTrainingData()
        {
            // Create sample feedback data for training
            var feedbackSamples = new List<FeedbackData>
                {
                    new() { CriteriaTitle = "Problem Solving", Score = 4, MaxScore = 4, PercentageScore = 100,
                            ScoreTitle = "Excellent", ScoreDescription = "Demonstrates exceptional problem-solving skills",
                            TaskTitle = "Analysis", GeneratedFeedback = "Outstanding problem-solving approach. You've shown excellent analytical thinking and methodical approach to complex problems." },

                    new() { CriteriaTitle = "Communication", Score = 3, MaxScore = 4, PercentageScore = 75,
                            ScoreTitle = "Good", ScoreDescription = "Clear communication with minor areas for improvement",
                            TaskTitle = "Presentation", GeneratedFeedback = "Good communication skills demonstrated. Your ideas are clear, though there's room for improvement in structure and flow." },

                    new() { CriteriaTitle = "Technical Skills", Score = 2, MaxScore = 4, PercentageScore = 50,
                            ScoreTitle = "Satisfactory", ScoreDescription = "Basic technical competency shown",
                            TaskTitle = "Implementation", GeneratedFeedback = "You've demonstrated basic technical skills. Focus on practicing advanced techniques and improving accuracy." },

                    new() { CriteriaTitle = "Critical Thinking", Score = 1, MaxScore = 4, PercentageScore = 25,
                            ScoreTitle = "Needs Improvement", ScoreDescription = "Limited evidence of critical analysis",
                            TaskTitle = "Evaluation", GeneratedFeedback = "This area needs significant development. Consider different perspectives and analyze information more deeply." }
                };

            var feedbackDataPath = Path.Combine(_environment.ContentRootPath, "ML", "feedback_data.csv");
            CreateCsvFile(feedbackDataPath, feedbackSamples);
        }

        private void TrainFeedbackModel()
        {
            // SKIP FEEDBACK MODEL TRAINING
            // The rule-based feedback generator is more effective for this use case
            // ML.NET doesn't support sequence-to-sequence text generation well
            // The feedback model will remain null, and GenerateFeedbackAsync will use rule-based approach
            
            var feedbackDataPath = Path.Combine(_environment.ContentRootPath, "ML", "feedback_data.csv");
            if (!File.Exists(feedbackDataPath)) return;
            
            // Optional: Log that we're skipping this
            Console.WriteLine("Skipping feedback model training - using rule-based generation instead");
        }

        private void TrainSentimentModel()
        {
            var sentimentData = new List<SentimentData>
                {
                    new() { Text = "Excellent work! Outstanding performance.", IsPositive = true },
                    new() { Text = "Good job! Nice improvement.", IsPositive = true },
                    new() { Text = "Well done! Keep it up.", IsPositive = true },
                    new() { Text = "Needs improvement. Try harder next time.", IsPositive = false },
                    new() { Text = "Poor performance. Significant issues found.", IsPositive = false },
                    new() { Text = "Satisfactory work with room for growth.", IsPositive = true }
                };

            var sentimentDataPath = Path.Combine(_environment.ContentRootPath, "ML", "sentiment_data.csv");
            CreateCsvFile(sentimentDataPath, sentimentData);

            var data = _mlContext.Data.LoadFromTextFile<SentimentData>(sentimentDataPath, separatorChar: ',', hasHeader: true);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", "Text")
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression("IsPositive", "Features"));

            _sentimentModel = pipeline.Fit(data);
            _mlContext.Model.Save(_sentimentModel, data.Schema, _sentimentModelPath);
        }

        private void CreateCsvFile<T>(string filePath, List<T> data)
        {
            var csv = new StringBuilder();
            var properties = typeof(T).GetProperties();

            // Header
            csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Data
            foreach (var item in data)
            {
                var values = properties.Select(p => p.GetValue(item)?.ToString()?.Replace(",", ";") ?? "");
                csv.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(filePath, csv.ToString());
        }

        private async Task<string> EnhanceFeedbackWithSuggestionsAsync(string feedback, int score, string scoreDescription)
        {
            var enhanced = new StringBuilder(feedback);

            // Add specific, actionable suggestions based on score
            enhanced.AppendLine();
            enhanced.AppendLine("**💡 Specific Suggestions:**");

            if (score <= 1)
            {
                enhanced.AppendLine("• Review foundational concepts and seek clarification on key principles");
                enhanced.AppendLine("• Practice with simpler examples before attempting complex problems");
                enhanced.AppendLine("• Schedule office hours to discuss specific challenges");
            }
            else if (score == 2)
            {
                enhanced.AppendLine("• Focus on accuracy and attention to detail");
                enhanced.AppendLine("• Practice similar problems to reinforce understanding");
                enhanced.AppendLine("• Consider different approaches to strengthen your methodology");
            }
            else if (score == 3)
            {
                enhanced.AppendLine("• Polish your technique to achieve consistency");
                enhanced.AppendLine("• Challenge yourself with more complex scenarios");
                enhanced.AppendLine("• Pay attention to minor details that can elevate your work");
            }
            else if (score >= 4)
            {
                enhanced.AppendLine("• Maintain this excellent standard in future work");
                enhanced.AppendLine("• Consider sharing your approach with classmates");
                enhanced.AppendLine("• Explore advanced applications of these concepts");
            }

            return enhanced.ToString();
        }

        private static double CalculateVariance(List<double> scores)
        {
            if (scores.Count == 0) return 0;

            var mean = scores.Average();
            var variance = scores.Sum(score => Math.Pow(score - mean, 2)) / scores.Count;
            return variance;
        }
    }
}
