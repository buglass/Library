using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Library.API.Services;
using Library.API.Entities;
using Microsoft.EntityFrameworkCore;
using Library.API.Models;
using Library.API.Helpers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Serialization;
using System.Linq;
using AspNetCoreRateLimit;
using System.Collections.Generic;

namespace Library.API
{
	/// <summary>
	/// REST Constraints:
	/// Client-Server:
	///		These should be separated. They can evolve separately.
	/// Statelessness:
	///		The necessary state to handle the request should be in the request itself.
	///		All the information to service a request should be in the request itself.
	///	Cacheable:
	///		Each response must explicitly state whether or not it can be cached.
	///	Layered:
	///		Supports x-tiered architecture. Each layer cannot know about any layer beyond the layers immediately next to it.
	///		The client therefore doesn't know which layer it's connected to.
	///		For example can include layers for handling; logging, throttling, exception handling, and caching.
	///	Code on Demand (optional):
	///		Server can extend client functionality, for example by supplying javaScript functionality that a web-based client could use.
	/// Uniform Interface:
	///		API and consumers share one technical interface. In the case of the HTTP protocol this would be uniform; URIs, Methods, and Media-Types.
	///		Good for cross-platform development because it supports uniform standards.
	///		Four separate sub-constraints:
	///		*	Identification of resources. The representation of a resource is separate to the actual resource.
	///			The returned data is different to the actual database object. Entities don't map exactly to Models such as Dtos.
	///		*	Manipulation of resources through representations. When the resource representation is returned there should
	///			be enough information (in the data and metadata) for the consumer to manipulate (modify or delete) that resource.
	///			contain enough data and metadata to allow further manipulation of, or interaction with, that resource.
	///		*	Self-dsecriptive message. Each message should contain enough information on how to process the message.
	///			For example if the message body is in json then the message header should specify json as the media type.
	///		*	HATEOAS (Hyper-media as the Engine of Application State).
	///			Provides metadata on how to consume and use the API. It's a self-documenting API.
	///		
	/// Richardson Maturity Model:
	/// Rates APIs by RESTful maturity.
	/// Can be used to describe how to progress an API from a simple application that doesn't care about standards (Level 0)
	/// to a fully RESTful API (Level 3).
	/// 
	///	Level 0: The swamp of POX (Plain Old XML).
	///		A protocol is used but not as it should be. For example HTTP is used but a single URI which doesn't use the call types correctly.
	///		A POST request is sent to http://localhost/myapi/ with some XML detailing the information required and the information is returned.
	///		A POST request is sent to http://localhost/myapi/ with some XML detailing the information for a new object and a new object is created.
	///	Level 1: Resources.
	///		Multiple resources are used and each resource maps to a URI for example:
	///		A POST request is sent to http://localhost/myapi/authors to get a list of authors.
	///		A POST request is sent to http://localhost/myapi/authors/{id} to get an author with a specific ID.
	///	Level 2: Verbs.
	///		Correct HTTP verbs (GET, POST, PUT, PATCH, DELETE) and status codes are used.
	///		A GET request is sent to http://localhost/myapi/authors to get a list of authors and 200 is returned.
	///		A POST request is sent to http://localhost/myapi/authors to create an author and 201 is returned.
	///	Level 3: Hypermedia.
	///		HATEOAS support for metadata. Links with hypermedia provide discoverability and effectively self-documentation.
	/// </summary>
	public class Startup
	{
		public static IConfiguration Configuration;

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc(
				setupAction =>
				{
					// ReturnHttpNotAcceptable forces consumer to specify a supported available data format
					// rather than defaulting to json when the consumer specifies an unsupported format such as xml.
					setupAction.ReturnHttpNotAcceptable = true;
					setupAction.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
					// Allow incoming requests (such as post) to use xml
					setupAction.InputFormatters.Add(new XmlDataContractSerializerInputFormatter());

					// v2.1 - Support DoD posts using XML
					var xmlDataContractSerializerInputFormatter = new XmlDataContractSerializerInputFormatter();
					xmlDataContractSerializerInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.authorwithdateofdeath.full+xml");
					setupAction.InputFormatters.Add(xmlDataContractSerializerInputFormatter);

					// v2 - Date of death support with versioning via media types
					var jsonInputFormatter = setupAction.InputFormatters.OfType<JsonInputFormatter>().FirstOrDefault();
					if (jsonInputFormatter != null)
					{
						jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.author.full+json");
						jsonInputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.authorwithdateofdeath.full+json");
					}

					// Add support for custom media type of 'application/vnd.marvin.hateoas+json' using json
					var jsonOutputFormatter = setupAction.OutputFormatters.OfType<JsonOutputFormatter>().FirstOrDefault();
					if (jsonOutputFormatter != null)
					{
						jsonOutputFormatter.SupportedMediaTypes.Add("application/vnd.marvin.hateoas+json");
					}
				})
				.AddJsonOptions(
					options =>
					{
						// Retain property name casing on serialization.
						options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
					});

