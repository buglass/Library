namespace Library.API.Helpers
{
	public class AuthorsResourceParameters
    {
		const int maxPageSize = 10;

		public int PageNumber { get; set; } = 1;

		private int _pageSize = 10;

		public int PageSize
		{
			get { return _pageSize; }
			set { _pageSize = value > maxPageSize ? maxPageSize : value; }
		}

		public string Genre { get; set; } // Support for filtering on the genre field

		public string SearchQuery { get; set; } // Searching support

		// Filtering vs. Searching.
		// Filtering has a field name with a value to filter on.
		// Searching has just a term to search on which could include all or any fields.

		public string OrderBy { get; set; } = "Name"; // Support for ordering (specifically by name in this case)

		public string Fields { get; set; } // Support for data shaping
    }
}
