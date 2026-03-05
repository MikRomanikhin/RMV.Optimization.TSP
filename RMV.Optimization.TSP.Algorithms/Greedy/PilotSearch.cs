using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Pilot Search for TSP
/// </summary>
public class PilotSearch( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	int nextStart = 0;

	/// <summary>
	/// Configures the current instance by loading application settings from the configuration section named "pilot."
	/// </summary>	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "pilot" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the Pilot Search algorithm by setting the starting city.
	/// </summary>
	/// <returns></returns>
	protected override TspResult? Initialize()
	{
		this.nextStart = 1;

		return RunPilotSearch( 0 );
	}

	/// <summary>
	/// Performs a single optimization epoch and returns the best result found between the current and previous best
	/// solutions.
	/// </summary>
	/// <param name="best">The current best solution to compare against the result of this epoch. Must not be null.</param>
	/// <returns>A TspResult representing the better of the provided best solution and the result of this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		if( this.nextStart >= base.Cities ) this.nextStart = 0;

		var result = RunPilotSearch( this.nextStart++ );

		return result.Tour + MARGIN < best.Tour ? result : best;
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

			tour += map[ currentCity, nextCity ].Weight;

			path.Add( nextCity );
			visited[ nextCity ] = true;
		}

		tour += map[ path[ ^1 ], start ].Weight; //complete the tour

		return new TspResult( tour, path );
	}


	int SelectNextCity( int start, int current, bool[] visited ) =>
		Enumerable.Range( 0, base.Cities ).Where( c => !visited[ c ] ).MinBy( c => SimulateTour( start, current, c, visited ) );


	/// <summary>
	/// Simulates a tour starting from the current city and considering a candidate city.
	/// </summary>
	/// <param name="start">The starting city of the tour.</param>
	/// <param name="current">The current city in the tour.</param>
	/// <param name="candidate">The candidate city to visit next.</param>
	/// <param name="visited">An array indicating which cities have been visited.</param>
	/// <returns></returns>
	double SimulateTour( int start, int current, int candidate, bool[] visited )
	{
		var copyVisited = ( bool[] )visited.Clone();
		copyVisited[ candidate ] = true;

		double cost = this.map[ current, candidate ].Weight;
		int lastCity = candidate;

		for( int i = 0; i < base.Cities - 1; i++ )
		{
			var tmp = Enumerable.Range( 0, base.Cities ).Where( j => !copyVisited[ j ] )
				.Select( j => new { cost = map[ lastCity, j ].Weight, nextCity = j } ).MinBy( x => x.cost );

			int nextCity = tmp?.nextCity ?? -1;
			double minCost = tmp?.cost ?? int.MaxValue;

			if( nextCity != -1 )
			{
				cost += minCost;
				copyVisited[ nextCity ] = true;
				lastCity = nextCity;
			}
		}

		cost += map[ lastCity, start ].Weight; // Add cost to return to the starting city

		return cost;
	}
	
}