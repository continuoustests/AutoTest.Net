using System;
using NUnit.Framework;
using AutoTest.WinForms;

namespace AutoTest.WinForms.Test
{
	[TestFixture]
	public class ArgumentParserTests
	{
		[Test]
		[TestCase(new[] {"/watch/path"},
			"/watch/path", null)]
		[TestCase(new[] {"--local-config-location=/home/bleh"},
			null, "/home/bleh")]
		[TestCase(new[] {"/watch/path", "--local-config-location=/home/bleh"},
			"/watch/path", "/home/bleh")]
		[TestCase(new[] {"--local-config-location=/home/bleh", "/watch/path"},
			"/watch/path", "/home/bleh")]
		public void Can_read_custom_local_config_location(string[] args, string watchToken, string configLocation)
		{
			var arguments = ArgumentParser.Parse(args);
			Assert.That(arguments.WatchToken, Is.EqualTo(watchToken));
			Assert.That(arguments.ConfigurationLocation, Is.EqualTo(configLocation));
		}
	}
}