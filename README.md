# TraceContextExample

W3C Trace Context 等についての詳細はブログに。

[【C#】ASP.NET Core と W3C Trace Context とお手軽ロギング。](https://blog.neno.dev/entry/2023/07/04/181843)

## Table of Contents
- [準備](#準備)
  - [Program.cs](#programcs)
  - [appsettings.json](#appsettingsjson)
- [ログ](#ログ)
  - [SimpleConsole](#simpleconsole)
  - [JsonConsole](#jsonconsole)
- [Visual Studio のおすすめ設定](#visual-studio-のおすすめ設定)


## 準備

### Program.cs

```cs
var builder = WebApplication.CreateBuilder(args);

// ~~ いろいろ省略 ~~

// dev 環境とかはこっち。
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true; // めっちゃ大事
});

// prod 環境とかでちゃんと構造化ロギングする時はこっち
//builder.Logging.AddJsonConsole(options =>
//{
//    options.IncludeScopes = true;
//    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
//    options.UseUtcTimestamp = true;
//    options.JsonWriterOptions = new JsonWriterOptions
//    {
//        Indented = false
//    };
//});

// app.UseHttpLogging() で使うサービスの追加
builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestProperties | HttpLoggingFields.ResponseStatusCode;
});

// app.UseExceptionHandler() で使うサービスの追加
builder.Services.AddProblemDetails();

var app = builder.Build();

// HttpMessageHandlerActivityOvserver は自前で実装したクラス。
// https://github.com/nenoNaninu/TraceContextExample/blob/main/src/ServiceA/Diagnostics/HttpMessageHandlerActivityOvserver.cs
var loggerFactory = app.Services.GetService<ILoggerFactory>()!;
DiagnosticListener.AllListeners.Subscribe(new HttpMessageHandlerActivityOvserver(loggerFactory));

// HTTP リクエストをロギングするためのミドルウェア
app.UseHttpLogging();
// 未処理の例外をハンドリングするためのミドルウェア
// prod 環境で何かと嬉しい
// - 500 を返すだけでなく、レスポンスボディに trace-id が含まれるようになる
// - ここで例外を catch しておかないと、上位の HttpLoggingMiddleware をも例外が突き抜ける
//     - 結果としてレスポンスのログが残らない
//     - 寧ろこれを挟んで置かないと例外が Kestrel.Core.Internal にまで突き抜けてしまう。
app.UseExceptionHandler();

// ~~ いろいろ省略 ~~

app.Run();
```

### appsettings.json
`"Microsoft.AspNetCore.HttpLogging": "Information"` の設定を加える。
以下のような感じになるはず。

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning",
            "Microsoft.AspNetCore.HttpLogging": "Information"
        }
    }
}
```

## ログ

### SimpleConsole

ServiceA

```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023
      Request:
      Protocol: HTTP/2
      Method: GET
      Scheme: https
      PathBase:
      Path: /first
info: ServiceA.Controllers.FirstController[0]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA)
      FirstController.Get()
info: System.Net.Http.HttpClient.Http2Client.LogicalHandler[100]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Start processing HTTP request GET http://localhost:5159/second
info: System.Net.Http.HttpClient.Http2Client.ClientHandler[100]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Sending HTTP request GET http://localhost:5159/second
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:196a6fb1beba1641, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6e9a81b89ca5f273 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:196a6fb1beba1641, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6e9a81b89ca5f273 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:196a6fb1beba1641, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6e9a81b89ca5f273 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.
info: System.Net.Http.HttpClient.Http2Client.ClientHandler[101]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      Received HTTP response headers after 2.1807ms - 200
info: System.Net.Http.HttpClient.Http2Client.LogicalHandler[101]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023 => ServiceA.Controllers.FirstController.Get (ServiceA) => HTTP GET http://localhost:5159/second
      End processing HTTP request after 2.2294ms - 200
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[2]
      => SpanId:6e9a81b89ca5f273, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:0000000000000000 => ConnectionId:0HMROP668KHGC => RequestPath:/first RequestId:0HMROP668KHGC:00000023
      Response:
      StatusCode: 200
```

ServiceB

```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B
      Request:
      Protocol: HTTP/2
      Method: GET
      Scheme: http
      PathBase:
      Path: /second
info: ServiceB.Controllers.SecondController[0]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB)
      SecondController.Get()
info: System.Net.Http.HttpClient.Http2Client.LogicalHandler[100]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Start processing HTTP request GET http://localhost:5002/third
info: System.Net.Http.HttpClient.Http2Client.ClientHandler[100]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Sending HTTP request GET http://localhost:5002/third
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:6203d4f905a12ddb, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:3bd3719aefa06118 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:6203d4f905a12ddb, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:3bd3719aefa06118 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:6203d4f905a12ddb, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:3bd3719aefa06118 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.
info: ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver[0]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.
info: System.Net.Http.HttpClient.Http2Client.ClientHandler[101]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      Received HTTP response headers after 1.0458ms - 200
info: System.Net.Http.HttpClient.Http2Client.LogicalHandler[101]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B => ServiceB.Controllers.SecondController.Get (ServiceB) => HTTP GET http://localhost:5002/third
      End processing HTTP request after 1.0795ms - 200
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[2]
      => SpanId:3bd3719aefa06118, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:196a6fb1beba1641 => ConnectionId:0HMROP668KHG0 => RequestPath:/second RequestId:0HMROP668KHG0:0000001B
      Response:
      StatusCode: 200
```

ServiceC

```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      => SpanId:c7edcce747b3a145, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6203d4f905a12ddb => ConnectionId:0HMROP668PKVC => RequestPath:/third RequestId:0HMROP668PKVC:0000001B
      Request:
      Protocol: HTTP/2
      Method: GET
      Scheme: http
      PathBase:
      Path: /third
info: ServiceC.Controllers.ThirdController[0]
      => SpanId:c7edcce747b3a145, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6203d4f905a12ddb => ConnectionId:0HMROP668PKVC => RequestPath:/third RequestId:0HMROP668PKVC:0000001B => ServiceC.Controllers.ThirdController.Get (ServiceC)
      ThirdController.Get()
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[2]
      => SpanId:c7edcce747b3a145, TraceId:a8f8ea4153ca4595861126ed8ba98570, ParentId:6203d4f905a12ddb => ConnectionId:0HMROP668PKVC => RequestPath:/third RequestId:0HMROP668PKVC:0000001B
      Response:
      StatusCode: 200
```

### JsonConsole

ServiceA

```
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":1,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: https\r\nPathBase: \r\nPath: /first","State":{"Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: https\r\nPathBase: \r\nPath: /first","Protocol":"HTTP/2","Method":"GET","Scheme":"https","PathBase":"","Path":"/first"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"}]}
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Controllers.FirstController","Message":"FirstController.Get()","State":{"Message":"FirstController.Get()","{OriginalFormat}":"FirstController.Get()"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"}]}
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":100,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.LogicalHandler","Message":"Start processing HTTP request GET http://localhost:5159/second","State":{"Message":"Start processing HTTP request GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"Start processing HTTP request {HttpMethod} {Uri}"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":100,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.ClientHandler","Message":"Sending HTTP request GET http://localhost:5159/second","State":{"Message":"Sending HTTP request GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"Sending HTTP request {HttpMethod} {Uri}"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.","name":"System.Net.Http.HttpRequestOut.Start","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:700e86ec9206471a, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7007e1809a501a35","SpanId":"700e86ec9206471a","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7007e1809a501a35"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.852Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.","name":"System.Net.Http.Request","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:700e86ec9206471a, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7007e1809a501a35","SpanId":"700e86ec9206471a","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7007e1809a501a35"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.","name":"System.Net.Http.HttpRequestOut.Stop","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:700e86ec9206471a, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7007e1809a501a35","SpanId":"700e86ec9206471a","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7007e1809a501a35"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.","name":"System.Net.Http.Response","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":101,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.ClientHandler","Message":"Received HTTP response headers after 2.3868ms - 200","State":{"Message":"Received HTTP response headers after 2.3868ms - 200","ElapsedMilliseconds":2.3868,"StatusCode":200,"{OriginalFormat}":"Received HTTP response headers after {ElapsedMilliseconds}ms - {StatusCode}"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":101,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.LogicalHandler","Message":"End processing HTTP request after 2.4559ms - 200","State":{"Message":"End processing HTTP request after 2.4559ms - 200","ElapsedMilliseconds":2.4559,"StatusCode":200,"{OriginalFormat}":"End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}"},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"},{"Message":"ServiceA.Controllers.FirstController.Get (ServiceA)","ActionId":"157c29a1-ddfc-4f6a-af59-291d985dca21","ActionName":"ServiceA.Controllers.FirstController.Get (ServiceA)"},{"Message":"HTTP GET http://localhost:5159/second","HttpMethod":"GET","Uri":"http://localhost:5159/second","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.855Z","EventId":2,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Response:\r\nStatusCode: 200","State":{"Message":"Response:\r\nStatusCode: 200","StatusCode":200},"Scopes":[{"Message":"SpanId:7007e1809a501a35, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:0000000000000000","SpanId":"7007e1809a501a35","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"0000000000000000"},{"Message":"ConnectionId:0HMROP3USVLKG","ConnectionId":"0HMROP3USVLKG"},{"Message":"RequestPath:/first RequestId:0HMROP3USVLKG:00000025","RequestId":"0HMROP3USVLKG:00000025","RequestPath":"/first"}]}
```

ServiceB

```
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":1,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: http\r\nPathBase: \r\nPath: /second","State":{"Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: http\r\nPathBase: \r\nPath: /second","Protocol":"HTTP/2","Method":"GET","Scheme":"http","PathBase":"","Path":"/second"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"}]}
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":0,"LogLevel":"Information","Category":"ServiceB.Controllers.SecondController","Message":"SecondController.Get()","State":{"Message":"SecondController.Get()","{OriginalFormat}":"SecondController.Get()"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"}]}
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":100,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.LogicalHandler","Message":"Start processing HTTP request GET http://localhost:5002/third","State":{"Message":"Start processing HTTP request GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"Start processing HTTP request {HttpMethod} {Uri}"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":100,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.ClientHandler","Message":"Sending HTTP request GET http://localhost:5002/third","State":{"Message":"Sending HTTP request GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"Sending HTTP request {HttpMethod} {Uri}"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Start.","name":"System.Net.Http.HttpRequestOut.Start","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:7a0e874c8a81f865, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:2b4fc7a6ca726978","SpanId":"7a0e874c8a81f865","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"2b4fc7a6ca726978"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.853Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Request.","name":"System.Net.Http.Request","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:7a0e874c8a81f865, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:2b4fc7a6ca726978","SpanId":"7a0e874c8a81f865","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"2b4fc7a6ca726978"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.HttpRequestOut.Stop.","name":"System.Net.Http.HttpRequestOut.Stop","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:7a0e874c8a81f865, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:2b4fc7a6ca726978","SpanId":"7a0e874c8a81f865","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"2b4fc7a6ca726978"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":0,"LogLevel":"Information","Category":"ServiceA.Diagnostics.HttpMessageHandlerActivityOvserver","Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.","State":{"Message":"Received activity event from HttpMessageHandler. Event name is System.Net.Http.Response.","name":"System.Net.Http.Response","{OriginalFormat}":"Received activity event from HttpMessageHandler. Event name is {EventName}."},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":101,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.ClientHandler","Message":"Received HTTP response headers after 1.0207ms - 200","State":{"Message":"Received HTTP response headers after 1.0207ms - 200","ElapsedMilliseconds":1.0207,"StatusCode":200,"{OriginalFormat}":"Received HTTP response headers after {ElapsedMilliseconds}ms - {StatusCode}"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":101,"LogLevel":"Information","Category":"System.Net.Http.HttpClient.Http2Client.LogicalHandler","Message":"End processing HTTP request after 1.0851ms - 200","State":{"Message":"End processing HTTP request after 1.0851ms - 200","ElapsedMilliseconds":1.0851,"StatusCode":200,"{OriginalFormat}":"End processing HTTP request after {ElapsedMilliseconds}ms - {StatusCode}"},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"},{"Message":"ServiceB.Controllers.SecondController.Get (ServiceB)","ActionId":"f7dabf08-c761-4986-9538-15cff3e78a9a","ActionName":"ServiceB.Controllers.SecondController.Get (ServiceB)"},{"Message":"HTTP GET http://localhost:5002/third","HttpMethod":"GET","Uri":"http://localhost:5002/third","{OriginalFormat}":"HTTP {HttpMethod} {Uri}"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":2,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Response:\r\nStatusCode: 200","State":{"Message":"Response:\r\nStatusCode: 200","StatusCode":200},"Scopes":[{"Message":"SpanId:2b4fc7a6ca726978, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:700e86ec9206471a","SpanId":"2b4fc7a6ca726978","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"700e86ec9206471a"},{"Message":"ConnectionId:0HMROP3USVN4H","ConnectionId":"0HMROP3USVN4H"},{"Message":"RequestPath:/second RequestId:0HMROP3USVN4H:0000001D","RequestId":"0HMROP3USVN4H:0000001D","RequestPath":"/second"}]}
```


ServiceC

```
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":1,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: http\r\nPathBase: \r\nPath: /third","State":{"Message":"Request:\r\nProtocol: HTTP/2\r\nMethod: GET\r\nScheme: http\r\nPathBase: \r\nPath: /third","Protocol":"HTTP/2","Method":"GET","Scheme":"http","PathBase":"","Path":"/third"},"Scopes":[{"Message":"SpanId:3e8fab9cf7709928, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7a0e874c8a81f865","SpanId":"3e8fab9cf7709928","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7a0e874c8a81f865"},{"Message":"ConnectionId:0HMROP3USVLAU","ConnectionId":"0HMROP3USVLAU"},{"Message":"RequestPath:/third RequestId:0HMROP3USVLAU:0000001D","RequestId":"0HMROP3USVLAU:0000001D","RequestPath":"/third"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":0,"LogLevel":"Information","Category":"ServiceC.Controllers.ThirdController","Message":"ThirdController.Get()","State":{"Message":"ThirdController.Get()","{OriginalFormat}":"ThirdController.Get()"},"Scopes":[{"Message":"SpanId:3e8fab9cf7709928, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7a0e874c8a81f865","SpanId":"3e8fab9cf7709928","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7a0e874c8a81f865"},{"Message":"ConnectionId:0HMROP3USVLAU","ConnectionId":"0HMROP3USVLAU"},{"Message":"RequestPath:/third RequestId:0HMROP3USVLAU:0000001D","RequestId":"0HMROP3USVLAU:0000001D","RequestPath":"/third"},{"Message":"ServiceC.Controllers.ThirdController.Get (ServiceC)","ActionId":"a02916d1-6256-4589-bd92-6c3342304ac7","ActionName":"ServiceC.Controllers.ThirdController.Get (ServiceC)"}]}
{"Timestamp":"2023-06-29T17:48:21.854Z","EventId":2,"LogLevel":"Information","Category":"Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware","Message":"Response:\r\nStatusCode: 200","State":{"Message":"Response:\r\nStatusCode: 200","StatusCode":200},"Scopes":[{"Message":"SpanId:3e8fab9cf7709928, TraceId:f1ee0bf7fbdd7ea5b423eddb86938bf9, ParentId:7a0e874c8a81f865","SpanId":"3e8fab9cf7709928","TraceId":"f1ee0bf7fbdd7ea5b423eddb86938bf9","ParentId":"7a0e874c8a81f865"},{"Message":"ConnectionId:0HMROP3USVLAU","ConnectionId":"0HMROP3USVLAU"},{"Message":"RequestPath:/third RequestId:0HMROP3USVLAU:0000001D","RequestId":"0HMROP3USVLAU:0000001D","RequestPath":"/third"}]}
```

## Visual Studio のおすすめ設定

![image](https://github.com/nenoNaninu/TraceContextExample/assets/27144255/fb2fa1db-e6ec-4a23-b052-3ac1ebb4adda)
