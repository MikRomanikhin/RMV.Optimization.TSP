using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolution Strategies algorithm for TSP
/// </summary>
public class EvolutionStrategies( TspMap map ) : TspAlgorithmBase( map )
{	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "es" ) ?? throw new ArgumentNullException( nameof( settings ) );		
	}

	protected override TspResult? Initialize() => base.BuildNearestTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var path = RandomSwap( best.Path );

		return Parallel2OptSearch( path );//Parallel2Point5OptSearch Local2Point5OptSearch Local3OptSearch Local2OptSearch
	}
	
}
