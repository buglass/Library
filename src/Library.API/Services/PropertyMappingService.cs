using System;
using System.Collections.Generic;
using System.Linq;
using Library.API.Entities;
using Library.API.Models;

namespace Library.API.Services
{
	/// <summary>
	/// Support for sorting
	/// </summary>
	public class PropertyMappingService : IPropertyMappingService
	{
		private Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
			new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Id", new PropertyMappingValue(new List<string> { "Id" } ) },
				{ "Genre", new PropertyMappingValue(new List<string> { "Genre" } ) },
				{ "Age", new PropertyMappingValue(new List<string> { "DateOfBirth" }, true ) },
				{ "Name", new PropertyMappingValue(new List<string> { "FirstName", "LastName" } ) }
			};

		//private IList<PropertyMapping<TSource, TDestination>> propertyMappings = new List<PropertyMapping<TSource, TDestination>>();
		// Use marker interface to allow this implementation without the need to resolve the generics
		private IList<IPropertyMapping> propertyMappings = new List<IPropertyMapping>();

		public PropertyMappingService()
		{
			propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
		}

		public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
		{
			var matchingMapping = propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

			if (matchingMapping.Count() == 1)
				return matchingMapping.Single()._mappingDictionary;

			throw new Exception($"Cannot find property mapping for <{typeof(TSource)}, {typeof(TDestination)}.");
		}

		/// <summary>
		/// Part of mechanism to ensure that the API consumer can be informed appropriately
		/// if they provided an invalid field for sorting etc.
		/// 
		/// Much of the logic is the same as the IQueryable.ApplySort extension
		/// </summary>
		public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
		{
			if (string.IsNullOrWhiteSpace(fields))
				return true;

			var propertyMapping = GetPropertyMapping<TSource, TDestination>();

			var fieldsCollection = fields.Split(',');

			foreach(var field in fieldsCollection)
			{
				var trimmedField = field.Trim();

				var indexOfFirstSpace = trimmedField.IndexOf(" ");
				var propertyName = indexOfFirstSpace == -1 ? trimmedField : trimmedField.Remove(indexOfFirstSpace);

				if (!propertyMapping.ContainsKey(propertyName))
					return false;
			}

			return true;
		}
    }
}
