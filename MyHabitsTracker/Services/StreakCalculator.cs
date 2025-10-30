using MyHabitsTracker.Models;
using System;
using System.Collections.Generic;

namespace MyHabitsTracker.Services
{
    /**
     * @brief Represents a continuous done streak period.
     */
    public class StreakPeriod
    {
        /** @brief Inclusive start date (UTC.Date). */
        public DateTime StartDate { get; set; }

        /** @brief Inclusive end date (UTC.Date). */
        public DateTime EndDate { get; set; }

        /** @brief Length in days (EndDate - StartDate + 1). */
        public int Length
        {
            get
            {
                return (this.EndDate.Date - this.StartDate.Date).Days + 1;
            }
        }
    }

    /**
     * @brief Utility for calculating habit streaks and simple statistics.
     */
    public static class StreakCalculator
    {
        /**
         * @brief Compute current streak (consecutive done days up to today) for a habit.
         *
         * @param habit Habit metadata (TrackType, TargetValue, IsPositive).
         * @param entries All HabitEntry objects for this habit (any date range).
         * @return Current streak length in days (0 if none).
         */
        public static int CalculateCurrentStreak(Habit habit, List<HabitEntry> entries)
        {
            DateTime today = DateTime.UtcNow.Date;
            Dictionary<DateTime, HabitEntry> map = BuildLatestEntryPerDateMap(entries);

            int streak = 0;
            DateTime day = today;

            while (true)
            {
                HabitEntry entryForDay = null;
                if (map.ContainsKey(day))
                {
                    entryForDay = map[day];
                }

                bool done = IsEntryDone(habit, entryForDay);

                if (done == true)
                {
                    streak = streak + 1;
                    day = day.AddDays(-1.0);
                    continue;
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        /**
         * @brief Compute the best (maximum) streak across all available entries.
         *
         * @param habit Habit metadata.
         * @param entries All HabitEntry objects for this habit.
         * @return Best streak length in days.
         */
        public static int CalculateBestStreak(Habit habit, List<HabitEntry> entries)
        {
            List<StreakPeriod> periods = GetStreakPeriods(habit, entries);
            int best = 0;
            for (int i = 0; i < periods.Count; i = i + 1)
            {
                StreakPeriod p = periods[i];
                if (p.Length > best)
                {
                    best = p.Length;
                }
            }

            return best;
        }

        /**
         * @brief Return all streak periods (start..end inclusive) in chronological order.
         *
         * Consecutive days where the habit is done are grouped into a StreakPeriod.
         *
         * @param habit Habit metadata.
         * @param entries All HabitEntry objects for this habit.
         * @return List of streak periods (empty if none).
         */
        public static List<StreakPeriod> GetStreakPeriods(Habit habit, List<HabitEntry> entries)
        {
            List<StreakPeriod> result = new List<StreakPeriod>();
            Dictionary<DateTime, HabitEntry> map = BuildLatestEntryPerDateMap(entries);

            // collect and sort dates
            List<DateTime> dates = new List<DateTime>();
            foreach (DateTime key in map.Keys)
            {
                dates.Add(key);
            }

            if (dates.Count == 0)
            {
                return result;
            }

            dates.Sort();

            DateTime currentStart = DateTime.MinValue;
            DateTime currentEnd = DateTime.MinValue;
            bool inRun = false;

            for (int i = 0; i < dates.Count; i = i + 1)
            {
                DateTime d = dates[i];
                HabitEntry e = map[d];
                bool done = IsEntryDone(habit, e);

                if (done == true)
                {
                    if (inRun == false)
                    {
                        currentStart = d;
                        currentEnd = d;
                        inRun = true;
                    }
                    else
                    {
                        // check if consecutive
                        TimeSpan diff = d.Date - currentEnd.Date;
                        if (diff.Days == 1)
                        {
                            currentEnd = d;
                        }
                        else
                        {
                            // gap: flush current and start new
                            StreakPeriod sp = new StreakPeriod();
                            sp.StartDate = currentStart.Date;
                            sp.EndDate = currentEnd.Date;
                            result.Add(sp);

                            currentStart = d;
                            currentEnd = d;
                        }
                    }
                }
                else
                {
                    if (inRun == true)
                    {
                        StreakPeriod sp = new StreakPeriod();
                        sp.StartDate = currentStart.Date;
                        sp.EndDate = currentEnd.Date;
                        result.Add(sp);

                        inRun = false;
                        currentStart = DateTime.MinValue;
                        currentEnd = DateTime.MinValue;
                    }
                }
            }

            // flush last run
            if (inRun == true)
            {
                StreakPeriod sp = new StreakPeriod();
                sp.StartDate = currentStart.Date;
                sp.EndDate = currentEnd.Date;
                result.Add(sp);
            }

            return result;
        }

        /**
         * @brief Produce a map of date -> completion (true/false) for a specified inclusive range.
         *
         * Dates with no entry are considered not done (false).
         *
         * @param habit Habit metadata (for completion logic).
         * @param entries All HabitEntry objects for this habit.
         * @param from Inclusive start date (UTC.Date).
         * @param to Inclusive end date (UTC.Date).
         * @return Dictionary mapping each date from->to to a boolean done flag.
         */
        public static Dictionary<DateTime, bool> GetDailyCompletionMap(Habit habit, List<HabitEntry> entries, DateTime from, DateTime to)
        {
            Dictionary<DateTime, bool> result = new Dictionary<DateTime, bool>();
            if (to.Date < from.Date)
            {
                return result;
            }

            Dictionary<DateTime, HabitEntry> map = BuildLatestEntryPerDateMap(entries);

            DateTime cursor = from.Date;
            while (cursor.Date <= to.Date)
            {
                HabitEntry e = null;
                if (map.ContainsKey(cursor) == true)
                {
                    e = map[cursor];
                }

                bool done = IsEntryDone(habit, e);
                result[cursor.Date] = done;
                cursor = cursor.AddDays(1.0);
            }

            return result;
        }

        /**
         * @brief Compute completion rate (percentage 0..100) over an inclusive date range.
         *
         * @param habit Habit metadata.
         * @param entries All HabitEntry objects for this habit.
         * @param from Inclusive start date (UTC.Date).
         * @param to Inclusive end date (UTC.Date).
         * @return double percentage between 0.0 and 100.0.
         */
        public static double GetCompletionRate(Habit habit, List<HabitEntry> entries, DateTime from, DateTime to)
        {
            Dictionary<DateTime, bool> map = GetDailyCompletionMap(habit, entries, from, to);

            int totalDays = 0;
            int doneDays = 0;
            foreach (DateTime key in map.Keys)
            {
                totalDays = totalDays + 1;
                if (map[key] == true)
                {
                    doneDays = doneDays + 1;
                }
            }

            if (totalDays == 0)
            {
                return 0.0;
            }

            double rate = ((double)doneDays * 100.0) / (double)totalDays;
            return rate;
        }

        /**
         * @brief Count total number of done days in an inclusive range.
         *
         * @param habit Habit metadata.
         * @param entries All HabitEntry objects for this habit.
         * @param from Inclusive start date.
         * @param to Inclusive end date.
         * @return number of days considered done.
         */
        public static int GetDoneDaysCount(Habit habit, List<HabitEntry> entries, DateTime from, DateTime to)
        {
            Dictionary<DateTime, bool> map = GetDailyCompletionMap(habit, entries, from, to);
            int count = 0;
            foreach (DateTime key in map.Keys)
            {
                if (map[key] == true)
                {
                    count = count + 1;
                }
            }

            return count;
        }

        /**
         * @brief Build a map of Date -> latest HabitEntry (latest CreatedAt wins).
         *
         * @param entries List of HabitEntry (may be null).
         * @return Dictionary mapping date (UTC.Date) to the selected HabitEntry.
         */
        private static Dictionary<DateTime, HabitEntry> BuildLatestEntryPerDateMap(List<HabitEntry> entries)
        {
            Dictionary<DateTime, HabitEntry> map = new Dictionary<DateTime, HabitEntry>();
            if (entries == null)
            {
                return map;
            }

            for (int i = 0; i < entries.Count; i = i + 1)
            {
                HabitEntry e = entries[i];
                DateTime d = e.Date.Date;

                if (map.ContainsKey(d) == false)
                {
                    map[d] = e;
                }
                else
                {
                    HabitEntry existing = map[d];
                    DateTime existingCreated = existing.CreatedAt;
                    DateTime newCreated = e.CreatedAt;
                    if (newCreated > existingCreated)
                    {
                        map[d] = e;
                    }
                }
            }

            return map;
        }

        /**
         * @brief Decide whether a given entry counts as done for the habit.
         *
         * This mirrors the same logic used elsewhere in the app (boolean/numeric/duration).
         *
         * @param habit Habit metadata.
         * @param entry HabitEntry or null.
         * @return true if entry counts as done.
         */
        private static bool IsEntryDone(Habit habit, HabitEntry entry)

        {
            if (entry == null)
            {
                return false;
            }

            // boolean
            if (habit.TrackType == 0)
            {
                return entry.Completed;
            }

            // numeric
            if (habit.TrackType == 1)
            {
                if (entry.NumericValue.HasValue == false)
                {
                    return false;
                }

                double val = entry.NumericValue.Value;

                if (habit.TargetValue.HasValue)
                {
                    double target = habit.TargetValue.Value;
                    if (habit.IsPositive)
                    {
                        return (val >= target);
                    }
                    else
                    {
                        return (val < target);
                    }
                }
                else
                {
                    if (habit.IsPositive)
                    {
                        return (val > 0.0);
                    }
                    else
                    {
                        return (val == 0.0);
                    }
                }
            }

            // duration
            if (habit.TrackType == 2)
            {
                if (entry.DurationSeconds.HasValue == false)
                {
                    return false;
                }

                int seconds = entry.DurationSeconds.Value;

                if (habit.TargetValue.HasValue)
                {
                    int target = (int)habit.TargetValue.Value;
                    if (habit.IsPositive)
                    {
                        return (seconds >= target);
                    }
                    else
                    {
                        return (seconds < target);
                    }
                }
                else
                {
                    if (habit.IsPositive)
                    {
                        return (seconds > 0);
                    }
                    else
                    {
                        return (seconds == 0);
                    }
                }
            }

            return false;
        }
    }
}