			// register the DbContext on the container, getting the connection string from
			// appSettings (note: use this during development; in a production environment,
			// it's better to store the connection string in an environment variable)
			var connectionString = Configuration["connectionStrings:libraryDBConnectionString"];
			services.AddDbContext<LibraryContext>(o => o.UseSqlServer(connectionString));

			// register the repository
			services.AddScoped<ILibraryRepository, LibraryRepository>();

			// register IUrlHelper to support returning pagination metadata from a controller
			services.AddSingleton<IActionContextAccessor, ActionContextAccessor>(); // Provides IUrlHelper with access to actions
																					//services.AddScoped<IUrlHelper, UrlHelper>(); // Generates URIs for an action
			services.AddScoped<IUrlHelper, UrlHelper>(
				implementationFactory =>
				{
					return new UrlHelper(actionContext: implementationFactory.GetService<IActionContextAccessor>().ActionContext);
				});


			// Register custom IPropertyMappingService to support sorting
			services.AddTransient<IPropertyMappingService, PropertyMappingService>();

			// Register custom service to support data shaping
			services.AddTransient<ITypeHelperService, TypeHelperService>();

			// Add some support for caching with HTTP cache headers
			//services.AddHttpCacheHeaders();
			// Can use this to configure the cache headers in the response for both the expiration and validation models
			services.AddHttpCacheHeaders(
				(expirationModelOptions)
					=>
						{ expirationModelOptions.MaxAge = 600; },
				(validationModelOptions)
					=>
						{ validationModelOptions.MustRevalidate = true; });

			// Use MS .Net Core package for response caching. Seems to be the only option available.
			// Does not generate response headers (like marvin does) but it does handle them.
			// Before v2 there are some really nasty bugs so avoid v1!
			services.AddResponseCaching();

			// Support for throttling...
			// Can throttle by IP and / or by client.
			// Can throttle by calls to specific controllers or methods
			// Can throttle by (for example); requests per day, requests per hour, and requests per controller.
			// Request headers for this are; X-Rate-Limit-Limit, X-Rate-Limit-Remaining, and X-Rate-Limit-Reset.
			// Disallowed requests will return a 429 response with an optional Retry-After header and a body explaining the condition.

			services.AddMemoryCache(); // Used to store throttling counters and rules

			// AspNetCoreRateLimit package contains two pieces of middleware for throttling by IP and by client.
			// Example for throttling by IP...
			// NB - Could also read this from the config file (not sure if a specific one should be used or just the appsettings.json)

			services.Configure<IpRateLimitOptions>(
				options =>
				{
					options.GeneralRules = new List<RateLimitRule>
					{
						new RateLimitRule // 10 requests every 5 minutes
						{
							Endpoint = "*",
							//Limit = 10,
							Limit = 1000,
							Period = "5m"
						},
						new RateLimitRule // 2 requests every 10 seconds
						{
							Endpoint = "*",
							//Limit = 2,
							Limit = 200,
							Period = "10s"
						}
					};
				});

			services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
			services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env,
			ILoggerFactory loggerFactory, LibraryContext libraryContext)
		{
			// Moved to Program.cs.
			//loggerFactory.AddNLog(); // Logging to a file. See github/aspnet/Logging for some more. Logging to debug and console already done in v2.

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				//app.UseExceptionHandler();

				// Production-friendly global error handling.
				app.UseExceptionHandler(
					appBuilder =>
					{
						appBuilder.Run(async context =>
						{
							// Get and log exception
							var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
							if (exceptionHandlerFeature != null)
							{
								ILogger logger = loggerFactory.CreateLogger("Production global exception logger");
								logger.LogError(
									eventId: 500,
									exception: exceptionHandlerFeature.Error,
									message: exceptionHandlerFeature.Error.Message);
							}

							context.Response.StatusCode = 500;
							await context.Response.WriteAsync("An error occurred.");
						});
					});
			}

			AutoMapper.Mapper.Initialize(
				config =>
					{
						config
						.CreateMap<Author, AuthorDto>()
						// Create 'projections'
						.ForMember(
							dest => dest.Name,
							option => option.MapFrom(
								source => $"{source.FirstName} {source.LastName}"))
						.ForMember(
							dest => dest.Age,
							option => option.MapFrom(
								//source => source.DateOfBirth.GetCurrentAge()));
								source => source.DateOfBirth.GetCurrentAge(source.DateOfDeath))); // v2 - Date of death support

						config.CreateMap<Book, BookDto>();

						config.CreateMap<AuthorForCreationDto, Author>();

						config.CreateMap<BookForCreationDto, Book>();

						config.CreateMap<BookForUpdateDto, Book>();

						config.CreateMap<Book, BookForUpdateDto>();

						// v2 - Date of death support
						config.CreateMap<AuthorForCreationWithDateOfDeathDto, Author>();
					});

			libraryContext.EnsureSeedDataForContext();

			// Register middleware for throttling before everything other than logging and exception handling
			// because the throttling might prevent any other middleware from being hit.
			app.UseIpRateLimiting();

			// Add cache store and do this before the cache headers (so that the header handler can generate responses
			// from the cache).
			app.UseResponseCaching();

