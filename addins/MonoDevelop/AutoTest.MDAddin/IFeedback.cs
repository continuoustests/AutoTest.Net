// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using Gtk;
using AutoTest.UI;
using AutoTest.Messages;

namespace AutoTest.MDAddin
{
	public interface IFeedback
	{
		void SetText(string status);
		void SetMarkupText(string status);
		void SetIcon(bool paused);

		void SetProgress(ImageStates state, string information);
		ListStore Store { get; }
		void GenerateSummary(RunReport report);
	}
}

