using System;

namespace SampleSQLInjectionDetectionApp
{
	[Serializable]
	public class SQLFormattingException : Exception
	{
		public SQLFormattingException()	{}

		public SQLFormattingException(string message): base(message) {}
	}
}