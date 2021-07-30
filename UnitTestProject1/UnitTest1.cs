using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void AllTheTests()
		{
			string n;
			n = "1/1/2018".FormatDateForSQL();
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "2/31/2018".FormatDateForSQL());
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "1".FormatDateForSQL());
			n = "1".FormatNumberForSQL();
			n = "1.1".FormatNumberForSQL();
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = " ".FormatNumberForSQL());
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "or 1=1".FormatNumberForSQL());
			n = "true".FormatBooleanForSQL();
			n = "True".FormatBooleanForSQL();
			n = "TRuE".FormatBooleanForSQL();
			n = "Yes".FormatBooleanForSQL();
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "1".FormatBooleanForSQL());
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "3".FormatBooleanForSQL());
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = "roger".FormatBooleanForSQL());
			Assert.ThrowsException<SampleSQLInjectionDetectionApp.SQLFormattingException>(() => n = " ".FormatBooleanForSQL());

			string all = "";
			for (int i = 0; i < 128; i++)
			{
				if (i != 34 && i != 39 && i != 59)
					all = all + (char)i;
				Assert.IsTrue(SQLExtensions.ValidateSQL(all, SQLExtensions.SelectRegex));
			}

			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = ''", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table where x = '''", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = ''''", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' order by 5", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table where x = 'ss order by 5", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' order by 5", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' and r = 'asdf' and d = 'ss' order by 5", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' and r = 'asdf' -- test ' order by 5", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' and r = 'asdf' /* test */ ' order by 5", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL(all, SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = \"\"", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table where x = \"\"\"", SQLExtensions.SelectRegex));

			//will error in the db
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table where x = 'ss''d' and r = 'asdf' '-- test ' order by 5", SQLExtensions.SelectRegex));
			Assert.IsTrue(SQLExtensions.ValidateSQL("select * from table order by 5", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table order by 5 union select delete insert", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table order by 5;", SQLExtensions.SelectRegex));
			Assert.IsFalse(SQLExtensions.ValidateSQL("select * from table order by 5" + (char)129, SQLExtensions.SelectRegex));

		}

	}
}