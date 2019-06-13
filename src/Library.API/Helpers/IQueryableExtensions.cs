using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Library.API.Services;

namespace Library.API.Helpers
{
	public static class IQueryableExtensions
    {
		public static IQueryable<T> ApplySort<T>(
			this IQueryable<T> source,
			string orderBy,
			Dictionary<string, PropertyMappingValue> mappingDictionary)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			if (mappingDictionary == null)
				throw new ArgumentNullException("mappingDictionary");

			if (string.IsNullOrEmpty(orderBy))
				return source; // No sorting to do

			// orderBy is comma delimited
			var orderByClauses = orderBy.Split(',');

			// Apply each orderBy clause in reverse order otherwise the IQueryable will be ordered in reverse
			foreach(var orderByClause in orderByClauses.Reverse())
			{
				var trimmedOrderByClause = orderByClause.Trim();

				bool orderDescending = trimmedOrderByClause.EndsWith(" desc");

				// Clause could be 'fieldName asc' or 'fieldname desc' so remove order direction clause
				var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
				var propertyName = indexOfFirstSpace == -1 ? trimmedOrderByClause : trimmedOrderByClause.Remove(indexOfFirstSpace);

				if (!mappingDictionary.ContainsKey(propertyName))
					throw new ArgumentException($"Key mapping for {propertyName} is missing");

				var propertyMappingValue = mappingDictionary[propertyName];

				if (propertyMappingValue == null)
					throw new ArgumentNullException("propertyMappingValue");

				// Iterate through property names in reverse and apply ordering
				foreach (var destinationProperty in propertyMappingValue.DestinationProperties.Reverse())
				{
					// Invert order direction if needed
					// For example ordering by age asc on the contract equates to ordering by DoB desc on the model
					if (propertyMappingValue.Revert)
						orderDescending = !orderDescending;

					source = source.OrderBy(destinationProperty + (orderDescending ? " descending" : " ascending"));
				}
			}

			return source;
		}
	}
}
