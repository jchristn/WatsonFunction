using System;

namespace WatsonFunction.FunctionBase
{
    public abstract class Application
    {
        public abstract Response Start(Request req); 
    }
}
