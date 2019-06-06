using System;
using System.Collections.Generic;
using System.Text;

namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// Function application.
    /// </summary>
    public class FunctionApplication
    {
        #region Public-Members

        /// <summary>
        /// The name of the application.  Use only ASCII characters and no spaces.
        /// </summary>

        public string Name;

        /// <summary>
        /// The user GUID to which this function is mapped.
        /// </summary>
        public string UserGUID;

        /// <summary>
        /// The list of functions mapped to this application.
        /// </summary>
        public List<Definition> Functions;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public FunctionApplication()
        {
            Functions = new List<Definition>();
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
