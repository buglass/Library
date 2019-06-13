using System.Collections.Generic;

namespace Library.API.Services
{
	/// <summary>
	/// Interface to register custom PropertyMappingService at startup
	/// </summary>
	public interface IPropertyMappingService
    {
		Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
		bool ValidMappingExistsFor<TSource, TDestination>(string fields);
	}
}
