using System;

using System.Collections.Generic;

public class Tests
{
	static public void Main2()
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

	}
}
