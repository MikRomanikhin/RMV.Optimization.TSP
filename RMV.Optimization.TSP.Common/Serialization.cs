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
	public static string? ToJson<T>( this T target, JsonSerializerOptions? options = null ) =>
		target == null ? null : JsonSerializer.Serialize( target, options ?? DefaultJsonSerializerOptions );


	/// <summary>
	/// Reconstruct object from Json string
	/// </summary>
	/// <typeparam name="T">target object type</typeparam>
	/// <param name="source">json string</param>
	/// <returns>deserialized object</returns>
	public static T? FromJson<T>( this string source, JsonSerializerOptions? options = null ) =>
		JsonSerializer.Deserialize<T>( source, options ?? DefaultJsonSerializerOptions );


	/// <summary>
	/// Creates a deep copy of the source object by serializing and deserializing
	/// </summary>
	/// <typeparam name="T">The type of the object to copy</typeparam>
	/// <param name="source">The source object to copy</param>
	/// <param name="options">Optional JSON serializer options</param>
	/// <returns>A deep copy of the source object</returns>
	public static T DeepCopyJson<T>( this T source, JsonSerializerOptions? options = null ) =>
		JsonSerializer.Deserialize<T>( JsonSerializer.Serialize( source, options ?? DefaultJsonSerializerOptions ), options ?? DefaultJsonSerializerOptions )!;


	/// <summary>
	/// Default JsonSerializerOptions for serialization and deserialization
	/// </summary>
	static readonly JsonSerializerOptions DefaultJsonSerializerOptions = InitializeDefaultJsonSerializerOptions();

	static JsonSerializerOptions InitializeDefaultJsonSerializerOptions()
	{
		var options = new JsonSerializerOptions {
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false,
			IgnoreReadOnlyProperties = true,
			ReferenceHandler = ReferenceHandler.IgnoreCycles,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		options.Converters.Add( new JsonStringEnumConverter( JsonNamingPolicy.CamelCase ) );

		return options;
	}

	#endregion

	/// <summary>
	/// Converts a collection to a string with the specified separator.
	/// Returns null if the collection is null or empty.
	/// </summary>
	public static string? Join<T>( this IEnumerable<T>? target, string separator = ", " ) =>
		target is not null && target.Any() ? string.Join( separator, target ) : null;
}

