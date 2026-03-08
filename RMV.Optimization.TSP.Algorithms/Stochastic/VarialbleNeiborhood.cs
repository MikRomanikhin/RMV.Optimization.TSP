using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Variable Neiborhood Search
/// </summary>	
public class VarialbleNeiborhoodSearch( TspMap map ) : TspAlgorithmBase( map )
{	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<TspConfigurationBase>( "vns" );
	}

	protected override TspResult? Initialize() => base.BuildNearestTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var dup = best.Clone(); // start with the best found solution from the previous epoch
		
		int neighborhood = 0;

		while( ++neighborhood < 3 )
		{
			var shakenTour = Shake( best.Path, neighborhood );

			var current = ParallelLocalSearch( shakenTour ); //ParallelLinKernighanSearch( shakenTour );				

			if( current < dup )
			{
				dup = current.Clone();
				neighborhood = 0; // restart from first neighborhood						
			}
		}

		return dup; // return the best found solution in this epoch
	}


	/// <summary>
	/// Shakes the given path by applying a neighborhood operation
	/// </summary>	
	static List<int> Shake( IList<int> path, int neighborhood )
	{
		var newTour = new List<int>( path );
		int cities = path.Count;

		switch( neighborhood )
		{
			case 1:  // Swap two cities		//newTour = RandomSwap( newTour );
				int i = Random.Shared.Next( cities );
				int j = Random.Shared.Next( cities );
				while( i == j ) j = Random.Shared.Next( cities );
				(newTour[ i ], newTour[ j ]) = (newTour[ j ], newTour[ i ]);
				break;

			case 2:  // 2-opt		//newTour = Random2OptSwap( newTour );
				int a = Random.Shared.Next( cities - 1 );
				int b = Random.Shared.Next( a + 1, cities );
				newTour.Reverse( a, b - a + 1 );
				break;
		}

		return newTour;
	}

	#region obsolete
	/// <summary>
	/// VNS async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	int count = 0;
	//	int noChanges = 0;

	//	base.timer.Start();

	//	var best = base.BuildNearestTour();

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			int neighborhood = 0;

	//			while( ++neighborhood < 3 )
	//			{
	//				var shakenTour = Shake( best.Path, neighborhood );

	//				var current = Local2OptSearch( shakenTour );	//LinKernighanSearch( shakenTour );				

	//				if( current < best )
	//				{
	//					best = current.Clone();

	//					neighborhood = 0; // restart from first neighborhood
	//					noChanges = 0;

	//					base.Draw( best.Tour, count, best.Path );
	//				}

	//				//if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
	//		}

	//		base.Draw( best.Tour, count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}
	//public async Task<TspResult> RunAsync()
	//{
	//	int count = 0;
	//	int noChanges = 0;

	//	base.timer.Start();

	//	var best = base.GetRandomTour();

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			var shakenTour = Shake( best );

	//			var localPath = LocalSearch( shakenTour.Path );

	//			double localTour = this.map.GetTour( localPath );

	//			if( localTour < best.Tour )
	//			{
	//				best = new TspResult { Tour = localTour, Path = localPath };					
	//				noChanges = 0;
	//			}				

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count, best.Path );
	//		}

	//		base.Draw( best.Tour, count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}

	//TspResult Shake( TspResult result )
	//{
	//	var copy = result.Clone() as TspResult;

	//	(Action accept, double delta) = base.Swap( copy.Path );

	//	if( delta < 0 )
	//	{
	//		copy.Tour += delta;
	//		accept!();
	//	}

	//	return copy;
	//}
	//public async Task<TspResult> RunAsync()
	//{
	//	int count = 0;
	//	int noChanges = 0;

	//	base.timer.Start();

	//	var best = base.GetRandomTour();

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{				
	//			var current = LocalSearch( best );

	//			if( current < best ) best = new TspResult { Tour = current.Tour, Path = current.Path };			

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count, best.Path );				
	//		}

	//		base.Draw( best.Tour, count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}
	//TspResult LocalSearch( TspResult best )
	//{
	//	double currentCost = best.Tour;
	//	int[] currentTour = new int[ base.Cities ];

	//	int count = 0;

	//	while( count < base.Cities / 2 )
	//	{
	//		var newTour = Shake( [ .. best.Path ], ++count );

	//		double newCost = base.map.GetTour( newTour );

	//		if( newCost + MARGIN < currentCost )
	//		{
	//			Array.Copy( newTour, currentTour, base.Cities );
	//			currentCost = newCost;
	//			count = 1; // Restart neighborhood search
	//		}
	//	}

	//	return new TspResult { Tour = currentCost, Path = currentTour };
	//}

	//static int[] Shake( int[] tour, int k )
	//{
	//	int[] newTour = ( int[] )tour.Clone();

	//	for( int i = 0; i < k; i++ )
	//	{
	//		int pos1 = Random.Shared.Next( tour.Length );
	//		int pos2 = Random.Shared.Next( tour.Length );

	//		while( pos2 == pos1 ) pos2 = Random.Shared.Next( tour.Length );

	//		(newTour[ pos1 ], newTour[ pos2 ]) = (newTour[ pos2 ], newTour[ pos1 ]);				
	//	}

	//	return newTour;
	//}
	#endregion
}
