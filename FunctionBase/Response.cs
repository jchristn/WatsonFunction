using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatsonFunction.FunctionBase
{
    public class Response
    {
        #region Public-Members

        public DateTime StartTime;
        public DateTime EndTime; 
        public double RuntimeMs; 
        public int HttpStatus
        {
            get
            {
                return _HttpStatus;
            }
            set
            {
                if (value < 100 || value > 599) throw new ArgumentException("HttpStatus must be between 100 and 599.");
                _HttpStatus = value;
            }
        }

        public string ContentType;
        public Dictionary<string, string> Headers; 

        public long ContentLength
        {
            get
            {
                return _ContentLength;
            }
            set
            {
                if (value < 0) throw new ArgumentException("Content length must be zero or greater.");
                _ContentLength = value;
            }
        }

        public byte[] Data
        {
            get
            {
                return _Data;
            }
            set
            {
                _Data = value;
                if (Data != null) _ContentLength = _Data.Length;
            }
        }

        #endregion

        #region Private-Members

        private int _HttpStatus;
        private long _ContentLength;
        private byte[] _Data; 

        #endregion

        #region Constructors-and-Factories

        public Response()
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
