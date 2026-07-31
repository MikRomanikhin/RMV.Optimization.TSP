using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Nearest Neighbour TSP algorithm
/// </summary>
public class NearestNeighbour( TspMap map ) : TspAlgorithmBase( map )
{

	/// <summary>
	/// Configures the algorithm
	/// </summary>	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<GlsSettings>( "nns" ) ?? throw new ArgumentNullException( nameof( settings ) );			
	}

	/// <summary>
	/// Performs a single optimization epoch
	/// </summary>
	protected override TspResult RunEpoch( TspResult best ) => base.BuildNearestTour();
	
}
