using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.API.Helpers
{
	/// <summary>
	/// Need to include pagination metadata such as; URIs to the previous and next pages,
	/// total item count, total number of pages, page number, page size
	/// 
	/// This is often included in the response body itself with metadata tag or a paging info tag.
	/// 
	/// This isn't correct because the API should return a representation of the requested resource
	/// with the requested media type. An envelope with a results field and a metadata field doesn't
	/// match what was requested.
	/// 
	/// The response body no longer matches the Accept header: this isn't application/json, it's a new media type.
	/// 
	/// This breaks the RESTful self-descriptive message constraint: the consumer does not know how to
	/// interpret the response with content-type application/json.
	/// 
	/// The pagination metadata should therefore be returned as a custom header in the response.
	/// </summary>
	public class PagedList<T> : List<T>
    {
		public PagedList(List<T> items, int count, int pageNumber, int pageSize)
		{
			TotalCount = count;
			PageSize = pageSize;
			CurrentPage = pageNumber;
			TotalPages = (int)Math.Ceiling(count / (double)pageSize);
			AddRange(items);
		}

		public static PagedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
		{
			var count = source.Count();
			var items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
			return new PagedList<T>(items.ToList(), count, pageNumber, pageSize);
		}

		public int CurrentPage { get; private set; }

		public int TotalPages { get; private set; }

		public int PageSize { get; private set; }

		public int TotalCount { get; private set; }

		public bool HasPrevious
		{
			get { return CurrentPage > 1; }
		}

		public bool HasNext
		{
			get { return CurrentPage < TotalPages; }
		}
	}
}
