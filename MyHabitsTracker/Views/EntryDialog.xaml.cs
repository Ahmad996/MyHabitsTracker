using System.Globalization;
using System.Windows;
using MyHabitsTracker.Models;

namespace MyHabitsTracker.Views
{
    /**
     * @brief Code-behind for the Entry dialog.
     */
    public partial class EntryDialog : Window
    {
        /** @brief The HabitEntry that will be created/edited. */
        public HabitEntry Entry { get; private set; }

        /** @brief Habit metadata used to determine which controls to show and validation rules. */
        private Habit _habit;

        /**
         * @brief Constructor.
         * @param habit The Habit metadata.
         * @param existingEntry If not null, dialog edits this entry; otherwise a new entry is created.
         */
        public EntryDialog(Habit habit, HabitEntry existingEntry)
        {
            InitializeComponent();

            _habit = habit;

            if (existingEntry == null)
            {
                HabitEntry tmp = new HabitEntry();
                tmp.HabitId = habit.Id;
                tmp.Date = DateTime.UtcNow.Date;
                this.Entry = tmp;
            }
            else
            {
                this.Entry = existingEntry;
            }

            this.Title = "Entry - " + ((habit.Title == null) ? string.Empty : habit.Title);

            // Ensure date is set
            if (this.Entry.Date == DateTime.MinValue)
            {
                this.Entry.Date = DateTime.UtcNow.Date;
            }

            this.DpDate.SelectedDate = this.Entry.Date;

            // Prefill numeric/duration fields
            if (this.Entry.NumericValue.HasValue)
            {
                this.TbNumeric.Text = this.Entry.NumericValue.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                this.TbNumeric.Text = string.Empty;
            }

            if (this.Entry.DurationSeconds.HasValue)
            {
                int total = this.Entry.DurationSeconds.Value;
                int h = total / 3600;
                int rem = total % 3600;
                int m = rem / 60;
                int s = rem % 60;
                this.TbHours.Text = h.ToString(CultureInfo.InvariantCulture);
                this.TbMinutes.Text = m.ToString(CultureInfo.InvariantCulture);
                this.TbSeconds.Text = s.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                this.TbHours.Text = "0";
                this.TbMinutes.Text = "0";
                this.TbSeconds.Text = "0";
            }

            this.TbNote.Text = (this.Entry.Note == null) ? string.Empty : this.Entry.Note;

            // Adjust UI to show only relevant controls
            ApplyUiForHabitType();

            // Set Completed checkbox initial state (will be overwritten for auto-compute on OK)
            this.CbCompleted.IsChecked = this.Entry.Completed;

            // Wire events
            this.BtnOk.Click += BtnOk_Click;
            this.BtnCancel.Click += BtnCancel_Click;
        }

        /**
         * @brief Show/hide controls according to habit.TrackType.
         */
        private void ApplyUiForHabitType()
        {
            if (_habit.TrackType == 0)
            {
                // Boolean
                this.SpNumeric.Visibility = Visibility.Collapsed;
                this.SpDuration.Visibility = Visibility.Collapsed;
                this.CbCompleted.IsEnabled = true;
            }
            else if (_habit.TrackType == 1)
            {
                // Numerical
                this.SpNumeric.Visibility = Visibility.Visible;
                this.SpDuration.Visibility = Visibility.Collapsed;
                this.TBCompleted.Visibility = Visibility.Collapsed;
                this.CbCompleted.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Duration
                this.SpNumeric.Visibility = Visibility.Collapsed;
                this.SpDuration.Visibility = Visibility.Visible;
                this.TBCompleted.Visibility = Visibility.Collapsed;
                this.CbCompleted.Visibility = Visibility.Collapsed;
            }
        }

        /**
         * @brief Cancel handler.
         */
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /**
         * @brief OK handler: validate inputs and populate Entry.
         */
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Date
            DateTime selDate;
            if (this.DpDate.SelectedDate.HasValue)
            {
                selDate = this.DpDate.SelectedDate.Value.Date;
            }
            else
            {
                selDate = DateTime.UtcNow.Date;
            }

            this.Entry.Date = selDate;

            if (_habit.TrackType == 0)
            {
                // Boolean: completed set by user
                this.Entry.Completed = (this.CbCompleted.IsChecked == true);
                this.Entry.NumericValue = null;
                this.Entry.DurationSeconds = null;
            }
            else if (_habit.TrackType == 1)
            {
                // Numerical: validate numeric input
                double parsed;
                if (double.TryParse(this.TbNumeric.Text, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) == false)
                {
                    MessageBox.Show("Please enter a valid numeric value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                this.Entry.NumericValue = parsed;
                this.Entry.DurationSeconds = null;

                // compute Completed automatically
                this.Entry.Completed = ComputeNumericDone(_habit, parsed);
            }
            else
            {
                // Duration: parse H/M/S and validate
                int hh;
                int mm;
                int ss;

                if (int.TryParse(this.TbHours.Text, out hh) == false)
                {
                    MessageBox.Show("Invalid hours value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (int.TryParse(this.TbMinutes.Text, out mm) == false)
                {
                    MessageBox.Show("Invalid minutes value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (int.TryParse(this.TbSeconds.Text, out ss) == false)
                {
                    MessageBox.Show("Invalid seconds value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (hh < 0 || mm < 0 || ss < 0)
                {
                    MessageBox.Show("Duration parts must not be negative.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (mm >= 60 || ss >= 60)
                {
                    MessageBox.Show("Minutes and seconds must be less than 60.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int totalSeconds = (hh * 3600) + (mm * 60) + ss;

                if (totalSeconds < 0)
                {
                    MessageBox.Show("Duration must be non-negative.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (totalSeconds > 24 * 3600)
                {
                    MessageBox.Show("Duration cannot exceed 24 hours.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                this.Entry.DurationSeconds = totalSeconds;
                this.Entry.NumericValue = null;

                // compute Completed automatically
                this.Entry.Completed = ComputeDurationDone(_habit, totalSeconds);
            }

            // Note
            string noteText = this.TbNote.Text;
            if (string.IsNullOrWhiteSpace(noteText))
            {
                this.Entry.Note = null;
            }
            else
            {
                this.Entry.Note = noteText.Trim();
            }

            this.Entry.CreatedAt = DateTime.UtcNow;

            this.DialogResult = true;
        }

        /**
         * @brief Compute whether a numeric entry counts as done.
         * @param h Habit metadata.
         * @param value Numeric value entered.
         * @return true if considered done, false otherwise.
         */
        private bool ComputeNumericDone(Habit h, double value)
        {
            if (h.TargetValue.HasValue)
            {
                double target = h.TargetValue.Value;
                if (h.IsPositive)
                {
                    return (value >= target);
                }
                else
                {
                    return (value < target);
                }
            }
            else
            {
                if (h.IsPositive)
                {
                    return (value > 0.0);
                }
                else
                {
                    return (value == 0.0);
                }
            }
        }

        /**
         * @brief Compute whether a duration entry counts as done.
         * @param h Habit metadata.
         * @param seconds Duration in seconds.
         * @return true if considered done, false otherwise.
         */
        private bool ComputeDurationDone(Habit h, int seconds)
        {
            if (h.TargetValue.HasValue)
            {
                int target = (int)h.TargetValue.Value;
                if (h.IsPositive)
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
    }
}
