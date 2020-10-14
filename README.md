![alt tag](https://github.com/jchristn/watsonfunction/blob/master/assets/watson.ico)

# WatsonFunction

```
                   __                   
   _      ______ _/ /__________  ____   
  | | /| / / __ `/ __/ ___/ __ \/ __ \  
  | |/ |/ / /_/ / /_(__  ) /_/ / / / /  
  |__/|__/\__,_/\__/____/\____/_/ /_/   

```
WatsonFunction is a simple function-as-a-service (FaaS) platform for hosting and executing C#/.NET Core function applications.  WatsonFunction includes the following components:

![alt tag](https://github.com/jchristn/watsonfunction/blob/master/assets/diagram.png)

## New in Version 1.0.x

- Initial release

## Components

- API Gateway - marshals incoming RESTful HTTP/HTTPS requests to worker nodes
- Message Bus - message queue based on BigQ (https://github.com/bigqio/bigq)
- Worker - nodes that execute functions

## Building Function Applications

Function applications must include ```WatsonFunction.FunctionBase``` and implement a method with the signature ```public override Response Start(Request req)```.  An example application can be found in the ```SampleApp``` project, and a simplified version is shown below.

Install the NuGet package ```WatsonFunction.FunctionBase``` to creation functions (https://www.nuget.org/packages/WatsonFunction.Base/).

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using WatsonFunction.FunctionBase;

namespace SampleApp
{
  public class App : Application
  { 
    public override Response Start(Request req)
    {
      Response resp = new Response();
      resp.Data = Encoding.UTF8.GetBytes("Hello!  " + req.Http.Method.ToUpper() + " " + req.Http.RawUrlWithoutQuery);
      resp.HttpStatus = 200;
      return resp;
    }
  }
}
```

## Defining Functions

Function applications are defined in the ```system.json``` file for the API gateway.  An application is a collection of functions, where each function has a series of HTTP-based triggers (verbs, URL parameters), and accessed via URL of the form ```http||https://[hostname]:[port]/[userguid]/[functionname]```.  API gateway nodes are stateless and can be deployed behind a loadbalancer (such as uscale, see https://github.com/jchristn/uscale) as required.

Incoming requests are evaluated against the list of functions to find the first match.  Function definitions can also specify required parameters such as the required querystring entries, headers, presence of the request body, or use of SSL.

An example ```system.json``` file for the API gateway is shown below, defining one function that can be accessed through HTTP GET/PUT/POST/DELETE, using the DLL file found in ```C:/WatsonFunction/DLLs/SampleApp.dll```.  It is critical to note that the explicit directory MUST be listed in ```BaseDirectory```; relative paths are not supported due to limitations in C# ```System.Activator```.

```
{
  "Webserver": {
    "Hostname": "*",
    "TcpPort": 9000,
    "Ssl": false,
    "Debug": false
  },
  "MessageQueue": {
    "TcpPort": 10000,
    "Debug": false,
    "Channels": {
      "MainChannel": "main",
      "HealthChannel": "health",
      "InvocationChannel": "invocation"	
    },
    "Applications": [
      { 
        "Name": "My Default Application",
        "UserGUID": "default",
        "Functions": [
          { 
            "FunctionName": "default",
            "UserGUID": "default",
            "Runtime": "NetCore22",
            "BaseDirectory": "C:/WatsonFunction/DLLs/",
            "EntryFile": "SampleApp.dll",
            "Triggers": [
              {
                "Methods": [ "GET", "PUT", "POST", "DELETE" ],
                "Required": {
                  "QuerystringEntries": null,
                  "Headers": null,
                  "RequestBody": false,
                  "RequireSsl": false
              }
            }
          ]
        }
      ]
    }
  ]
}
```

## Message Bus

The message bus for WatsonFunction relies on BigQ (https://github.com/bigqio/bigq) though the code could be easily modified to use virtually any message queue.  Three channels are created (main, health, invocation), though only the invocation channel is used today.

## Worker Nodes

Worker nodes need to have access to the filesystem location where functions are stored.  As of this release, functions are invoked using the *same user account* as was used to start the worker node itself.  Thus it is recommended that the platform currently be used only for deployments where the function source code is *trusted*.  Alternatively, run the worker nodes in containers.

Worker nodes listen on the invocation channel, deserialize messages to the ```Request``` object, invoke the function, and return the ```Response``` object returned by the function through the message bus to the API gateway, which then marshals the response to the caller.

Worker nodes must have access to the ```BaseDirectory``` (which must be explicit and not relative) to read the ```EntryFile```.

## Supported Runtimes

As of this release, only .NET Core functions are supported.

## Administrative APIs

As of v1.0.0, only one administrative API is available: ```GET /_directory```, which returns a JSON object containing all of the applications (and functions) hosted on the platform.

```
GET /_directory
(no request body)
Response:
200/OK
[
  {
    "Name": "default",
    "UserGUID": "default",
    "Functions": [
      {
        "FunctionName": "default",
        "UserGUID": "default",
        "Runtime": 0,
        "BaseDirectory": "C:/WatsonFunction/DLLs/",
        "EntryFile": "SampleApp.dll",
        "Triggers": [
          {
            "Methods": [
              "GET",
              "PUT",
              "POST",
              "DELETE"
            ],
            "Required": {
              "RequestBody": false,
              "RequireSsl": false
            }
          }
        ]
      }
    ]
  }
]
```

## Roadmap

The following items are planned for future releases:

- Ongoing monitoring of CPU, memory utilization on worker nodes to influence invocation decisions
- Function caching on worker nodes to reduce invocation time and improve performance
- Logging beyond simple Console.WriteLine
- Invoking functions under specific user accounts to control security
- Structured error responses (JSON objects vs plaintext)
- Additional runtimes

## Version History

New features from previous versions will be shown below.

v1.0.x

- Initial release
