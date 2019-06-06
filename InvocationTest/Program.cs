using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Text;

using WatsonFunction;
using WatsonFunction.FunctionCore;
using WatsonFunction.FunctionBase;

namespace InvocationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = Common.InputString("Filename:", null, false);
            string tempFilename = Guid.NewGuid().ToString();

            try
            {
                var dll = Assembly.LoadFile(filename);

                foreach (Type type in dll.GetExportedTypes())
                {
                    dynamic c = Activator.CreateInstance(type);
                     
                    Request req = new Request();
                    req.BaseDirectory = Path.GetDirectoryName(filename);
                    req.EntryFile = Path.GetFileName(filename);
                    req.FunctionGUID = "0000";
                    req.FunctionName = "Test function";
                    req.Runtime = RuntimeEnvironment.NetCore22; 
                    req.TriggerType = Request.TriggerTypes.Http;
                    req.UserGUID = "1111";

                    req.Http = new Request.HttpParameters();
                    req.Http.ContentLength = 5;
                    req.Http.Data = Encoding.UTF8.GetBytes("Hello");
                    req.Http.FullUrl = "http://www.foo.com/foo/bar?foo=bar";
                    req.Http.Headers = null;
                    req.Http.Method = "GET";

                    req.Http.Querystring = new Dictionary<string, string>();
                    req.Http.Querystring.Add("foo", "bar");

                    req.Http.RawUrl = "/foo/bar?foo=bar";
                    req.Http.RawUrlWithoutQuery = "/foo/bar";
                    req.Http.SourceIp = "127.0.0.1";
                    req.Http.SourcePort = 8000;
                    req.Http.Ssl = false;
                    
                    Response resp = c.Start(req);

                    resp.EndTime = DateTime.Now;
                    resp.RuntimeMs = Common.TotalMsFrom(resp.StartTime);

                    if (resp != null)
                    {
                        Console.WriteLine("Response:"); 
                        Console.WriteLine("- Content Length : " + resp.ContentLength);
                        Console.WriteLine("- Data           : " + Encoding.UTF8.GetString(resp.Data));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Common.SerializeJson(e));
            }
            finally
            {
            } 
        } 
    }
}
