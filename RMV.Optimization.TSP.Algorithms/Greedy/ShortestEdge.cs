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
		double tour = start.Weight;

		List<int> path = [ start.Head, start.Tail, start.Head, ]; // Start with the first two nodes and return to the start

		HashSet<int> candidates = Enumerable.Range( 0, this.Cities ).Except( path ).ToHashSet();
		
		int count = 0;

		while( candidates.Any() )
		{											
			var best = candidates.SelectMany( node => Enumerable.Range( 0, path.Count - 1 ).Select( i => 
				new { node, position = i + 1, cost = GetCost( node, i, path ) } ) ).MinBy( x => x.cost );

			path.Insert( best.position, best.node );
			candidates.Remove( best.node );

			tour += best.cost;
			base.Draw( tour, ++count, path );		
		}

		return new TspResult( tour, path );
	}

	double GetCost( int node, int i, List<int> path ) => 
		map[ path[ i ], node ].Weight + map[ node, path[ i + 1 ] ].Weight - map[ path[ i ], path[ i + 1 ] ].Weight;


	#region obsolete
	//TspResult CheapestInsert()
	//{
	//	var start = this.Map.Edges.MinBy( e => e.Value.Weight ).Value;
	//	double tour = start.Weight;

	//	List<int> path = [ start.Head, start.Tail, start.Head, ]; // Start with the first two nodes and return to the start

	//	HashSet<int> candidates = Enumerable.Range( 0, this.Cities ).Except( path ).ToHashSet();

	//	while( candidates.Any() )
	//	{
	//		int count = 0;

	//		var best = candidates.SelectMany( node => Enumerable.Range( 0, path.Count - 1 )
	//				.Select( i => {
	//					count++;
	//					double cost = Map[ path[ i ], node ].Weight + Map[ node, path[ i + 1 ] ].Weight - Map[ path[ i ], path[ i + 1 ] ].Weight;
	//					return new { node, position = i + 1, cost, idx = count };
	//				} ) ).MinBy( x => x.cost );

	//		path.Insert( best.position, best.node );
	//		candidates.Remove( best.node );

	//		tour += best.cost;
	//		base.Draw( tour, 0, path );
	//	}

	//	return new TspResult( tour, path );
	//}

	//TspResult CheapestInsert()
	//{
	//	var start = this.Map.Edges.MinBy( e => e.Value.Weight ).Value;
	//	double tour = start.Weight;

	//	List<int> path = [ start.Head, start.Tail, start.Head, ]; // Start with the first two nodes and return to the start

	//	HashSet<int> candidates = Enumerable.Range( 0, this.Cities ).Except( path ).ToHashSet();

	//	while( candidates.Any() )
	//	{
	//		int bestNode = -1;
	//		int position = -1;

	//		double minCost = double.MaxValue;
	//		int count = 0;

	//		foreach( int node in candidates )
	//		{
	//			for( int i = 0; i < path.Count - 1; i++ )
	//			{
	//				count++;
	//				double cost = Map[ path[ i ], node ].Weight + Map[ node, path[ i + 1 ] ].Weight - Map[ path[ i ], path[ i + 1 ] ].Weight;

	//				if( cost + MARGIN < minCost )
	//				{
	//					minCost = cost;
	//					bestNode = node;
	//					position = i + 1;
	//				}
	//			}
	//		}

	//		path.Insert( position, bestNode );
	//		candidates.Remove( bestNode );

	//		tour += minCost;
	//		base.Draw( tour, count, path );
	//	}

	//	return new TspResult( tour, path );
	//}

	//public TspResult CheapestInsert1()
	//{
	//	var start = this.Map.Edges.MinBy( e => e.Value.Weight ).Value;
	//	double tour = start.Weight;

	//	double minCost = double.MaxValue;

	//	List<int> path = [ start.Head, start.Tail, start.Head, ]; //Start with the first two nodes and return to the start

	//	HashSet<int> candidates = Enumerable.Range( 0, this.Cities ).Except( path ).ToHashSet();																										 

	//	while( candidates.Any() )
	//	{
	//		int bestNode = -1;
	//		int position = -1;
	//		int count = 0;

	//		foreach( int node in candidates )
	//		{
	//			var tmp = Enumerable.Range( 0, path.Count - 1 ).
	//				OrderBy( i => Map[ path[ i ], node ].Weight + Map[ node, path[ i + 1 ] ].Weight - Map[ path[ i ], path[ i + 1 ] ].Weight ).
	//				Select( ( i, index ) => Tuple.Create( i, index, 
	//				Map[ node, i ].Weight + Map[ node, path[ i + 1 ] ].Weight - Map[ path[ i ], path[ i + 1 ] ].Weight ) ).First();

	//			if( tmp.Item3 < minCost )
	//			{
	//				minCost = tmp.Item3;
	//				bestNode = tmp.Item1;
	//				position = tmp.Item2 + 1;
	//			}
	//		}

	//		path.Insert( position, bestNode );
	//		candidates.Remove( bestNode );

	//		base.Draw( tour, 0, path );
	//	}

	//	return new TspResult( tour, path );
	//}
	#endregion
}
