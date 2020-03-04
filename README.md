# SQLInjectionDetection
A Set of SQLExtensions you can easily add to any .Net App to Detect most SQL Injection

Warning!  This code will not detect all SQL Injection Risks!
The purpose of this code is to provide you an example, with real code you can download and use, to harden your legacy, as well as future .Net applications against the risk of SQL Injection.

When you have an existing .Net code base full of SQL statements, and you want to reduce the chance that there are SQL injection risks in the code, you may decide to perform a review of every SQL statement in order to confirm that they are all coded correctly; or you may hire another company to do this for you.  But one problem with this approach is that the code is only "SQL Injection Free" from the moment the review is completed until people start modifying and adding to the code again.

What you should strive for is a way to make sure every past and future SQL statement gets tested for SQL Injection risk before it runs, and that is what this class provides you.  If you follow the patterns described here, I believe you can significantly reduce the risk that your code has bugs leading to SQL Injection and it will stay that way going forward.

The primary tool to add SQL Injection detection into your application is to stop using the .ExecuteReader and .ExecuteNonQuery methods.  Instead, you will use the Decorator pattern to create your own method to be called in place of those two, and that method will include code to do some SQL Injection detection.  That code also behaves like the Proxy pattern in that it will make the actual call to the database after finding no SQL Injection risk.  The benefit of this approach is that you can then regularly scan your entire code base for the use of .ExecuteReader and .ExecuteNonQuery knowing that there should be no cases of those methods, other than the exception cases you expect.

Another benefit of using the Decorator pattern to implement SQL Injection Detection is that you can also easily add other features such as:
* Logging every SQL that is executed
* Logging and blocking every SQL that is a SQL Injection risk
* Altering every SQL on the fly.  One scenario where this could be helpful is that if you renamed a table in the database but had SQL all over that needed to change, you could possibly add a find/replace to every SQL on the fly to change the table name, allowing you more time to find and correct all stored SQL fragments with the old table name.

I will repeat my first warning.  This does not detect all forms of SQL Injection, but it will detect most of them.  Here is what causes the class to throw an exception:
* Finding  single apostrophes (single quotes) that do not have a matching single apostrophe (single quote)
* Finding double quotes that do not have a matching double quote.  This is only needed if the SQL Server has SET QUOTED_IDENTIFIER OFF.  However, you may also want to use this if you database is MySQL or some other DBMS.
* Finding a comment within the SQL
* Finding an ASCII value great than 127
* Finding a semicolon
* After extracting the strings and comments, finding any of a specific configurable list of keywords in a SELECT statement such as DELETE, SYSOBJECTS, TRUNCATE, DROP, XP_CMDSHELL
	
The code is written to be easy to change if you don't want to enforce any of the rules above, or if you need to add similar rules because you have a special scenario or a DBMS besides SQL Server.

The SQLExtensions class provides additional methods to help your coders reduce the risk of SQL Injection and those are some methods to help coders format variables in SQL when doing so with a parameter is not an option.  The most useful of these methods is FormatStringForSQL and it could be used as shown here to enclose a string in SQL quotes as well as replace any single quotes contained within the value with two single quotes.
	string sql = "select * from customers where firstname like " + nameValue.FormatStringForSQL();
Another advantage of using a method like this is that it makes it easy for you to change how you handle the formatting of strings everywhere within your code at once if you discover that you need to make a change.  For example, perhaps you decide to move your application from SQL Server to MySQL and therefore that you also need to replace double quotes.  You could make the change within this method instead of reviewing your entire code base to make the change one by one for each SQL.

I also provided a custom Exception primarily to show how easy it is to implement custom exceptions and because I think it is useful for this extension class.  This also provides you more flexibility for handling exceptions.

I also made enabling/disabling and configuration of the SQL Injection detections easy to change so that you could import those rules at runtime if desired so that different applications could have different rules.  Perhaps one of your applications needs to allow semicolons in SQL but the others don't.  I think that should be easy to implement because I want you to implement the most stringent rules everywhere you can but support weaker rules when it is necessary to do so but only where it is necessary to do so, not globally.  The rules are "Lazy Loaded" when needed, then cached, to support the ability to change them while an application is running by calling the InvalidateCache method.


I suggest you take the following steps to implement this class:
1. Get the SQLExtensions.cs class file into a project in your code base. You will also need the CustomExceptions.cs class file.  The program.cs just contains a sample usage and there is also a UnitTest1.cs class.
2. Comment out all the lines in ReallyValidateSQL except for the "return true"
3. Do a find an replace across your entire code base to replace ExecuteReader with ExecuteSafeReader
4. Compile and test.  Your app should still work exactly the same at this point.
5. Review the Customizable Validation Properties and decided which ones you want to implement, then uncomment the lines you commented out in ReallyValidateSQL 
6. Decide if you need to and want to replace dynamically constructed SQL in your application with any of the four FormatSQL… extension methods provided.
7. Provide me feedback