			// Add http cache headers before MVC because the caching framework might determine that the MVC framework isnt called
			// NB - When testing this in Postman must disable Settings -> General -> Headers -> Send no-cache header!
			// This is on by default to ensure that API calls are are always hit without the cache intervening.
			// Cache headers will now be included in the response.
			// The cache headers will include an ETag. This allows a request header to be added of If-None-Match with a value of the ETag
			// which gives the client some control over the caching.
			app.UseHttpCacheHeaders();

			app.UseMvc();
		}

		/*
		 * Caching and Concurrency
		 * 
		 * The responsibility for allowing caching is with the server but the responsibility for implementing it is on the client
		 * so it's very simple to implement here with just some minor changes in Startup so there are some notes here.
		 * 
		 * The cache site between the API (the Controller in MVC) and the consumer.
		 * In this implementation UseHttpCacheHeaders is implemented before MVC (with UseResponseCaching) implemented
		 * before that so the cache store is available for the cache to use.
		 * 
		 * There are two models for caching; expiration and validation.
		 * 
		 * Expiration is best supported with a Cache-Control response header which can state things like the max age of the cache.
		 * If the cache has not expired then a 304 is returned without a response body.
		 * This works for a private client-side cache by reducing calls from the client to the cache.
		 * This works for a shared server-side cache by reducing calls from the cache to the API.
		 * This is good for static content like images and web-pages but not for dynamic content which can frequently change.
		 * 
		 * Validation is a better fit for dynamic content. This validates the freshness of a cached response.
		 * Strong validation changes when any change occurs whereas weak validation changes only when a significant change occurs.
		 * An ETag (provided in a cacheable response) is a good implementation for the validation model. If the etag is prefixed with
		 * w/ then it's considered weak. This decision is made on the server.
		 * The request headers 'If-Modified-Since' and 'If-None-Match' can be used to implement datetime and etag caching respectively.
		 * 
		 * Validation and expiration models can be combined on both the client and the server for the strongest model.
		 * This is implemented in ConfigureServices.
		 * 
		 * There are various cache control directives for the headers on the response from the server and some of these can be
		 * over-ridden by headers on the request from the client. These include; Freshness, Cache-Type, Validation, and Other.
		 * 
		 * A component is needed to check the headers and return the appropriate response but there isn't currently any
		 * middle-ware available to do this for .Net Core. There's a [ResponseCache] attribute but it doesn't actually do anything
		 * and it doesn't support ETags. ASP.Net has CacheCow. Therefore this implementation uses custom project Marvin.Cache.Headers
		 * (which is available as a Nuget package).
		 * 
		 * *************************************************************************************************************************
		 * 
		 * For concurrency; pessimistic concurrency cannot be used in a RESTful API because locking a resource for editing isn't valid
		 * without a state.
		 * 
		 * Optimistic concurrency can be achieved by using the ETag because the ETag acts as a token for the response. If the request
		 * puts this against the If-Match header then the server will return a 412 when the response would no longer match the ETag.
		 * EG; user one gets resource, user two gets resource, both users have the same etag, user two modifies resource (so the response
		 * would now have a different etag and this is returned to user two in the response), user one modifies the resource with the now
		 * out-dated etag in the If-Matches header, user one recieves a 412.
		 * 
		 * Like caching; this is all done on the client so while the server needs to have the middleware available to support etags
		 * there is no actual implementation of concurrency on the server.
		 * 
		 * *************************************************************************************************************************
		 * 
		 * NB - When testing caching (and concurrency?) using Postman then disable Settings -> General -> Headers -> Send no-cache header.
		 * This is enabled by default because usually during development the controller must be reached each time but not for cache testing.
		 */

		/*
		 * Can write automated API tests in Postman using JavaScript for example the following
		 * runs under the Tests tab for the GetAuthor call...
		 * 
		 * Actually the second one doesn't quite work!
		 * 
		 * http://localhost:6058/api/authors/76053df4-6687-4353-8937-b45556748abe
		 * 
		 * pm.test("Status code is 200", function () {
		 *		pm.response.to.have.status(200);
		 * });
		 *
		 *	pm.test("Id matches returned id", function () {
		 *		var json = JSON.parse(responseBody);
		 *		//var idMatches = json.Id = "76053df4-6687-4353-8937-b45556748abe"; // passes
		 *		var idMatches = json.id == "76053df4-6687-4353-8937-b45556748axe"; // fails
		 *
		 *		idMatches === true;
		 *	});
		 *	
		 *	Postman - Runner allows a suite of tests to be run for a folder of saved calls
		 *	Newman is a command line test collecion runner for Postman which allows collections of test suites to be run for CI
		 */

		/*
		 * Documentation
		 * 
		 * Need some less technical documentation for API consumers.
		 * Swagger OpenAPI is a definition format for describing RESTful APIs - currently at v3.
		 * Swashbuckle implements this by automatically generating the documentation.
		 * Unfortunately Swashbuckle only supports OpenAPI v2 so lacks support for features such as action over-loading.
		 */
	}
}