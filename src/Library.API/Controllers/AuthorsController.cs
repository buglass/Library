using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Library.API.Controllers
{
	//[Route("api/[controller]")]
	[Route("api/authors")] // Prefer hard-coding so contract remains the same.
	public class AuthorsController : Controller
    {
		private readonly ILibraryRepository _libraryRepository;
		private readonly IUrlHelper _urlHelper;
		private readonly IPropertyMappingService _propertyMappingService; // Support query string validation
		private readonly ITypeHelperService _typeHelperService; // Support data shaping

		public AuthorsController(
			ILibraryRepository libraryRepository,
			IUrlHelper urlHelper,
			IPropertyMappingService propertyMappingService,
			ITypeHelperService typeHelperService)
		{
			_libraryRepository = libraryRepository;
			_urlHelper = urlHelper;
			_propertyMappingService = propertyMappingService;
			_typeHelperService = typeHelperService;
		}

		////[HttpGet("api/authors")]
		//[HttpGet()] // is just the standard get with the controller routing
		//public IActionResult GetAuthors()
		//{
		//	//return new JsonResult(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(_libraryRepository.GetAuthors()));
		//	return Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(_libraryRepository.GetAuthors()));
		//}

		//const int maxAuthorPageSize = 20; Now handled by AuthorsResourceParameters helper class

		/// <summary>
		/// GetAuthors implementation with paging.
		/// [FromQuery] attribute specifies that the parameters come from the query string
		/// (although this will be done automatically anyway, like the id inputs).
		/// It allows us to specify if the querystring parameter is named differently to the input parameter,
		/// for example [FromQuery(Name="page")]  int pageNumber = 1.
		/// </summary>
		//[HttpGet()]
		//public IActionResult GetAuthors(
		//	[FromQuery] int pageNumber = 1,
		//	[FromQuery] int pageSize = 10)
		// Can replace these inputs with the AuthorsResourceParameters helper class and the routing will automatically map
		// query string parameters by name
		//
		// AuthorsResourceParameters can also support filtering and searching
		//
		// For dynamic HATEOAS support add links to response body while wrapping authors in an envelope 
		//
		// Added support for HttpHead - this provides information on the response headers without the payload
		// so allows consumers to test for changes, validation, accessibility etc.
		[HttpGet(Name = "GetAuthors")]
		[HttpHead]
		public IActionResult GetAuthors(
			AuthorsResourceParameters authorsResourceParameters,
			[FromHeader(Name = "Accept")] string mediaType) // Get 'Accept' detail (for accepted media type) from header
		{
			// Only include links if they're requested via the custom media type...
			// Detect parameters from header

			// Need to check incoming field mappings in query string and return appropriate status code if they're invalid
			if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
				return BadRequest();

			// Validate requested fields (for data shaping).
			if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
				return BadRequest();

			// Created AuthorsResourceParameters helper class to handle different parameters.

			PagedList<Author> authors = _libraryRepository.GetAuthors(authorsResourceParameters);

			// Dynamic HATEOAS: Previous page metadata will now be contained in links
			//var previousPageLink = authors.HasPrevious
			//	? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
			//	: null;

			// Dynamic HATEOAS: Next page metadata will now be contained in links
			//var nextPageLink = authors.HasNext
			//	? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
			//	: null;

			// Only include pagination data if requested by the media type
			//var paginationMetadata = new
			//{
			//	totalCount = authors.TotalCount,
			//	pageSize = authors.PageSize,
			//	currentPage = authors.CurrentPage,
			//	totalPages = authors.TotalPages,
			//	//previousPageLink = previousPageLink, // Dynamic HATEOAS: Previous page metadata will now be contained in links
			//	//nextPageLink = nextPageLink // Dynamic HATEOAS: Next page metadata will now be contained in links
			//};

			//// Add pagination metadata to the response as a custom header.
			//Response.Headers.Add(
			//	key: "X-Pagination",
			//	value: JsonConvert.SerializeObject(paginationMetadata));

			//return Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors));

			// Support for data shaping - although I'm a little confused that this is running after the call to the repository!
			// Is this because the repo data hasn't been executed yet? Presumably not considering all the operations already done.
			// Perhaps that's an alternative approach as this doesn't seem to be the most efficient implementation.
			//return Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors).ShapeData(authorsResourceParameters.Fields));

			// Implementation with dynamic HATEOAS support
			var authorDtos = AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors);

			// Only include links if requested
			// application/vnd.marvin.hateoas+json - media-type of vendor.[vendor's reference].[support for hateoas].[return as json]
			// TODO - Add this support for GetAuthor GET and POST requests
			// Need to include a root document to inform the consumer on how to interact with the rest of the API
			if (mediaType == "application/vnd.marvin.hateoas+json")
			{
				// Support HATEOAS by including links in body as json

				var paginationMetadata = new
				{
					totalCount = authors.TotalCount,
					pageSize = authors.PageSize,
					currentPage = authors.CurrentPage,
					totalPages = authors.TotalPages
				};

				Response.Headers.Add(
					key: "X-Pagination",
					value: JsonConvert.SerializeObject(paginationMetadata));

				var links = CreateLinksForAuthors(
					authorsResourceParameters: authorsResourceParameters,
					hasNext: authors.HasNext,
					hasPrevious: authors.HasPrevious);

				IEnumerable<ExpandoObject> dataShapedAuthorDtos = authorDtos.ShapeData(authorsResourceParameters.Fields);

				var dataShapedAuthorDtosWithLinks = dataShapedAuthorDtos.Select(authorDto =>
				{
					var authorDtoAsDictionary = authorDto as IDictionary<string, object>;

				// Get links for only the fields requested
				var authorLinks = CreateLinksForAuthor(
						id: (Guid)authorDtoAsDictionary["Id"],
						fields: authorsResourceParameters.Fields);

					authorDtoAsDictionary.Add("links", authorLinks);

					return authorDtoAsDictionary;
				});

				var linkedCollectionResource = new
				{
					value = dataShapedAuthorDtosWithLinks,
					links = links
				};

				return Ok(linkedCollectionResource);
			}
			else
			{
				// Return meta-data in header (no HATEOAS support)

				var previousPageLink = authors.HasPrevious
					? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
					: null;

				var nextPageLink = authors.HasNext
					? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
					: null;

				var paginationMetadata = new
				{
					totalCount = authors.TotalCount,
					pageSize = authors.PageSize,
					currentPage = authors.CurrentPage,
					totalPages = authors.TotalPages,
					previousPageLink = previousPageLink,
					nextPageLink = nextPageLink
				};

				Response.Headers.Add(
					key: "X-Pagination",
					value: JsonConvert.SerializeObject(paginationMetadata));

				return Ok(authorDtos.ShapeData(authorsResourceParameters.Fields));
			}
		}

		/// <summary>
		/// Implementation with dynamic HATEOAS support
		/// 
		/// Add links to response body while wrapping authors in an envelope
		/// </summary>
		//[HttpGet(Name = "GetAuthors")]
		//public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
		//{
		//	// Need to check incoming field mappings in query string and return appropriate status code if they're invalid
		//	if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(authorsResourceParameters.OrderBy))
		//		return BadRequest();

		//	// Validate requested fields (for data shaping).
		//	if (!_typeHelperService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
		//		return BadRequest();

		//	// Created AuthorsResourceParameters helper class to handle different parameters.

		//	PagedList<Author> authors = _libraryRepository.GetAuthors(authorsResourceParameters);

		//	var previousPageLink = authors.HasPrevious
		//		? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage)
		//		: null;

		//	var nextPageLink = authors.HasNext
		//		? CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.NextPage)
		//		: null;

		//	var paginationMetadata = new
		//	{
		//		totalCount = authors.TotalCount,
		//		pageSize = authors.PageSize,
		//		currentPage = authors.CurrentPage,
		//		totalPages = authors.TotalPages,
		//		previousPageLink = previousPageLink,
		//		nextPageLink = nextPageLink
		//	};

		//	// Add pagination metadata to the response as a custom header.
		//	Response.Headers.Add(
		//		key: "X-Pagination",
		//		value: JsonConvert.SerializeObject(paginationMetadata));

		//	//return Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors));

		//	// Support for data shaping - although I'm a little confused that this is running after the call to the repository!
		//	// Is this because the repo data hasn't been executed yet? Presumably not considering all the operations already done.
		//	// Perhaps that's an alternative approach as this doesn't seem to be the most efficient implementation.
		//	return Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authors).ShapeData(authorsResourceParameters.Fields));
		//}

		private string CreateAuthorsResourceUri(AuthorsResourceParameters authorsResourceParameters, ResourceUriType resourceUriType)
		{
			switch (resourceUriType)
			{
				case ResourceUriType.PreviousPage:
					return _urlHelper.Link(
						routeName:
							"GetAuthors",
						values:
							new
							{
								fields = authorsResourceParameters.Fields,
								orderBy = authorsResourceParameters.OrderBy,
								searchQuery = authorsResourceParameters.SearchQuery,
								genre = authorsResourceParameters.Genre,
								pageNumber = authorsResourceParameters.PageNumber - 1,
								pageSize = authorsResourceParameters.PageSize
							});
				case ResourceUriType.NextPage:
					return _urlHelper.Link(
						routeName:
							"GetAuthors",
						values:
							new
							{
								fields = authorsResourceParameters.Fields,
								orderBy = authorsResourceParameters.OrderBy,
								searchQuery = authorsResourceParameters.SearchQuery,
								genre = authorsResourceParameters.Genre,
								pageNumber = authorsResourceParameters.PageNumber + 1,
								pageSize = authorsResourceParameters.PageSize
							});
				case ResourceUriType.Current: // Added for dynamic HATEOAS support
				default:
					return _urlHelper.Link(
						routeName:
							"GetAuthors",
						values:
							new
							{
								fields = authorsResourceParameters.Fields,
								orderBy = authorsResourceParameters.OrderBy,
								searchQuery = authorsResourceParameters.SearchQuery,
								genre = authorsResourceParameters.Genre,
								pageNumber = authorsResourceParameters.PageNumber,
								pageSize = authorsResourceParameters.PageSize
							});
			}
		}

		//[HttpGet("{id}", Name = "GetAuthor")] // Added name for CreatedAtRoute to refer to
		//public IActionResult GetAuthor(Guid id)
		//{
		//	return
		//		_libraryRepository.AuthorExists(id) // Optional but needs an additional call.
		//		?
		//		Ok(AutoMapper.Mapper.Map<AuthorDto>(_libraryRepository.GetAuthor(id)))
		//		:
		//		(IActionResult)NotFound();
		//}

		/// <summary>
		/// Implementation with data shaping support
		/// </summary>
		[HttpGet("{id}", Name = "GetAuthor")]
		public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
		{
			if (!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
				return BadRequest();

			// This currently violates the REST constraint that the API should allow manipulation of resources through the presentation.
			// This is because we're not providing enough information to the consumer to manipulate the resource that we're returning.
			// If the consumer only sends the name in the fields requested then the id won't be provided, for example.
			// We could return the resource URI in the response but HATEOAS is a better solution...

			// Some further considerations for functionality that could be supported are:
			// * Expanding child resources like http://localhost:6058/api/authors?expand=books
			// * Shaping expanded resources like http://localhost:6058/api/authors?fields=id,name,books.title
			// * Complex filters like contains: http://localhost:6058/api/authors?genre=contains('Horror')

			//return
			//	_libraryRepository.AuthorExists(id)
			//	?
			//	Ok(AutoMapper.Mapper.Map<AuthorDto>(_libraryRepository.GetAuthor(id)).ShapeData(fields))
			//	:
			//	(IActionResult)NotFound();

			// Need a dynamic solution to supporting HATEOAS for authors because they implement data shaping using ExpandoObjects

			if (!_libraryRepository.AuthorExists(id))
				return NotFound();

			var responseWithDataShaping =
				((IDictionary<string, object>)
				(AutoMapper.Mapper.Map<AuthorDto>(_libraryRepository.GetAuthor(id))
				.ShapeData(fields)));

			responseWithDataShaping.Add("links", CreateLinksForAuthor(id, fields));

			return Ok(responseWithDataShaping);
		}


		[HttpPost(Name = "CreateAuthor")] // Add name for support for the RootController
		[RequestHeaderMatchesMediaType("Content-type", new[] { "application/vnd.marvin.author.full+json" } )] // Versioning support from request header
		public IActionResult CreateAuthor(
			[FromBody] AuthorForCreationDto author)
		{
			// This automatically handles basic validation (such as text where a datetime is expected) because
			// the incoming data could not be serialized into the incoming object type (AuthorForCreationDto)
			// and the incoming object type is subsequently null.
			if (author == null)
				return BadRequest();

			// TODO - Add some validation

			var authorEntity = AutoMapper.Mapper.Map<Author>(author);

			_libraryRepository.AddAuthor(authorEntity);

			// Option one: Throw exception if save failed to be captured by global error handler in middleware.
			// Expensive but allows central error handling.
			if (!_libraryRepository.Save())
				throw new Exception();

			// Need to return URI which consumer can use to get created entity. Can use CreatedAtRoute for this.

			// Deprecated and replaced with support for HATEOAS on POST (below).
			//return CreatedAtRoute(
			//	routeName: "GetAuthor",
			//	routeValues: new { id = authorEntity.Id },
			//	value: AutoMapper.Mapper.Map<AuthorDto>(authorEntity));

			// Option two: Return 500 if save failed. Bypasses middleware but it's performant.
			//return
			//	_libraryRepository.Save()
			//	?
			//	CreatedAtRoute()
			//	:
			//	StatusCode(500, "err");

			// Support for dynamic HATEOAS on POST
			var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);
			var links = CreateLinksForAuthor(authorToReturn.Id, null); // Null fields as no data shaping
			// Need an ExpandoObject to enable us to add links to the response 
			var linkedResourceToReturn = (IDictionary<string, object>)authorToReturn.ShapeData(null); // ExpandoObject returned when null input provided
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetAuthor",
				routeValues: new { id = linkedResourceToReturn["Id"] }, // Happens to be same as object Id but technically should be from ExpandoObject
				value: linkedResourceToReturn);
		}

		/// <summary>
		/// Support for date of death with versioning
		/// Add custom constraint of RequestHeaderMatchesMediaType to ensure that the consumer is presenting with the correct version
		/// In this case the code is exactly the same as CreateAuthor which is coincidental. It could change depending on implementations
		/// of mapping or validation etc.
		/// </summary>
		[HttpPost(Name = "CreateAuthorWithDateOfDeath")]
		[RequestHeaderMatchesMediaType("Content-type",
			new[] { "application/vnd.marvin.authorwithdateofdeath.full+json",
					"application/vnd.marvin.authorwithdateofdeath.full+xml"})] // v2.1 - Support XML for DoD inputs (goodness knows why)
		// For metadata output we might want to create additional constraints or we might want to use the same approach as we did for HATEOAS
		// Add a new constraint for the Accept header...
		// This causes an error unless the custom attribute RequestHeaderMatchesMediaType is adjusted to allow multiple attributes of this type.
		// [RequestHeaderMatchesMediaType("Accept", new[] { "..." })]
		//
		// Other options include projects which attempt to describe languages for link representations and to include media type descriptions in
		// a resource representation with links to handle metadata without separate documents...
		// HAL - Hypertext Application Language
		// SIREN - Structured Interface for Representation Entities
		// JSON-LD - Light-weight linked data format
		// JSON-API - Specification for building JSON APIs which includes a way to represent links
		// ODATA - Effort to standardise REST APIs! Becoming industry standard?
		public IActionResult CreateAuthorWithDateOfDeath(
			[FromBody] AuthorForCreationWithDateOfDeathDto author)
		{
			if (author == null)
				return BadRequest();

			var authorEntity = AutoMapper.Mapper.Map<Author>(author);

			_libraryRepository.AddAuthor(authorEntity);

			if (!_libraryRepository.Save())
				throw new Exception();

			var authorToReturn = AutoMapper.Mapper.Map<AuthorDto>(authorEntity);
			var links = CreateLinksForAuthor(authorToReturn.Id, null);
			var linkedResourceToReturn = (IDictionary<string, object>)authorToReturn.ShapeData(null);
			linkedResourceToReturn.Add("links", links);

			return CreatedAtRoute(
				routeName: "GetAuthor",
				routeValues: new { id = linkedResourceToReturn["Id"] },
				value: linkedResourceToReturn);
		}

		[HttpPost("{id}")]
		public IActionResult BlockAuthorCreation(Guid id)
		{
			// Need to handle an attempted post with a guid.
			// With an existing guid will just be treated automatically as 404.
			// With a non-existing guid the same will happen but should strictly be a 409 conflict.
			// Create a post call which matches the URI of the get call then explicitly handle the incoming request.

			return
				_libraryRepository.AuthorExists(id)
				?
				new StatusCodeResult(StatusCodes.Status409Conflict)
				:
				(IActionResult)NotFound();
		}

		[HttpDelete("{id}", Name = "DeleteAuthor")]
		public IActionResult DeleteAuthor(Guid id)
		{
			return
				_libraryRepository.AuthorExists(id)
				?
				DeleteAuthor(_libraryRepository.GetAuthor(id))
				:
				NotFound();
		}

		private IActionResult DeleteAuthor(Author author)
		{
			// Cascade is on so the books will be automatically deleted and there's no logic required to do this here.

			_libraryRepository.DeleteAuthor(author);

			if (!_libraryRepository.Save())
				throw new Exception($"Delete author {author.Id} save failed");

			return NoContent();
		}

		// TODO - Delete collection of authors in AuthorCollectionsController.
		// Terrible idea really because this would delete all the others and all the books of authors so all of the data!
		// Would be funny though!

		/// <summary>
		/// Supporting HATEOAS using the statically typed approach.
		/// Need a dynamic solution to supporting HATEOAS for authors because they implement data shaping using ExpandoObjects.
		/// Input parameters are the same as for GetAuthor action
		/// </summary>
		private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
		{
			var links = new List<LinkDto>();

			if (string.IsNullOrWhiteSpace(fields)) // No data shaping
			{
				links.Add(new LinkDto(
					href: _urlHelper.Link("GetAuthor", new { id = id }),
					rel: "self",
					method: "GET"));
			}
			else
			{
				links.Add(new LinkDto(
					href: _urlHelper.Link("GetAuthor", new { id = id, fields = fields }),
					rel: "self",
					method: "GET"));
			}

			links.Add(new LinkDto(
				href: _urlHelper.Link("DeleteAuthor", new { id = id }),
				rel: "delete_author",
				method: "DELETE"));

			links.Add(new LinkDto(
				href: _urlHelper.Link("CreateBookForAuthor", new { authorId = id }),
				rel: "create_book_for_author",
				method: "POST"));

			links.Add(new LinkDto(
				href: _urlHelper.Link("GetBooksForAuthor", new { authorId = id }),
				rel: "books",
				method: "GET"));

			return links;
		}

		/// <summary>
		/// Dynamically typed support for HATEOAS with a collection of objects
		/// </summary>
		private IEnumerable<LinkDto> CreateLinksForAuthors(
			AuthorsResourceParameters authorsResourceParameters,
			bool hasNext,		// support paging
			bool hasPrevious)   // support paging
		{
			var links = new List<LinkDto>();

			// self
			links.Add(
				new LinkDto(
					href: CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
					rel: "self",
					method: "GET"));

			if (hasNext)
				links.Add(
					new LinkDto(
						href: CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
						rel: "nextPage",
						method: "GET"));

			if (hasPrevious)
				links.Add(
					new LinkDto(
						href: CreateAuthorsResourceUri(authorsResourceParameters, ResourceUriType.Current),
						rel: "previousPage",
						method: "GET"));

			return links;
		}

		/// <summary>
		/// Support for the OPTIONS HTTP calls which returns information on the available HTTP calls for the controller
		/// </summary>
		[HttpOptions]
		public IActionResult GetAuthorsOptions()
		{
			Response.Headers.Add("Allow", "GET,OPTIONS,POST");
			return Ok();
		}
	}
}
