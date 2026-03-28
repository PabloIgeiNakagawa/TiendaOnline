using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TiendaOnline.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .FirstOrDefault()
                            ?.GetCustomAttribute<DisplayAttribute>()
                            ?.Name ?? enumValue.ToString();
        }

        public static IEnumerable<SelectListItem> ToSelectList<T>(this T? selectedValue) where T : struct, Enum
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new SelectListItem
                {
                    // Convertimos el valor del enum a int y luego a string para el Value del <option>
                    Value = Convert.ToInt32(e).ToString(),
                    Text = e.GetDisplayName(),
                    Selected = selectedValue.HasValue && e.Equals(selectedValue.Value)
                });
        }
    }
}
