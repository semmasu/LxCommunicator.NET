using Newtonsoft.Json;

namespace Loxone.Communicator;

public class LxIpInfo {
	public int Code { get; set; }
	public string DataCenter { get; set; } = default!;

	[JsonProperty("DNS-Status")]
	public string DnsStatus { get; set; } = default!;

	[JsonIgnore]
	public string Ip => IpHttps.Split(':')[0];

	[JsonProperty("IPHTTPS")]
	public string IpHttps { get; set; } = default!;

	[JsonIgnore]
	public int Port => Convert.ToInt32(IpHttps.Split(':')[1]);

	[JsonProperty("PortOpenHTTPS")]
	public bool PortOpenHttps { get; set; }

	public bool RemoteConnect { get; set; }

	public string GetBaseUri(string serial) {
		return $"{Ip.Replace(".", "-")}.{serial}.dyndns.{DataCenter}";
	}
}