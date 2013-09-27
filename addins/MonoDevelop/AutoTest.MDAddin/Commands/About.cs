// Copyright (c) 2013, Eberhard Beilharz
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
using System;
using Gtk;
using MonoDevelop.Components.Commands;

namespace AutoTest.MDAddin.Commands
{
	public class About: CommandHandler
	{
		protected override void Run()
		{
			using (var dlg = new AboutDialog())
			{
				dlg.ProgramName = "AutoTest.NET MonoDevelop Addin";
				dlg.Comments = "v1.0";
				dlg.Copyright = "Copyright (c) 2010-2011\nGreg Young, Svein Arne Ackenhausen\n\n" +
					"MonoDevelop Addin:\nCopyright (c) 2013\nEberhard Beilharz";
				dlg.Website = "http://www.continuoustests.com";
				dlg.License = "The MIT License\n\nCopyright (c) 2010-2011 Greg Young, Svein Arne Ackenhausen\n" +
					"Copyright (c) 2013 Eberhard Beilharz\n\nPermission is hereby granted, " +
					"free of charge, to any person obtaining a copy of this software and associated documentation " +
					"files (the \"Software\"), to deal in the Software without restriction, including " +
					"without limitation the rights to use, copy, modify, merge, publish, distribute, " +
					"sublicense, and/or sell copies of the Software, and to permit persons to whom the " +
					"Software is furnished to do so, subject to the following conditions:" +
					"\n\nThe above copyright notice and this permission notice shall be included in " +
					"all copies or substantial portions of the Software." +
					"\n\nTHE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
					"IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS " +
					"FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR " +
					"COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER " +
					"IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION " +
					"WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.";
				dlg.WrapLicense = true;
				dlg.Run();
				dlg.Destroy();
			}
		}
	}
}

