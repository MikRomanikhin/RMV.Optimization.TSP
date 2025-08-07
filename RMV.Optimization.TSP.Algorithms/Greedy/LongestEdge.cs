using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Longest Insert for TSP
/// </summary>
public class LongestEdge( TspMap map ) : TspAlgorithmBase( map )
{	

	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<IlsSettings>( "long" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult RunEpoch( TspResult best ) => FarthestInsert();

	/// <summary>
	/// Farthest Insert algorithm
	/// </summary>
	TspResult FarthestInsert()
	{
		var start = map.Edges.MaxBy( e => e.Value.Weight ).Value;
		double tour = start.Weight;		

		List<int> path = [ start.Head, start.Tail ]; // Initialize the tour with the two farthest points		
		HashSet<int> visited = new( path );

		while( path.Count < base.Cities ) // Insert the farthest unvisited point into the tour
		{
			// Find the farthest unvisited point
			var next = Enumerable.Range( 0, base.Cities ).Where( i => !visited.Contains( i ) ).
				Select( i => new { Index = i, distance = path.Min( t => map[ t, i ].Weight ) } ).MaxBy( x => x.distance );

			int nextPoint = next?.Index ?? -1;
			double maxDist = next?.distance ?? -1;

			// Find the best place to insert the point into the tour 
			var best = Enumerable.Range( 0, path.Count ).Select( i => {
				int j = ( i + 1 ) % path.Count;
				return new { Position = j, Delta = GetCost( nextPoint, i, j, path ) };
			} ).MinBy( x => x.Delta );					

			path.Insert( best.Position, nextPoint ); // Insert the point into the tour
			visited.Add( nextPoint );

			tour +=  best.Delta;
			base.Draw( tour, 0, path );
		}

		return new TspResult( tour, path );
	}

	double GetCost( int node, int i, int j, List<int> path ) =>
		map[ path[ i ], node ].Weight + map[ node, path[ j ] ].Weight - map[ path[ i ], path[ j ] ].Weight;

	#region obsolete
	//TspResult FarthestInsert()
	//{		
	//	var start = this.Map.Edges.MaxBy( e => e.Value.Weight ).Value;
	//	double tour = start.Weight;

	//	int count = 0;

	//	List<int> path = [ start.Head, start.Tail ]; // Initialize the tour with the two farthest points		
	//	HashSet<int> candidates = Enumerable.Range( 0, this.Cities ).Except( path ).ToHashSet();

	//	while( candidates.Any() ) // Insert the farthest unvisited point into the tour
	//	{			
	//		var best = candidates.SelectMany( node => Enumerable.Range( 0, path.Count - 1 ).Select( i => {
	//			int j = ( i + 1 ) % path.Count;
	//			return new { node, Position = j, Cost = GetCost( node, i, j, path ) }; 
	//		} ) ).MaxBy( x => x.Cost );

	//		int position = best?.Position ?? -1;
	//		double increase = best?.Cost ?? double.MaxValue;

	//		path.Insert( position, best.node );
	//		candidates.Remove( best.node );

	//		tour += increase;
	//		base.Draw( tour, count, path );
	//	}
	//	return new TspResult( tour, path );
	//}

	//static List<int> FarthestInsertion( (double x, double y)[] points )
	//{
	//	int n = points.Length;
	//	if( n < 3 ) return Enumerable.Range( 0, n ).ToList();

	//	// Start with the two farthest points
	//	int start = 0, farthest = 1;
	//	double maxDistance = 0;
	//	for( int i = 0; i < n; i++ )
	//	{
	//		for( int j = i + 1; j < n; j++ )
	//		{
	//			double dist = Distance( points[ i ], points[ j ] );
	//			if( dist > maxDistance )
	//			{
	//				maxDistance = dist;
	//				start = i;
	//				farthest = j;
	//			}
	//		}
	//	}

	//	// Initialize the tour with the two farthest points
	//	List<int> tour = new List<int> { start, farthest };
	//	HashSet<int> visited = new HashSet<int>( tour );

	//	// Insert the farthest unvisited point into the tour
	//	while( tour.Count < n )
	//	{
	//		int nextPoint = -1;
	//		double maxDist = -1;

	//		// Find the farthest unvisited point
	//		for( int i = 0; i < n; i++ )
	//		{
	//			if( !visited.Contains( i ) )
	//			{
	//				double minDistToTour = tour.Min( t => Distance( points[ t ], points[ i ] ) );
	//				if( minDistToTour > maxDist )
	//				{
	//					maxDist = minDistToTour;
	//					nextPoint = i;
	//				}
	//			}
	//		}

	//		// Find the best place to insert the point into the tour
	//		int bestInsertPos = -1;
	//		double minIncrease = double.MaxValue;
	//		for( int i = 0; i < tour.Count; i++ )
	//		{
	//			int j = ( i + 1 ) % tour.Count;
	//			double increase = Distance( points[ tour[ i ] ], points[ nextPoint ] ) +
	//									Distance( points[ nextPoint ], points[ tour[ j ] ] ) -
	//									Distance( points[ tour[ i ] ], points[ tour[ j ] ] );
	//			if( increase < minIncrease )
	//			{
	//				minIncrease = increase;
	//				bestInsertPos = j;
	//			}
	//		}

	//		// Insert the point into the tour
	//		tour.Insert( bestInsertPos, nextPoint );
	//		visited.Add( nextPoint );
	//	}

	//	return tour;
	//}
	#endregion

}
