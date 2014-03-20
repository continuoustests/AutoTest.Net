using System;
using AutoTest.Messages;
using BellyRub.UI;

namespace AutoTest.Server.Handlers
{
	class FocusHandler : IHandler
	{
        private Action<string, object> _dispatch;
        private Browser _browser;

        public FocusHandler(Browser browser) {
            _browser = browser;
        }

        public void DispatchThrough(Action<string, object> dispatcher) {
            _dispatch = dispatcher;
        }

        public void OnInternalMessage(object message) {
            if (message.Is<ExternalCommandMessage>()) {
                var commandMessage = (ExternalCommandMessage)message;
                if (commandMessage.Sender == "EditorEngine")
                {
                    var msg = EditorEngineMessage.New(commandMessage.Sender + " " + commandMessage.Command);
                    if (msg.Arguments.Count == 2 &&
                        msg.Arguments[0].ToLower() == "autotest.net" &&
                        msg.Arguments[1].ToLower() == "setfocus")
                    {
                        _browser.BringToFront();
                    }
                }
            }
        }
	}
}