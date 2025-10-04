using System.Globalization;
using System.Windows;
using MyHabitsTracker.Models;

namespace MyHabitsTracker.Views
{
    /**
     * @brief Code-behind for the Habit creation/edit dialog.
     */
    public partial class HabitDialog : Window
    {
        /** @brief The habit model that will be created or edited. */
        public Habit Habit { get; private set; }

        /** @brief True if dialog is editing an existing habit. */
        private bool _isEditingExisting;

        /**
         * @brief Constructor for creating a new habit.
         */
        public HabitDialog()
        {
            InitializeComponent();

            Habit newHabit = new Habit();
            this.Habit = newHabit;
            _isEditingExisting = false;

            // Default: show checkbox type selected
            this.CbType.SelectedIndex = 0;

            // Wire events
            this.CbType.SelectionChanged += CbType_SelectionChanged;
            this.BtnOk.Click += BtnOk_Click;
            this.BtnCancel.Click += BtnCancel_Click;

            // Initialize duration targets with zeros
            this.TbTargetHours.Text = "0";
            this.TbTargetMinutes.Text = "0";
            this.TbTargetSeconds.Text = "0";
        }

        /**
         * @brief Constructor for editing an existing habit.
         * @param existing Habit loaded from database.
         *
         * When editing existing habit, the type/positive/target fields are disabled.
         */
        public HabitDialog(Habit existing)
        {
            InitializeComponent();

            this.Habit = existing;
            _isEditingExisting = true;

            // Fill controls
            this.TbTitle.Text = (existing.Title == null) ? string.Empty : existing.Title;
            this.TbDesc.Text = (existing.Description == null) ? string.Empty : existing.Description;

            this.CbPositive.IsChecked = existing.IsPositive;
            this.CbPositive.IsEnabled = false; // cannot change positive flag on edit

            int idx = existing.TrackType;
            if (idx < 0)
            {
                idx = 0;
            }

            this.CbType.SelectedIndex = idx;
            this.CbType.IsEnabled = false; // cannot change type on edit

            // Fill and disable target controls based on type
            if (existing.TrackType == 1)
            {
                // Numerical
                if (existing.TargetValue.HasValue)
                {
                    this.TbTargetNumeric.Text = existing.TargetValue.Value.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    this.TbTargetNumeric.Text = string.Empty;
                }

                this.SpTargetNumeric.Visibility = Visibility.Visible;
                this.TbTargetNumeric.IsEnabled = false;
                this.SpTargetDuration.Visibility = Visibility.Collapsed;
            }
            else if (existing.TrackType == 2)
            {
                // Duration: target stored in seconds
                if (existing.TargetValue.HasValue)
                {
                    int totalSeconds = (int)existing.TargetValue.Value;
                    int h = totalSeconds / 3600;
                    int rem = totalSeconds % 3600;
                    int m = rem / 60;
                    int s = rem % 60;

                    this.TbTargetHours.Text = h.ToString(CultureInfo.InvariantCulture);
                    this.TbTargetMinutes.Text = m.ToString(CultureInfo.InvariantCulture);
                    this.TbTargetSeconds.Text = s.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    this.TbTargetHours.Text = "0";
                    this.TbTargetMinutes.Text = "0";
                    this.TbTargetSeconds.Text = "0";
                }

                this.SpTargetDuration.Visibility = Visibility.Visible;
                this.TbTargetHours.IsEnabled = false;
                this.TbTargetMinutes.IsEnabled = false;
                this.TbTargetSeconds.IsEnabled = false;
                this.SpTargetNumeric.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Checkbox
                this.SpTargetNumeric.Visibility = Visibility.Collapsed;
                this.SpTargetDuration.Visibility = Visibility.Collapsed;
            }

            // Wire events
            this.CbType.SelectionChanged += CbType_SelectionChanged;
            this.BtnOk.Click += BtnOk_Click;
            this.BtnCancel.Click += BtnCancel_Click;
        }

        /**
         * @brief Handler when user changes the TrackType (creation mode).
         *
         * Shows/hides the target controls depending on selected type.
         */
        private void CbType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isEditingExisting)
            {
                // ignore changes in edit mode (combobox disabled)
                return;
            }

            int sel = this.CbType.SelectedIndex;
            if (sel == 1)
            {
                // Numerical
                this.SpTargetNumeric.Visibility = Visibility.Visible;
                this.SpTargetDuration.Visibility = Visibility.Collapsed;
            }
            else if (sel == 2)
            {
                // Duration
                this.SpTargetNumeric.Visibility = Visibility.Collapsed;
                this.SpTargetDuration.Visibility = Visibility.Visible;
            }
            else
            {
                // Checkbox
                this.SpTargetNumeric.Visibility = Visibility.Collapsed;
                this.SpTargetDuration.Visibility = Visibility.Collapsed;
            }
        }

        /**
         * @brief Cancel button handler.
         */
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /**
         * @brief OK button handler: validate input and write values back into Habit model.       
         */
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Title required
            if (string.IsNullOrWhiteSpace(this.TbTitle.Text))
            {
                MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.Habit.Title = this.TbTitle.Text.Trim();

            string desc = this.TbDesc.Text;
            if (string.IsNullOrWhiteSpace(desc))
            {
                this.Habit.Description = null;
            }
            else
            {
                this.Habit.Description = desc.Trim();
            }

            if (_isEditingExisting == false)
            {
                // Allow setting positive flag and type on creation
                this.Habit.IsPositive = (this.CbPositive.IsChecked == true);

                int sel = this.CbType.SelectedIndex;
                if (sel < 0)
                {
                    sel = 0;
                }

                this.Habit.TrackType = sel;

                if (sel == 1)
                {
                    // Numeric target
                    double parsed;
                    if (double.TryParse(this.TbTargetNumeric.Text, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) == false)
                    {
                        if (string.IsNullOrWhiteSpace(this.TbTargetNumeric.Text) == false)
                        {
                            MessageBox.Show("Numeric target is invalid. Use a numeric value.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        // empty target allowed
                        this.Habit.TargetValue = null;
                    }
                    else
                    {
                        if (parsed < 0.0)
                        {
                            MessageBox.Show("Numeric target must be non-negative.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        this.Habit.TargetValue = parsed;
                    }
                }
                else if (sel == 2)
                {
                    // Duration target from hh/mm/ss
                    int hh;
                    int mm;
                    int ss;

                    if (int.TryParse(this.TbTargetHours.Text, out hh) == false)
                    {
                        MessageBox.Show("Invalid hours in duration target.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (int.TryParse(this.TbTargetMinutes.Text, out mm) == false)
                    {
                        MessageBox.Show("Invalid minutes in duration target.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (int.TryParse(this.TbTargetSeconds.Text, out ss) == false)
                    {
                        MessageBox.Show("Invalid seconds in duration target.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    if (totalSeconds > 24 * 3600)
                    {
                        MessageBox.Show("Duration target cannot exceed 24 hours.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    this.Habit.TargetValue = (double)totalSeconds;
                }
                else
                {
                    // Checkbox: clear target
                    this.Habit.TargetValue = null;
                }
            }
            else
            {
                // Editing existing habit: do not change TrackType, IsPositive, or Target
                // Controls are disabled in the UI; nothing to do here.
            }

            this.Habit.UpdatedAt = DateTime.UtcNow;
            if (this.Habit.Id == 0)
            {
                this.Habit.CreatedAt = DateTime.UtcNow;
            }

            this.DialogResult = true;
        }
    }
}
