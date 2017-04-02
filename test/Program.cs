using RestSharp;
using System;
using System.Collections.Generic;
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

		private void  check_response(IRestResponse response)
		{
			int status_code = (int)response.StatusCode;
			if (status_code != 200)
			{
				throw new Exception($"Status code: {status_code}");
			}
		}


		public JObject document_info(string document_id)
		{

			var client = new RestClient();
			client.BaseUrl = new Uri(_partner_url);
			string url =  $"/partner/documents/{document_id}";
	
			var request = new RestRequest(url);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

			IRestResponse response = client.Execute(request);
			check_response(response);
			JObject result = JObject.Parse(response.Content);
			return result;
		}

		public void  delete_document(string document_id)
		{

			var client = new RestClient(_partner_url);
			string url = $"/partner/documents/{document_id}";
			var request = new RestRequest(url, Method.DELETE);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

			IRestResponse response = client.Execute(request);
			check_response(response);
		}

		public void archive_document(string document_id)
		{

			var client = new RestClient(_partner_url);
			string url = $"/partner/documents/{document_id}/archive";

			var request = new RestRequest(url, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

			IRestResponse response = client.Execute(request);
			check_response(response);

		}

		public void unarchive_document(string document_id)
		{

			var client = new RestClient(_partner_url);
			string url = $"/partner/documents/{document_id}/unarchive";

			var request = new RestRequest(url, Method.POST);
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());

			IRestResponse response = client.Execute(request);
			check_response(response);

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
			check_response(response);
			Console.WriteLine(response.Content);
			JArray result = JArray.Parse(response.Content);
			return result;
      }


		public string export_document(string document_id, string user_id, string template_id="", string format = "docx", Dictionary<string, string> settings=null)
		{
			return "foo";
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
			check_response(response);
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


		var sd = new SMASHDOCs(client_id, client_key, partner_url, debug);
		JArray result = sd.list_templates();
		Console.WriteLine(result);
	    JObject result2  = sd.new_document("my title", "my description");
		Console.WriteLine(result2);
		string document_id = (string) result2["documentId"];
		JObject metadata = sd.document_info(document_id);
		Console.WriteLine(metadata);

		sd.archive_document(document_id);
		sd.unarchive_document(document_id);

		sd.delete_document(document_id);
		metadata = sd.document_info(document_id);




     }
}
