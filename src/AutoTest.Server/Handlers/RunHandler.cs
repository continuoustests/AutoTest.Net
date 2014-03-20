using System;
using System.Dynamic;
using System.Collections.Generic;
using AutoTest.Messages;
using AutoTest.Core.Messaging;

namespace AutoTest.Server.Handlers
{
	class RunHandler : IHandler, IClientHandler, IInternalMessageHandler
	{
        private Action<string, object> _dispatch;
        private IMessageBus _bus;
        private bool _detectRecursiveRun = false;

        public RunHandler(IMessageBus bus) {
            _bus = bus;
        }

        public void DispatchThrough(Action<string, object> dispatcher) {
            _dispatch = dispatcher;
        }

        public Dictionary<string, Action<dynamic>> GetClientHandlers() {
            var handlers = new Dictionary<string, Action<dynamic>>();
            handlers.Add("abort-run", (msg) => {
                _bus.Publish(new AbortMessage(""));    
            });

            return handlers;
        }

        public void OnInternalMessage(object message) {
            if (message.Is<RunStartedMessage>()) {
                _dispatch("run-started", new {
                    files = ((RunStartedMessage)message).Files
                });
            }
            if (message.Is<RunFinishedMessage>()) {
                _dispatch("run-finished", null);
            }
        }
	}
}