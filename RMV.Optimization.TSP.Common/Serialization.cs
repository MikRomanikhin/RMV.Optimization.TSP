using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace RMV.Optimization.TSP.Common;

/// <summary>
/// XML and JSON Serialization extenders
/// </summary>
public static class Serialization
{
   #region Xml -------------------------------------------------------------   

   /// <summary>
   /// Serialize to XML with namespace
   /// </summary>
   /// <typeparam name="T">target object type</typeparam>
   /// <param name="target">target object</param>
   /// <param name="XmlSerializerNamespaces">namespace</param>		
   /// <returns>XML string</returns>
   public static string? ToXml<T>( this T target, XmlSerializerNamespaces? namespaces = null )
   {
      if( target == null ) return null;

      var stringWriter = new StringWriter();

      using var xmlWriter = XmlWriter.Create( stringWriter, new XmlWriterSettings { OmitXmlDeclaration = true } );

      var serializer = new XmlSerializer( typeof( T ) );

      var blank = new XmlSerializerNamespaces();
      blank.Add( "", "" );

      serializer.Serialize( xmlWriter, target, namespaces ?? blank );

      return stringWriter.ToString();
   }

   /// <summary>
   /// Reconstruct an object from an XML string
   /// </summary>
   /// <typeparam name="T">object type</typeparam>
   /// <param name="source">source string</param>		
   /// <returns>deserialized object</returns>
   public static T? FromXml<T>( this string source ) where T : class
   {
      if( string.IsNullOrEmpty( source ) ) return null;

      var serializer = new XmlSerializer( typeof( T ) );

      using var stream = new MemoryStream( new UTF8Encoding().GetBytes( source ) );

      return serializer.Deserialize( stream ) as T;
   }

   #endregion

    
   #region Json ------------------------------------------------------------

   /// <summary>
   /// Json serializing
   /// </summary>
   /// <typeparam name="T">target object type</typeparam>
   /// <param name="target">target object</param>
   /// <returns>Json string</returns>
   public static string? ToJson<T>( this T target, JsonSerializerOptions? options = null )
   {
      return target == null ? null : JsonSerializer.Serialize( target, options ?? DefaultJsonSerializerOptions );
   }

   /// <summary>
   /// Json serializing async
   /// </summary>
   /// <typeparam name="T">target object type</typeparam>
   /// <param name="target">target object</param>
   /// <returns>Json string</returns>
   public static async Task<string?> ToJsonAsync<T>( this T target, JsonSerializerOptions? options = null )
   {
      if( target == null ) return null;

      using var stream = new MemoryStream();
      await JsonSerializer.SerializeAsync<T>( stream, target, options );

      stream.Position = 0;
      using var reader = new StreamReader( stream );
      return await reader.ReadToEndAsync();
   }

   /// <summary>
   /// Reconstruct object from Json string
   /// </summary>
   /// <typeparam name="T">target object type</typeparam>
   /// <param name="source">json string</param>
   /// <returns>deserialized object</returns>
   public static T? FromJson<T>( this string source, JsonSerializerOptions? options = null )
   {
      return JsonSerializer.Deserialize<T>( source, options );
   }

   /// <summary>
   /// Reconstruct object from Json string async
   /// </summary>
   /// <typeparam name="T">target object type</typeparam>
   /// <param name="source">json string</param>
   /// <returns>deserialized object</returns>
   public static async Task<T?> FromJsonAsync<T>( this string source, JsonSerializerOptions options = null )
   {
      Stream stream = new MemoryStream( Encoding.UTF8.GetBytes( source ) );

      return await JsonSerializer.DeserializeAsync<T>( stream, options );
   }
  
  
   static JsonSerializerOptions DefaultJsonSerializerOptions
   {
      get
      {
         var options = new JsonSerializerOptions
         {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,//IgnoreNullValues = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
         };

         options.Converters.Add( new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) );

         return options;
      }
   }

	#endregion

	/// <summary>
	/// Coverts string collection to string with separators
	/// </summary> 
	/// <summary>
	/// Converts a collection to a string with the specified separator.
	/// Returns null if the collection is null or empty.
	/// </summary>      
	public static string? Join<T>( this IEnumerable<T>? target, string separator = ", " ) =>
		target is not null && target.Any() ? string.Join( separator, target ) : null;
}

