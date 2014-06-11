using System;
using System.Linq;
using System.Dynamic;
using System.Collections;
using System.Collections.Generic;
using AutoTest.Messages;
using AutoTest.Core.Caching;
using AutoTest.Core.Messaging;
using AutoTest.Core.Caching.Projects;

namespace AutoTest.Server.Handlers
{
	class TriggerRunHandler : IHandler, IClientHandler
	{
        private ICache _cache;
        private IMessageBus _bus;

        public TriggerRunHandler(ICache cache, IMessageBus bus) {
            _cache = cache;
            _bus = bus;
        }

        public void DispatchThrough(Action<string, object> dispatcher) {
        }

        public Dictionary<string, Action<dynamic>> GetClientHandlers() {
            var handlers = new Dictionary<string, Action<dynamic>>();
            handlers.Add("build-test-all", (msg) => {
                var message = new ProjectChangeMessage();
                var projects = _cache.GetAll<Project>();
                foreach (var project in projects) {
                    if (project.Value == null)
                        continue;
                    project.Value.RebuildOnNextRun();
                    message.AddFile(new ChangedFile(project.Key));
                }
                _bus.Publish(message);
            });
            handlers.Add("build-test-projects", (msg) => {
                var message = new ProjectChangeMessage();
                var projects = ((IEnumerable<object>)msg.projects).Select(x => x.ToString());
                projects.ToList().ForEach(x => message.AddFile(new ChangedFile(x)));
                _bus.Publish(message);
            });

            return handlers;
        }
	}
}