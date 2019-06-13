using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Library.API.Entities
{
    public class Author
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTimeOffset DateOfBirth { get; set; }

		// To demonstrate versioning; add support for DateOfDeath (v2).
		// Make it optional on POST (but not mandatory so backwards compatibility is supported)
		public DateTimeOffset? DateOfDeath { get; set; } // NB - Add migration to update DB.

		[Required]
        [MaxLength(50)]
        public string Genre { get; set; }

        public ICollection<Book> Books { get; set; }
            = new List<Book>();
    }
}
