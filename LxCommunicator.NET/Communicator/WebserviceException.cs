using System.Runtime.Serialization;

namespace Loxone.Communicator; 

/// <summary>
///     Exception that indicates an error in a webservice.
/// </summary>
public class WebserviceException : Exception {
	/// <summary>
	///     Throws a new WebserviceException
	/// </summary>
	public WebserviceException() { }

	/// <summary>
	///     Throws a new WebserviceException
	/// </summary>
	/// <param name="message">The error message</param>
	/// <param name="response">the response that caused the error</param>
	public WebserviceException(string message, WebserviceResponse response) : base(GetMessage(message, response)) {
		Response = response;
	}

	/// <summary>
	///     Throws a new WebserviceException
	/// </summary>
	/// <param name="message">The error message</param>
	public WebserviceException(string message) : base(message) { }

	/// <summary>
	///     Throws a new WebserviceException
	/// </summary>
	/// <param name="message">The error message</param>
	/// <param name="inner">An inner exception</param>
	public WebserviceException(string message, Exception inner) : base(message, inner) { }

	protected WebserviceException(
		SerializationInfo info,
		StreamingContext context) : base(info, context) {
	}

	/// <summary>
	///     The response from communicating with the webservice
	/// </summary>
	public WebserviceResponse Response { get; }

	private static string GetMessage(string message, WebserviceResponse response) {
		switch (response.ClientCode) {
			case null:
				return message;
			case 401:
				return $"{message} (Unauthorized)";
			case 403:
				return $"{message} (Not enough rights for the request)";
			case 423:
				return $"{message} (The requesting user is disabled)";
			case 503:
				return $"{message} (Service Unavailable; The Miniserver is restarting and not ready for requests)";
			case 901:
				return $"{message} (Maximum number of allowed concurrent connections reached)";
			case 409:
				return $"{message} (This code is already in use)";
			case 406:
				return $"{message} (Invalid code)";
			case 429:
				return $"{message} (Brute force detected. To many requests)";
			default:
				return $"{message} (Code {response.ClientCode})";
		}
	}
}