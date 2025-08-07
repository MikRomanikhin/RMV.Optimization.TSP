using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Random Search for TSP
/// </summary>
public class RandomSearch( TspMap map ) : TspAlgorithmBase( map )
{

	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "random" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult Initialize() => InitializeTour();

	protected override TspResult RunEpoch( TspResult best ) => Parallel2OptSearch( best.Path );

}
