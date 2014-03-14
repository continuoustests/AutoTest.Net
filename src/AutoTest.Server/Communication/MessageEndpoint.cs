using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BellyRub;
using AutoTest.UI;
using AutoTest.Messages;
using AutoTest.Core.DebugLog;
using AutoTest.Core.Launchers;

namespace AutoTest.Server.Communication
{
    class EmptyBehavior : IListItemBehaviour
    {
        public int Left { get; set; }
        public int Width { get; set; }
        public string Name { get { return "Name"; } }
        public bool Visible { get; set; }
    }

    class MessageEndpoint : IMessageForwarder, IDisposable
    {
        private Engine _engine;
        private FeedbackProvider _feedbackProvider;
        private IApplicatonLauncher _launcher;

        private long _itemCounter = 0;
        private List<KeyValuePair<long, CacheBuildMessage>> _buildMessages =
            new List<KeyValuePair<long, CacheBuildMessage>>();
        private List<KeyValuePair<long, CacheTestMessage>> _testMessages =
            new List<KeyValuePair<long, CacheTestMessage>>();

        public bool IsAlive { get { return _engine.HasConnectedClients; } }

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            if (_engine != null) {
                _engine.Stop();
                _engine = null;
            }
        }

        public MessageEndpoint(IApplicatonLauncher launcher)
        {
            var messageHandlers = new Dictionary<string, Action<dynamic>>();
            messageHandlers.Add("build-test-all", (msg) => {
                var message = new ProjectChangeMessage();
                var cache = AutoTest.Core.Configuration.BootStrapper.Services.Locate<AutoTest.Core.Caching.ICache>();
                var configuration = AutoTest.Core.Configuration.BootStrapper.Services.Locate<AutoTest.Core.Configuration.IConfiguration>();
                var bus = AutoTest.Core.Configuration.BootStrapper.Services.Locate<AutoTest.Core.Messaging.IMessageBus>();
                var projects = cache.GetAll<AutoTest.Core.Caching.Projects.Project>();
                foreach (var project in projects)
                {
                    if (project.Value == null)
                        continue;
                    project.Value.RebuildOnNextRun();
                    message.AddFile(new ChangedFile(project.Key));
                }
                bus.Publish(message);
            });
            messageHandlers.Add("goto", (msg) => {
                Console.WriteLine("Link clicked: " + msg.file.ToString());
                _launcher.LaunchEditor(msg.file.ToString(), (int)msg.line, (int)msg.column);
            });

            _launcher = launcher;
            _engine = new Engine()
                .OnConnected(() => {
                    Console.WriteLine("Client connected");
                })
                .OnDisconnected(() => {
                    Console.WriteLine("Client disconnected");
                })
                .OnReceive((msg) => {
                    if (!messageHandlers.ContainsKey(msg.Subject))
                        return;
                    messageHandlers[msg.Subject](msg.Body);
                })
                .OnSendException((ex) => {
                    Console.WriteLine(ex.ToString());
                });
            _engine.Start();

            _feedbackProvider = new FeedbackProvider(
                new EmptyBehavior(),
                new EmptyBehavior(),
                new EmptyBehavior(),
                new EmptyBehavior()
            ).OnGoToReference(
                (file,line,column) => {
                    _launcher.LaunchEditor(file, line, column);
                })
            .OnShutdown(() => {
                    dynamic o = new ExpandoObject();
                    send("shutdown", o);
                })
            .OnGoToType(
                (assembly,typename) => {
                    return false;
                })
            .OnDebugTest(
                (test) => {
                })
            .OnCancelRun(
                () => { 
                })
            .OnPrepareForFocus(
                () => {
                    System.Diagnostics.Process.Start("oi", "process set-to-foreground window \"AutoTest.Server\" \"AutoTest.Net - Connected\"");
                })
            .OnClearList(
                () => {
                    _buildMessages.Clear();
                    _testMessages.Clear();
                    dynamic o = new ExpandoObject();
                    send("remove-all", o);
                })
            .OnClearBuilds(
                (project) => {
                    lock (_buildMessages) {
                        var toRemove = new List<KeyValuePair<long, CacheBuildMessage>>();
                        foreach (var item in _buildMessages) {
                            if (project == null || item.Value.Project.Equals(project))
                                toRemove.Add(item);
                        }
                        var ids = new List<long>();
                        foreach (var item in toRemove) {
                            ids.Add(item.Key);
                            _buildMessages.Remove(item);
                        }
                        dynamic o = new ExpandoObject();
                        o.ids = ids.ToArray();
                        send("remove-builditems", o);
                    }
                })
            .OnIsInFocus(
                () => {
                    return false;
                })
            .OnImageStateChange(
                (state, information) => {
                    dynamic o = new ExpandoObject();
                    o.state = state.ToString().ToLower();
                    o.information = information;
                    send("picture-update", o);
                })
            .OnPrintMessage(
                (msg, color, normal) => {
                    dynamic o = new ExpandoObject();
                    o.message = msg;
                    o.color = color;
                    o.normal = normal;
                    send("status-information", o);
                })
            .OnStoreSelected(
                () => {
                    send("selected-store", new ExpandoObject());
                })
            .OnRestoreSelected(
                (check) => {
                    send("selected-restore", new ExpandoObject());
                })
            .OnAddItem(
                (type, message, color, tag) => {
                    var id = getNextId();
                    dynamic o = new ExpandoObject();
                    o.id = id;
                    o.type = type;
                    o.message = message;
                    o.color = color;
                    if (tag.GetType() == typeof(CacheBuildMessage)) {
                        var msg = (CacheBuildMessage)tag;
                        lock (_buildMessages) {
                            _buildMessages.Add(new KeyValuePair<long, CacheBuildMessage>(id, msg));
                        }
                        if (color == "Red")
                            o.error = msg;
                        else
                            o.warning = msg;
                    } else {
                        var msg = (CacheTestMessage)tag;
                        lock (_testMessages) {
                            _testMessages.Add(new KeyValuePair<long, CacheTestMessage>(id, msg));
                        }
                        if (color == "Red")
                            o.failed = msg;
                        else
                            o.ignored = msg;
                    }
                    send("add-item", o);
                })
            .OnRemoveBuildItem(
                (check) => {
                    lock (_buildMessages) {
                        var items = _buildMessages.Where(x => check(x.Value)).ToArray();
                        if (items.Length == 1) {
                            dynamic o = new ExpandoObject();
                            o.id = items[0].Key;
                            send("remove-builditem", o);
                            _buildMessages.Remove(items[0]);
                        }
                    }
                })
            .OnRemoveTest(
                (check) => {
                    lock (_testMessages) {
                        var items = _testMessages.Where(x => check(x.Value)).ToArray();
                        if (items.Length == 1) {
                            dynamic o = new ExpandoObject();
                            o.id = items[0].Key;
                            send("remove-testitem", o);
                            _testMessages.Remove(items[0]);
                        }
                    }
                })
            .OnSetSummary(
                (m) => {
                    dynamic o = new ExpandoObject();
                    o.message = m;
                    send("run-completed", o);
                })
            .OnExists(
                (check) => {
                    foreach (var item in _buildMessages) {
                        if (check(item))
                            return true;
                    }
                    foreach (var item in _testMessages) {
                        if (check(item))
                            return true;
                    }
                    return false;
                })
            .OnGetSelectedItem(
                () => {
                    return null;
                })
            .OnGetWidth(() => 200);

            _feedbackProvider.CanGoToTypes = false;
            _feedbackProvider.ShowRunInformation = true;
            _feedbackProvider.CanDebug = false;
            _feedbackProvider.Initialize();
        }

        public void Forward(object message)
        {
            _feedbackProvider.ConsumeMessage(message);
        }

        private void send(string type, object msg)
        {
            _engine.Send(type, msg);
        }

        private long getNextId()
        {
            var id = _itemCounter;
            _itemCounter++;
            return id;
        }
    }
}