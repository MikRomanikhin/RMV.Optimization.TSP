using System.Diagnostics;
using System.Linq;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Branch and Bound algorithm for TSP.
/// Uses a cost matrix with row/column reduction to compute lower bounds, then explores partial tours in a best-first (lowest bound) order.
/// </summary>
public class BranchAndBound( TspMap map ) : TspAlgorithmBase( map )
{
		
	#region Configure ----------------------------------------------------------

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BbSettings>( "bnb" ) ?? throw new ArgumentNullException( nameof( settings ) );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );		
	}

	BbSettings settings;

	#endregion


	#region Initialize ---------------------------------------------------------	

	///<summary
	/// Initializes by running a nearest-neighbour heuristic to obtain an upper bound.
	///</summary>
	protected override TspResult Initialize() => base.Initialize();

	/// <summary>	
	/// Runs the Branch and Bound search, starting with the initial upper bound from the heuristic solution.
	/// </summary>
	protected override TspResult RunEpoch( TspResult best ) => Search( best );

	#endregion


	#region Solve --------------------------------------------------------------

	/// <summary>
	/// Branch and Bound solver using iterative DFS with bounding.
	/// Uses O(n) stack depth and eagerly prunes branches whose lower bound exceeds the current best.
	/// </summary>
	TspResult Search( TspResult best )
	{
		int n = base.Cities;
		double upperBound = best.Tour;

		double[] dist = BuildDistanceTable( n );
		double[,] costMatrix = BuildCostMatrix( n );
		double rootBound = ReduceMatrix( costMatrix, n );

		var rootVisited = new bool[ n ];
		rootVisited[ 0 ] = true;

		var queue = new PriorityQueue<BnBNode, double>();
		queue.Enqueue( new BnBNode {
			Path = [ 0 ],
			Visited = rootVisited,
			Matrix = costMatrix,
			Bound = rootBound,
			Level = 0
		}, rootBound );

		int explored = 0;

		while( queue.Count > 0 )
		{
			Debug.WriteLine( $"Memory: {GC.GetTotalMemory( false )}, Queue: {queue.Count}" );
			queue.TryDequeue( out var node, out var priority );
			++explored;
			Debug.WriteLine( $"Explored: {explored}, Queue: {queue.Count}, node.Bound={node.Bound}, upperBound={upperBound}" );

			int current = node.Path[ ^1 ];

			if( node.Level == n - 1 )
			{
				Debug.WriteLine( "Leaf node reached: " + node.Path.Join() );
				double returnCost = dist[ current * n ];
				double tour = node.Bound + returnCost;

				if( tour + MARGIN < upperBound )
				{
					upperBound = tour;
					best = new TspResult( tour, [ .. node.Path ] );
					Draw( best.Tour, ++explored, best.Path );
				}

				continue;
			}

			Span<(int city, double cost)> candidates = n <= 128 ? stackalloc (int, double)[ n ] : new (int city, double cost)[ n ];
			int candidateCount = 0;

			for( int next = 0; next < n; next++ )
			{
				if( !node.Visited[ next ] )
				{
					candidates[ candidateCount++ ] = (next, node.Matrix[ current, next ]);
				}
			}

			candidates[ ..candidateCount ].Sort( ( a, b ) => a.cost.CompareTo( b.cost ) );

			for( int index = candidateCount - 1; index >= 0; index-- )
			{
				int next = candidates[ index ].city;
				double edgeCost = candidates[ index ].cost;

				if( edgeCost >= double.MaxValue / 2 ) continue;

				double[,] childMatrix = new double[ n, n ];
				Buffer.BlockCopy( node.Matrix, 0, childMatrix, 0, sizeof( double ) * n * n );

				for( int k = 0; k < n; k++ ) childMatrix[ current, k ] = double.MaxValue / 2;
				for( int k = 0; k < n; k++ ) childMatrix[ k, next ] = double.MaxValue / 2;
				childMatrix[ next, 0 ] = double.MaxValue / 2;

				double childBound = node.Bound + edgeCost + ReduceMatrix( childMatrix, n );

				Debug.WriteLine( $"childBound={childBound}, upperBound={upperBound}" );
				if( childBound + MARGIN >= upperBound ) continue;

				var childVisited = new bool[ n ];
				Array.Copy( node.Visited, childVisited, n );
				childVisited[ next ] = true;

				if( queue.Count < settings.Size )
				{
					queue.Enqueue( new BnBNode {
						Path = [ .. node.Path, next ],
						Visited = childVisited,
						Matrix = childMatrix,
						Bound = childBound,
						Level = node.Level + 1
					}, childBound );
					Debug.WriteLine( $"------------ Queue enqueue: {queue.Count}" );
				}
				else
				{
					Debug.WriteLine( "************* Queue limit reached, terminating search." );
					return best;
				}
			}
		}

		return best;
	}
	//TspResult Search( TspResult best )
	//{
	//	int n = base.Cities;
	//	double upperBound = best.Tour;

	//	double[] dist = BuildDistanceTable( n );
	//	double[,] costMatrix = BuildCostMatrix( n );
	//	double rootBound = ReduceMatrix( costMatrix, n );

	//	var rootVisited = new bool[ n ];
	//	rootVisited[ 0 ] = true;

	//	var stack = new Stack<BnBNode>();
	//	stack.Push( new BnBNode {
	//		Path = [ 0 ],
	//		Visited = rootVisited,
	//		Matrix = costMatrix,
	//		Bound = rootBound,
	//		Level = 0
	//	} );

	//	int explored = 0;

	//	while( stack.Count > 0 )
	//	{
	//		Debug.WriteLine( $"Memory: {GC.GetTotalMemory( false )}, Stack: {stack.Count}" );
	//		var node = stack.Pop();
	//		++explored;
	//		Debug.WriteLine( $"Explored: {explored}, Stack: {stack.Count}, node.Bound={node.Bound}, upperBound={upperBound}" );			
	//		// if( node.Bound + MARGIN >= upperBound ) continue;

	//		int current = node.Path[ ^1 ];

	//		if( node.Level == n - 1 )
	//		{				
	//			Debug.WriteLine( "Leaf node reached: " + node.Path.Join() );
	//			double returnCost = dist[ current * n ];
	//			double tour = node.Bound + returnCost;

	//			if( tour + MARGIN < upperBound )
	//			{
	//				upperBound = tour;
	//				best = new TspResult( tour, [ .. node.Path ] );
	//				Draw( best.Tour, ++explored, best.Path );
	//			}

	//			continue;
	//		}

	//		Span<(int city, double cost)> candidates = n <= 128 ? stackalloc (int, double)[ n ] : new (int city, double cost)[ n ];
	//		int candidateCount = 0;

	//		for( int next = 0; next < n; next++ )
	//		{
	//			if( !node.Visited[ next ] )
	//			{
	//				candidates[ candidateCount++ ] = (next, node.Matrix[ current, next ]);
	//			}
	//		}

	//		candidates[ ..candidateCount ].Sort( ( a, b ) => a.cost.CompareTo( b.cost ) );

	//		for( int index = candidateCount - 1; index >= 0; index-- )
	//		{
	//			int next = candidates[ index ].city;
	//			double edgeCost = candidates[ index ].cost;

	//			if( edgeCost >= double.MaxValue / 2 ) continue;

	//			double[,] childMatrix = new double[ n, n ];
	//			Buffer.BlockCopy( node.Matrix, 0, childMatrix, 0, sizeof( double ) * n * n );

	//			for( int k = 0; k < n; k++ ) childMatrix[ current, k ] = double.MaxValue / 2;
	//			for( int k = 0; k < n; k++ ) childMatrix[ k, next ] = double.MaxValue / 2;
	//			childMatrix[ next, 0 ] = double.MaxValue / 2;

	//			double childBound = node.Bound + edgeCost + ReduceMatrix( childMatrix, n );

	//			Debug.WriteLine( $"childBound={childBound}, upperBound={upperBound}" );
	//			if( childBound + MARGIN >= upperBound ) continue;

	//			var childVisited = new bool[ n ];
	//			Array.Copy( node.Visited, childVisited, n );
	//			childVisited[ next ] = true;

	//			if( stack.Count < settings.Size )
	//			{
	//				stack.Push( new BnBNode {
	//					Path = [ .. node.Path, next ],
	//					Visited = childVisited,
	//					Matrix = childMatrix,
	//					Bound = childBound,
	//					Level = node.Level + 1
	//				} );
	//				Debug.WriteLine( $"------------ Stack push: {stack.Count}" );
	//			}
	//			else
	//			{
	//				Debug.WriteLine( "************* Stack limit reached, terminating search." );
	//				return best;
	//			}
	//		}

	//		//if( explored % base.settings.Redraw == 0 && best != null ) Draw( best.Tour, explored, best.Path );
	//	}		

	//	return best;
	//}

	#endregion


	#region Distance Table -----------------------------------------------------

	/// <summary>
	/// Builds a flat n×n distance lookup array for O(1) access without dictionary overhead
	/// </summary>
	double[] BuildDistanceTable( int n )
	{
		double[] dist = new double[ n * n ];

		for( int i = 0; i < n; i++ )
		{
			for( int j = 0; j < n; j++ )
			{
				dist[ i * n + j ] = i == j ? double.MaxValue / 2 : base.map[ i, j ].Weight;
			}
		}

		return dist;
	}

	#endregion


	#region Cost Matrix --------------------------------------------------------

	/// <summary>
	/// Builds the initial NxN cost matrix from the map edges
	/// </summary>
	double[,] BuildCostMatrix( int n )
	{
		double[,] matrix = new double[ n, n ];

		for( int i = 0; i < n; i++ )
		{
			for( int j = 0; j < n; j++ )
			{
				matrix[ i, j ] = i == j ? double.MaxValue / 2 : base.map[ i, j ].Weight;
			}
		}

		return matrix;
	}	


	/// <summary>
	/// Reduces all rows and columns by subtracting their minimum value.
	/// Returns the total reduction cost (lower bound contribution).
	/// </summary>
	static double ReduceMatrix( double[,] matrix, int n )
	{
		double reduction = 0;
		const double INF = double.MaxValue / 2;

		for( int i = 0; i < n; i++ )
		{
			double rowMin = INF;

			for( int j = 0; j < n; j++ )
			{
				if( matrix[ i, j ] < rowMin ) rowMin = matrix[ i, j ];
			}

			if( rowMin > 0 && rowMin < INF )
			{
				reduction += rowMin;

				for( int j = 0; j < n; j++ )
				{
					if( matrix[ i, j ] < INF ) matrix[ i, j ] -= rowMin;
				}
			}
		}

		for( int j = 0; j < n; j++ )
		{
			double colMin = INF;

			for( int i = 0; i < n; i++ )
			{
				if( matrix[ i, j ] < colMin ) colMin = matrix[ i, j ];
			}

			if( colMin > 0 && colMin < INF )
			{
				reduction += colMin;

				for( int i = 0; i < n; i++ )
				{
					if( matrix[ i, j ] < INF ) matrix[ i, j ] -= colMin;
				}
			}
		}

		return reduction;
	}	

	#endregion

}

/// <summary>
/// Configuration Settings
/// </summary>
class BbSettings : TspConfigurationBase
{
	public int Size { get; set; }
}

#region BnBNode ---------------------------------------------------------------

/// <summary>
/// Represents a node in the Branch and Bound search tree
/// </summary>
sealed class BnBNode
{
	/// <summary>Partial path built so far</summary>
	public List<int> Path { get; init; }

	/// <summary>Visited city flags</summary>
	public bool[] Visited { get; init; }

	/// <summary>Reduced cost matrix at this node</summary>
	public double[,] Matrix { get; init; }

	/// <summary>Lower bound (accumulated cost + matrix reduction)</summary>
	public double Bound { get; init; }

	/// <summary>Depth in the search tree (number of cities in the partial tour minus 1)</summary>
	public int Level { get; init; }
}

#endregion
