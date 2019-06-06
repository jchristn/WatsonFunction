using System;
using System.Collections.Generic;
using System.Text;

using WatsonFunction.FunctionBase;

namespace SampleApp
{
    public class App : Application
    { 
        public override Response Start(Request req)
        {
            Response resp = new Response();
            resp.Data = Encoding.UTF8.GetBytes("Hello!  " + req.Http.Method.ToUpper() + " " + req.Http.RawUrlWithoutQuery);
            resp.Headers = new Dictionary<string, string>();
            resp.Headers.Add("Hello", "World");
            resp.HttpStatus = 200;
            return resp;
        }
    }
}
