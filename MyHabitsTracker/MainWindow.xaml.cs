using MyHabitsTracker.Models;
using MyHabitsTracker.Services;
using MyHabitsTracker.Views;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MyHabitsTracker
{
    /**
     * @brief Code-behind for MainWindow. Shows habit list and handles actions.
     */
    public partial class MainWindow : Window
    {
        /** @brief Data access service. */
        private HabitService _habitService;

        /**
         * @brief DTO for DataGrid rows.
         */
        private class HabitRow
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public bool IsPositive { get; set; }
            public int TrackType { get; set; }
            public double? TargetValue { get; set; }
            public int CurrentStreak { get; set; }
            public int BestStreak { get; set; }
            public string TodayStatus { get; set; }
            public string Description { get; set; }

            /**
             * @brief Text describing the habit details (target or type).
             */
            public string Details
            {
                get
                {
                    if (this.TrackType == 0)
                    {
                        return "Checkbox";
                    }
                    else if (this.TrackType == 1)
                    {
                        if (this.TargetValue.HasValue)
                        {
                            return "Target: " + this.TargetValue.Value.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            return "Numerical";
                        }
                    }
                    else
                    {
                        if (this.TargetValue.HasValue)
                        {
                            return "Duration target: " + ConvertSecondsToPretty((int)this.TargetValue.Value);
                        }
                        else
                        {
                            return "Duration";
                        }
                    }
                }
            }

            /**
             * @brief Convert seconds to the form hh:mm:ss.
             * @param seconds Number of seconds.
             * @return string in the form hh:mm:ss.
             */
            public static string ConvertSecondsToPretty(int seconds)
            {
                int h = seconds / 3600;
                int rem = seconds % 3600;
                int m = rem / 60;
                int s = rem % 60;
                string result = string.Empty;
                if (h > 0)
                {
                    result = result + h.ToString() + "h";
                }

                if (m > 0)
                {
                    if (result.Length > 0) { result = result + " "; }
                    result = result + m.ToString() + "m";
                }

                if (s > 0)
                {
                    if (result.Length > 0) { result = result + " "; }
                    result = result + s.ToString() + "s";
                }

                if (result.Length == 0)
                {
                    result = "0s";
                }

                return result;
            }

            /**
             * @brief Show Positive as "Yes" or "No".
             */
            public string PositiveText
            {
                get
                {
                    return (this.IsPositive ? "Yes" : "No");
                }
            }

            public HabitRow()
            {
                Title = string.Empty;
                TodayStatus = "No entry";
                Description = string.Empty;
                CurrentStreak = 0;
                BestStreak = 0;
            }
        }

        /**
         * @brief Constructor.
         */
        public MainWindow()
        {
            InitializeComponent();

            _habitService = new HabitService();

            this.Loaded += MainWindow_Loaded;
            this.BtnNew.Click += BtnNew_Click;
            this.BtnEdit.Click += BtnEdit_Click;
            this.BtnDelete.Click += BtnDelete_Click;
            this.BtnExport.Click += BtnExport_Click;
            this.BtnStatistics.Click += BtnStatistics_Click;
            this.DgHabits.MouseDoubleClick += DgHabits_MouseDoubleClick;
            this.DgHabits.SelectionChanged += DgHabits_SelectionChanged;
        }

        /**
         * @brief Window loaded handler.
         */
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshHabits();
            UpdateDescriptionPanel(null);
        }

        /**
         * @brief Refresh the DataGrid: loads habits, computes streaks and today's status.
         */
        private void RefreshHabits()
        {
            List<Habit> habits = _habitService.GetAllHabits();
            List<HabitRow> rows = new List<HabitRow>();
           
            for (int i = 0; i < habits.Count; i++)
            {
                Habit h = habits[i];
                DateTime fromDate = DateTime.UtcNow.AddYears(-1);
                DateTime toDate = DateTime.UtcNow;
                List<HabitEntry> entries = _habitService.GetEntriesForHabit(h.Id, fromDate, toDate);
                
                //TODO: calculate Streak                
                int current = 0;
                int best = 0;

                HabitRow row = new HabitRow();
                row.Id = h.Id;
                row.Title = h.Title;
                row.IsPositive = h.IsPositive;
                row.TrackType = h.TrackType;
                row.TargetValue = h.TargetValue;
                row.CurrentStreak = current;
                row.BestStreak = best;
                row.Description = (h.Description == null ? string.Empty : h.Description);

                // Today status logic
                DateTime today = DateTime.UtcNow.Date;
                HabitEntry todayEntry = _habitService.GetEntry(h.Id, today);
                if (todayEntry == null)
                {
                    row.TodayStatus = "No entry";
                }
                else
                {
                    if (h.TrackType == 0)
                    {
                        // Boolean: Done / Not done
                        if (todayEntry.Completed)
                        {
                            row.TodayStatus = "Done";
                        }
                        else
                        {
                            row.TodayStatus = "Not done";
                        }
                    }
                    else if (h.TrackType == 1)
                    {
                        // Numerical
                        string numberText;
                        if (todayEntry.NumericValue.HasValue)
                        {
                            numberText = todayEntry.NumericValue.Value.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            numberText = "No value";
                        }

                        bool done = IsNumericEntryDone(h, todayEntry);
                        if (done)
                        {
                            row.TodayStatus = numberText + " (done)";
                        }
                        else
                        {
                            row.TodayStatus = numberText + " (not done)";
                        }
                    }
                    else
                    {
                        // Duration
                        string durationText;
                        if (todayEntry.DurationSeconds.HasValue)
                        {
                            durationText = HabitRow.ConvertSecondsToPretty(todayEntry.DurationSeconds.Value);
                        }
                        else
                        {
                            durationText = "No duration";
                        }

                        bool done = IsDurationEntryDone(h, todayEntry);
                        if (done)
                        {
                            row.TodayStatus = durationText + " (done)";
                        }
                        else
                        {
                            row.TodayStatus = durationText + " (not done)";
                        }
                    }
                }

                rows.Add(row);
            }

            this.DgHabits.ItemsSource = rows;
        }

        /**
         * @brief Determines whether a numerical entry counts as done.
         * @param h Habit meta.
         * @param e HabitEntr.
         * @return true if done, false otherwise.
         */
        private bool IsNumericEntryDone(Habit h, HabitEntry e)
        {
            if (e == null)
            {
                return false;
            }

            if (!e.NumericValue.HasValue)
            {
                return false;
            }

            double value = e.NumericValue.Value;

            if (h.TargetValue.HasValue)
            {
                double target = h.TargetValue.Value;
                if (h.IsPositive)
                {
                    return (value >= target);
                }
                else
                {
                    // bad habit: done if value less than target
                    return (value < target);
                }
            }
            else
            {
                // no explicit target
                if (h.IsPositive)
                {
                    return (value > 0.0);
                }
                else
                {
                    // treat 0 as done for bad habit
                    return (value == 0.0);
                }
            }
        }

        /**
         * @brief Determines whether a duration entry counts as done.
         * @param h Habit meta.
         * @param e HabitEntry.
         * @return true if done, false otherwise.
         */
        private bool IsDurationEntryDone(Habit h, HabitEntry e)
        {
            if (e == null)
            {
                return false;
            }

            if (!e.DurationSeconds.HasValue)
            {
                return false;
            }

            int seconds = e.DurationSeconds.Value;

            if (h.TargetValue.HasValue)
            {
                double target = h.TargetValue.Value;
                if (h.IsPositive)
                {
                    return (seconds >= (int)target);
                }
                else
                {
                    return (seconds < (int)target);
                }
            }
            else
            {
                if (h.IsPositive)
                {
                    return (seconds > 0);
                }
                else
                {
                    return (seconds == 0);
                }
            }
        }

        /**
         * @brief New habit button handler.
         */
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            HabitDialog dialog = new HabitDialog();
            Nullable<bool> r = dialog.ShowDialog();
            if (r == true)
            {
                Habit created = dialog.Habit;
                _habitService.CreateHabit(created);
                RefreshHabits();
            }
        }

        /**
         * @brief Edit habit handler.
         */
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            object sel = this.DgHabits.SelectedItem;
            if (sel == null)
            {
                MessageBox.Show("Please select a habit to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            HabitRow hr = sel as HabitRow;
            if (hr == null)
            {
                return;
            }

            List<Habit> all = _habitService.GetAllHabits();
            Habit target = null;
            int idx;
            for (idx = 0; idx < all.Count; idx = idx + 1)
            {
                if (all[idx].Id == hr.Id)
                {
                    target = all[idx];
                    break;
                }
            }

            if (target == null)
            {
                MessageBox.Show("Habit not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            HabitDialog dialog = new HabitDialog(target);
            Nullable<bool> res = dialog.ShowDialog();
            if (res == true)
            {
                Habit updated = dialog.Habit;
                _habitService.UpdateHabit(updated);
                RefreshHabits();
            }
        }

        /**
         * @brief Delete habit.
         */
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            object sel = this.DgHabits.SelectedItem;
            if (sel == null)
            {
                MessageBox.Show("Please select a habit to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            HabitRow hr = sel as HabitRow;
            if (hr == null)
            {
                return;
            }

            MessageBoxResult conf = MessageBox.Show("Delete this habit and all its data?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (conf == MessageBoxResult.Yes)
            {
                _habitService.DeleteHabit(hr.Id);
                RefreshHabits();
                UpdateDescriptionPanel(null);
            }
        }

        /**
         * @brief Export to CSV handler (Later).
         */
        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string file = System.IO.Path.Combine(desktop, "mht_export.csv");
            string resultPath = _habitService.ExportAllCsv(file);
            MessageBox.Show("Exported to: " + resultPath, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /**
         * @brief Statistics button placeholder (Later).
         */
        private void BtnStatistics_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Statistics feature will be implemented later.", "Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /**
         * @brief Double-click row: open EntryDialog for today's entry (create or edit) and refresh UI.
         */
        private void DgHabits_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            object sel = this.DgHabits.SelectedItem;
            if (sel == null)
            {
                return;
            }

            HabitRow hr = sel as HabitRow;
            if (hr == null)
            {
                return;
            }

            // find habit
            List<Habit> all = _habitService.GetAllHabits();
            Habit habit = null;
            int idx;
            for (idx = 0; idx < all.Count; idx = idx + 1)
            {
                if (all[idx].Id == hr.Id)
                {
                    habit = all[idx];
                    break;
                }
            }

            if (habit == null)
            {
                return;
            }

            DateTime today = DateTime.UtcNow.Date;
            HabitEntry entry = _habitService.GetEntry(habit.Id, today);

            EntryDialog dlg = new EntryDialog(habit, entry);
            dlg.Owner = this;
            Nullable<bool> dr = dlg.ShowDialog();
            if (dr == true)
            {
                HabitEntry saved = dlg.Entry;
                _habitService.UpsertEntry(saved);
                RefreshHabits();
                UpdateDescriptionPanel(habit.Description);
            }
        }

        /**
         * @brief When selection changes, show/hide description panel.
         */
        private void DgHabits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object sel = this.DgHabits.SelectedItem;
            if (sel == null)
            {
                UpdateDescriptionPanel(null);
                return;
            }

            HabitRow hr = sel as HabitRow;
            if (hr == null)
            {
                UpdateDescriptionPanel(null);
                return;
            }

            UpdateDescriptionPanel(hr.Description);
        }

        /**
         * @brief Updates the bottom description area. If desc is null or empty, hides it.
         * @param desc Description text or null.
         */
        private void UpdateDescriptionPanel(string desc)
        {
            if (string.IsNullOrWhiteSpace(desc))
            {
                this.TbDescTitle.Visibility = Visibility.Collapsed;
                this.TbDescription.Visibility = Visibility.Collapsed;
                this.TbDescription.Text = string.Empty;
            }
            else
            {
                this.TbDescTitle.Visibility = Visibility.Visible;
                this.TbDescription.Visibility = Visibility.Visible;
                this.TbDescription.Text = desc;
            }
        }
    }
}
