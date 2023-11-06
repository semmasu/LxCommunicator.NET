using Loxone.Communicator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LxCommunicator.NET.Example.Http;

internal class Program {
	private static async Task Main(string[] args) {
		using HttpWebserviceClient client = await HttpWebserviceClient.GetClient("504F94A115C4", 2, "8449DEE3-6BCB-4E94-93BD-046AA20BC1DA", "SuMa.TestApp");
		using TokenHandler handler = new(client, "admin");
		handler.SetPassword("ExperteSuter2023");
		await client.Authenticate(handler);
		try {
			foreach (KeyValuePair<string, string> one in new Dictionary<string, string>()) {
			}

			string x = (await client.SendWebservice(new WebserviceRequest<string>("jdev/sps/enumdev", EncryptionType.Request))).Value;
			WebserviceResponse x2 = await client.SendWebservice(new WebserviceRequest("jdev/sps/enumdev", EncryptionType.Request));
			string y2 = Encoding.UTF8.GetString(x2.Content);

			WebserviceResponse r = await client.SendWebservice(new WebserviceRequest("data/LoxAPP3.json", EncryptionType.Request));
			string y = Encoding.UTF8.GetString(r.Content);
			string version = (await client.SendWebservice(new WebserviceRequest<string>("jdev/cfg/version", EncryptionType.Request))).Value;
			Console.WriteLine($"Version: {version}");
		}
		finally {
			await handler.KillToken();
		}
	}
}