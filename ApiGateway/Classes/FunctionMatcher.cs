using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using WatsonFunction.FunctionBase;

using WatsonWebserver;

namespace WatsonFunction.ApiGateway.Classes
{
    public class FunctionMatcher
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly object _FunctionLock = new object();
        private List<FunctionApplication> _Apps;
        private List<Definition> _Definitions;

        #endregion

        #region Constructors-and-Factories

        public FunctionMatcher(List<FunctionApplication> apps)
        {
            if (apps == null) throw new ArgumentNullException(nameof(apps));

            _Apps = apps;
            _Definitions = new List<Definition>();

            foreach (FunctionApplication app in _Apps)
            {
                foreach (Definition def in app.Functions)
                {
                    _Definitions.Add(def);
                }
            }

            _Apps = _Apps.Distinct().ToList();
        }

        #endregion

        #region Public-Methods

        public Definition Match(
            HttpRequest req, 
            string userGuid, 
            string functionName, 
            out Trigger trigger, 
            out Request fcnRequest,
            out ErrorCode error)
        {
            trigger = null;
            fcnRequest = null;
            error = ErrorCode.FunctionNotFound;
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (String.IsNullOrEmpty(userGuid)) throw new ArgumentNullException(nameof(userGuid));
            if (String.IsNullOrEmpty(functionName)) throw new ArgumentNullException(nameof(functionName));
            if (_Apps == null || _Apps.Count < 1)
            {
                Console.WriteLine("No defined functions");
                return null;
            }

            List<Definition> candidates = null;

            lock (_FunctionLock)
            {
                try
                {
                    candidates = _Definitions.Where(
                        f =>
                            f.UserGUID.ToLower().Equals(userGuid.ToLower())
                            && f.FunctionName.ToLower().Equals(functionName.ToLower())).ToList();
                }
                catch (Exception)
                {
                    candidates = null;
                }
            }

            if (candidates == null || candidates.Count < 1)
            {
                Console.WriteLine("No functions matching user GUID " + userGuid + " name " + functionName);
                return null;
            }

            foreach (Definition curr in candidates)
            {
                List<Trigger> currTriggers = curr.Triggers;
                if (curr.Triggers == null || curr.Triggers.Count < 1) continue;
                 
                List<Trigger> matched = new List<Trigger>();

                foreach (Trigger currTrigger in curr.Triggers)
                { 
                    // check method
                    if (currTrigger.Methods.Contains(req.Method.ToString().ToUpper()))
                    {
                        // check request body
                        if (currTrigger.Required.RequestBody && req.Data == null)
                        {
                            continue;
                        }

                        // check SSL
                        if (currTrigger.Required.RequireSsl && !req.FullUrl.StartsWith("https://"))
                        {
                            continue;
                        }

                        // check headers
                        if (!QuerystringMatch(currTrigger.Required.Headers, req.Headers.Keys.ToList()))
                        {
                            continue;
                        }

                        // check querystring
                        if (!QuerystringMatch(currTrigger.Required.QuerystringEntries, req.QuerystringEntries.Keys.ToList()))
                        {
                            continue;
                        }

                        matched.Add(currTrigger);
                    }

                    if (matched != null && matched.Count > 0) break;
                }

                if (matched == null || matched.Count < 1)
                {
                    Console.WriteLine("No triggers match");
                    error = ErrorCode.NoMatch;
                }

                trigger = matched[0];

                fcnRequest = new Request();
                fcnRequest.BaseDirectory = curr.BaseDirectory;
                fcnRequest.EntryFile = curr.EntryFile; 
                fcnRequest.FunctionName = curr.FunctionName;
                fcnRequest.Runtime = curr.Runtime; 
                fcnRequest.TriggerType = Request.TriggerTypes.Http;
                fcnRequest.UserGUID = curr.UserGUID;

                fcnRequest.Http = new Request.HttpParameters();
                fcnRequest.Http.ContentLength = req.ContentLength;
                fcnRequest.Http.Data = req.Data; 
                fcnRequest.Http.FullUrl = req.FullUrl;
                fcnRequest.Http.Headers = req.Headers;
                fcnRequest.Http.Method = req.Method.ToString().ToLower();
                fcnRequest.Http.Querystring = req.QuerystringEntries;
                fcnRequest.Http.RawUrl = req.RawUrlWithQuery;
                fcnRequest.Http.RawUrlWithoutQuery = req.RawUrlWithoutQuery;
                fcnRequest.Http.SourceIp = req.SourceIp;
                fcnRequest.Http.SourcePort = req.SourcePort;
                fcnRequest.Http.Ssl = req.FullUrl.StartsWith("https://"); 

                return curr;
            }

            return null;
        }

        #endregion

        #region Private-Methods

        private bool QuerystringMatch(List<string> required, List<string> candidate)
        { 
            if (required == null || required.Count < 1) return true;
            
            foreach (string currRequired in required)
            {
                if (candidate != null && candidate.Count > 0 && candidate.Contains(currRequired)) continue;
                else return false;
            }

            return true;
        }

        #endregion

        #region Public-Embedded-Classes

        public enum ErrorCode
        {
            Success,
            NoMatch,
            FunctionNotFound,
            MissingRequestBody,
            MissingQuerystringParameters,
            SslRequired
        }

        #endregion
    }
}
