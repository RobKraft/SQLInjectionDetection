using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using SampleSQLInjectionDetectionApp;
#if NETCOREAPP
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
public static class SQLExtensions
{
	public static SqlDataReader ExecuteSafeReader(this SqlCommand sqlcommand)
	{
		if (!sqlcommand.CommandType.Equals(CommandType.StoredProcedure))
		{
			var sql = sqlcommand.CommandText;
			//Options: You could Add logging of the SQL here to track every query ran
			//Options: You could edit SQL - for example if you had renamed a table in the database
			if (!ValidateSQL(sql, SelectRegex))
				return null;
		}

		return sqlcommand.ExecuteReader();
	}
	public static int ExecuteSafeNonQuery(this SqlCommand sqlcommand)
	{
		if (!sqlcommand.CommandType.Equals(CommandType.StoredProcedure))
		{
			var sql = sqlcommand.CommandText;
			//Options: You could Add logging of the SQL here to track every query ran
			//Options: You could edit SQL - for example if you had renamed a table in the database
			if (!ValidateSQL(sql, ModifyRegex))
				return 0;
		}

		return sqlcommand.ExecuteNonQuery();
	}
	public static bool ValidateSQL(string sql, Regex keywordRegex)
	{
		bool valid;
		try
		{
			valid = ReallyValidateSQL(sql, keywordRegex);
		}
		//TODO - Do you want to catch the exception here or not?
		catch (SQLFormattingException ex)
		{
			valid = false;
			//TODO - Log the exception?
			var x = ex.Message;
		}
		return valid;
	}

	private static bool ReallyValidateSQL(string sql, Regex keywordRegex)
	{
		//Check the ascii regex first because the subsequent steps may modify the sql and remove ascii chracters
		CheckSQLForIllegalKeywords(sql, AsciiRegex);
		string newSQL = ExtractProperlyEncodedStringsAndComments(sql);
		//Above turns:	SELECT * FROM TABLE WHERE COMMENT LIKE '%delete%'
		//		to:		SELECT * FROM TABLE WHERE COMMENT LIKE  
		CheckSQLForIllegalKeywords(newSQL, keywordRegex);
		return true;
	}

