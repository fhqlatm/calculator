// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//
// DateCalculator.xaml.h
// Declaration of the DateCalculator class
//

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using CalculatorApp;
using CalculatorApp.ViewModel;
using CalculatorApp.ViewModel.Common;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.System.UserProfile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace CalculatorApp
{
    [Windows.Foundation.Metadata.WebHostHidden]
    public sealed partial class DateCalculator
    {
        public DateCalculator()
        {
            InitializeComponent();

            // Set Calendar Identifier
            DateDiff_FromDate.CalendarIdentifier = localizationSettings.GetCalendarIdentifier();
            DateDiff_ToDate.CalendarIdentifier = localizationSettings.GetCalendarIdentifier();

            // Setting the FirstDayofWeek
            DateDiff_FromDate.FirstDayOfWeek = localizationSettings.GetFirstDayOfWeek();
            DateDiff_ToDate.FirstDayOfWeek = localizationSettings.GetFirstDayOfWeek();

            // Setting the Language explicitly is not required,
            // this is a workaround for the bug in the control due to which
            // the displayed date is incorrect for non Gregorian Calendar Systems
            // The displayed date doesn't honor the shortdate format, on setting the Language the format is refreshed
            DateDiff_FromDate.Language = localizationSettings.GetLocaleName();
            DateDiff_ToDate.Language = localizationSettings.GetLocaleName();

            // Set Min and Max Dates according to the Gregorian Calendar(1601 & 9999)
            var calendar = new Calendar();
            var today = calendar.GetDateTime();

            var lunarCalender = new Calendar();
            int[] v = new int[3];
            v = getLunarDate(calendar.Year, calendar.Month, calendar.Day);
            lunarCalender.Year = v[0];
            lunarCalender.Month = v[1];
            lunarCalender.Day = v[2];
            var lunarToday = lunarCalender.GetDateTime();

            calendar.ChangeCalendarSystem(CalendarIdentifiers.Gregorian);
            calendar.Day = 1;
            calendar.Month = 1;
            calendar.Year = c_minYear;
            var minYear = calendar.GetDateTime(); // 1st January, 1601
            DateDiff_FromDate.MinDate = minYear;
            DateDiff_ToDate.MinDate = minYear;

            calendar.Day = 31;
            calendar.Month = 12;
            calendar.Year = c_maxYear;
            var maxYear = calendar.GetDateTime(); // 31st December, 9878
            DateDiff_FromDate.MaxDate = maxYear;
            DateDiff_ToDate.MaxDate = maxYear;

            // Set the PlaceHolderText for CalendarDatePicker
            DateTimeFormatter dateTimeFormatter = LocalizationService.GetInstance().GetRegionalSettingsAwareDateTimeFormatter(
                "day month year",
                localizationSettings.GetCalendarIdentifier(),
                ClockIdentifiers.TwentyFourHour); // Clock Identifier is not used

            DateDiff_FromDate.DateFormat = "day month year";
            DateDiff_FromDate2.DateFormat = "day month year";
            DateDiff_ToDate.DateFormat = "day month year";
            DateDiff_ToDate2.DateFormat = "day month year";

            var placeholderText = dateTimeFormatter.Format(today);
            var placeholderText2 = dateTimeFormatter.Format(lunarToday);

            DateDiff_FromDate.PlaceholderText = placeholderText;
            DateDiff_FromDate2.PlaceholderText = placeholderText2;
            DateDiff_ToDate.PlaceholderText = placeholderText;
            DateDiff_ToDate2.PlaceholderText = placeholderText2;

            CopyMenuItem.Text = AppResourceProvider.GetInstance().GetResourceString("copyMenuItem");
            DateCalculationOption.SelectionChanged += DateCalcOption_Changed;
        }

        public void CloseCalendarFlyout()
        {
            if (DateDiff_FromDate.IsCalendarOpen)
            {
                DateDiff_FromDate.IsCalendarOpen = false;
            }

            if (DateDiff_ToDate.IsCalendarOpen)
            {
                DateDiff_ToDate.IsCalendarOpen = false;
            }

            if ((AddSubtract_FromDate != null) && (AddSubtract_FromDate.IsCalendarOpen))
            {
                AddSubtract_FromDate.IsCalendarOpen = false;
            }
        }

        public void SetDefaultFocus()
        {
            DateCalculationOption.Focus(FocusState.Programmatic);
        }

        private void FromDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e)
        {
            if (e.NewDate != null)
            {
                var dateCalcViewModel = (DateCalculatorViewModel)DataContext;
                dateCalcViewModel.FromDate = e.NewDate.Value;
                TraceLogger.GetInstance().LogDateCalculationModeUsed(false);

                int[] v = new int[3];
                
                v = getLunarDate(DateDiff_FromDate.Date.Value.Year, DateDiff_FromDate.Date.Value.Month, DateDiff_FromDate.Date.Value.Day);

                DateDiff_FromDate2.Date = new DateTime(v[0], v[1], v[2]);
            }
            else
            {
                ReselectCalendarDate(sender, e.OldDate.Value);
            }
        }

        private void ToDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e)
        {
            if (e.NewDate != null)
            {
                var dateCalcViewModel = (DateCalculatorViewModel)this.DataContext;
                dateCalcViewModel.ToDate = e.NewDate.Value;
                TraceLogger.GetInstance().LogDateCalculationModeUsed(false);

                int[] v = new int[3];

                v = getLunarDate(DateDiff_ToDate.Date.Value.Year, DateDiff_ToDate.Date.Value.Month, DateDiff_ToDate.Date.Value.Day);

                DateDiff_ToDate2.Date = new DateTime(v[0], v[1], v[2]);
            }
            else
            {
                ReselectCalendarDate(sender, e.OldDate.Value);
            }
        }

        private void AddSubtract_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs e)
        {
            if (e.NewDate != null)
            {
                var dateCalcViewModel = (DateCalculatorViewModel)this.DataContext;
                dateCalcViewModel.StartDate = e.NewDate.Value;
                TraceLogger.GetInstance().LogDateCalculationModeUsed(true);
            }
            else
            {
                ReselectCalendarDate(sender, e.OldDate.Value);
            }
        }

        private void OffsetValue_Changed(object sender, SelectionChangedEventArgs e)
        {
            var dateCalcViewModel = (DateCalculatorViewModel)this.DataContext;
            // do not log diagnostics for no-ops and initialization of combo boxes
            if (dateCalcViewModel.DaysOffset != 0 || dateCalcViewModel.MonthsOffset != 0 || dateCalcViewModel.YearsOffset != 0)
            {
                TraceLogger.GetInstance().LogDateCalculationModeUsed(true);
            }
        }

        private void OnCopyMenuItemClicked(object sender, RoutedEventArgs e)
        {
            var calcResult = (TextBlock)ResultsContextMenu.Target;

            CopyPasteManager.CopyToClipboard(calcResult.Text);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

        private void DateCalcOption_Changed(object sender, SelectionChangedEventArgs e)
        {
            FindName("AddSubtractDateGrid");
            var dateCalcViewModel = (DateCalculatorViewModel)this.DataContext;

            // From Date Field needs to persist across Date Difference and Add Substract Date Mode.
            // So when the mode dropdown changes, update the other datepicker with the latest date.
            if (dateCalcViewModel.IsDateDiffMode)
            {
                if (AddSubtract_FromDate.Date == null)
                {
                    return;
                }
                DateDiff_FromDate.Date = AddSubtract_FromDate.Date.Value;
            }
            else
            {
                if (DateDiff_FromDate.Date == null)
                {
                    // If no date has been picked, then this can be null.
                    return;
                }
                AddSubtract_FromDate.Date = DateDiff_FromDate.Date.Value;
            }
        }

        private void AddSubtractDateGrid_Loaded(object sender, RoutedEventArgs e)
        {
            AddSubtract_FromDate.PlaceholderText = DateDiff_FromDate.PlaceholderText;
            AddSubtract_FromDate.CalendarIdentifier = localizationSettings.GetCalendarIdentifier();
            AddSubtract_FromDate.FirstDayOfWeek = localizationSettings.GetFirstDayOfWeek();
            AddSubtract_FromDate.Language = localizationSettings.GetLocaleName();

            AddSubtract_FromDate.MinDate = DateDiff_FromDate.MinDate;
            AddSubtract_FromDate.MaxDate = DateDiff_FromDate.MaxDate;
            AddSubtract_FromDate.DateFormat = "day month year";
        }

        private void AddSubtractOption_Checked(object sender, RoutedEventArgs e)
        {
            RaiseLiveRegionChangedAutomationEvent(false);
        }

        private void ReselectCalendarDate(CalendarDatePicker calendarDatePicker, DateTimeOffset? dateTime)
        {
            // Reselect the unselected Date
            calendarDatePicker.Date = dateTime;

            // Dismiss the Calendar flyout
            calendarDatePicker.IsCalendarOpen = false;
        }

        private void OffsetDropDownClosed(object sender, object e)
        {
            RaiseLiveRegionChangedAutomationEvent(false);
        }

        private void CalendarFlyoutClosed(object sender, object e)
        {
            var dateCalcViewModel = (DateCalculatorViewModel)this.DataContext;
            RaiseLiveRegionChangedAutomationEvent(dateCalcViewModel.IsDateDiffMode);
        }

        private void RaiseLiveRegionChangedAutomationEvent(bool isDateDiffMode)
        {
            TextBlock resultTextBlock = isDateDiffMode ? DateDiffAllUnitsResultLabel : DateResultLabel;
            string automationName = AutomationProperties.GetName(resultTextBlock);
            TextBlockAutomationPeer.FromElement(resultTextBlock).RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        private void OnVisualStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            TraceLogger.GetInstance().LogVisualStateChanged(ViewMode.Date, e.NewState.Name, false);
        }

        // We choose 2550 as the max year because CalendarDatePicker experiences clipping
        // issues just after 2558.  We would like 9999 but will need to wait for a platform
        // fix before we use a higher max year.  This platform issue is tracked by
        // TODO: MSFT-9273247
        private const int c_maxYear = 2550;
        private const int c_minYear = 1601;

        private static readonly LocalizationSettings localizationSettings = LocalizationSettings.GetInstance();

        int[] lunarDayOfMonth = new int[6]{ 29, 30, 58, 59, 59, 60 };

        int[] lunarDayOfMonthFrac = new int[6]{ 0, 0, 29, 30, 30, 30 };

        int[,] lunarDayOfMonthIndex = new int[50,12]{
            // 2001 ~ 2010
            { 1, 1, 1, 2, 1, 0, 0, 1, 0, 1, 0, 1 },
            { 1, 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0 },
            { 1, 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1 },
            { 0, 4, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 0 },
            { 1, 0, 1, 0, 1, 0, 4, 1, 1, 0, 1, 1 },
            { 0, 0, 1, 0, 0, 1, 0, 1, 1, 1, 0, 1 },
            { 1, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1 },
            { 1, 1, 0, 0, 4, 0, 1, 0, 1, 0, 1, 1 },
            { 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1 },
            // 2011 ~ 2020
            { 1, 0, 1, 1, 0, 1, 0, 0, 1, 0, 1, 0 },
            { 1, 0, 5, 1, 0, 1, 0, 0, 1, 0, 1, 0 },
            { 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1 },
            { 0, 1, 0, 1, 0, 1, 0, 1, 4, 1, 0, 1 },
            { 0, 1, 0, 0, 1, 0, 1, 1, 1, 0, 1, 1 },
            { 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 1 },
            { 1, 0, 0, 1, 2, 1, 0, 1, 0, 1, 1, 1 },
            { 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 1 },
            { 1, 0, 1, 0, 1, 0, 0, 1, 0, 1, 0, 1 },
            { 1, 0, 1, 4, 1, 0, 0, 1, 0, 1, 0, 1 },
            // 2021 ~ 2030
            { 0, 1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0 },
            { 1, 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1 },
            { 0, 4, 1, 0, 1, 0, 1, 1, 0, 1, 0, 1 },
            { 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0 },
            { 1, 0, 1, 0, 0, 4, 1, 0, 1, 1, 1, 0 },
            { 1, 0, 1, 0, 0, 1, 0, 1, 0, 1, 1, 1 },
            { 0, 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 1 },
            { 0, 1, 1, 0, 4, 0, 1, 0, 0, 1, 1, 0 },
            { 1, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 1 },
            { 0, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0 },
            // 2031 ~ 2040
            { 1, 0, 4, 1, 0, 1, 1, 0, 1, 0, 1, 0 },
            { 1, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1 },
            { 0, 1, 0, 0, 1, 0, 4, 1, 1, 1, 0, 1 },
            { 0, 1, 0, 0, 1, 0, 1, 0, 1, 1, 1, 0 },
            { 1, 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1 },
            { 1, 1, 0, 1, 0, 3, 0, 0, 1, 0, 1, 1 },
            { 1, 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1 },
            { 1, 1, 0, 1, 0, 1, 0, 1, 0, 0, 1, 0 },
            { 1, 1, 0, 1, 4, 1, 0, 1, 0, 1, 0, 0 },
            { 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 0 },
            // 2041 ~ 2050
            { 1, 0, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1 },
            { 0, 4, 0, 1, 0, 1, 0, 1, 1, 0, 1, 1 },
            { 0, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1 },
            { 1, 0, 1, 0, 0, 1, 2, 1, 0, 1, 1, 1 },
            { 1, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1 },
            { 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 0, 1 },
            { 1, 0, 1, 1, 3, 0, 1, 0, 0, 1, 0, 1 },
            { 0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 0, 0 },
            { 1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0 },
            { 1, 0, 3, 0, 1, 0, 1, 1, 0, 1, 1, 0 }
        };

        int[] lunarDayOfYear = new int[50]{
            384, 354, 355, 384, 354, 385, 354, 354, 384, 354, // 2001 ~ 2010
            354, 384, 355, 384, 355, 354, 384, 354, 354, 384, // 2011 ~ 2020
            354, 355, 384, 354, 384, 355, 354, 383, 355, 354, // 2021 ~ 2030
            384, 355, 384, 354, 354, 384, 354, 354, 384, 355, // 2031 ~ 2040
            355, 384, 354, 384, 354, 354, 384, 353, 355, 384  // 2041 ~ 2050
        };

        int[] solarDayNum = new int[12]{ 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        int getSolarDayOfMonth(int y, int m)
        { // 월별 일수 계산
            if (m != 2)
                return solarDayNum[m - 1];

            if ((y % 400 == 0) || ((y % 100 != 0) && (y % 4 == 0)))
                return 29;
            else
                return 28;
        }

        int getSolarDayOfYear(int y)
        {
            if ((y % 400 == 0) || ((y % 100 != 0) && (y % 4 == 0)))
                return 366;
            else
                return 365;
        }

        int getTotalDaySolar(int y, int m, int d)
        {
            // 양력 2001/01/24 = 음력 2001/01/01
            int[] solarBasis = new int[3]{ 2001, 1, 24 };
            int i;
            int ret = 0;

            for (i = solarBasis[0]; i < y; i++)
                ret += getSolarDayOfYear(i);
            for (i = 1; i < m; i++)
                ret += getSolarDayOfMonth(y, i);
            ret += d;
            for (i = 1; i < solarBasis[1]; i++)
                ret -= getSolarDayOfMonth(solarBasis[0], i);
            ret -= solarBasis[2];

            return ret;
        }

        int[] getLunarDate(int solarYear, int solarMonth, int solarDay)
        {
            int[] v = new int[3];
            int y = -1, m = -1, d = 0, f;
            int totalDay = getTotalDaySolar(solarYear, solarMonth, solarDay);

            while (totalDay >= lunarDayOfYear[++y])
                totalDay -= lunarDayOfYear[y];
            while (totalDay >= (d = lunarDayOfMonth[lunarDayOfMonthIndex[y,++m]]))
                totalDay -= d;
            d = totalDay;

            f = lunarDayOfMonthFrac[lunarDayOfMonthIndex[y,m]];
            if (d >= f)
                d -= f;

            v[0] = (y + 1) + 2000;
            v[1] = (m + 1);
            v[2] = (d + 1);

            return v;
        }

    }
}
