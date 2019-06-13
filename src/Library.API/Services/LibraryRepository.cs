using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Services
{
    public class LibraryRepository : ILibraryRepository
    {
        private LibraryContext _context;
		private readonly IPropertyMappingService _propertyMappingService;

		public LibraryRepository(LibraryContext context, IPropertyMappingService propertyMappingService)
        {
            _context = context;
			_propertyMappingService = propertyMappingService;
        }

        public void AddAuthor(Author author)
        {
            author.Id = Guid.NewGuid();
            _context.Authors.Add(author);

            // the repository fills the id (instead of using identity columns)
            if (author.Books.Any())
            {
                foreach (var book in author.Books)
                {
                    book.Id = Guid.NewGuid();
                }
            }
        }

        public void AddBookForAuthor(Guid authorId, Book book)
        {
            var author = GetAuthor(authorId);
            if (author != null)
            {
                // if there isn't an id filled out (ie: we're not upserting),
                // we should generate one
                if (book.Id == Guid.Empty)
                {
                    book.Id = Guid.NewGuid();
                }
                author.Books.Add(book);
            }
        }

        public bool AuthorExists(Guid authorId)
        {
            return _context.Authors.Any(a => a.Id == authorId);
        }

        public void DeleteAuthor(Author author)
        {
            _context.Authors.Remove(author);
        }

        public void DeleteBook(Book book)
        {
            _context.Books.Remove(book);
        }

        public Author GetAuthor(Guid authorId)
        {
            return _context.Authors.FirstOrDefault(a => a.Id == authorId);
        }

		//public IEnumerable<Author> GetAuthors()
		//{
		//    return _context.Authors.OrderBy(a => a.FirstName).ThenBy(a => a.LastName);
		//}
		// Implementation to support paging
		public PagedList<Author> GetAuthors(AuthorsResourceParameters authorsResourceParameters)
		{
			//  return
			//		_context.Authors
			//		.OrderBy(a => a.FirstName)
			//		.ThenBy(a => a.LastName)
			//		.Skip(authorsResourceParameters.PageSize * (authorsResourceParameters.PageNumber - 1))
			//		.Take(authorsResourceParameters.PageSize);

			//var collection = _context.Authors.OrderBy(a => a.FirstName).ThenBy(a => a.LastName).AsQueryable(); // AsQueryable for IQueryable to support filtering

			// Support sorting from consumer. Requires mapping from consumer contract to model properties
			// which in this case is from 'Name' to 'First name' and 'Last name'.
			// Using System.Linq.Dynamic.Sort for providing a string to OrderBy.

			var mappingDictionary = _propertyMappingService.GetPropertyMapping<AuthorDto, Author>();

			// Apply sort to collection using a custom ApplySort extension on IQueryable.
			var collection = _context.Authors.ApplySort(
				authorsResourceParameters.OrderBy,
				mappingDictionary);

			// Filtering
			if (!string.IsNullOrEmpty(authorsResourceParameters.Genre))
			{
				string genreFilter = authorsResourceParameters.Genre.Trim().ToLowerInvariant();
				collection = collection.Where(a => a.Genre.ToLowerInvariant() == genreFilter);
			}

			// Searching (simple hard-coded implementation). Could use Lucene?
			if (!string.IsNullOrEmpty(authorsResourceParameters.SearchQuery))
			{
				string searchQuery = authorsResourceParameters.SearchQuery.Trim().ToLowerInvariant();

				collection = collection
					.Where(a => a.Genre.ToLowerInvariant().Contains(searchQuery)
					|| a.FirstName.ToLowerInvariant().Contains(searchQuery)
					|| a.LastName.ToLowerInvariant().Contains(searchQuery));
			}


			// paging
			return PagedList<Author>.Create(
				source: collection,
				pageNumber: authorsResourceParameters.PageNumber,
				pageSize: authorsResourceParameters.PageSize);
		}


		public IEnumerable<Author> GetAuthors(IEnumerable<Guid> authorIds)
        {
            return _context.Authors.Where(a => authorIds.Contains(a.Id))
                .OrderBy(a => a.FirstName)
                .OrderBy(a => a.LastName)
                .ToList();
        }

        public void UpdateAuthor(Author author)
        {
            // no code in this implementation
        }

        public Book GetBookForAuthor(Guid authorId, Guid bookId)
        {
            return _context.Books
              .Where(b => b.AuthorId == authorId && b.Id == bookId).FirstOrDefault();
        }

        public IEnumerable<Book> GetBooksForAuthor(Guid authorId)
        {
            return _context.Books
                        .Where(b => b.AuthorId == authorId).OrderBy(b => b.Title).ToList();
        }

        public void UpdateBookForAuthor(Book book)
        {
            // no code in this implementation
        }

        public bool Save()
        {
            return (_context.SaveChanges() >= 0);
        }
    }
}
