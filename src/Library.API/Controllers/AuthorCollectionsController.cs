using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
	[Route("api/authorcollections")]
    public class AuthorCollectionsController : Controller
    {
		private readonly ILibraryRepository _libraryRepository;

		public AuthorCollectionsController(ILibraryRepository libraryRepository)
		{
			_libraryRepository = libraryRepository;
		}

		[HttpPost]
		public IActionResult CreateAuthorCollection([FromBody] IEnumerable<AuthorForCreationDto> authorCollection)
		{
			if (authorCollection == null)
				return BadRequest();

			var authorEntities = AutoMapper.Mapper.Map<IEnumerable<Author>>(authorCollection);

			foreach(var authorEntity in authorEntities)
			{
				_libraryRepository.AddAuthor(authorEntity);
			}

			if (!_libraryRepository.Save())
				throw new Exception("Creating author collection save error");

			// Need to come back to this.
			// Currently we don't have a way of returning an aggregate key for a call which returns an author collection.
			// This is part of the separation between the 'outer facing contract' and the data store.
			// This can involve working with array keys and composite keys.
			// An array key is a list of keys like 1, 2, 3.
			// A composite key is a key-value pair like key1=1, key2=2, key3=3.
			//return CreatedAtRoute(
			//	routeName: null,
			//	routeValues: null,
			//	value: null);
			//return Ok(); // Temporary hack

			return CreatedAtRoute(
				routeName:
					"GetAuthorCollection",
				routeValues:
					new { ids = string.Join(",", AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorEntities).Select(authorDto => authorDto.Id)) },
				value:
					AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(authorEntities));

			/* A composite key works much the same but the KVPs allow more complex data than just a 1-2-1 mapping.
			 * So for example you could match on an id and a name.
			 * Would need a route template with two keys which map to two parameters in the action signature.
			 * 
			 * http://localhost:6058/api/authorcollections/(key1=value1,key2=value2)
			 * 
			 * Not something that's required here but something that would be good to introduce somewhere.
			 */
		}

		[HttpGet("({ids})", Name = "GetAuthorCollection")]
		public IActionResult GetAuthorCollection(
			//IEnumerable<Guid> ids
			[ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids) // ids bound using custom array model binder
		{
			// A method which provides a collection of authors from an array key (key1, key2, key3)
			// Need a custom array model binder.

			if (ids == null)
				return BadRequest();

			return
				_libraryRepository.GetAuthors(ids).Count() == ids.Count()
				?
				Ok(AutoMapper.Mapper.Map<IEnumerable<AuthorDto>>(_libraryRepository.GetAuthors(ids)))
				:
				(IActionResult)NotFound();
		}
    }
}
