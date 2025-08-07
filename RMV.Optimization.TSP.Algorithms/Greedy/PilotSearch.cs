using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Pilot Search for TSP
/// </summary>
public class PilotSearch( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{
	readonly TspMap Map = map;

	/// <summary>
	/// Pilot Search async wrapper
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		TspResult best = null;
		base.timer.Start();

		await Task.Run( () => { best = RunPilotSearch( 0 ); } );

		base.timer.Stop();

		return best;
	}

	/// <summary>
	/// Pilot Search algorithm implementation
	/// </summary>	
	TspResult RunPilotSearch( int start )
	{
		var visited = new bool[ base.Cities ];
		List<int> path = [ start ]; 

		visited[ start ] = true;
		double tour = 0;

		while( path.Count < base.Cities )
		{
			int currentCity = path.Last();

			int nextCity = SelectNextCity( start, currentCity, visited );

			tour += Map[ currentCity, nextCity ].Weight;

			path.Add( nextCity );
			visited[ nextCity ] = true;
		}

		tour += Map[ path[ ^1 ], start ].Weight; //complete the tour

		return new TspResult( tour, path );
	}
	

	int SelectNextCity( int start, int current, bool[] visited ) =>	
		Enumerable.Range( 0, base.Cities ).Where( c => !visited[ c ] ).MinBy( c => SimulateTour( start, current, c, visited ) );
		

	double SimulateTour( int start, int current, int candidate, bool[] visited )
	{
		var copyVisited = ( bool[] )visited.Clone();
		copyVisited[ candidate ] = true;

		double cost = this.Map[ current, candidate ].Weight;
		int lastCity = candidate;

		for( int i = 0; i < base.Cities - 1; i++ )
		{			
			var tmp = Enumerable.Range( 0, base.Cities ).Where( j => !copyVisited[ j ] )
				.Select( ( j, index ) => new { cost = this.Map[ lastCity, j ].Weight, nextCity = index } ).MinBy( x => x.cost ); 			

			int nextCity = tmp?.nextCity ?? -1;
			double minCost = tmp?.cost ?? int.MaxValue;

			if( nextCity != -1 )
			{
				cost += minCost;
				copyVisited[ nextCity ] = true;
				lastCity = nextCity;
			}
		}

		cost += this.Map[ lastCity, start ].Weight; // Add cost to return to the starting city

		return cost;
	}

	#region obsolete
	//public async Task<TspResult> RunAsync()
	//{
	//	var best = new TspResult( double.MaxValue, new int[ base.Cities ] );

	//	base.timer.Start();

	//	await Task.Run( () => 
	//	{
	//		int count = 0;

	//		for( int city = 0; city < base.Cities; city++ )
	//		{
	//			var result = RunPilotSearch( city );

	//			if( result < best ) //tour length check
	//			{
	//				best = result;

	//				base.Draw( best.Tour, ++count, best.Path );
	//			}
	//		}
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}
	#endregion
}
