namespace MyHabitsTracker.Models
{
    /**
     * @brief Represents a habit definition.
     */
    public class Habit
    {
        /** @brief Unique identifier. */
        public int Id { get; set; }

        /** @brief Short title of the habit. */
        public string Title { get; set; }

        /** @brief Optional description. */
        public string Description { get; set; }

        /** @brief True if habit is positive (do this), false if negative (avoid this). */
        public bool IsPositive { get; set; }

        /** @brief Tracking type: 0=Boolean,1=Numerical,2=Duration (seconds). */
        public int TrackType { get; set; }

        /** @brief Optional numeric target (used for numerical tracking). */
        public double? TargetValue { get; set; }

        /** @brief Creation timestamp (UTC). */
        public DateTime CreatedAt { get; set; }

        /** @brief Last update timestamp (UTC). */
        public DateTime UpdatedAt { get; set; }

        /** @brief Default constructor. */
        public Habit()
        {
            Title = string.Empty;
            Description = null;
            IsPositive = true;
            TrackType = 0;
            TargetValue = null;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}