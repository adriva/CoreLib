using System;
using System.Threading;

namespace Adriva.Extensions.Reports
{
    public static class ValueFormatters
    {
        public static DateTime StartOfYear(object value) => new DateTime(DateTime.Today.Year, 1, 1);

        public static DateTime StartOfMonth(object value) => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public static DateTime StartOfWeek(object value)
        {
            var systemFirstDayOfWeek = Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

            DateTime date = DateTime.Today;
            while (systemFirstDayOfWeek != date.DayOfWeek)
            {
                date = date.AddDays(-1);
            }
            return date;
        }

        public static DateTime EndOfMonth(object value)
        {
            var date = DateTime.Today.AddMonths(1);
            return date.AddDays(-(date.Day - 1)).AddMilliseconds(-1);
        }

        public static DateTime EndOfYear(object value) => new DateTime(1 + DateTime.Today.Year, 1, 1).AddMilliseconds(-1);

        public static DateTime EndOfWeek(object value)
        {
            var systemFirstDayOfWeek = Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

            DateTime date = DateTime.Today;

            if (systemFirstDayOfWeek == date.DayOfWeek) date = date.AddDays(1);

            while (systemFirstDayOfWeek != date.DayOfWeek)
            {
                date = date.AddDays(1);
            }
            return date.AddMilliseconds(-1);
        }

        public static DateTime AddDays(object value)
        {
            if (value is int deltaDays || int.TryParse(Convert.ToString(value), out deltaDays))
            {
                return DateTime.Today.AddDays(deltaDays);
            }
            return DateTime.Today;
        }

    }
}