using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Library.API.Helpers
{
	public static class IEnumerableExtensions
    {
		/// <summary>
		/// Support for data shaping
		/// 
		/// NB - This approach results in property name casing being lost on serialization.
		/// A new contract resolver (CamelCasePropertyNamesContractResolver) can be configured to resolve this.
		/// </summary>
		public static IEnumerable<ExpandoObject> ShapeData<TSource>(
			this IEnumerable<TSource> source,
			string fields)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			// 1 - Populate the propertyInfoList with the properties required
			// Do this once and re-use it for each object in the incoming collection to save on expensive reflection operations
			var propertyInfoList = new List<PropertyInfo>();
			if (string.IsNullOrWhiteSpace(fields))
			{
				// Use all of the public properties from the object
				propertyInfoList.AddRange(typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance));
			}
			else
			{
				var fieldCollection = fields.Split(',');

				foreach(var field in fieldCollection)
				{
					var propertyName = field.Trim();

					PropertyInfo propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

					if (propertyInfo == null)
						throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");

					propertyInfoList.Add(propertyInfo);
				}
			}

			// 2 - Populate an ExpandoObject with the property data required
			var expandoObjects = new List<ExpandoObject>();
			foreach (TSource sourceObject in source)
			{
				var expandoObject = new ExpandoObject();

				foreach (var propertyInfo in propertyInfoList)
				{
					var propertyValue = propertyInfo.GetValue(sourceObject);
					((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
				}

				expandoObjects.Add(expandoObject);
			}

			return expandoObjects;
		}
    }
}
