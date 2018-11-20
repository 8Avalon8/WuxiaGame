using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using System.Threading;


/* Based on the JSON parser from 
 * http://techblog.procurios.nl/k/618/news/view/14605/14863/How-do-I-write-my-own-parser-for-JSON.html
 * 
 * I simplified it so that it doesn't throw exceptions
 * and can be used in Unity iPhone with maximum code stripping.
 */
/// <summary>
/// This class encodes and decodes JSON strings.
/// Spec. details, see http://www.json.org/
/// 
/// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
/// All numbers are parsed to doubles.
/// </summary>


public class MiniJSON
{
	private const int TOKEN_NONE = 0;
	private const int TOKEN_CURLY_OPEN = 1;
	private const int TOKEN_CURLY_CLOSE = 2;
	private const int TOKEN_SQUARED_OPEN = 3;
	private const int TOKEN_SQUARED_CLOSE = 4;
	private const int TOKEN_COLON = 5;
	private const int TOKEN_COMMA = 6;
	private const int TOKEN_STRING = 7;
	private const int TOKEN_NUMBER = 8;
	private const int TOKEN_TRUE = 9;
	private const int TOKEN_FALSE = 10;
	private const int TOKEN_NULL = 11;
	private const int BUILDER_CAPACITY = 2000;

	/// <summary>
	/// On decoding, this value holds the position at which the parse failed (-1 = no error).
	/// </summary>
	protected static int lastErrorIndex = -1;
	protected static string lastDecode = "";

    public static bool encodeCharacteFlag = true;


	/// <summary>
	/// Parses the string json into a value
	/// </summary>
	/// <param name="json">A JSON string.</param>
	/// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
	public static object jsonDecode( string json )
	{
		// save the string for debug information
		MiniJSON.lastDecode = json;

		if( json != null )
		{
			char[] charArray = json.ToCharArray();
			int index = 0;
			bool success = true;
			object value = MiniJSON.parseValue( charArray, ref index, ref success );

			if( success )
				MiniJSON.lastErrorIndex = -1;
			else
				MiniJSON.lastErrorIndex = index;

			return value;
		}
		else
		{
			return null;
		}
	}


	/// <summary>
	/// Converts a Hashtable / ArrayList / Dictionary(string,string) object into a JSON string
	/// </summary>
	/// <param name="json">A Hashtable / ArrayList</param>
	/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
	public static string jsonEncode( object json , bool sort = false)
	{
		var builder = new StringBuilder( BUILDER_CAPACITY );
        var success = MiniJSON.serializeValue(json, builder, sort);
		
		return ( success ? builder.ToString() : null );
	}


	/// <summary>
	/// On decoding, this function returns the position at which the parse failed (-1 = no error).
	/// </summary>
	/// <returns></returns>
	public static bool lastDecodeSuccessful()
	{
		return ( MiniJSON.lastErrorIndex == -1 );
	}


	/// <summary>
	/// On decoding, this function returns the position at which the parse failed (-1 = no error).
	/// </summary>
	/// <returns></returns>
	public static int getLastErrorIndex()
	{
		return MiniJSON.lastErrorIndex;
	}


	/// <summary>
	/// If a decoding error occurred, this function returns a piece of the JSON string 
	/// at which the error took place. To ease debugging.
	/// </summary>
	/// <returns></returns>
	public static string getLastErrorSnippet()
	{
		if( MiniJSON.lastErrorIndex == -1 )
		{
			return "";
		}
		else
		{
			int startIndex = MiniJSON.lastErrorIndex - 5;
			int endIndex = MiniJSON.lastErrorIndex + 15;
			if( startIndex < 0 )
				startIndex = 0;

			if( endIndex >= MiniJSON.lastDecode.Length )
				endIndex = MiniJSON.lastDecode.Length - 1;

			return MiniJSON.lastDecode.Substring( startIndex, endIndex - startIndex + 1 );
		}
	}

	
	#region Parsing
	
