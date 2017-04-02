using RestSharp;
using System;
using System.Collections.Generic;
using Jose;
using System.Text;
using System.IO;
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
				Console.WriteLine("Error:");
				Console.WriteLine(response.Content);
				throw new Exception($"Status code: {status_code}");
			}
		}

		public IRestResponse make_request(string url, Method method, Dictionary<string, object> data = null)
		{

			var client = new RestClient();
			client.BaseUrl = new Uri(_partner_url);
			var request = new RestRequest(url, method);

			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());
			if (data != null)
			{
				request.AddJsonBody(data);
			}
			IRestResponse response = client.Execute(request);
			check_response(response);
			return response;
		}

		public JObject document_info(string document_id)
		{
			string url =  $"/partner/documents/{document_id}";
			IRestResponse response = make_request(url, Method.GET);
			return JObject.Parse(response.Content);
		}

		public void  delete_document(string document_id)
		{
			string url = $"/partner/documents/{document_id}";
			make_request(url, Method.DELETE);
		}

		public void archive_document(string document_id)
			{
				string url = $"/partner/documents/{document_id}/archive";
				make_request(url, Method.POST);
			}
			

		public void unarchive_document(string document_id)
		{
			string url = $"/partner/documents/{document_id}/unarchive";
			make_request(url, Method.POST);
		}

		public JArray list_templates() {
			IRestResponse response = make_request("/partner/templates/word", Method.GET);
			return  JArray.Parse(response.Content);
        }


		public string export_document(string document_id, string user_id, string template_id="", string format = "docx", Dictionary<string, string> settings=null)
		{
			var data = new Dictionary<string, object>()
			{
				{"userId", user_id}
			};

			string url = "";
			if (format == "html")
			{
				url = $"/partner/documents/{document_id}/export/html";
			}
			else if (format == "sdxml")
			{
				url = $"/partner/documents/{document_id}/export/sdxml";
			}
			else if (format == "docx")
			{
				url = $"/partner/documents/{document_id}/export/word";
				if (settings == null)
				{
					data["settings"] = new Dictionary<string, string>();
				}
				else			{
					data["settings"] = settings;
				}
				data["templateId"] = template_id;
			}
			else
			{
				throw new Exception($"Unknown format {format}");
			}


			var client = new RestClient();
			client.BaseUrl = new Uri(_partner_url);
			var request = new RestRequest(Method.POST);
			Console.WriteLine(url); 
			request.Resource = url;
			request.AddHeader("Content-Type", "application/json");
			request.AddHeader("x-client-id", _client_id);
			request.AddHeader("Authorization", "Bearer " + get_token());
			request.AddJsonBody(data);

			IRestResponse response = client.Execute(request);
			check_response(response);

			string ext = (format == "docx") ? ".docx" : ".zip";
			string fn = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ext;
			FileStream fs = new FileStream(fn, FileMode.CreateNew);
			BinaryWriter bw = new BinaryWriter(fs);
			bw.Write(response.RawBytes);
			bw.Close();
			fs.Close();
			return fn;
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

			IRestResponse response = make_request("/partner/documents/create", Method.POST, data);
			return JObject.Parse(response.Content);
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
		JArray templates = sd.list_templates();
		Console.WriteLine(templates);
	    JObject result2  = sd.new_document("my title", "my description");
		Console.WriteLine(result2);
		string document_id = (string) result2["documentId"];
		JObject metadata = sd.document_info(document_id);
		Console.WriteLine(metadata);

		sd.archive_document(document_id);
		sd.unarchive_document(document_id);


		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: "", format: "html"));
		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: "", format: "sdxml"));
		string template_id = (string) templates[0]["id"];
		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: template_id, format: "docx"));

		sd.delete_document(document_id);
     }
}
