using System;
using System.IO;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


[TestFixture]
public class SMASHDOCTests
{

    SMASHDOCs sd = null;
    Dictionary<string, string> user_data = null;

    string CREDENTIALS = "/Users/ajung/.smashdocs.json";

    [SetUp]
    protected void SetUp()
    {

        string client_id;
        string client_key;
        string partner_url;
        bool debug;

        if (File.Exists(CREDENTIALS))
        {
            JObject settings = JObject.Parse(File.ReadAllText(CREDENTIALS));
            client_id = (string)settings["SMASHDOCS_CLIENT_ID"];
            client_key = (string)settings["SMASHDOCS_CLIENT_KEY"];
            partner_url = (string)settings["SMASHDOCS_PARTNER_URL"];
            debug = false;
        }
        else
        {
            client_id = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_ID");
            client_key = Environment.GetEnvironmentVariable("SMASHDOCS_CLIENT_KEY");
            partner_url = Environment.GetEnvironmentVariable("SMASHDOCS_PARTNER_URL");
            debug = Environment.GetEnvironmentVariable("SMASHDOCS_DEBUG") != null;
        }
        user_data = new Dictionary<string, string>()
            {
                {"email", "info@xx.de"},
                {"firstname", "Henry"},
                {"lastname", "Miller"},
                {"userId", "testuser"},
                {"company", "Dummies Ltd"}
            };

        sd = new SMASHDOCs(client_id, client_key, partner_url, debug: debug, group_id: "testgrp");
    }


    [Test]
    public void get_documents()
    {
        JArray result = sd.get_documents();
    }

    [Test]
    public void new_document()
    {
        JObject result = sd.new_document(title: "my title", description: "my description", user_data: user_data);
        string document_id = (string)result["documentId"];
        string url = (string)result["documentAccessLink"];
        Assert.IsTrue(url.StartsWith("https://partner"));
    }
}
