using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO; 
using System.Reflection;
using System.Text;

using WatsonFunction;
using WatsonFunction.FunctionCore;
using WatsonFunction.FunctionBase;

namespace WatsonFunction.Worker.Classes
{
    public class Invoker
    {
        #region Public-Members

        public bool ConsoleDebug
        {
            get
            {
                return _ConsoleDebug;
            }
            set
            {
                _ConsoleDebug = value;
            }
        }

        #endregion

        #region Private-Members

        private Request _Request = null;
        private bool _ConsoleDebug = false;

        #endregion

        #region Constructors-and-Factories

        public Invoker(Request req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            _Request = req;

            if (!_Request.BaseDirectory.EndsWith("/")) _Request.BaseDirectory += "/";
        }

        #endregion

        #region Public-Methods

        public Response Invoke()
        {
            try
            {
                Assembly dll = Assembly.LoadFile(EntryFile());
                 
                foreach (Type type in dll.GetExportedTypes())
                {
                    DateTime startTime = DateTime.Now;
                    dynamic c = Activator.CreateInstance(type);
                    Response resp = c.Start(_Request);
                    resp.StartTime = DateTime.Now;
                    resp.EndTime = DateTime.Now;
                    resp.RuntimeMs = Common.TotalMsFrom(resp.StartTime);

                    Console.WriteLine(_Request.UserGUID + "/" + _Request.FunctionName + " [" + resp.RuntimeMs + "ms]");

                    if (_ConsoleDebug)
                    {
                        Console.WriteLine("Response:"); 
                        Console.WriteLine("- Content Length : " + resp.ContentLength);
                        Console.WriteLine("- Data           : " + Encoding.UTF8.GetString(resp.Data));
                    }
                     
                    return resp;
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Invocation exception: " + Common.SerializeJson(e));
                return null;
            }
        }

        #endregion

        #region Private-Methods

        private string EntryFile()
        {
            return _Request.BaseDirectory + _Request.EntryFile;
        }

        #endregion
    }
}
