using System.Collections.Generic;

namespace Library.API.Models
{
	/// <summary>
	/// Wrapper to support HATEOAS using the statically typed approach for collections
	/// </summary>
	public class LinkedCollectionResourceWrapperDto<T> : LinkedResourceBaseDto
		where T : LinkedResourceBaseDto
    {
		public IEnumerable<T> Value { get; set; }

		public LinkedCollectionResourceWrapperDto(IEnumerable<T> value)
		{
			Value = value;
		}
    }
}
