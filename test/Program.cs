using RestSharp;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

public class SMASHDOCsException : Exception
{
	public SMASHDOCsException()
	{
	}

	public SMASHDOCsException(string message)
	: base(message)
	{
	}

	public SMASHDOCsException(string message, Exception inner)
	: base(message, inner)
	{
	}
}

public class SMASHDOCs
{
	string _client_id;
	string _client_key;
	string _partner_url;
	string _group_id;
	bool _debug;

	public SMASHDOCs(string client_id, string client_key, string partner_url, string group_id, bool debug = false)
	{
		_client_id = client_id;
		_client_key = client_key;
		_partner_url = partner_url;
		_debug = debug;
		_group_id = group_id;
	}

	static long ToUnixTime(DateTime dateTime)
	{
		return (int)(dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
	}

	private string get_token()
	{
		DateTime issued = DateTime.Now;
		string iss = Guid.NewGuid().ToString();
		long iat = ToUnixTime(issued);
		string jti = Guid.NewGuid().ToString();

		var payload = new Dictionary<string, object>()
			{
				{"iss", iss},
				{"iat", iat},
				{"jti", jti}
			};

		return JWT.Encode(payload, Encoding.ASCII.GetBytes(_client_key), JwsAlgorithm.HS256);
	}

	void check_response(IRestResponse response)
	{
		int status_code = (int)response.StatusCode;
		if (status_code != 200)
		{
			string msg = $"Error: HTTP/{status_code}";
			msg += $"\nMsg: {response.Content}";
			msg += $"\nURL: {response.ResponseUri}";
			msg += "\n";
			throw new SMASHDOCsException(msg);
		}
	}

	public IRestResponse make_request(string url, Method method, Dictionary<string, object> data = null, string filename = null)
	{
		var client = new RestClient(_partner_url);
		var request = new RestRequest(url, method);

		request.AddHeader("Content-Type", "application/json");
		request.AddHeader("x-client-id", _client_id);
		request.AddHeader("Authorization", "Bearer " + get_token());

		if (filename != null)
		{
			request.AddParameter("data", JsonConvert.SerializeObject(data));
			request.AddFile("file", filename);
			request.AddHeader("Content-Type", "multipart/form-data");
		}
		else
		{
			if (data != null)
				request.AddJsonBody(data);
		}

		if (_debug)
		{
            Console.WriteLine("### REQUEST ###");
			Console.WriteLine($"URL: {url}");
			Console.WriteLine($"Method: {method}");
			Console.WriteLine("Parameters:");
			request.Parameters.ForEach(i => Console.WriteLine("\t{0}", i));
		}

		IRestResponse response = client.Execute(request);
        int status_code = (int) response.StatusCode;
		if (_debug)
		{
            Console.WriteLine("### RESPONSE ###");
			Console.WriteLine($"HTTP status: {status_code}");
			Console.WriteLine("Headers:");
            foreach (var h in response.Headers) {
                Console.WriteLine($"\t{h}");
            }
		}

		check_response(response);
		return response;
	}

	public JObject document_info(string document_id)
	{
		string url = $"/partner/documents/{document_id}";
		IRestResponse response = make_request(url, Method.GET);
		return JObject.Parse(response.Content);
	}

	public void delete_document(string document_id)
	{
		string url = $"/partner/documents/{document_id}";
		make_request(url, Method.DELETE);
	}

	public void review_document(string document_id)
	{
		string url = $"/partner/documents/{document_id}/review";
		make_request(url, Method.POST);
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

	public JArray list_templates()
	{
		string url = "/partner/templates/word";
		IRestResponse response = make_request(url, Method.GET);
		return JArray.Parse(response.Content);
	}

	public string export_document(string document_id, string user_id, string template_id = "", string format = "docx", Dictionary<string, string> settings = null)
	{
		var data = new Dictionary<string, object>()
			{
				{"userId", user_id
				}
			};

		string url = "";

		switch (format)
		{
			case "html":
				url = $"/partner/documents/{document_id}/export/html";
				break;

			case "docx":
				url = $"/partner/documents/{document_id}/export/word";
				if (settings == null)
					data["settings"] = new Dictionary<string, string>();
				else
					data["settings"] = settings;
				data["templateId"] = template_id;
				break;

			case "sdxml":
				url = $"/partner/documents/{document_id}/export/sdxml";
				break;
			default:
				throw new Exception($"Unknown format {format}");

		}

		IRestResponse response = make_request(url, Method.POST, data);

		string ext = (format == "docx") ? ".docx" : ".zip";
		string fn = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ext;
		FileStream fs = new FileStream(fn, FileMode.CreateNew);
		BinaryWriter bw = new BinaryWriter(fs);
		bw.Write(response.RawBytes);
		bw.Close();
		fs.Close();
		return fn;
	}

	public JArray get_documents(string user_id = null, string group_id = null)
	{
		var data = new Dictionary<string, object>();
		if (user_id != null)
			data["userId"] = user_id;
		if (group_id != null)
			data["groupId"] = group_id;

		string url = "/partner/documents/list";
		IRestResponse response = make_request(url, Method.GET, data: data);
		return JArray.Parse(response.Content);
	}

	public JObject upload_document(string filename, string title = "", string description = "", string role = "", string status = "draft", Dictionary<string, string> user_data = null)
	{
		var data = new Dictionary<string, object>()
			{
				{"user", user_data},
				{"title", title},
				{"description", description},
				{"userRole", role},
				{"groupId", _group_id},
				{"status", status},
				{"sectionHistory", true}
			};

		string endpoint = filename.ToLower().EndsWith(".docx") ? "word" : "sdxml";
		string url = $"/partner/imports/{endpoint}/upload";
		IRestResponse response = make_request(url,
											  Method.POST,
											  data: data,
											  filename: filename);
		return JObject.Parse(response.Content);
	}

	public JObject new_document(string title = "", string description = "", string role = "editor", string status = "draft", Dictionary<string, string> user_data = null)
	{
		var data = new Dictionary<string, object>()
			{
				{"user", user_data},
				{"title", title},
				{"description", description},
				{"userRole", role},
				{"groupId", _group_id},
				{"status", status},
				{"sectionHistory", true}
			};

		IRestResponse response = make_request("/partner/documents/create", Method.POST, data);
		return JObject.Parse(response.Content);
	}
}


public class Runner
{
	static public void Main()
	{
		string client_id = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_ID");
		string client_key = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_KEY");
		string partner_url = Environment.GetEnvironmentVariable("SMASHDOCS_PARTNER_URL");
		bool debug = Environment.GetEnvironmentVariable("SMASHDOCS_DEBUG") != null;

		var user_data = new Dictionary<string, string>()
			{
				{"email", "info@xx.de"},
				{"firstname", "Henry"},
				{"lastname", "Miller"},
				{"userId", "testuser"},
				{"company", "Dummies Ltd"}
			};

		var sd = new SMASHDOCs(client_id, client_key, partner_url, debug: debug, group_id: "testgrp");

		JArray r0 = sd.get_documents(user_id: "ajung");
		Console.WriteLine(r0);

		JObject r1 = sd.upload_document("/tmp/test.docx", role: "editor", user_data: user_data);
		Console.WriteLine(r1);

		JArray templates = sd.list_templates();
		Console.WriteLine(templates);
		JObject result2 = sd.new_document("my title", "my description", user_data: user_data);
		Console.WriteLine(result2);
		string document_id = (string)result2["documentId"];
		JObject metadata = sd.document_info(document_id);
		Console.WriteLine(metadata);

		sd.archive_document(document_id);
		sd.unarchive_document(document_id);


		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: "", format: "html"));
		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: "", format: "sdxml"));
		string template_id = (string)templates[0]["id"];
		Console.WriteLine(sd.export_document(document_id, user_id: "ajung", template_id: template_id, format: "docx"));

		sd.delete_document(document_id);

		result2 = sd.new_document("my title", "my description");
		Console.WriteLine(result2);
		document_id = (string)result2["documentId"];
		sd.review_document(document_id);
	}
}
