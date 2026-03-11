
using Microsoft.Extensions.Configuration;

namespace RMV.Optimization.TSP.Common;

/// <summary>
/// Custom configuration section handler
/// </summary>
public static class ConfigManager
{
	static ConfigManager()
	{
		root = GetConfigurationRoot( AppDomain.CurrentDomain.BaseDirectory );
	}

	static readonly IConfigurationRoot root;

	static IConfigurationRoot GetConfigurationRoot( string path ) =>
		new ConfigurationBuilder().SetBasePath( path ).AddJsonFile( "appSettings.json" ).Build();
	

	/// <summary>
	/// Retrieves custom configuration section by type or name 
	/// </summary>
	/// <typeparam name="T">section type</typeparam>
	/// <param name="name">section name</param>
	/// <returns>configuration section</returns>
	public static T? GetSection<T>( string? name = null ) => root.GetSection( name ?? typeof( T ).Name ).Get<T>();

	/// <summary>
	/// Retrieves single value
	/// </summary>
	/// <typeparam name="T">value type</typeparam>
	/// <param name="name">value name</param>
	/// <returns>value</returns>
	public static T? GetValue<T>( string name ) => root.GetValue<T>( name );
}

