using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Library.API.Helpers
{
	public static class ObjectExtensions
    {
		public static ExpandoObject ShapeData<TSource>(
			this TSource source,
			string fields)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var expandoObject = new ExpandoObject();

			if (string.IsNullOrEmpty(fields))
			{
				var propertyInfos = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				foreach (var propertyInfo in propertyInfos)
				{
					var propertyValue = propertyInfo.GetValue(source);
					((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
				}
			}
			else
			{
				var fieldCollection = fields.Split(',');
				foreach (var field in fieldCollection)
				{
					var propertyName = field.Trim();

					PropertyInfo propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

					if (propertyInfo == null)
						throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");

					var propertyValue = propertyInfo.GetValue(source);
					((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
				}
			}

			return expandoObject;
		}
	}
}
