using System;

namespace WatsonFunction.FunctionBase
{
    /// <summary>
    /// A function application.  Must implement the method Start.
    /// </summary>
    public abstract class Application
    {
        /// <summary>
        /// The method invoked when a trigger is matched.
        /// </summary>
        /// <param name="req">Request.</param>
        /// <returns>Response.</returns>
        public abstract Response Start(Request req); 
    }
}