	private static void CheckSQLForIllegalKeywords(string sql, Regex regex)
	{
		MatchCollection invalidKeywords = regex.Matches(sql.ToLower());
		if (invalidKeywords.Count > 0)
			throw new SQLFormattingException($"Illegal keywords {string.Join(",", invalidKeywords)} found in SQL");
	}
#region Check for MisMatched Delimiters (aka missing single quotes) and comments
	public static string ExtractProperlyEncodedStringsAndComments(string sql)
	{
		sql = ExtractStringsForThisDelimiter(sql, "'");
		sql = ExtractStringsForThisDelimiter(sql, "\"");
		return sql;
	}
	private static string ExtractStringsForThisDelimiter(string sql, string delimiter)
	{
		string output = ExtractStringsFromSQL(sql, delimiter);
		if (output.Contains(delimiter))
		{
			//We have an extra occurrence of it
			throw new SQLFormattingException($"Unmatched delimiter {delimiter} found in SQL");
		}
		return output;
	}
	private static string ExtractStringsFromSQL(string sql, string delimiter)
	{
		//This line basically replaces two single quotes ('') with nothing ()
		//because the two single quotes are used to escape a single quote so that it is part of the value in a parameter
		string sqlWithStringsRemoved = sql.Replace(delimiter + delimiter, "");

		int firstOccurrenceOfDelimiter = sqlWithStringsRemoved.IndexOf(delimiter);
		if (firstOccurrenceOfDelimiter < 0)
			return sqlWithStringsRemoved;

		//Extract comments from the SQL because they may contain delimiters that throw off the parsing
		for (int i = 0; i < CommentTagSets.GetLength(0); i++)
		{
			var result2 = RemoveCommentIfFound(sqlWithStringsRemoved, firstOccurrenceOfDelimiter, CommentTagSets[i, 0], CommentTagSets[i, 1]);
#region NEW SYNTAX OPTION BIT 3 of 3
			//if (result.stopParsing)
			//	return result.sql;
#endregion
			if (result2.Item1)
				return result2.Item2;
		}

		int secondOccurrenceOfDelimiter = sqlWithStringsRemoved.IndexOf(delimiter, firstOccurrenceOfDelimiter + 1);
		if (secondOccurrenceOfDelimiter < 0)
		{
			//the code that called this will throw an exception
			//We don't throw here because excluding a comment may also lead to a state where there is an unmatched delimiter
			return sqlWithStringsRemoved;
		}

		//Remove the values between the delimiters from the SQL so that subsequent steps can look for illegal keywords
		sqlWithStringsRemoved = sqlWithStringsRemoved.Substring(0, firstOccurrenceOfDelimiter) + sqlWithStringsRemoved.Substring(secondOccurrenceOfDelimiter + delimiter.Length);
		firstOccurrenceOfDelimiter = sqlWithStringsRemoved.IndexOf(delimiter);
		if (firstOccurrenceOfDelimiter >= 0) //If there is another string, we need to remove that one too
			return ExtractStringsFromSQL(sqlWithStringsRemoved, delimiter);
		return sqlWithStringsRemoved;
	}

#region NEW SYNTAX OPTION BIT 1 of 3
	//private static (bool stopParsing, string sql) RemoveCommentIfFound(string sql, int firstOccurrenceOfDelimiter)
#endregion
	private static Tuple<bool, string> RemoveCommentIfFound(string sql, int firstOccurrenceOfDelimiter, string commentBeginTag, string commentEndTag = "")
	{
		bool stopParsingSQL = false;
		int commentStart = sql.IndexOf(commentBeginTag);
		if (commentStart > -1 && commentStart < firstOccurrenceOfDelimiter)
		{
			if (RejectIfCommentFound)
				throw new SQLFormattingException($"Comment {commentBeginTag} found in SQL");
			if (!string.IsNullOrWhiteSpace(commentEndTag))
			{
				int commentEnd = sql.IndexOf(commentEndTag);
				if (commentEnd >= 0)
				{
					sql = sql.Substring(0, commentStart) + sql.Substring(commentEnd + commentEndTag.Length);
				}
				else
				{
					stopParsingSQL = true;  //Begin comment found but no end comment found
					sql = sql.Substring(0, commentStart);
				}
			}
			else
			{
				stopParsingSQL = true;
				sql = sql.Substring(0, commentStart);
			}
		}
#region NEW SYNTAX OPTION BIT 2 of 3
		//return (stopParsing, sql);
#endregion
		return new Tuple<bool, string>(stopParsingSQL, sql);
	}
#endregion
#region Customizable Validation Properties
	//I lazy load some of the properties as an example of how to easily support reading in the values from configurations stored elsewhere
	private static void LoadFromConfig()
	{
		_asciiPattern = "[^\u0000-\u007F]";
		_selectpattern = @"\b(union|information_schema|insert|update|delete|truncate|drop|reconfigure|sysobjects|waitfor|xp_cmdshell)\b|(;)";
		_modifypattern = @"\b(union|information_schema|truncate|drop|reconfigure|sysobjects|waitfor|xp_cmdshell)\b|(;)";
		_rejectIfCommentFound = true;
		_commentTagSets = new string[2, 2] { { "--", "" }, { "/*", "*/" } };
	}

	#region Comment Tag Sets
	//Microsoft SQL Server supports two comment syntaxes.  1) Anything following --,  2) Anything enclosed in /* */
	private static string[,] _commentTagSets = null;
	public static string[,] CommentTagSets
	{
		get
		{
			if (_commentTagSets == null)
			{
				LoadFromConfig();
			}
			return _commentTagSets;
		}
	}
	#endregion
	#region RejectComments Flag
	private static bool? _rejectIfCommentFound = null;
	public static bool RejectIfCommentFound
	{
		get
		{
			if (_rejectIfCommentFound == null)
			{
				LoadFromConfig();
			}
			return (bool)_rejectIfCommentFound;
		}
	}
	#endregion
	#region select pattern and regex
	private static string _selectpattern;
	public static string SelectPattern
	{
		get
		{
			if (_selectpattern == null)
			{
				LoadFromConfig();
			}
			return _selectpattern;
		}
	}
	private static Regex _selectRegex = null;
	public static Regex SelectRegex
	{
		get
		{
			if (_selectRegex == null)
			{
				_selectRegex = new Regex(SelectPattern);
			}
			return _selectRegex;
		}
	}
#endregion
#region Modify Pattern and regex
	static string _modifypattern;
	public static string ModifyPattern
	{
		get
		{
			if (_modifypattern == null)
			{
				LoadFromConfig();
			}
			return _modifypattern;
		}
	}
	private static Regex _modifyRegex = null;
	public static Regex ModifyRegex
	{
		get
		{
			if (_modifyRegex == null)
			{
				_modifyRegex = new Regex(ModifyPattern);
			}
			return _modifyRegex;
		}
	}
#endregion

#region ASCII pattern and regex
	private static string _asciiPattern;
	public static string AsciiPattern
	{
		get
		{
			if (_asciiPattern == null)
			{
				LoadFromConfig();
			}
			return _asciiPattern;
		}
	}

