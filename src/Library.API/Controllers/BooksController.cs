using System;
using System.Collections.Generic;
using System.Linq;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Library.API.Controllers
{
	[Route("api/authors/{authorid}/books")]
	public class BooksController : Controller
    {
		private readonly ILibraryRepository _libraryRepository;
		private readonly ILogger<BooksController> _logger;
		private readonly IUrlHelper _urlHelper;

		public BooksController(
			ILibraryRepository libraryRepository,
			ILogger<BooksController> logger,
			IUrlHelper urlHelper)
		{
			_libraryRepository = libraryRepository;
			_logger = logger;
			_urlHelper = urlHelper; // Supporting HATEOAS using the statically typed approach - Use this to generate links
		}

		[HttpGet(Name = "GetBooksForAuthor")] // Name to support statically typed HATEOAS
		public IActionResult GetBooksForAuthor(Guid authorId)
		{
			return
				_libraryRepository.AuthorExists(authorId)
				?
				//Ok(AutoMapper.Mapper.Map<IEnumerable<BookDto>>(_libraryRepository.GetBooksForAuthor(authorId)))
				// Support HATEOAS using the statically typed approach by including links in response.
				//
				// This means that each book has it's actions metadata available but the overall collection doesn't.
				//Ok(
				//	AutoMapper.Mapper.Map<IEnumerable<BookDto>>(
				//		_libraryRepository.GetBooksForAuthor(authorId))
				//	.Select(
				//		mappedBook => CreateLinksForBook(mappedBook)))
				// Return metadata for the collection...
				Ok(
					CreateLinksForBooks(
						new LinkedCollectionResourceWrapperDto<BookDto>(
							AutoMapper.Mapper.Map<IEnumerable<BookDto>>(
								_libraryRepository.GetBooksForAuthor(authorId))
							.Select(
								mappedBook => CreateLinksForBook(mappedBook)))))
				:
				(IActionResult)NotFound();
		}

		[HttpGet("{id}", Name = "GetBookForAuthor")]
		public IActionResult GetBookForAuthor(Guid authorId, Guid id)
		{
			/*
			 * A constraint for a RESTful API is that when information about an entity is returned then it
			 * must contain enough data to enable the consumer to manipulate that resource. In this case
			 * we're returning an ID but not a URI which could be used to delete or update that resource.
			 * 
			 * The URI not the ID identifies the resource to a consumer.
			 * 
			 * This will be dealt with (in this case) by HATEOAS in a later module of the course.
			 */

			return
				_libraryRepository.AuthorExists(authorId) && _libraryRepository.GetBookForAuthor(authorId, id) != null
				?
				//Ok(AutoMapper.Mapper.Map<BookDto>(_libraryRepository.GetBookForAuthor(authorId, id)))
				// Support HATEOAS using the statically typed approach by including links in response
				Ok(CreateLinksForBook(AutoMapper.Mapper.Map<BookDto>(_libraryRepository.GetBookForAuthor(authorId, id))))
				:
				(IActionResult)NotFound();
		}

		[HttpPost(Name = "CreateBookForAuthor")]
		public IActionResult CreateBookForAuthor(
			Guid authorId,
			[FromBody] BookForCreationDto book)
		{
			if (book == null)
				return BadRequest();

			// The whole validation approach used relies on; models, data annotations, and rules in a way which
			// leads to duplication of code and merging of concerns. JeremySkinner's FluentValidation is worth a look.

			if (book.Title == book.Description)
				ModelState.AddModelError(nameof(BookForCreationDto), "Please enter a proper description for the book.");

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState); // Custom ObjectResult for 422

			if (!_libraryRepository.AuthorExists(authorId))
				return NotFound();

			var bookEntity = AutoMapper.Mapper.Map<Book>(book);

			_libraryRepository.AddBookForAuthor(authorId, bookEntity);

			if (!_libraryRepository.Save())
				throw new Exception($"Creating a book for author {authorId} failed on save.");

			//return CreatedAtRoute(
			//	routeName: "GetBookForAuthor",
			//	routeValues: new { authorId = authorId, id = bookEntity.Id },
			//	value: AutoMapper.Mapper.Map<BookDto>(bookEntity));

			// Support HATEOAS using the statically typed approach by including links in response
			return CreatedAtRoute(
				routeName: "GetBookForAuthor",
				routeValues: new { authorId = authorId, id = bookEntity.Id },
				value: CreateLinksForBook(AutoMapper.Mapper.Map<BookDto>(bookEntity)));
		}


		[HttpDelete("{id}", Name = "DeleteBookForAuthor")] // Supporting HATEOAS using the statically typed approach requires a name for generating the URI
		public IActionResult DeleteBookForAuthor(Guid authorId, Guid id)
		{
			return
				_libraryRepository.AuthorExists(authorId) && _libraryRepository.GetBookForAuthor(authorId, id) != null
				?
				DeleteBookForAuthor(_libraryRepository.GetBookForAuthor(authorId, id))
				:
				NotFound();
		}

		private IActionResult DeleteBookForAuthor(Book book)
		{
			_libraryRepository.DeleteBook(book);

			if (!_libraryRepository.Save())
				throw new Exception($"Delete book {book.Id} for author {book.AuthorId} save failed");

			_logger.LogInformation(
				eventId: 100,
				message: $"Book {book.Id} for author {book.AuthorId} was deleted.");

			return NoContent();
		}

		//[HttpPut("{id}")]
		//public IActionResult UpdateBookForAuthor(
		//	Guid authorId,
		//	Guid id,
		//	[FromBody] BookForUpdateDto bookUpdates)
		//{
		//	if (bookUpdates == null)
		//		return BadRequest();

		//	if (!_libraryRepository.AuthorExists(authorId) || _libraryRepository.GetBookForAuthor(authorId, id) == null)
		//		return NotFound();

		//	Book bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

		//	// Succinct way of; mapping bookEntity to book while applying the updates to both.
		//	AutoMapper.Mapper.Map(bookUpdates, bookFromRepo);

		//	// No code in this implementation of UpdateBookForAuthor.
		//	// Part of repository pattern which means the logic is persistence ignorant.
		//	// The persistence is effectively mocked.
		//	// Working on a repo contract not on an implementation.
		//	// In this case Entity Framework Core will automatically handle the update on save...
		//	_libraryRepository.UpdateBookForAuthor(bookFromRepo);

		//	if (!_libraryRepository.Save())
		//		throw new Exception($"Save failed when updating book {id} for author {authorId}");

		//	return NoContent(); // Could return Ok() but why not a route with the get information!? Because CreatedAtRoute is for creating a resource
		//}

		// Upserting is using PUT to do an insert.
		// This allows the API consumer and the API application to share responsibility for creating the GUID...
		// (one reason why GUIDs are better than ints for the identifier).
		// This is allowed by the REST standard and the flexibility provides some benefits; the consumer doesn't need to
		// find the URI first, the consumer can use the same call for inserting and updating - if the object doesn't exist
		// then it will be created.
		// In addition, PUT is idempotent which means that both the first call (which will create) and subsequent calls (which will update)
		// will all return 204.

		/// <summary>
		/// Version of the method which supports upserting
		/// </summary>
		[HttpPut("{id}", Name = "UpdateBookForAuthor")] // Supporting HATEOAS using the statically typed approach requires a name for generating the URI
		public IActionResult UpdateBookForAuthor(
			Guid authorId,
			Guid id,
			[FromBody] BookForUpdateDto bookUpdates)
		{
			if (bookUpdates == null)
				return BadRequest();

			if (bookUpdates.Title == bookUpdates.Description)
				ModelState.AddModelError(nameof(BookForUpdateDto), "Please enter a proper description for the book.");

			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState); // Custom ObjectResult for 422

			if (!_libraryRepository.AuthorExists(authorId))
				return NotFound();

			if (_libraryRepository.GetBookForAuthor(authorId, id) == null) // Replace NotFound with create logic
			{
				var bookToAdd = AutoMapper.Mapper.Map<Book>(bookUpdates);
				bookToAdd.Id = id; // Now allowing consumer to set this

				_libraryRepository.AddBookForAuthor(authorId, bookToAdd);

				if (!_libraryRepository.Save())
					throw new Exception($"Save failed when upserting book {id} for author {authorId}");

				// Upserting is creating a resource so return CreatedAtRoute

				return CreatedAtRoute(
					routeName: "GetBookForAuthor",
					routeValues: new { authorId = authorId, id = id },
					value: AutoMapper.Mapper.Map<BookDto>(bookToAdd));
			}

			Book bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);

			// Succinct way of; mapping bookEntity to book while applying the updates to both.
			AutoMapper.Mapper.Map(bookUpdates, bookFromRepo);

			// No code in this implementation of UpdateBookForAuthor.
			// Part of repository pattern which means the logic is persistence ignorant.
			// The persistence is effectively mocked.
			// Working on a repo contract not on an implementation.
			// In this case Entity Framework Core will automatically handle the update on save...
			_libraryRepository.UpdateBookForAuthor(bookFromRepo);

			if (!_libraryRepository.Save())
				throw new Exception($"Save failed when updating book {id} for author {authorId}");

			return NoContent(); // Could return Ok() but why not a route with the get information!?
		}

		// TODO - Implement put request for the collection of books for an authors in BookCollectionsController.
		// This would replace the existing collection of books with the new collection.
		// This would not normally be implemented due to it's destructive nature but it would be interesting.

		[HttpPatch("{id}", Name = "PartiallyUpdateBookForAuthor")] // Supporting HATEOAS using the statically typed approach requires a name for generating the URI
		public IActionResult PartiallyUpdateBookForAuthor(
			Guid authorId,
			Guid id,
			[FromBody] JsonPatchDocument<BookForUpdateDto> bookPatchDocument)
		{
			if (bookPatchDocument == null)
				return BadRequest();

			//if (!_libraryRepository.AuthorExists(authorId) || _libraryRepository.GetBookForAuthor(authorId, id) == null)
			//	return NotFound();
			//
			// Update for upserting
			if (!_libraryRepository.AuthorExists(authorId))
				return NotFound();

			if (_libraryRepository.GetBookForAuthor(authorId, id) == null)
			{
				// NB - When using upserting with patch the fields which aren't set will retain their default values...
				// so like inserting with defaults but not like patching in this use case.

				var bookToAddDto = new BookForUpdateDto();
				//bookPatchDocument.ApplyTo(bookToAddDto);
				// Begin validation code
				bookPatchDocument.ApplyTo(bookToAddDto, ModelState);

				if (bookToAddDto.Title == bookToAddDto.Description)
					ModelState.AddModelError(nameof(BookForUpdateDto), "Please enter a proper description for the book.");

				TryValidateModel(bookToAddDto);

				if (!ModelState.IsValid)
					return new UnprocessableEntityObjectResult(ModelState);

				// End validation code

				var bookToAddEntity = AutoMapper.Mapper.Map<Book>(bookToAddDto);
				bookToAddEntity.Id = id;

				_libraryRepository.UpdateBookForAuthor(bookToAddEntity);

				if (!_libraryRepository.Save())
					throw new Exception($"Save failed when updating book {id} for author {authorId}");

				return CreatedAtRoute(
					routeName: "GetBookForAuthor",
					routeValues: new { authorId = authorId, id = id },
					value: AutoMapper.Mapper.Map<BookDto>(bookToAddEntity));
			}

			Book bookFromRepo = _libraryRepository.GetBookForAuthor(authorId, id);		// Get existing entity
			var bookToPatch = AutoMapper.Mapper.Map<BookForUpdateDto>(bookFromRepo);    // Map existing entity to update DTO
			//bookPatchDocument.ApplyTo(bookToPatch);									// Apply updates to update DTO
			bookPatchDocument.ApplyTo(bookToPatch, ModelState);                         // Validate patch document

			if (bookToPatch.Title == bookToPatch.Description)
				ModelState.AddModelError(nameof(BookForUpdateDto), "Please enter a proper description for the book.");

			// Validates patched entity
			TryValidateModel(bookToPatch);

			// Check patch document validation applied earlier
			if (!ModelState.IsValid)
				return new UnprocessableEntityObjectResult(ModelState);

			AutoMapper.Mapper.Map(bookToPatch, bookFromRepo);

			_libraryRepository.UpdateBookForAuthor(bookFromRepo);

			if (!_libraryRepository.Save())
				throw new Exception($"Save failed when updating book {id} for author {authorId}");

			return NoContent();
		}

		/// <summary>
		/// Supporting HATEOAS using the statically typed approach
		/// </summary>
		private BookDto CreateLinksForBook(BookDto book)
		{
			// Ensure each method has a Name so we can generate the URI

			// This results in the book including a set of links which tell the consumer which actions can
			// be taken and how to take them.

			// Add HTTP links for each action which can be performed for this book
			book.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("GetBookForAuthor", new { id = book.Id }), // Similar to CreatedAtRoute
					rel: "self", // rel is a value we choose
					method: "GET"));

			book.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("DeleteBookForAuthor", new { id = book.Id }),
					rel: "delete_book",
					method: "DELETE"));

			book.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("UpdateBookForAuthor", new { id = book.Id }),
					rel: "update_book",
					method: "PUT"));

			book.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("PartiallyUpdateBookForAuthor", new { id = book.Id }),
					rel: "partially_update_book",
					method: "PATCH"));

			return book;
		}

		/// <summary>
		/// Use wrapper implementation to supporting HATEOAS for collections using the statically typed approach
		/// </summary>
		private LinkedCollectionResourceWrapperDto<BookDto> CreateLinksForBooks(
			LinkedCollectionResourceWrapperDto<BookDto> booksWrapper)
		{
			booksWrapper.Links.Add(
				new LinkDto(
					href: _urlHelper.Link("GetBooksForAuthor", new { }),
					rel: "self",
					method: "GET"));

			return booksWrapper;
		}
	}
}
