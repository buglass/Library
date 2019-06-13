namespace Library.API.Services
{
	/// <summary>
	/// A 'marker interface' (an empty interface) used in this case to allow the PropertyMappingService
	/// to use the PropertyMapping without needing to resolve the generics (of TSource and TDestination).
	/// </summary>
	public interface IPropertyMapping
    {
    }
}
