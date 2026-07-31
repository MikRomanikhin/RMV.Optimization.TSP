using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Cheapest Insert for TSP
/// </summary>
public class ShortestEdge( TspMap map ) : TspAlgorithmBase( map )
{	

	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "short" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		var result = CheapestInsert();

		// Apply local search to improve the constructed tour
		result = ParallelLocalSearch( result.Path );

		return result.Tour + MARGIN < best.Tour ? result : best;
	}


	/// <summary>
	/// Cheapest insert algorithm: builds tour by starting with shortest edge and iteratively inserting cities at minimum cost positions
	/// </summary>
	TspResult CheapestInsert()
	{
		// Guard against very small maps
		if( base.Cities < 2 ) return TspResult.Build( this.map, [ 0 ] );

		var start = base.map.Edges.MinBy( e => e.Value.Weight ).Value;

		List<int> path = [ start.Head, start.Tail ];

		// Use direct HashSet constructor instead of LINQ spread operator for better performance
		var candidates = new HashSet<int>( Enumerable.Range( 0, this.Cities ).Except( path ) );

		int count = 0;
		double tourCost = map[ path[ 0 ], path[ 1 ] ].Weight + map[ path[ 1 ], path[ 0 ] ].Weight; // Initial 2-city tour

		while( candidates.Count > 0 )
		{
			int bestNode = -1;
			int bestPosition = -1;
			double minCost = double.MaxValue;

			foreach( int node in candidates )
			{
				for( int i = 0; i < path.Count; i++ )
				{
					int next = ( i + 1 ) % path.Count;

					double cost = map[ path[ i ], node ].Weight + map[ node, path[ next ] ].Weight - map[ path[ i ], path[ next ] ].Weight;

					if( cost < minCost )
					{
						minCost = cost;
						bestNode = node;
						bestPosition = i + 1;
					}
				}
			}

			if( bestNode == -1 ) break; // Safety guard — no valid insertion found

			// Wrap-around: inserting after the last city means inserting at position 0
			if( bestPosition >= path.Count ) bestPosition = 0;

			path.Insert( bestPosition, bestNode );
			candidates.Remove( bestNode );

			// Update tour cost incrementally instead of recalculating from scratch
			tourCost += minCost;

			// Draw progress (partial or complete tour)
			base.Draw( tourCost, ++count, path );
		}

		return TspResult.Build( this.map, path );
	}
	
}
