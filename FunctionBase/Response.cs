using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// Response from a function that was invoked.
    /// </summary>
    public class Response
    {
        #region Public-Members

        /// <summary>
        /// Time when the function was invoked.
        /// </summary>
        public DateTime StartTime;

        /// <summary>
        /// Time when the function finished execution.
        /// </summary>
        public DateTime EndTime; 

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        public double RuntimeMs; 

        /// <summary>
        /// HTTP status to return to the requestor.
        /// </summary>
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

        /// <summary>
        /// Content type of the response data.
        /// </summary>
        public string ContentType;

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> Headers; 

        /// <summary>
        /// Content length of the response data.
        /// </summary>
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

        /// <summary>
        /// Response data.
        /// </summary>
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

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Response()
        {
            _HttpStatus = 200;
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
