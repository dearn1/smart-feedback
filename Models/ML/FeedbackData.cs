using Microsoft.ML.Data;

namespace smart_feedback.Models.ML
{
    public class FeedbackData
    {
        [LoadColumn(0)]
        public string CriteriaTitle { get; set; }

        [LoadColumn(1)]
        public float Score { get; set; }

        [LoadColumn(2)]
        public float MaxScore { get; set; }

        [LoadColumn(3)]
        public float PercentageScore { get; set; }

        [LoadColumn(4)]
        public string ScoreDescription { get; set; }

        [LoadColumn(5)]
        public string ScoreTitle { get; set; }

        [LoadColumn(6)]
        public string TaskTitle { get; set; }

        [LoadColumn(7)]
        public string CustomComment { get; set; }

        [LoadColumn(8)]
        public string GeneratedFeedback { get; set; }
    }

    public class FeedbackPrediction
    {
        [ColumnName("PredictedLabel")]
        public string GeneratedFeedback { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; }
    }

    public class SentimentData
    {
        [LoadColumn(0)]
        public string Text { get; set; }

        [LoadColumn(1)]
        public bool IsPositive { get; set; }
    }

    public class SentimentPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsPositive { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
