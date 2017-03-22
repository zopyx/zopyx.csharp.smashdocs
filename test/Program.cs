using RestSharp;
using System;
using System.Collections.Generic;
using JWT;
using Jose;
using System.Text;
using RestSharp.Deserializers;
using RestSharp.Serializers;


using Newtonsoft.Json;

public class HelloWorld
{

    public class SMASHDOCs {
        
		private string _client_id;
		private string _client_key;
		private string _partner_url;
		private bool _debug;

      public SMASHDOCs(string client_id, string client_key, string partner_url, bool debug) {
			_client_id = client_id;
			_client_key = client_key;
			_partner_url = partner_url;
			_debug = debug;
        }

		static long ToUnixTime(DateTime dateTime)
		{
			return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}

		private string get_token()
		{
			DateTime issued = DateTime.Now;
			string iss = Guid.NewGuid().ToString();
			long  iat = ToUnixTime(issued);
			string jti = Guid.NewGuid().ToString();
										
			var payload = new Dictionary<string, object>()
			{
				{"iss", iss},
				{"iat", iat},
				{"jti", jti}
			};

			return Jose.JWT.Encode(payload, Encoding.ASCII.GetBytes(_client_key), JwsAlgorithm.HS256);
		}

        public string new_document() {

			Console.WriteLine(get_token());

            var client = new RestClient();
			client.ClearHandlers();
			RestSharp.Deserializers.JsonDeserializer jsonDeserializer = new JsonDeserializer();
			client.AddHandler("application/json", jsonDeserializer);
			client.BaseUrl = new Uri(_partner_url);
            var request = new RestRequest();
			request.Resource = "/partner/templates/word";
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

            IRestResponse response = client.Execute(request);
			Console.WriteLine("{0}", response.Content);

			dynamic d = JsonConvert.DeserializeObject<dynamic>(response.Content);

			/*
			Console.WriteLine("{0}", response.ResponseStatus);
			Console.WriteLine("{0}", (int)response.StatusCode); 
			Console.WriteLine("{0}", response.Content);
			Console.WriteLine("{0}", response.Content.Length);

*/
			return "foo";
      }

    }

    static public void Main ()
    {
		string client_id = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_ID");
		string client_key = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_KEY");
		string partner_url = Environment.GetEnvironmentVariable("SMASHDOCS_PARTNER_URL");
		string _debug = Environment.GetEnvironmentVariable("SMASHDOCS_DEBUG");
		bool debug = false;

		try
		{
			if (_debug.Length > 0)
				debug = true;
		}
		catch (Exception)
		{ 
		}


		Console.WriteLine("Client ID: {0}", client_id);
		Console.WriteLine("Client KEY: {0}", client_key);
		Console.WriteLine("Partner URL: {0}", partner_url);
		Console.WriteLine("DEBUG: {0}", debug);


        SMASHDOCs sd = new SMASHDOCs(client_id, client_key, partner_url, debug);
		var result = sd.new_document();

    }
}
