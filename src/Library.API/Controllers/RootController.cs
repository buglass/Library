using System.Collections.Generic;
using Library.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
	[Route("api")]
    public class RootController : Controller
    {
		private readonly IUrlHelper _urlHelper;

		/// <summary>
		/// Controller to return general documentation on how to interact with the API to the consumer.
		/// </summary>
		public RootController(IUrlHelper urlHelper)
		{
			_urlHelper = urlHelper;
		}

		[HttpGet(Name = "GetRoot" )]
		public IActionResult GetRoot([FromHeader(Name = "Accept")] string mediaType)
		{
			if (mediaType == "application/vnd.marvin.hateoas+json")
			{
				// Return the root document with links to starting points of API interaction.
				// Authors is a good starting point because those API calls return data which describes other areas
				// (such as further interaction with authors and interactions with books).
				//
				// This shows where to start but not exactly how to start (eg with POST).
				//
				// CreateAuthor and GetAuthor return similar objects both using the application/json media-type.
				// Best to have custom media-types (such as vnd.marvin.author.friendly+json for GetAuthor and 
				// vnd.marvin.author.full+json for CreateAuthor).
				//
				// This creates the potential for an evolvable API (eg. a media type which returns new fields)
				// so leads to versioning.
				//
				// Version can be through; URI (api/v1/authors), query string (api/authors/api-version=v1),
				// or header ("api-version"=v1) however these are not all applicable to RESTful systems.
				// In theory we shouldn't because "websites don't have versions". HATEOAS helps because
				// it provides feedback on the API interaction.
				//
				// A solution to this problem is 'COL' (Code on Demand) - the optional constraint which states
				// that a RESTful API can extend client functionality. This can be implemented by responding
				// with javaScript which clients can run - if they're javaScript-based. Most clients also
				// don't completely self-generate anyway...
				//
				// In this case we'll use versioning via media-types to handle changes in representations.
				// For example application/vnd.marvin.author.friendly.v1+json and application/vnd.marvin.author.friendly.v2+json.

				var links = new List<LinkDto>();

				links.Add(new LinkDto(
					href: _urlHelper.Link("GetRoot", new { }),
					rel: "self",
					method: "GET"));

				links.Add(new LinkDto(
					href: _urlHelper.Link("GetAuthors", new { }),
					rel: "authors",
					method: "GET"));

				links.Add(new LinkDto(
					href: _urlHelper.Link("CreateAuthor", new { }),
					rel: "create_author",
					method: "POST"));

				return Ok(links);
			}
			else
			{
				return NoContent();
			}
		}
    }
}
