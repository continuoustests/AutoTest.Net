using System;

namespace AutoTest.WinForms
{
	public class Arguments
    {
        public string WatchToken { get; set; }
        public string ConfigurationLocation { get; set; }
    }

	public class ArgumentParser
	{
		public static Arguments Parse(string[] arguments)
		{
			var parsed = new Arguments();
			foreach (var argument in arguments) {
				if (!argument.StartsWith("--")) {
					parsed.WatchToken = argument;
				} else {
					if (argument.StartsWith("--local-config-location="))
						parsed.ConfigurationLocation = argument.Replace("--local-config-location=", "");
				}
			}
			return parsed;
		}
	}
}