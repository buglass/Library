using System.Collections.Generic;

namespace Library.API.Models
{
	/// <summary>
	/// Supporting HATEOAS using the statically typed approach
	/// with a base class including links which model classes can extend.
	/// 
	/// The alternative is a dynamically typed approach which will support anonymous types (and therefore data shaping).
	/// </summary>
	public abstract class LinkedResourceBaseDto
    {
		public List<LinkDto> Links { get; set; } = new List<LinkDto>();
    }
}
