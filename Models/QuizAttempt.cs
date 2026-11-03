namespace VertexLearn.Models
{
    public class QuizAttempt
    {
        public int AttemptId { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int Score { get; set; }
        public int TotalQuestions { get; set; }
        public int PassPercent { get; set; }
        public DateTime AttemptedAt { get; set; }

        public int Percent => TotalQuestions == 0 ? 0 : (int)Math.Round(Score * 100.0 / TotalQuestions);
        public bool Passed => Percent >= PassPercent;
    }

    /// <summary>One answered question inside an attempt (used for the result breakdown).</summary>
    public class AttemptAnswer
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
        public string? SelectedOption { get; set; }
        public bool IsCorrect { get; set; }

        public string OptionText(string? letter) => letter switch
        {
            "A" => OptionA,
            "B" => OptionB,
            "C" => OptionC,
            "D" => OptionD,
            _ => "No answer"
        };
    }
}
