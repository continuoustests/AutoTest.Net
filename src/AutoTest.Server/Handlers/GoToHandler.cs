using System;
using System.Dynamic;
using System.Collections.Generic;
using AutoTest.Core.Launchers;

namespace AutoTest.Server.Handlers
{
	class GoToHandler : IHandler, IClientHandler
	{
        private Action<string, object> _dispatcher;
        private IApplicatonLauncher _launcher;

        public GoToHandler(IApplicatonLauncher launcher) {
            _launcher = launcher;
        }

        public void DispatchThrough(Action<string, object> dispatcher) {
            _dispatcher = dispatcher;
        }

        public Dictionary<string, Action<dynamic>> GetClientHandlers() {
            var handlers = new Dictionary<string, Action<dynamic>>();
            handlers.Add("goto", (msg) => {
                _launcher.LaunchEditor(msg.file.ToString(), (int)msg.line, (int)msg.column);
            });

            return handlers;
        }
	}
}