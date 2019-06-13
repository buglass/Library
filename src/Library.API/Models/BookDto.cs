using System;

namespace Library.API.Models
{
	// To support HATEOAS using the statically typed approach, extend the LinkedResourceBaseDto base class

	public class BookDto : LinkedResourceBaseDto
    {
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public Guid AuthorId { get; set; }
	}
}
