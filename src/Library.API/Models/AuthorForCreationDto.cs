using System;
using System.Collections.Generic;

namespace Library.API.Models
{
	public class AuthorForCreationDto
    {
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTimeOffset DateOfBirth { get; set; }
		public string Genre { get; set; }

		// "Extend" to allow creation of an author with books
		// Actually makes sense to just add the property because the AuthorsController CreateAuthor method
		// will automatically deserialize the incoming request whether or not it contains books.
		//
		// I'm uncertain though. The response from the author create includes the URI for getting
		// the author but to get to the created books you have to go the author/id/books. Is this really OK?
		public ICollection<BookForCreationDto> Books { get; set; } = new List<BookForCreationDto>();
	}
}