	private static Regex _asciiRegex = null;
	public static Regex AsciiRegex
	{
		get
		{
			if (_asciiRegex == null)
			{
				_asciiRegex = new Regex(AsciiPattern);
			}
			return _asciiRegex;
		}
	}
	#endregion
	public static void InvalidateCache()
	{
		_asciiRegex = null;
		_asciiPattern = null;
		_modifyRegex = null;
		_modifypattern = null;
		_selectRegex = null;
		_selectpattern = null;
	}
	#endregion

	#region SQL Formatters
	/// <summary>
	/// The method replaces each single quote with two single quotes and adds single quotes to the beginning and end of the value
	/// Example usage: string sql = "select * from customers where firstname like " + nameValue.FormatStringForSQL();
	/// Code Design Tip: Although the method is simple and you could simply add single quotes at each point in the code where needed instead
	/// of calling a method, this technique allows you to change it for ALL your SQL queries using it if you realize later a change is needed,
	/// Perhaps to handle double quotes instead of SQL quotes to port your code to a different DBMS.
	/// </summary>
	/// <param name="sql">The parameter value that needs to be enclosed in single quotes</param>
	/// <returns>The parameter value with proper encoding of strings and single quotes</returns>
	public static string FormatStringForSQL(this string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
			sql = "";
		sql = sql.Replace("'", "''");
		sql = "'" + sql + "'";
		return sql;
	}
	/// <summary>
	/// The method verifies the number is really a number while adding the value to the SQL statement
	/// Example usage: string sql = "select * from customers where price = " + nameValue.FormatNumberForSQL();
	/// </summary>
	/// <param name="sql"></param>
	/// <returns></returns>
	public static string FormatNumberForSQL(this string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
			sql = "";
		if (double.TryParse(sql, out double number))
			return number.ToString();
		throw new SQLFormattingException("Hey, someone is trying to sql inject over here!");
	}
	/// <summary>
	/// The method verifies the date is really a valid date while adding the value to the SQL statement
	/// Example usage: string sql = "select * from customers where StartDate = " + nameValue.FormatDateForSQL();
	/// WARNING: Do not use this as written without evaluating how you need to format Dates and Times and DateTimes for your application
	/// </summary>
	/// <param name="sql"></param>
	/// <returns></returns>
	public static string FormatDateForSQL(this string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
			sql = "";
		//bool b = DateTime.TryParseExact(sql, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out datetime);
		if (DateTime.TryParse(sql, out DateTime datetimeValue))
			return "'" + datetimeValue.ToString() + "'";
		throw new SQLFormattingException("Hey, someone is trying to sql inject over here!");
	}
	/// <summary>
	/// The method verifies the value is really a boolean while adding the value to the SQL statement
	/// Example usage: string sql = "select * from customers where StartDate = " + nameValue.FormatBooleanForSQL();
	/// Notice that you could add logic to handle custom values like Yes and No for true and false
	/// </summary>
	/// <param name="sql"></param>
	/// <returns></returns>
	public static string FormatBooleanForSQL(this string sql)
	{
		if (string.IsNullOrWhiteSpace(sql))
			sql = "";
		if (sql.Equals("yes", StringComparison.OrdinalIgnoreCase))
			return "1";
		if (sql.Equals("no", StringComparison.OrdinalIgnoreCase))
			return "0";
		if (bool.TryParse(sql, out bool boolValue))
		{
			if (boolValue == true)
				return "1";
			return "0";
		}
		throw new SQLFormattingException("Hey, someone is trying to sql inject over here!");
	}
#endregion
}