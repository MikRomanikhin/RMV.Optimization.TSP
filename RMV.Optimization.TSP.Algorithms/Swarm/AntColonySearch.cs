using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.ACO;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Ant Colony Optimization for TSP
/// </summary>
public class AntColonySearch( TspMap map ) : TspAlgorithmBase( map )
{

	#region Initialize ---------------------------------------------------------	

	AcoMap Map;
	AcoSettings settings;	

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<AcoSettings>( "aco" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );

		this.Map = new AcoMap( this.map, this.settings );		
	}

	

	protected override TspResult? Initialize()
	{
		var result = base.BuildNearestTour();
		this.settings.Nearest = result.Tour;

		return result;
	}

	protected override TspResult RunEpoch( TspResult best ) => this.Map.RunEpoch( best );


	#endregion


	#region obsolete

	/// <summary>
	/// ACO async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{	
	//	base.timer.Start();			

	//	int count = 0;
	//	int noChanges = 0;		

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			bool improved = this.Map.RunEpoch( noChanges );

	//			if( improved )
	//			{
	//				noChanges = 0;

	//				base.Draw( this.Map.Best.Tour, count, this.Map.Best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( this.Map.Best.Tour, count );						
	//		}

	//		base.Draw( this.Map.Best.Tour, count, this.Map.Best.Path );
	//	} );

	//	base.timer.Stop();

	//	return new TspResult( this.Map.Best.Tour, this.Map.Best.Path );
	//}



	//public async Task<TspResult> RunAsync()
	//{
	//	base.timer.Start();

	//	var result = new NearestNeighbour( map ).GetNearest( 0 );
	//	this.settings.Nearest = result.Tour;

	//	//Ant best = new() { Tour = result.Tour, Path = [..result.Path] };	

	//	int count = 0;
	//	int noChanges = 0;

	//	await Task.Run( () => {
	//		while( true )
	//		{
	//			this.Map.RunEpoch( algorithm );//, result.Path[ 0 ] );

	//			if( this.Map.Improved ) noChanges = 0;

	//			//var current = this.Map.LocalBest; //local best
	//			//if( current.Tour + MARGIN < best.Tour )  
	//			//{
	//			//	best = current.Clone() as Ant; //global best

	//			//	this.Map.GlobalBest = best;

	//			//	noChanges = 0;	//base.Draw( best.Tour, count, best.Path );
	//			//}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( this.Map.Best.Tour, count, this.Map.Best.Path );

	//			if( ++noChanges > settings.Limit ) break;
	//		}

	//		base.Draw( this.Map.Best.Tour, ++count, this.Map.Best.Path );
	//	} );

	//	base.timer.Stop();

	//	return new TspResult { Tour = this.Map.Best.Tour, Path = this.Map.Best.Path };
	//}
	#endregion
}
