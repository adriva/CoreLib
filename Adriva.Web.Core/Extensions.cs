using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Adriva.Web.Core
{
    public static class Extensions
    {
        public static IEnumerable<SelectListItem> ToSelectList<T>(this IEnumerable<T> items, Func<T, string> textFactory, Func<T, string> valueFactory, string selectedValue = null)
        {
            if (null == items) yield break;

            foreach (var item in items)
            {
                string value = valueFactory(item);
                var selectListItem = new SelectListItem(textFactory(item), value);
                selectListItem.Selected = (null != value && 0 == string.Compare(value, selectedValue, StringComparison.Ordinal));
                yield return selectListItem;
            }
        }
    }
}