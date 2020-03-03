using System;

namespace ConsoleApp1
{
	[Serializable]
	public class SQLFormattingException : Exception
	{
		public SQLFormattingException()	{}

		public SQLFormattingException(string message): base(message) {}
	}
}