	protected static Hashtable parseObject( char[] json, ref int index )
	{
		Hashtable table = new Hashtable();
		int token;

		// {
		nextToken( json, ref index );

		bool done = false;
		while( !done )
		{
			token = lookAhead( json, index );
			if( token == MiniJSON.TOKEN_NONE )
			{
				return null;
			}
			else if( token == MiniJSON.TOKEN_COMMA )
			{
				nextToken( json, ref index );
			}
			else if( token == MiniJSON.TOKEN_CURLY_CLOSE )
			{
				nextToken( json, ref index );
				return table;
			}
			else
			{
				// name
				string name = parseString( json, ref index );
				if( name == null )
				{
					return null;
				}

				// :
				token = nextToken( json, ref index );
				if( token != MiniJSON.TOKEN_COLON )
					return null;

				// value
				bool success = true;
				object value = parseValue( json, ref index, ref success );
				if( !success )
					return null;

				table[name] = value;
			}
		}

		return table;
	}

	
	protected static ArrayList parseArray( char[] json, ref int index )
	{
		ArrayList array = new ArrayList();

		// [
		nextToken( json, ref index );

		bool done = false;
		while( !done )
		{
			int token = lookAhead( json, index );
			if( token == MiniJSON.TOKEN_NONE )
			{
				return null;
			}
			else if( token == MiniJSON.TOKEN_COMMA )
			{
				nextToken( json, ref index );
			}
			else if( token == MiniJSON.TOKEN_SQUARED_CLOSE )
			{
				nextToken( json, ref index );
				break;
			}
			else
			{
				bool success = true;
				object value = parseValue( json, ref index, ref success );
				if( !success )
					return null;

				array.Add( value );
			}
		}

		return array;
	}

	
	protected static object parseValue( char[] json, ref int index, ref bool success )
	{
		switch( lookAhead( json, index ) )
		{
			case MiniJSON.TOKEN_STRING:
				return parseString( json, ref index );
			case MiniJSON.TOKEN_NUMBER:
				return parseNumber( json, ref index );
			case MiniJSON.TOKEN_CURLY_OPEN:
				return parseObject( json, ref index );
			case MiniJSON.TOKEN_SQUARED_OPEN:
				return parseArray( json, ref index );
			case MiniJSON.TOKEN_TRUE:
				nextToken( json, ref index );
				return Boolean.Parse( "TRUE" );
			case MiniJSON.TOKEN_FALSE:
				nextToken( json, ref index );
				return Boolean.Parse( "FALSE" );
			case MiniJSON.TOKEN_NULL:
				nextToken( json, ref index );
				return null;
			case MiniJSON.TOKEN_NONE:
				break;
		}

		success = false;
		return null;
	}


	protected static string parseString0( char[] json, ref int index )
	{
		string s = "";
		char c;

		eatWhitespace( json, ref index );
		
		// "
		c = json[index++];

		bool complete = false;
		while( !complete )
		{
			if( index == json.Length )
				break;

			c = json[index++];
			if( c == '"' )
			{
				complete = true;
				break;
			}
			else if( c == '\\' )
			{
				if( index == json.Length )
					break;

				c = json[index++];
				if( c == '"' )
				{
					s += '"';
				}
				else if( c == '\\' )
				{
					s += '\\';
				}
				else if( c == '/' )
				{
					s += '/';
				}
				else if( c == 'b' )
				{
					s += '\b';
				}
				else if( c == 'f' )
				{
					s += '\f';
				}
				else if( c == 'n' )
				{
					s += '\n';
				}
				else if( c == 'r' )
				{
					s += '\r';
				}
				else if( c == 't' )
				{
					s += '\t';
				}
				else if( c == 'u' )
				{
					int remainingLength = json.Length - index;
					if( remainingLength >= 4 )
					{
						char[] unicodeCharArray = new char[4];
						Array.Copy( json, index, unicodeCharArray, 0, 4 );

						uint codePoint = UInt32.Parse( new string( unicodeCharArray ), System.Globalization.NumberStyles.HexNumber );
						
						// convert the integer codepoint to a unicode char and add to string
						s += Char.ConvertFromUtf32( (int)codePoint );

						// skip 4 chars
						index += 4;
					}
					else
					{
						break;
					}
				}
			}
			else
			{
				s += c;
			}

		}

		if( !complete )
			return null;

		return s;
	}
    
    #region 新的两个版本的ParseString
    private delegate string ParseStringDelegate(char[] json, ref int index);
    private static ParseStringDelegate parseString = parseString2;

    public static void SetParseStringVersion(int i)
    {
        if (i == 0)
            parseString = parseString0;
        else if (i == 1)
            parseString = parseString1;
        else if (i == 2)
            parseString = parseString2;
    }

    private const int CHAR_BUFFER_SIZE = 64 * 1024;
    private static char[] _CharBuffer = new char[CHAR_BUFFER_SIZE];

