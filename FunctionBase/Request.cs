using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatsonFunction.FunctionBase
{
    public class Request
    {
        #region Public-Members
         
        public string FunctionGUID;
        public string FunctionName;
        public string UserGUID;
        public RuntimeEnvironment Runtime;
        public string BaseDirectory;
        public string EntryFile; 
        public TriggerTypes TriggerType;
        public HttpParameters Http;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        public enum TriggerTypes
        {
            Http
        }

        public class HttpParameters
        {
            public string SourceIp;
            public int SourcePort;
            public bool Ssl;
            public string FullUrl;
            public string RawUrl;
            public string RawUrlWithoutQuery;
            public string Method;
            public Dictionary<string, string> Querystring;
            public Dictionary<string, string> Headers;
            public bool UseStream;
            public long ContentLength;
            public byte[] Data;
            public Stream DataStream;
        }

        #endregion
    }
}
