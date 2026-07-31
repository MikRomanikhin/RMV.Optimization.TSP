using RMV.Common.Configuration;
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
				
		result = ParallelLocalSearch( result.Path ); // Apply local search to improve the constructed tour

		return result.Tour + MARGIN < best.Tour ? result : best;
	}


	/// <summary>
	/// Pilot Search algorithm implementation
	/// </summary>	
	TspResult RunPilotSearch( int start )
	{
		// Guard against very small maps: TSP requires at least 2 cities
		if( base.Cities < 2 ) return TspResult.Build( this.map, [ start ] );

		var visited = new bool[ base.Cities ];
		List<int> path = [ start ];

		visited[ start ] = true;
		double tour = 0;

		while( path.Count < base.Cities )
		{
			int currentCity = path.Last();

			int nextCity = SelectNextCity( start, currentCity, visited );

			// Safety check: SelectNextCity should always return a valid city (0 <= nextCity < Cities)
			if( nextCity < 0 || nextCity >= base.Cities )
				break; // Abort this tour to avoid invalid state; RunEpoch will compare with best

			tour += map[ currentCity, nextCity ].Weight;

			path.Add( nextCity );
			visited[ nextCity ] = true;
		}

		tour += map[ path[ ^1 ], start ].Weight; //complete the tour

		return new TspResult( tour, path );
	}

	/// <summary>
	/// Selects the next city to visit based on the simulated tour cost.
	/// Works in-place on the visited array, restoring state to avoid expensive cloning.
	/// Pure greedy: always picks the best lookahead candidate.
	/// </summary>
	/// <param name="start">The starting city of the tour.</param>
	/// <param name="current">The current city in the tour.</param>
	/// <param name="visited">An array indicating which cities have been visited. State is restored after this call.</param>
	/// <returns>The index of the next city to visit.</returns>
	int SelectNextCity( int start, int current, bool[] visited )
	{
		int bestCity = -1;
		double bestCost = double.MaxValue;

		for( int candidate = 0; candidate < base.Cities; candidate++ )
		{
			if( visited[ candidate ] ) continue;

			// Mark candidate visited, simulate, then restore
			visited[ candidate ] = true;
			double cost = this.map[ current, candidate ].Weight + SimulateTourFrom( start, candidate, visited );
			visited[ candidate ] = false; // Restore state

			if( cost < bestCost )
			{
				bestCost = cost;
				bestCity = candidate;
			}
		}

		// If no candidates found, use greedy fallback (nearest unvisited)
		if( bestCity == -1 )
		{
			double nearest = double.MaxValue;
			for( int candidate = 0; candidate < base.Cities; candidate++ )
			{
				if( !visited[ candidate ] )
				{
					double edgeCost = this.map[ current, candidate ].Weight;
					if( edgeCost < nearest )
					{
						nearest = edgeCost;
						bestCity = candidate;
					}
				}
			}
		}

		return bestCity;
	}


	/// <summary>
	/// Simulates a tour from the given starting city, greedily completing the remaining path.
	/// Works in-place on the visited array, restoring all mutations before returning.
	/// This avoids expensive array cloning while maintaining algorithm correctness.
	/// </summary>
	/// <param name="start">The starting city of the tour (to close the loop).</param>
	/// <param name="lastCity">The current city in the simulation.</param>
	/// <param name="visited">Array tracking visited cities. All mutations are restored upon return.</param>
	/// <returns>The cost to complete the tour from lastCity onwards</returns>
	double SimulateTourFrom( int start, int lastCity, bool[] visited )
	{
		double cost = 0;
		int currentCity = lastCity;
		var visitedCities = new List<int>(); // Track cities visited during simulation for restoration

		// Greedily visit the nearest unvisited city until all cities are visited
		for( int i = 0; i < base.Cities - 1; i++ )
		{
			int nextCity = -1;
			double minCost = double.MaxValue;

			// Find nearest unvisited city from current position
			for( int j = 0; j < base.Cities; j++ )
			{
				if( !visited[ j ] )
				{
					double edgeCost = this.map[ currentCity, j ].Weight;
					if( edgeCost < minCost )
					{
						minCost = edgeCost;
						nextCity = j;
					}
				}
			}

			if( nextCity == -1 )
				break; // No more unvisited cities

			cost += minCost;
			visited[ nextCity ] = true;
			visitedCities.Add( nextCity );
			currentCity = nextCity;
		}

		// Restore all visited state before returning
		foreach( int city in visitedCities )
		{
			visited[ city ] = false;
		}

		// Add cost to return to the starting city
		cost += this.map[ currentCity, start ].Weight;

		return cost;
	}
	
}