using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using MyHabitsTracker.Data;
using MyHabitsTracker.Models;

namespace MyHabitsTracker.Services
{
    /**
     * @brief CRUD operations and entry management for habits.
     *
     * All write operations use explicit transactions to satisfy reliability requirements.
     */
    public class HabitService
    {
        /**
         * @brief Constructor ensures DB exists.
         */
        public HabitService()
        {
            Database.EnsureDatabase();
        }

        /**
         * @brief Get all habits ordered by Title.
         * @return List of Habit objects.
         */
        public List<Habit> GetAllHabits()
        {
            List<Habit> list = new List<Habit>();
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Title, Description, IsPositive, TrackType, TargetValue, CreatedAt, UpdatedAt FROM Habits ORDER BY Title";
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Habit h = new Habit();
                            h.Id = reader.GetInt32(0);
                            h.Title = reader.GetString(1);
                            if (reader.IsDBNull(2) == false)
                            {
                                h.Description = reader.GetString(2);
                            }
                            else
                            {
                                h.Description = null;
                            }

                            h.IsPositive = (reader.GetInt32(3) == 1);
                            h.TrackType = reader.GetInt32(4);

                            if (reader.IsDBNull(5) == false)
                            {
                                h.TargetValue = reader.GetDouble(5);
                            }
                            else
                            {
                                h.TargetValue = null;
                            }

                            string createdText = reader.GetString(6);
                            string updatedText = reader.GetString(7);
                            h.CreatedAt = DateTime.Parse(createdText, null, DateTimeStyles.RoundtripKind);
                            h.UpdatedAt = DateTime.Parse(updatedText, null, DateTimeStyles.RoundtripKind);

                            list.Add(h);
                        }
                    }
                }
            }
            finally
            {
                conn.Dispose();
            }

            return list;
        }

        /**
         * @brief Create a new habit and return inserted id.
         * @param h Habit object to insert (Id ignored).
         * @return New habit Id.
         */
        public int CreateHabit(Habit h)
        {
            int newId = 0;
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteTransaction tx = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                            "INSERT INTO Habits (Title, Description, IsPositive, TrackType, TargetValue, CreatedAt, UpdatedAt) " +
                            "VALUES ($title, $desc, $ispos, $track, $target, $created, $updated); " +
                            "SELECT last_insert_rowid();";
                        cmd.Parameters.AddWithValue("$title", h.Title);
                        if (h.Description == null)
                        {
                            cmd.Parameters.AddWithValue("$desc", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("$desc", h.Description);
                        }

                        cmd.Parameters.AddWithValue("$ispos", (h.IsPositive ? 1 : 0));
                        cmd.Parameters.AddWithValue("$track", h.TrackType);

                        if (h.TargetValue.HasValue)
                        {
                            cmd.Parameters.AddWithValue("$target", h.TargetValue.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("$target", DBNull.Value);
                        }

                        string now = DateTime.UtcNow.ToString("o");
                        cmd.Parameters.AddWithValue("$created", now);
                        cmd.Parameters.AddWithValue("$updated", now);

                        object scalar = cmd.ExecuteScalar();
                        long idLong = (long)scalar;
                        newId = (int)idLong;
                    }

                    tx.Commit();
                }
            }
            finally
            {
                conn.Dispose();
            }

            return newId;
        }

        /**
         * @brief Update an existing habit (Id must be set).
         * @param h Habit with updated fields.
         */
        public void UpdateHabit(Habit h)
        {
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteTransaction tx = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                            "UPDATE Habits SET Title=$title, Description=$desc, IsPositive=$ispos, TrackType=$track, TargetValue=$target, UpdatedAt=$updated WHERE Id=$id";
                        cmd.Parameters.AddWithValue("$title", h.Title);

                        if (h.Description == null)
                        {
                            cmd.Parameters.AddWithValue("$desc", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("$desc", h.Description);
                        }

                        cmd.Parameters.AddWithValue("$ispos", (h.IsPositive ? 1 : 0));
                        cmd.Parameters.AddWithValue("$track", h.TrackType);

                        if (h.TargetValue.HasValue)
                        {
                            cmd.Parameters.AddWithValue("$target", h.TargetValue.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("$target", DBNull.Value);
                        }

                        cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
                        cmd.Parameters.AddWithValue("$id", h.Id);

                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }

        /**
         * @brief Delete habit and cascade delete entries.
         * @param id Habit id to delete.
         */
        public void DeleteHabit(int id)
        {
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteTransaction tx = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "DELETE FROM Habits WHERE Id=$id";
                        cmd.Parameters.AddWithValue("$id", id);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }

        /**
         * @brief Get a single entry by habit id and date.
         * @param habitId Habit id.
         * @param date Date (date-only portion used).
         * @return HabitEntry or null if not found.
         */
        public HabitEntry GetEntry(int habitId, DateTime date)
        {
            HabitEntry entry = null;
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, HabitId, Date, Completed, NumericValue, DurationSeconds, Note, CreatedAt FROM HabitEntries WHERE HabitId=$hid AND Date=$date";
                    cmd.Parameters.AddWithValue("$hid", habitId);
                    string dateText = date.ToString("yyyy-MM-dd");
                    cmd.Parameters.AddWithValue("$date", dateText);

                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            entry = new HabitEntry();
                            entry.Id = reader.GetInt32(0);
                            entry.HabitId = reader.GetInt32(1);
                            string dtText = reader.GetString(2);
                            entry.Date = DateTime.Parse(dtText);
                            entry.Completed = (reader.GetInt32(3) == 1);

                            if (reader.IsDBNull(4) == false)
                            {
                                entry.NumericValue = reader.GetDouble(4);
                            }
                            else
                            {
                                entry.NumericValue = null;
                            }

                            if (reader.IsDBNull(5) == false)
                            {
                                entry.DurationSeconds = reader.GetInt32(5);
                            }
                            else
                            {
                                entry.DurationSeconds = null;
                            }

                            if (reader.IsDBNull(6) == false)
                            {
                                entry.Note = reader.GetString(6);
                            }
                            else
                            {
                                entry.Note = null;
                            }

                            entry.CreatedAt = DateTime.Parse(reader.GetString(7));
                        }
                    }
                }
            }
            finally
            {
                conn.Dispose();
            }

            return entry;
        }

        /**
         * @brief Get habit entries between two dates inclusive.
         * @param habitId Habit id.
         * @param from Start date inclusive.
         * @param to End date inclusive.
         * @return List of HabitEntry objects ordered by Date ascending.
         */
        public List<HabitEntry> GetEntriesForHabit(int habitId, DateTime from, DateTime to)
        {
            List<HabitEntry> list = new List<HabitEntry>();
            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, HabitId, Date, Completed, NumericValue, DurationSeconds, Note, CreatedAt FROM HabitEntries WHERE HabitId=$hid AND Date BETWEEN $from AND $to ORDER BY Date";
                    cmd.Parameters.AddWithValue("$hid", habitId);
                    cmd.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));

                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            HabitEntry e = new HabitEntry();
                            e.Id = reader.GetInt32(0);
                            e.HabitId = reader.GetInt32(1);
                            e.Date = DateTime.Parse(reader.GetString(2));
                            e.Completed = (reader.GetInt32(3) == 1);
                            if (reader.IsDBNull(4) == false)
                            {
                                e.NumericValue = reader.GetDouble(4);
                            }
                            else
                            {
                                e.NumericValue = null;
                            }

                            if (reader.IsDBNull(5) == false)
                            {
                                e.DurationSeconds = reader.GetInt32(5);
                            }
                            else
                            {
                                e.DurationSeconds = null;
                            }

                            if (reader.IsDBNull(6) == false)
                            {
                                e.Note = reader.GetString(6);
                            }
                            else
                            {
                                e.Note = null;
                            }

                            e.CreatedAt = DateTime.Parse(reader.GetString(7));
                            list.Add(e);
                        }
                    }
                }
            }
            finally
            {
                conn.Dispose();
            }

            return list;
        }

        /**
         * @brief Insert or update an entry for a habit on a specific date.
         * @param entry HabitEntry to upsert (HabitId and Date must be set).
         */
        public void UpsertEntry(HabitEntry entry)
        {
            // Check if exists:
            HabitEntry existing = GetEntry(entry.HabitId, entry.Date);

            SqliteConnection conn = Database.GetConnection();
            try
            {
                using (SqliteTransaction tx = conn.BeginTransaction())
                {
                    using (SqliteCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        if (existing == null)
                        {
                            cmd.CommandText = "INSERT INTO HabitEntries (HabitId, Date, Completed, NumericValue, DurationSeconds, Note, CreatedAt) VALUES ($hid, $date, $completed, $num, $dur, $note, $created)";
                            cmd.Parameters.AddWithValue("$hid", entry.HabitId);
                            cmd.Parameters.AddWithValue("$date", entry.Date.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("$completed", (entry.Completed ? 1 : 0));

                            if (entry.NumericValue.HasValue)
                            {
                                cmd.Parameters.AddWithValue("$num", entry.NumericValue.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$num", DBNull.Value);
                            }

                            if (entry.DurationSeconds.HasValue)
                            {
                                cmd.Parameters.AddWithValue("$dur", entry.DurationSeconds.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$dur", DBNull.Value);
                            }

                            if (entry.Note == null)
                            {
                                cmd.Parameters.AddWithValue("$note", DBNull.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$note", entry.Note);
                            }

                            cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));

                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            cmd.CommandText = "UPDATE HabitEntries SET Completed=$completed, NumericValue=$num, DurationSeconds=$dur, Note=$note WHERE HabitId=$hid AND Date=$date";
                            cmd.Parameters.AddWithValue("$completed", (entry.Completed ? 1 : 0));

                            if (entry.NumericValue.HasValue)
                            {
                                cmd.Parameters.AddWithValue("$num", entry.NumericValue.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$num", DBNull.Value);
                            }

                            if (entry.DurationSeconds.HasValue)
                            {
                                cmd.Parameters.AddWithValue("$dur", entry.DurationSeconds.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$dur", DBNull.Value);
                            }

                            if (entry.Note == null)
                            {
                                cmd.Parameters.AddWithValue("$note", DBNull.Value);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("$note", entry.Note);
                            }

                            cmd.Parameters.AddWithValue("$hid", entry.HabitId);
                            cmd.Parameters.AddWithValue("$date", entry.Date.ToString("yyyy-MM-dd"));

                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
            finally
            {
                conn.Dispose();
            }
        }

        /**
         * @brief Export all data to CSV file.
         * @param destPath Full destination path (file will be overwritten).
         * @return Destination path on success.
         */
        public string ExportAllCsv(string destPath)
        {
            List<Habit> habits = GetAllHabits();

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(destPath, false, System.Text.Encoding.UTF8))
            {
                sw.WriteLine("HabitId,HabitTitle,Date,Completed,NumericValue,DurationSeconds,Note");
                int i;
                for (i = 0; i < habits.Count; i = i + 1)
                {
                    Habit h = habits[i];
                    List<HabitEntry> entries = GetEntriesForHabit(h.Id, DateTime.UtcNow.AddYears(-10), DateTime.UtcNow);
                    int j;
                    for (j = 0; j < entries.Count; j = j + 1)
                    {
                        HabitEntry e = entries[j];
                        string titleEscaped = h.Title.Replace("\"", "\"\"");
                        string noteEscaped = (e.Note == null) ? "" : e.Note.Replace("\"", "\"\"");
                        string numericText;
                        if (e.NumericValue.HasValue)
                        {
                            numericText = e.NumericValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            numericText = "";
                        }

                        string durationText;
                        if (e.DurationSeconds.HasValue)
                        {
                            durationText = e.DurationSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            durationText = "";
                        }

                        sw.WriteLine(h.Id.ToString() + ",\"" + titleEscaped + "\"," + e.Date.ToString("yyyy-MM-dd") + "," + (e.Completed ? "1" : "0") + "," + numericText + "," + durationText + ",\"" + noteEscaped + "\"");
                    }
                }

                sw.Flush();
            }

            return destPath;
        }
    }
}
