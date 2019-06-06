using System;
using System.Collections.Generic;
using System.Text;
 
namespace WatsonFunction.FunctionBase
{
    public class Trigger
    {
        #region Public-Members
         
        public List<string> Methods { get; set; }
        public Params Required { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public Trigger()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        public class Params
        {
            public List<string> QuerystringEntries;
            public List<string> Headers;
            public bool RequestBody;
            public bool RequireSsl;
        }

        #endregion 
    }
}
