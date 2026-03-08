using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolution Strategies algorithm for TSP
/// </summary>
public class EvolutionStrategies( TspMap map ) : TspAlgorithmBase( map )
{
	/// <summary>
	/// Configures algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "es" ) ?? throw new ArgumentNullException( nameof( settings ) );		
	}
	

	/// <summary>
	/// Performs a single optimization epoch 
	/// </summary>
	/// <param name="best">The current best solution to the traveling salesman problem. Must not be null.</param>
	/// <returns>A new TspResult representing the best solution found during this epoch an improvement over the input solution</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var path = RandomSwap( best.Path );

		return ParallelLocalSearch( path );
	}
	
}
