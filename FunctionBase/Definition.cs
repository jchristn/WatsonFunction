using System;
using System.Collections.Generic;
using System.Text;
 
namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// Function application definition.
    /// </summary>
    public class Definition
    {
        #region Public-Members
         
        /// <summary>
        /// The name of the function.  Use only ASCII characters and no spaces, as this name will be used in requests sent to the API gateway in the URL.
        /// </summary>
        public string FunctionName; // no spaces

        /// <summary>
        /// The user GUID to which this function is mapped.
        /// </summary>
        public string UserGUID;

        /// <summary>
        /// The runtime for the function.
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
        /// The list of triggers and conditions that should be met to invoke the function.
        /// </summary>
        public List<Trigger> Triggers;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Definition()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        #endregion
    }
}
