using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Threading;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Nancy;
using Nancy.TinyIoc;
using Nancy.Conventions;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BellyRub
{
    public class Engine : IDisposable
    {
        public class Message
        {
            public string Subject { get; private set; }
            public dynamic Body { get; private set; }

            public Message(string subject, dynamic body) {
                Subject = subject;
                Body = body;
            }
        }

        private NancyHost _host;
        private WebSocketServer _server;
        private WebSocketServiceManager _serverServiceManager;
        private Chat _serverChatService;
        private Browser _browser = new Browser();

        private Action<Exception> _onSendException = (ex) => {};

        public bool HasConnectedClients { get { return _serverServiceManager.SessionCount > 0; } }

        public Engine() {
            _serverChatService = new Chat();
        }
        
        public Engine OnReceive(Action<Message> action) {
            _serverChatService.OnReceive(action);
            return this;
        }

        public Engine OnConnected(Action action) {
            _serverChatService.OnConnected(action);
            return this;
        }

        public Engine OnDisconnected(Action action) {
            _serverChatService.OnDisconnected(action);
            return this;
        }

        public Engine OnSendException(Action<Exception> action) {
            _onSendException = action;
            return this;
        }

        public void Start() {
            _host = new NancyHost(new Uri("http://localhost:1234"));
            _host.Start();
            _server = new WebSocketServer("ws://localhost:1235");
            _serverServiceManager = _server.WebSocketServices;
            _server.AddWebSocketService<Chat>("/chat", () => _serverChatService);
            _server.Start();

            _browser.Launch();
            waitForFirstClient();
        }

        public void Stop() {
            if (_host == null)
                return;
            _browser.Kill();
            try {
                _host.Stop();
            } catch {
            }
            _server.Stop();
        }

        public void Send(string subject, object body) {
            try {
                dynamic payload = new ExpandoObject();
                payload.subject = subject;
                payload.body = body;
                var json = LowercaseJsonSerializer.SerializeObject(payload);
                _serverChatService.SendMessage(json);
            } catch (Exception ex) {
                _onSendException(ex);
            }
        }

        public void Dispose() {
            Stop();
        }

        private void waitForFirstClient() {
            var timeout = DateTime.Now.AddSeconds(10);
            while (!HasConnectedClients && DateTime.Now < timeout) {
                Thread.Sleep(10);
            }
        }
    }

    public class Chat : WebSocketService
    {
        private Action<Engine.Message> _onReceive = (msg) => {};
        private Action _onConnected = () => {};
        private Action _onDisconnected = () => {};

        public Chat()
        {
        }

        public void OnReceive(Action<Engine.Message> action) {
            _onReceive = action;
        }

        public void OnConnected(Action action) {
            _onConnected = action;
        }

        public void OnDisconnected(Action action) {
            _onDisconnected = action;
        }

        public void SendMessage(string json) {
            Sessions.Broadcast(json);
        }

        protected override void OnOpen() {
            _onConnected();
            Console.WriteLine("Client connected");
        }

        protected override void OnClose(CloseEventArgs e) {
            _onDisconnected();
            Console.WriteLine("Client disconnected");
        }

        protected override void OnMessage (MessageEventArgs e)
        {
            dynamic msg = JObject.Parse(e.Data);
            _onReceive(new Engine.Message(msg.subject.ToString(), msg.body));
        }
    }

    public class RESTBootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines) {
        }
    
        protected override void ConfigureConventions(NancyConventions nancyConventions) {
            Conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("site", @"site")
            );
            base.ConfigureConventions(nancyConventions);
        }
    }

    class LowercaseJsonSerializer : DefaultContractResolver
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new SnakecaseContractResolver()
        };

        public static string SerializeObject(object o)
        {
            return JsonConvert.SerializeObject(o, Settings);
        }

        public class SnakecaseContractResolver : DefaultContractResolver
        {
            protected override string ResolvePropertyName(string propertyName)
            {
                return propertyName.ToDelimitedString('_');
            }
        }
    }
     
    public static class StringExtensions
    {
        public static string ToDelimitedString(this string @string, char delimiter)
        {
            var camelCaseString = @string.ToCamelCaseString();
            var sb = new StringBuilder();
            foreach (var chr in InsertDelimiterBeforeCaps(camelCaseString, delimiter)) {
                sb.Append(chr);
            }
            return sb.ToString();
        }
     
        public static string ToCamelCaseString(this string @string)
        {
            if (string.IsNullOrEmpty(@string) || !char.IsUpper(@string[0]))
            {
                return @string;
            }
            string lowerCasedFirstChar =
                char.ToLower(@string[0], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            if (@string.Length > 1)
            {
                lowerCasedFirstChar = lowerCasedFirstChar + @string.Substring(1);
            }
            return lowerCasedFirstChar;
        }
     
        private static IEnumerable InsertDelimiterBeforeCaps(IEnumerable input, char delimiter)
        {
            bool lastCharWasUppper = false;
            foreach (char c in input)
            {
                if (char.IsUpper(c))
                {
                    if (!lastCharWasUppper)
                    {
                        yield return delimiter;
                        lastCharWasUppper = true;
                    }
                    yield return char.ToLower(c);
                    continue;
                }
     
                yield return c;
                lastCharWasUppper = false;
            }
        }
    }

    class Browser
    {
        private Process _process;

        public void Launch() {
            var chrome = "/opt/google/chrome/chrome";
            if (File.Exists(chrome))
                _process = Process.Start(chrome, "--app=http://localhost:1234/site/index.html");
        }

        public void Kill() {
            if (_process == null)
                return;
            if (_process.HasExited)
                return;
            _process.Kill();
        }
    }
}