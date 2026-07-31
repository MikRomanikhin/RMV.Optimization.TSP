using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Random Search for TSP
/// </summary>
public class RandomSearch( TspMap map ) : TspAlgorithmBase( map )
{
	/// <summary>
	/// Configures the algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "random" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Single optimization epoch running a random search followed by a local search.
	/// </summary>
	/// <param name="best">The current best solution to use as the starting point for the epoch. Cannot be null.</param>
	/// <returns>A new TspResult instance representing the best solution found during this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best ) => ParallelLocalSearch( best.Path );
}
