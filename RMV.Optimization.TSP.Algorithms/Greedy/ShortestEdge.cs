using RMV.Optimization.TSP.Common;
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

	protected override TspResult RunEpoch( TspResult best ) => CheapestInsert();


	/// <summary>
	/// Cheapest insert algorithm
	/// </summary>
	TspResult CheapestInsert()
	{
		var start = base.map.Edges.MinBy( e => e.Value.Weight ).Value;

		List<int> path = [ start.Head, start.Tail ];

		HashSet<int> candidates = [ .. Enumerable.Range( 0, this.Cities ).Except( path ) ];

		int count = 0;

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

			if( candidates.Count == 0 ) // Only compute tour length for drawing once the path is complete
			{
				base.Draw( map.GetTourLength( path ), ++count, path );
				continue;
			}

			// Compute partial tour cost manually for the in-progress path
			double partialTour = 0;
			for( int i = 0; i < path.Count; i++ )
			{
				int next = ( i + 1 ) % path.Count;
				partialTour += map[ path[ i ], path[ next ] ].Weight;
			}

			base.Draw( partialTour, ++count, path );
		}

		return TspResult.Build( this.map, path );
	}
	
}
