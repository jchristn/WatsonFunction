using System;
using System.Collections.Generic;
using System.Text;
 
namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// Trigger that can cause invocation of a function.
    /// </summary>
    public class Trigger
    {
        #region Public-Members
         
        /// <summary>
        /// Allowed HTTP methods.
        /// </summary>
        public List<string> Methods { get; set; }

        /// <summary>
        /// HTTP parameters required.
        /// </summary>
        public Params Required { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Trigger()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        /// <summary>
        /// Trigger parameters.
        /// </summary>
        public class Params
        {
            /// <summary>
            /// Indicates which query string entries must be present.
            /// </summary>
            public List<string> QuerystringEntries;

            /// <summary>
            /// Indicates which headers must be present.
            /// </summary>
            public List<string> Headers;

            /// <summary>
            /// Indicates whether or not a request body must be present.
            /// </summary>
            public bool RequestBody;

            /// <summary>
            /// Indicates whether or not SSL must have been used in the request.
            /// </summary>
            public bool RequireSsl;
        }

        #endregion 
    }
}
