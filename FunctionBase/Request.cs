using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// Request details for a request that matched a trigger.
    /// </summary>
    public class Request
    {
        #region Public-Members
         
        /// <summary>
        /// GUID of the function.
        /// </summary>
        public string FunctionGUID;

        /// <summary>
        /// Name of the function.
        /// </summary>
        public string FunctionName;

        /// <summary>
        /// User GUID.
        /// </summary>
        public string UserGUID;

        /// <summary>
        /// Function runtime.
        /// </summary>
        public RuntimeEnvironment Runtime; 

        /// <summary>
        /// The base directory where the files supporting the function are located.  This must be an *explicit* path and NOT a *relative* path.
        /// </summary>
        public string BaseDirectory;

        /// <summary>
        /// The entry file containing the function.
        /// </summary>
        public string EntryFile;

        /// <summary>
        /// The type of trigger that caused invocation of the function.
        /// </summary>
        public TriggerTypes TriggerType;

        /// <summary>
        /// HTTP parameters from the request.
        /// </summary>
        public HttpParameters Http;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Request()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        /// <summary>
        /// Type of trigger.
        /// </summary>
        public enum TriggerTypes
        {
            Http
        }

        /// <summary>
        /// HTTP request parameters.
        /// </summary>
        public class HttpParameters
        {
            /// <summary>
            /// Source IP address.
            /// </summary>
            public string SourceIp;

            /// <summary>
            /// Source port.
            /// </summary>
            public int SourcePort;

            /// <summary>
            /// Indicates whether or not SSL was used.
            /// </summary>
            public bool Ssl;

            /// <summary>
            /// Full URL associated with the request.
            /// </summary>
            public string FullUrl;

            /// <summary>
            /// Raw URL associated with the request.
            /// </summary>
            public string RawUrl;

            /// <summary>
            /// Raw URL including the querystring.
            /// </summary>
            public string RawUrlWithoutQuery;

            /// <summary>
            /// HTTP method.
            /// </summary>
            public string Method;

            /// <summary>
            /// Querystring entries included in the request.
            /// </summary>
            public Dictionary<string, string> Querystring;

            /// <summary>
            /// Headers included in the request.
            /// </summary>
            public Dictionary<string, string> Headers;

            /// <summary>
            /// Content length of the data contained in the request.
            /// </summary>
            public long ContentLength;

            /// <summary>
            /// Request data.
            /// </summary>
            public byte[] Data;
        }

        #endregion
    }
}
