using RestSharp;
using System;
using System.Collections.Generic;
using JWT;
using Jose;
using System.Text;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Collections;


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




        public JArray list_templates() {

            var client = new RestClient();
			client.BaseUrl = new Uri(_partner_url);
            var request = new RestRequest();
			request.Resource = "/partner/templates/word";
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

            IRestResponse response = client.Execute(request);
			Console.WriteLine(response.Content);
			JArray result = JArray.Parse(response.Content);
			return result;
      }

		public JObject new_document(string title="", string description="", string role="editor", string status="draft")
		{
			var user_data = new Dictionary<string, string>()
			{
				{"email", "info@xx.de"},
				{"firstname", "Henry"},
				{"lastname", "Miller"},
				{"userId", "testuser"},
				{"company", "Dummies Ltd"},
			};
			var data = new Dictionary<string, object>()
			{
				{"user", user_data},
				{"title", title},
				{"description", description},
				{"userRole", role},
				{"groupId", "testgrp"},
				{"status", status},
				{"sectionHistory", true}
			};
			var client = new RestClient();
			client.BaseUrl = new Uri(_partner_url);
			var request = new RestRequest(Method.POST);
			request.Resource = "/partner/documents/create";
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());
			request.AddJsonBody(data);
						       
			IRestResponse response = client.Execute(request);
			JObject result = JObject.Parse(response.Content);
			return result;
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


        SMASHDOCs sd = new SMASHDOCs(client_id, client_key, partner_url, debug);
		JArray result = sd.list_templates();
		Console.WriteLine(result);
	    JObject result2  = sd.new_document("my title", "my description");
		Console.WriteLine(result2);
    }
}
