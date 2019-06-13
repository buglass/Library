using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Library.API.Helpers
{
	public class ArrayModelBinder : IModelBinder
    {
		public Task BindModelAsync(ModelBindingContext bindingContext)
		{
			// Our binder works only on enumerable types
			if (!bindingContext.ModelMetadata.IsEnumerableType)
			{
				bindingContext.Result = ModelBindingResult.Failed();
				return Task.CompletedTask;
			}

			// Get the inputted value through the value provider
			var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).ToString();

			// We expect a list of Guids. If the value is null or whitespace then return null
			if (string.IsNullOrWhiteSpace(value))
			{
				bindingContext.Result = ModelBindingResult.Success(null);
				return Task.CompletedTask;
			}

			// Get the enumerable's type (we expect a Guid)
			var elementType = bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0];

			// Now we can get a converter which will (in this case) convert string types to guids
			var converter = TypeDescriptor.GetConverter(elementType);

			// Split the string delimited value and convert each string in turn
			var values = value.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => converter.ConvertFromString(x.Trim()))
				.ToArray();

			// Instantiate an array of the specified type and length then populate it with data
			var typedValues = Array.CreateInstance(elementType, values.Length); // Instantiate array of type and length
			values.CopyTo(array: typedValues, index: 0); // Populate it with values data from index 0
			bindingContext.Model = typedValues; // Set incoming binding context model to the values

			// Return success with the populated model
			bindingContext.Result = ModelBindingResult.Success(bindingContext.Model); // Successful result with data
			return Task.CompletedTask;
		}
    }
}
