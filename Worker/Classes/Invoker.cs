using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO; 
using System.Reflection;
using System.Text;

using SyslogLogging;

using WatsonFunction;
using WatsonFunction.FunctionCore;
using WatsonFunction.FunctionBase;

namespace WatsonFunction.Worker.Classes
{
    public class Invoker
    {
        #region Public-Members

        public bool Debug
        {
            get
            {
                return _Debug;
            }
            set
            {
                _Debug = value;
            }
        }

        #endregion

        #region Private-Members

        private bool _Debug = false;
        private LoggingModule _Logging;
        private Request _Request = null; 

        #endregion

        #region Constructors-and-Factories

        public Invoker(LoggingModule logging, Request req)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (req == null) throw new ArgumentNullException(nameof(req));

            _Logging = logging;
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

                    _Logging.Log(LoggingModule.Severity.Debug, "Worker Invoker " + _Request.UserGUID + "/" + _Request.FunctionName + " [" + resp.RuntimeMs + "ms]");
                     
                    if (_Debug)
                    {
                        _Logging.Log(LoggingModule.Severity.Debug, "Worker Invoker response:");
                        _Logging.Log(LoggingModule.Severity.Debug, "- Content Length : " + resp.ContentLength);

                        if (resp.Data != null && resp.Data.Length > 0)
                            _Logging.Log(LoggingModule.Severity.Debug, "- Data           : " + Encoding.UTF8.GetString(resp.Data));
                        else
                            _Logging.Log(LoggingModule.Severity.Debug, "- Data           : [null]");
                    }

                    return resp;
                }

                return null;
            }
            catch (Exception e)
            {
                _Logging.Log(LoggingModule.Severity.Alert, "Worker Invoker exception while invoking:");
                _Logging.LogException("Worker", "Invoker", e);
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