    /// <summary> 这个版本最快 </summary>
    private static string parseString2(char[] json, ref int index)
    {
        int dstIndex = 0;
        char c;
        eatWhitespace(json, ref index);

        // "
        c = json[index++];

        while (true)
        {
            if (index == json.Length || dstIndex >= CHAR_BUFFER_SIZE)
                return null; //目前版本的MiniJson都是如此表示错误发生了。

            c = json[index++];
            if (c == '"')
            {
                break;
            }
            else if (c == '\\')
            {
                if (index == json.Length)
                    return null;

                switch (json[index++])
                {
                    case '"':
                        _CharBuffer[dstIndex++] = '"';
                        break;
                    case '\\':
                        _CharBuffer[dstIndex++] = '\\';
                        break;
                    case '/':
                        _CharBuffer[dstIndex++] = '/';
                        break;
                    case 'b':
                        _CharBuffer[dstIndex++] = '\b';
                        break;
                    case 'f':
                        _CharBuffer[dstIndex++] = '\f';
                        break;
                    case 'n':
                        _CharBuffer[dstIndex++] = '\n';
                        break;
                    case 'r':
                        _CharBuffer[dstIndex++] = '\r';
                        break;
                    case 't':
                        _CharBuffer[dstIndex++] = '\t';
                        break;
                    case 'u':
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            uint codePoint = UInt32.Parse(new string(json, index, 4), System.Globalization.NumberStyles.HexNumber);
                            char[] xx = Char.ConvertFromUtf32((int)codePoint).ToCharArray();
                            Array.Copy(xx, 0, _CharBuffer, dstIndex, xx.Length);

                            dstIndex += xx.Length;
                            index += 4;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    default:
                        break; //原始逻辑如此；如果 \后面的东西不是我们所想，则忽略这个\
                }
            }
            else
            {
                _CharBuffer[dstIndex++] = c;
            }
        }
        return new string(_CharBuffer, 0, dstIndex);
    }

