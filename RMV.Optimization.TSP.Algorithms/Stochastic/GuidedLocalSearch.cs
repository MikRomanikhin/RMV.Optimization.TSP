using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Guided Local Search
/// </summary>
public class GuidedLocalSearch( TspMap map ) : TspAlgorithmBase( map )
{	
	double lambda = 1;	

	protected override void Configure()
	{		
		base.settings = ConfigManager.GetSection<GlsSettings>( "gls" ) ?? throw new ArgumentNullException( nameof( settings ) );
		var glsSettings = ( GlsSettings )base.settings;

		lambda = 0.3 * glsSettings.Optima / base.Cities;
	}

	protected override TspResult Initialize() => base.BuildNearestTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var result = LocalSearch( best );

		var utilities = GetFeatureUtilities( best.Path );

		UpdatePenalties( best.Path, utilities );

		return result;
	}

	#region obsolete
	/// <summary>
	/// GLS async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	base.timer.Start();

	//	int count = 0;
	//	int noChanges = 0;

	//	var best = base.BuildNearestTour();//BuildRandomTour();		

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			var result = LocalSearch( best ); 

	//			var utilities = GetFeatureUtilities( best.Path );

	//			UpdatePenalties( best.Path, utilities );

	//			if( result < best )
	//			{
	//				best = result.Clone();

	//				noChanges = 0;

	//				base.Draw( best.Tour, count, best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );			
	//		}

	//		base.Draw( best.Tour, ++count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}	
	//TspResult LocalSearch( TspResult best )
	//{
	//	double oldCost = UpdateTotalCost( best.Path );

	//	int noChanges = 0;

	//	while( true )
	//	{
	//		var newPath = TwoOptSwap( best.Path );
	//		double tour = base.map.GetTour( newPath );

	//		double cost = UpdateTotalCost( newPath );

	//		if( cost < oldCost )
	//		{
	//			best = new TspResult { Tour = tour, Path = newPath }; // result.Clone() as TspResult;

	//			oldCost = cost;
	//			noChanges = 0;
	//		}

	//		//if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count, best.Path );

	//		if( ++noChanges > 1000 ) break;
	//	}

	//	return best;
	//}
	#endregion

	/// <summary>
	/// Local Search
	/// </summary>
	TspResult LocalSearch( TspResult best )
	{
		double oldCost = UpdateTotalCost( best.Path );

		int count = 0;
		int noChanges = 0;

		while( true )
		{			
			pauseEvent.Wait(); // Pause/resume support

			var result = Swap( best );	//result.UpdateTour( this.Map );

			double cost = UpdateTotalCost( result.Path );

			if( cost < oldCost )
			{
				best = result.Clone();
				oldCost = cost;
				noChanges = 0;
				base.Draw( best.Tour, count, best.Path );
			}			

			if( ++noChanges > 200 ) break;
		}

		return best;
	}

	TspResult Swap( TspResult result )
	{
		var copy = result.Clone();

		(Action accept, double delta) = base.Swap( copy.Path );

		if( delta < 0 )
		{
			copy.Tour += delta;
			accept!();
		}

		return copy;
	}

	double UpdateTotalCost( IList<int> path )
	{
		double cost = 0;

		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];				

			cost += base.map[ c1, c2 ].Weight + this.lambda * base.map[ c1, c2 ].Penalty;
		}		

		return cost;
	}

	double[] GetFeatureUtilities( IList<int> path )
	{
		var utilities = new double[ path.Count ];		

		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];			

			utilities[ i ] = base.map[ c1, c2 ].Weight / ( 1.0 + base.map[ c1, c2 ].Penalty );
		}

		return utilities;
	}

	void UpdatePenalties( IList<int> path, double[] utilities )
	{
		double max = utilities.Max();

		for( int i = 0; i < path.Count; i++ )
		{
			int c1 = path[ i ];
			int c2 = i == path.Count - 1 ? path[ 0 ] : path[ i + 1 ];			

			if( utilities[ i ] + MARGIN > max ) base.map[ c1, c2 ].Penalty += 1.0;
		}
	}
}

/// <summary>
/// Configuration Settings
/// </summary>
class GlsSettings : TspConfigurationBase
{
	public double Optima { get; set; }	
}
