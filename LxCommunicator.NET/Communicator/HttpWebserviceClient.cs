using Newtonsoft.Json;
using System.Text;

namespace Loxone.Communicator;

/// <summary>
///     Client to handle httpWebservices to loxone miniserver. Use <see cref="HttpWebserviceClient" /> for communicating
///     via http or derive from it to create your own httpClient.
/// </summary>
public class HttpWebserviceClient : WebserviceClient {
	private readonly string _scheme;
	private CancellationTokenSource? _cancellationTokenSource;

	/// <summary>
	///     Creates a new instance of the httpWebserviceClient.
	/// </summary>
	/// <param name="ip">IP adress of the miniserver</param>
	/// <param name="port">Port of the miniserver</param>
	/// <param name="permissions">Permissions of the connecting user</param>
	/// <param name="deviceUuid">Uuid of the connecting device</param>
	/// <param name="deviceInfo">Info of the connecting device</param>
	/// <param name="useHttps"></param>
	public HttpWebserviceClient(string ip, int port, int permissions, string deviceUuid, string deviceInfo, bool useHttps = false)
		: base(ip, port) {
		HttpClient = new HttpClient();
		Session = new Session(this, permissions, deviceUuid, deviceInfo);
		_scheme = useHttps ? "https" : "http";
	}

	/// <summary>
	///     Creates a new instance of the httpWebserviceClient.
	/// </summary>
	/// <param name="ip">IP adress of the miniserver</param>
	/// <param name="port">Port of the miniserver</param>
	/// <param name="session">Session object containing info used for connection</param>
	/// <param name="useHttps"></param>
	public HttpWebserviceClient(string ip, int port, Session session, bool useHttps = false)
		: base(ip, port) {
		HttpClient = new HttpClient();
		Session = session;
		_scheme = useHttps ? "https" : "http";
	}

	/// <summary>
	///     The httpClient used for sending the messages
	/// </summary>
	private HttpClient HttpClient { get; }

	/// <summary>
	///     Provides info required for the authentication on the miniserver
	/// </summary>
	/// <param name="handler">The tokenHandler that should be used</param>
	public override Task Authenticate(TokenHandler handler) {
		TokenHandler = handler;
		return Task.CompletedTask;
	}

	/// <summary>
	///     Disposes the WebserviceClient
	/// </summary>
	public override void Dispose() {
		base.Dispose();
		HttpClient?.Dispose();
		_cancellationTokenSource?.Dispose();
	}

	/// <summary>
	///     Creates a new instance of the httpWebserviceClient, resolves connec.
	/// </summary>
	/// <param name="serial">Miniserver serial</param>
	/// <param name="permissions">Permissions of the connecting user</param>
	/// <param name="deviceUuid">Uuid of the connecting device</param>
	/// <param name="deviceInfo">Info of the connecting device</param>
	/// <param name="cancellationToken"></param>
	public static async Task<HttpWebserviceClient> GetClient(string serial, int permissions, string deviceUuid, string deviceInfo, CancellationToken cancellationToken = default) {
		serial = serial.Replace(":", "").Trim().ToUpper();
		using HttpClient httpClient = new();
		HttpResponseMessage ipResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"http://dns.loxonecloud.com/?getip&snr={serial}&json=true"), cancellationToken);
		ipResponse.EnsureSuccessStatusCode();
		LxIpInfo ipInfo = JsonConvert.DeserializeObject<LxIpInfo>(await ipResponse.Content.ReadAsStringAsync())!;
		return new HttpWebserviceClient(ipInfo.GetBaseUri(serial), ipInfo.Port, permissions, deviceUuid, deviceInfo, ipInfo.PortOpenHttps);
	}

	/// <summary>
	///     Sends a webservice to the miniserver
	/// </summary>
	/// <param name="request">The Request that should be sent</param>
	/// <returns>The Response the miniserver returns</returns>
	public override async Task<WebserviceResponse> SendWebservice(WebserviceRequest request) {
		WebserviceRequest encRequest = await GetEncryptedRequest(request);
		Uri url = new UriBuilder {
			Scheme = _scheme,
			Host = IP,
			Port = Port,
			Path = encRequest.Command,
			Query = encRequest.Queries.ToString()
		}.Uri;
		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = new CancellationTokenSource(request.Timeout);
		HttpResponseMessage? httpResponse = await HttpClient.GetAsync(url.OriginalString, _cancellationTokenSource.Token);
		byte[] responseContent = await httpResponse.Content.ReadAsByteArrayAsync();
		_cancellationTokenSource?.Dispose();
		if (httpResponse.IsSuccessStatusCode && request.Encryption == EncryptionType.RequestAndResponse) {
			//decypt response if needed
			responseContent = Encoding.UTF8.GetBytes(Cryptography.AesDecrypt(Encoding.UTF8.GetString(responseContent), Session));
		}

		WebserviceResponse response = new WebserviceResponse(null, responseContent, (int)httpResponse.StatusCode);
		encRequest.TryValidateResponse(response);
		return response;
	}

	/// <summary>
	///     Creates a clone of a request and encrypts it
	/// </summary>
	/// <param name="request">The request that should be encrypted</param>
	/// <returns>the encrypted clone of the given request</returns>
	private async Task<WebserviceRequest> GetEncryptedRequest(WebserviceRequest request) {
		if (request == null) {
			return null;
		}

		WebserviceRequest encRequest = (WebserviceRequest)request.Clone();
		if (request.NeedAuthentication && TokenHandler != null) {
			//add authentication if needed
			if (TokenHandler.Token == null) {
				await TokenHandler.RequestNewToken();
			}

			encRequest.Queries.Add("autht", await TokenHandler.GetTokenHash());
			encRequest.Queries.Add("user", TokenHandler.Username);
			encRequest.NeedAuthentication = false;
			if (encRequest.Encryption == EncryptionType.None) {
				encRequest.Encryption = EncryptionType.Request;
			}
		}

		switch (encRequest.Encryption) {
			case EncryptionType.Request:
				encRequest.Command = "jdev/sys/enc/";
				break;
			case EncryptionType.RequestAndResponse:
				encRequest.Command = "jdev/sys/fenc/";
				break;
			case EncryptionType.None:
			default:
				return encRequest;
		}

		string query = encRequest.Queries.HasKeys() ? $"?{encRequest.Queries}" : "";
		encRequest.Command += Uri.EscapeDataString(Cryptography.AesEncrypt($"salt/{Session.Salt}/{request.Command}{query}", Session));
		encRequest.Queries.Clear();
		encRequest.Queries.Add("sk", await Session.GetSessionKey());
		encRequest.Encryption = EncryptionType.None;
		return encRequest;
	}
}