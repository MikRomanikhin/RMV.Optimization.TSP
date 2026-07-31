using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Longest Insert algorithm for TSP
/// </summary>
public class LongestEdge( TspMap map ) : TspAlgorithmBase( map )
{
	/// <summary>
	/// Configures the algorithm settings 
	/// </summary>	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "long" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Executes the algorithm for one epoch
	/// </summary>
	/// <param name="best">The current best solution to compare against the result of this epoch. Must not be null.</param>
	/// <returns>A TspResult representing the better of the provided best solution and the result of this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var result = FarthestInsert();

		// Apply local search to improve the constructed tour
		result = ParallelLocalSearch( result.Path );

		return result.Tour + MARGIN < best.Tour ? result : best;
	}

	/// <summary>
	/// Farthest Insert algorithm: starts with the longest edge, iteratively inserts farthest unvisited cities at cheapest positions
	/// </summary>
	TspResult FarthestInsert()
	{
		// Guard against very small maps
		if( base.Cities < 2 ) return TspResult.Build( this.map, [ 0 ] );

		var start = map.Edges.MaxBy( e => e.Value.Weight ).Value;

		List<int> path = [ start.Head, start.Tail ];
		HashSet<int> visited = new( path );

		while( path.Count < base.Cities )
		{
			// Find the farthest unvisited point using explicit loop instead of LINQ
			int nextPoint = -1;
			double maxDist = -1;

			foreach( int candidate in Enumerable.Range( 0, base.Cities ) )
			{
				if( visited.Contains( candidate ) ) continue;

				// Find minimum distance from this candidate to any point in the current path
				double minDistToPath = double.MaxValue;
				foreach( int pathCity in path )
				{
					double dist = map[ pathCity, candidate ].Weight;
					if( dist < minDistToPath )
						minDistToPath = dist;
				}

				// Track the farthest candidate
				if( minDistToPath > maxDist )
				{
					maxDist = minDistToPath;
					nextPoint = candidate;
				}
			}

			if( nextPoint == -1 ) break; // No more unvisited cities

			// Find the best (cheapest) position to insert the farthest point using explicit loop
			int bestPosition = 0;
			double minCost = double.MaxValue;

			for( int i = 0; i < path.Count; i++ )
			{
				int j = ( i + 1 ) % path.Count;
				double cost = GetCost( nextPoint, i, j, path );

				if( cost < minCost )
				{
					minCost = cost;
					bestPosition = i + 1;
				}
			}

			// Handle wrap-around for circular tour
			if( bestPosition >= path.Count ) bestPosition = 0;

			path.Insert( bestPosition, nextPoint );
			visited.Add( nextPoint );
		}

		// Calculate final tour cost correctly using GetTourLength
		double tour = map.GetTourLength( path );

		base.Draw( tour, 0, path );

		return new TspResult( tour, path );
	}

	/// <summary>
	/// Calculates the cost of inserting a node between two nodes in the current path
	/// </summary>
	/// <param name="node">The node to be inserted</param>
	/// <param name="i">The index of the first node in the current path segment</param>
	/// <param name="j">The index of the second node in the current path segment</param>
	/// <param name="path">The current path of the tour</param>
	/// <returns>The cost of inserting the node between the specified nodes in the path</returns>
	double GetCost( int node, int i, int j, List<int> path ) =>
		map[ path[ i ], node ].Weight + map[ node, path[ j ] ].Weight - map[ path[ i ], path[ j ] ].Weight;	

}
