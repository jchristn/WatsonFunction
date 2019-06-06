using System;
using System.Collections.Generic;
using System.Text;
 
namespace WatsonFunction.FunctionBase
{
    public class Definition
    {
        #region Public-Members
         
        public string FunctionName; // no spaces
        public string UserGUID;
        public RuntimeEnvironment Runtime;
        public string BaseDirectory;
        public string EntryFile; 
        public List<Trigger> Triggers;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

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