    /// <summary>
    /// 这个版本比原始版本还慢
    /// </summary>
    private static string parseString1(char[] json, ref int index)
    {
        StringBuilder sb = new StringBuilder(8 * 1024);

        char c;
        eatWhitespace(json, ref index);

        // "
        c = json[index++];

        bool complete = false;
        while (!complete)
        {
            if (index == json.Length)
                break;

            c = json[index++];
            if (c == '"')
            {
                complete = true;
                break;
            }
            else if (c == '\\')
            {
                if (index == json.Length)
                    break;

                switch (json[index++])
                {
                    case '"':
                        sb.Append('"');
                        break;
                    case '\\':
                        sb.Append('\\');
                        break;
                    case '/':
                        sb.Append('/');
                        break;
                    case 'b':
                        sb.Append('\b');
                        break;
                    case 'f':
                        sb.Append('\f');
                        break;
                    case 'n':
                        sb.Append('\n');
                        break;
                    case 'r':
                        sb.Append('\r');
                        break;
                    case 't':
                        sb.Append('\t');
                        break;
                    case 'u':
                        int remainingLength = json.Length - index;
                        if (remainingLength >= 4)
                        {
                            sb.Append(DecodeUTF(json, index));

                            // skip 4 chars
                            index += 4;
                        }
                        else
                        {
                            break;
                        }
                        break;
                    default:
                        return null;
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        if (!complete)
            return null;

        return sb.ToString();
    }

    private static string DecodeUTF(char[] json, int index)
    {
        uint codePoint = UInt32.Parse(new string(json, index, 4), System.Globalization.NumberStyles.HexNumber);
        return Char.ConvertFromUtf32((int)codePoint);
    }
    #endregion


    protected static double parseNumber( char[] json, ref int index )
	{
		eatWhitespace( json, ref index );

		int lastIndex = getLastIndexOfNumber( json, index );
		int charLength = ( lastIndex - index ) + 1;
		char[] numberCharArray = new char[charLength];

		Array.Copy( json, index, numberCharArray, 0, charLength );
		index = lastIndex + 1;
		return Double.Parse( new string( numberCharArray ) ); // , CultureInfo.InvariantCulture);
	}
	
	
	protected static int getLastIndexOfNumber( char[] json, int index )
	{
		int lastIndex;
		for( lastIndex = index; lastIndex < json.Length; lastIndex++ )
			if( "0123456789+-.eE".IndexOf( json[lastIndex] ) == -1 )
			{
				break;
			}
		return lastIndex - 1;
	}
	
	
	protected static void eatWhitespace( char[] json, ref int index )
	{
		for( ; index < json.Length; index++ )
			if( " \t\n\r".IndexOf( json[index] ) == -1 )
			{
				break;
			}
	}
	
	
	protected static int lookAhead( char[] json, int index )
	{
		int saveIndex = index;
		return nextToken( json, ref saveIndex );
	}

	
	protected static int nextToken( char[] json, ref int index )
	{
		eatWhitespace( json, ref index );

		if( index == json.Length )
		{
			return MiniJSON.TOKEN_NONE;
		}
		
		char c = json[index];
		index++;
		switch( c )
		{
			case '{':
				return MiniJSON.TOKEN_CURLY_OPEN;
			case '}':
				return MiniJSON.TOKEN_CURLY_CLOSE;
			case '[':
				return MiniJSON.TOKEN_SQUARED_OPEN;
			case ']':
				return MiniJSON.TOKEN_SQUARED_CLOSE;
			case ',':
				return MiniJSON.TOKEN_COMMA;
			case '"':
				return MiniJSON.TOKEN_STRING;
			case '0':
			case '1':
			case '2':
			case '3':
			case '4': 
			case '5':
			case '6':
			case '7':
			case '8':
			case '9':
			case '-': 
				return MiniJSON.TOKEN_NUMBER;
			case ':':
				return MiniJSON.TOKEN_COLON;
		}
		index--;

		int remainingLength = json.Length - index;

		// false
		if( remainingLength >= 5 )
		{
			if( json[index] == 'f' &&
				json[index + 1] == 'a' &&
				json[index + 2] == 'l' &&
				json[index + 3] == 's' &&
				json[index + 4] == 'e' )
			{
				index += 5;
				return MiniJSON.TOKEN_FALSE;
			}
		}

		// true
		if( remainingLength >= 4 )
		{
			if( json[index] == 't' &&
				json[index + 1] == 'r' &&
				json[index + 2] == 'u' &&
				json[index + 3] == 'e' )
			{
				index += 4;
				return MiniJSON.TOKEN_TRUE;
			}
		}

		// null
		if( remainingLength >= 4 )
		{
			if( json[index] == 'n' &&
				json[index + 1] == 'u' &&
				json[index + 2] == 'l' &&
				json[index + 3] == 'l' )
			{
				index += 4;
				return MiniJSON.TOKEN_NULL;
			}
		}

		return MiniJSON.TOKEN_NONE;
	}

	#endregion
	
	
	#region Serialization
	
	protected static bool serializeObjectOrArray( object objectOrArray, StringBuilder builder )
	{
		if( objectOrArray is Hashtable )
		{
			return serializeObject( (Hashtable)objectOrArray, builder );
		}
		else if( objectOrArray is ArrayList )
			{
				return serializeArray( (ArrayList)objectOrArray, builder );
			}
			else
			{
				return false;
			}
	}

	
	protected static bool serializeObject( Hashtable anObject, StringBuilder builder, bool sort = false )
	{
        builder.Append( "{" );
        ICollection e = anObject.Keys;
        if (sort)
        {
            ArrayList a = new ArrayList(e);
            sortArrayList(a);
            e = a;
        }

		bool first = true;
		foreach( string key in e )
		{
			object value = anObject[key];
			
			if( !first )
			{
				builder.Append( ", " );
			}

			serializeString( key, builder );
			builder.Append( ":" );
			if( !serializeValue( value, builder ) )
			{
				return false;
			}

			first = false;
		}

		builder.Append( "}" );
		return true;
	}

    private static void sortArrayList(ArrayList list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            StringBuilder sb = new StringBuilder();
            serializeString(list[i] as string, sb);
            list[i] = sb.ToString();
        }

        list.Sort();

        for (int i = 0; i < list.Count; i++)
        {
            int index = 0;
            list[i] = parseString((list[i] as string).ToCharArray(), ref index);
        }
    }
	
	
	protected static bool serializeDictionary( Dictionary<string,string> dict, StringBuilder builder , bool sort = false )
	{
		builder.Append( "{" );
		
		bool first = true;

        ICollection e = dict.Keys;
        if (sort)
        {
            ArrayList a = new ArrayList(e);
            sortArrayList(a);
            e = a;
        }

        foreach (string key in e)
		{
            string value = dict[key];

			if( !first )
				builder.Append( ", " );

            serializeString(key, builder);
			builder.Append( ":" );
			serializeString( value, builder );

			first = false;
		}

		builder.Append( "}" );
		return true;
	}

    protected static bool serializeList(IList anArray, StringBuilder builder, bool sort = false)
    {
        builder.Append("[");

        bool first = true;

        if (sort)
        {
            if(anArray is List<int>)
            {
                (anArray as List<int>).Sort();
            }
            else if (anArray is List<float>)
            {
                (anArray as List<float>).Sort();
            }
            else if (anArray is List<long>)
            {
                (anArray as List<long>).Sort();
            }
            else if (anArray is List<string>)
            {
                (anArray as List<string>).Sort();
            }
        }

        for (int i = 0; i < anArray.Count; i++)
        {
            object value = anArray[i];

            if (!first)
            {
                builder.Append(", ");
            }

            if (!serializeValue(value, builder))
            {
                return false;
            }

            first = false;
        }

        builder.Append("]");
        return true;
    }

    protected static bool serializeArray(ArrayList anArray, StringBuilder builder, bool sort = false)
	{
		builder.Append( "[" );

		bool first = true;

        if (sort)
        {
            anArray.Sort();
        }

		for( int i = 0; i < anArray.Count; i++ )
		{
			object value = anArray[i];

			if( !first )
			{
				builder.Append( ", " );
			}

			if( !serializeValue( value, builder ) )
			{
				return false;
			}

			first = false;
		}

		builder.Append( "]" );
		return true;
	}

	
	protected static bool serializeValue( object value, StringBuilder builder, bool sort = false)
	{
		//Type t = value.GetType();
		//UnityEngine.Debug.Log("type: " + t.ToString() + " isArray: " + t.IsArray);

		if( value == null )
		{
			builder.Append( "null" );
		}
		else if( value.GetType().IsArray )
		{
			serializeArray( new ArrayList( (ICollection)value ), builder, sort);
		}
		else if( value is string )
		{
			serializeString( (string)value, builder );
		}
		else if( value is Char )
		{
			serializeString( Convert.ToString( (char)value ), builder );
		}
		else if( value is decimal )
		{
			serializeString( Convert.ToString( (decimal)value ), builder );
		}
		else if( value is Hashtable )
		{
			serializeObject( (Hashtable)value, builder, sort);
		}
		else if( value is Dictionary<string,string> )
		{
			serializeDictionary( (Dictionary<string,string>)value, builder , sort);
		}
		else if( value is ArrayList)
		{
			serializeArray( (ArrayList)value, builder, sort);
		}
        else if(value is IList && value.GetType().IsGenericType)
        {
            serializeList((IList)value, builder, sort);
        }
        else if( ( value is Boolean ) && ( (Boolean)value == true ) )
		{
			builder.Append( "true" );
		}
		else if( ( value is Boolean ) && ( (Boolean)value == false ) )
		{
			builder.Append( "false" );
		}
		else if( value.GetType().IsPrimitive )
		{
            serializeNumber( value , builder );
		}
		else
		{
			return false;
		}

		return true;
	}

	
	protected static void serializeString( string aString, StringBuilder builder )
	{
		builder.Append( "\"" );

		char[] charArray = aString.ToCharArray();
		for( int i = 0; i < charArray.Length; i++ )
		{
			char c = charArray[i];
			if( c == '"' )
			{
				builder.Append( "\\\"" );
			}
			else if( c == '\\' )
			{
				builder.Append( "\\\\" );
			}
			else if( c == '\b' )
			{
				builder.Append( "\\b" );
			}
			else if( c == '\f' )
			{
				builder.Append( "\\f" );
			}
			else if( c == '\n' )
			{
				builder.Append( "\\n" );
			}
			else if( c == '\r' )
			{
				builder.Append( "\\r" );
			}
			else if( c == '\t' )
			{
				builder.Append( "\\t" );
			}
			else
			{
				int codepoint = Convert.ToInt32( c );
				if( ( codepoint >= 32 ) && ( codepoint <= 126 ) )
				{
					builder.Append( c );
				}
				else
				{
                    if (!encodeCharacteFlag)
                    {
                        builder.Append(c);
                    }
                    else
                    {
                        builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                    }
                    
				}
			}
		}

		builder.Append( "\"" );
	}

	
	protected static void serializeNumber( object number, StringBuilder builder )
	{
		builder.Append( Convert.ToString( number ) ); // , CultureInfo.InvariantCulture));
	}
	
	#endregion
	
}



#region Extension methods

public static class MiniJsonExtensions
{
	public static string toJson( this Hashtable obj, bool sort = false)
	{
        return MiniJSON.jsonEncode( obj , sort);
	}
	
	
	public static string toJson( this Dictionary<string,string> obj, bool sort = false )
	{
		return MiniJSON.jsonEncode( obj, sort );
	}

    public static string toJson(this IList obj)
    {
        return MiniJSON.jsonEncode(obj);
    }


    public static ArrayList arrayListFromJson( this string json )
	{
		return MiniJSON.jsonDecode( json ) as ArrayList;
	}


	public static Hashtable hashtableFromJson( this string json )
	{
		return MiniJSON.jsonDecode( json ) as Hashtable;
	}

}

#endregion


