using Microsoft.Extensions.Configuration;

namespace RMV.Optimization.TSP.Common;

/// <summary>
/// Custom JSON configuration section handler
/// </summary>
public static class ConfigManager
{

	#region Initialise ---------------------------------------------------------

	/// <summary>
	/// The root of the configuration (thread-safe after initialization)
	/// </summary>
	static readonly IConfigurationRoot root = GetConfigurationRoot( ResolveBasePath() );

	/// <summary>
	/// Resolves the base path for configuration files
	/// </summary>
	/// <returns>The base directory for configuration files</returns>
	static string ResolveBasePath()
	{
		var basePath = AppDomain.CurrentDomain.BaseDirectory;  // Try current domain base directory first

		if( !string.IsNullOrEmpty( basePath ) && Directory.Exists( basePath ) )
		{
			return basePath;
		}

		var entryAssembly = System.Reflection.Assembly.GetEntryAssembly(); // Fallback to entry assembly location

		if( entryAssembly != null )
		{
			basePath = Path.GetDirectoryName( entryAssembly.Location );
			if( !string.IsNullOrEmpty( basePath ) && Directory.Exists( basePath ) )
			{
				return basePath;
			}
		}

		return Directory.GetCurrentDirectory(); // Last resort: current working directory
	}

	/// <summary>
	/// Gets the current environment name
	/// </summary>
	/// <returns>Environment name (Development, Staging, Production, etc.)</returns>
	static string GetEnvironment()
	{
		// Check multiple environment variable names to support all app types
		return Environment.GetEnvironmentVariable( "ASPNETCORE_ENVIRONMENT" )
			?? Environment.GetEnvironmentVariable( "DOTNETCORE_ENVIRONMENT" )
			?? Environment.GetEnvironmentVariable( "APP_ENVIRONMENT" )
			?? "Production";
	}

	/// <summary>
	/// Gets the configuration root
	/// </summary>
	/// <param name="path">The base path for the configuration files</param>
	/// <returns>The configuration root</returns>
	static IConfigurationRoot GetConfigurationRoot( string path )
	{
		var environment = GetEnvironment();

		return new ConfigurationBuilder()
			.SetBasePath( path )
			.AddJsonFile( "appSettings.json", optional: true, reloadOnChange: false )
			// support environment-specific overrides (e.g., appSettings.Development.json)
			.AddJsonFile( $"appSettings.{environment}.json", optional: true, reloadOnChange: false )
			.Build();
	}

	#endregion


	/// <summary>
	/// Binds the root of the configuration directly to a type
	/// </summary>
	public static T? GetRoot<T>() => root.Get<T>();


	/// <summary>
	/// Retrieves custom configuration section by type or name 
	/// </summary>
	/// <typeparam name="T">section type</typeparam>
	/// <param name="name">section name (optional)</param>
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

