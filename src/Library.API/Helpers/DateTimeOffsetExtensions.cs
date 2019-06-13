using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helpers
{
    public static class DateTimeOffsetExtensions
    {
		//public static int GetCurrentAge(this DateTimeOffset dateTimeOffset)
		//{
		//    var currentDate = DateTime.UtcNow;
		//    int age = currentDate.Year - dateTimeOffset.Year;

		//    if (currentDate < dateTimeOffset.AddYears(age))
		//    {
		//        age--;
		//    }

		//    return age;
		//}

		/// <summary>
		/// v2 - add support for date of death
		/// </summary>
		public static int GetCurrentAge(this DateTimeOffset dateTimeOffset, DateTimeOffset? dateOfDeath)
		{
			var dateToCalculateTo = dateOfDeath.HasValue ? dateOfDeath.Value : DateTime.UtcNow;

			int age = dateToCalculateTo.Year - dateTimeOffset.Year;

			if (dateToCalculateTo < dateTimeOffset.AddYears(age))
			{
				age--;
			}

			return age;
		}
	}
}
