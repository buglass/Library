using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Library.API.Entities
{
	public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LibraryContext>
	{
		public LibraryContext CreateDbContext(string[] args)
		{
			IConfigurationRoot configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.Build();

			var builder = new DbContextOptionsBuilder<LibraryContext>();
			var connectionString = "Data Source=EA202966;Integrated Security=True;Database=LibraryDB;MultipleActiveResultSets=true";
			builder.UseSqlServer(connectionString);
			return new LibraryContext(builder.Options);
		}
	}
}