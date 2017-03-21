using RestSharp;
using System;
 
public class HelloWorld
{

    public class SMASHDOCs {
        
		private string _client_id;
		private string _client_key;
		private string _partner_url;
		private bool _debug;

        public SMASHDOCs(string client_id, string client_key, string partner_url, Boolean debug) {
			_client_id = client_id;
			_client_key = client_key;
			_partner_url = partner_url;
			_debug = debug;
        }

        public void bar() {

			Console.WriteLine(_partner_url);
            Console.WriteLine("Bar");

            var client = new RestClient();
            client.BaseUrl = new Uri("http://zopyx.com");

            var request = new RestRequest();
            request.Resource = "/";

            IRestResponse response = client.Execute(request);
			Console.WriteLine("{0}", response.ResponseStatus);
			Console.WriteLine("{0}", (int)response.StatusCode); 
			Console.WriteLine("{0}", response.Content);
      }

    }

    static public void Main ()
    {
		string client_id = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_ID");
		string client_key = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_KEY");
		string partner_url = Environment.GetEnvironmentVariable("SMASHDOCS_PARTNER_URL");
		bool debug = Environment.GetEnvironmentVariable("SMASHDOCS_DEBUG").Length >0;

		Console.WriteLine("Client ID: {0}", client_id);
		Console.WriteLine("Client KEY: {0}", client_key);
		Console.WriteLine("Partner URL: {0}", partner_url);
		Console.WriteLine("DEBUG: {0}", debug);

        SMASHDOCs f1 = new SMASHDOCs(client_id, client_key, partner_url, debug);
		f1.bar();

    }
}
