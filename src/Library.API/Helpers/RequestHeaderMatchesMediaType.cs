using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Library.API.Helpers
{
	// AllowMultiple allows multiple media types
	[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
	public class RequestHeaderMatchesMediaTypeAttribute : Attribute, IActionConstraint
	{
		private readonly string _requestHeaderToMatch;
		private readonly string[] _mediaTypes;

		public RequestHeaderMatchesMediaTypeAttribute(
			string requestHeaderToMatch,
			string[] mediaTypes)
		{
			_requestHeaderToMatch = requestHeaderToMatch;
			_mediaTypes = mediaTypes;
		}

		public int Order
		{
			get { return 0; } // return 0 so that constraint runs at same stage as calls
		}

		public bool Accept(ActionConstraintContext context)
		{
			var requestHeaders = context.RouteContext.HttpContext.Request.Headers;

			if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
				return false;

			// if one of the media types matches then return true
			foreach(var mediaType in _mediaTypes)
			{
				if (string.Equals(requestHeaders[_requestHeaderToMatch].ToString(), mediaType, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
	}
}
