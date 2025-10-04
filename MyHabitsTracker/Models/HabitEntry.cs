namespace MyHabitsTracker.Models
{
    /**
     * @brief Represents a single habit entry (one day).
     */
    public class HabitEntry
    {
        /** @brief Unique identifier. */
        public int Id { get; set; }

        /** @brief Foreign key to Habit.Id. */
        public int HabitId { get; set; }

        /** @brief Date of entry (date-only). */
        public DateTime Date { get; set; }

        /** @brief For boolean tracking: 1/0. */
        public bool Completed { get; set; }

        /** @brief For numerical tracking: stored numeric value. */
        public double? NumericValue { get; set; }

        /** @brief For duration tracking: seconds. */
        public int? DurationSeconds { get; set; }

        /** @brief Optional note. */
        public string Note { get; set; }

        /** @brief Creation timestamp (UTC). */
        public DateTime CreatedAt { get; set; }

        /** @brief Default constructor. */
        public HabitEntry()
        {
            Date = DateTime.UtcNow.Date;
            Completed = false;
            NumericValue = null;
            DurationSeconds = null;
            Note = null;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
