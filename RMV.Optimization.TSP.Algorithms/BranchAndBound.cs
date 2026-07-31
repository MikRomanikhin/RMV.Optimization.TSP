using System.Diagnostics;

using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Branch and Bound algorithm for TSP.
/// Computes a valid lower bound at every search node via reduced cost matrix (row + column
/// reduction), then explores nodes depth-first with best-bound child ordering and prunes any
/// branch whose bound meets or exceeds the current upper bound.
/// </summary>
public class BranchAndBound( TspMap map ) : TspAlgorithmBase( map )
{
	#region Initialize ---------------------------------------------------------

	const double INF = double.MaxValue / 2;

	protected override void Configure()
	{
		this.bbSettings = ConfigManager.GetSection<BbSettings>( "bnb" ) ?? throw new ArgumentNullException( nameof( bbSettings ) );
		base.settings = this.bbSettings;
	}

	BbSettings bbSettings;

	#endregion


	/// <summary>
	/// Seeds the search with the best nearest-neighbour tour (over all starting cities) refined by
	/// 2-opt, giving Branch and Bound a tight initial upper bound so its pruning is effective from
	/// the very first epoch.
	/// </summary>
	protected override TspResult Initialize() => base.Local2OptSearch( base.BuildNearestTour().Path );


	bool searchCompleted = false;

	/// <summary>
	/// Runs Branch and Bound to completion on the first call, then returns the cached result on
	/// subsequent calls. Unlike iterative heuristics that improve over multiple epochs, B&amp;B is
	/// an exact algorithm — repeating epochs would just re-explore the same subtrees with no benefit.
	/// </summary>
	protected override TspResult RunEpoch( TspResult best )
	{
		if( searchCompleted ) return best; // already done, skip subsequent epoch calls

		try
		{
			return Search( best );
		}
		finally
		{
			searchCompleted = true;
		}
	}


	#region Search -------------------------------------------------------------

	/// <summary>
	/// B&amp;B search node.
	/// <list type="bullet">
	///   <item><see cref="Bound"/>   — accumulated lower bound (reduction total so far).</item>
	///   <item><see cref="Matrix"/>  — current reduced n×n cost matrix (flat row-major).</item>
	///   <item><see cref="Path"/>    — partial path array of length <see cref="PathLen"/>.</item>
	///   <item><see cref="Visited"/> — set of visited city indices.</item>
	/// </list>
	/// </summary>
	readonly record struct BbNode( double Bound, double[] Matrix, int[] Path, int PathLen, HashSet<int> Visited );

	/// <summary>
	/// Branch and Bound solver: depth-first search with best-bound child ordering.
	/// <para>
	/// Children are pushed onto a stack in <em>descending</em> bound order, so the child
	/// with the lowest bound is always on top — combining fast leaf discovery (O(n) pops to
	/// first complete tour) with aggressive pruning as the upper bound tightens.
	/// </para>
	/// <para>
	/// At each node the lower bound equals the sum of all row/column reductions applied so far.
	/// When branching on edge (u → v):
	/// </para>
	/// <list type="number">
	///   <item>Read <c>reducedEdgeCost = matrix[u, v]</c> (already reduced, so ≥ 0).</item>
	///   <item>Set row[u] and col[v] to ∞  (u has departed, v has been entered).</item>
	///   <item>Set <c>matrix[v, 0]</c> to ∞  if v is not the last city — prevents closing a sub-tour back to the start prematurely.</item>
	///   <item>Re-reduce the matrix; record the additional reduction.</item>
	///   <item><c>childBound = parentBound + reducedEdgeCost + additionalReduction</c>.</item>
	///   <item>Prune if <c>childBound ≥ upperBound</c>.</item>
	/// </list>
	/// <para>
	/// At a leaf node (all n cities visited) the tour cost is re-computed from the original
	/// distance table — the reduced matrix values are not directly comparable to actual distances.
	/// </para>
	/// </summary>
	TspResult Search( TspResult best )
	{
		int n = base.Cities;
		if( n < 2 ) return best;

		Debug.WriteLine( $"B&B START: n={n}, initialUpperBound={best.Tour:F4}" );

		double upperBound = best.Tour;
		double[] dist = BuildDistanceTable( n );

		// Build initial reduced matrix and compute the root lower bound
		double[] rootMatrix = (double[])dist.Clone();
		double rootBound = ReduceMatrix( rootMatrix, n );

		// Depth-first search with best-bound child ordering.
		// Children are pushed in descending bound order so the cheapest sits on top of the stack.
		var stack = new Stack<BbNode>();
		var rootPath = new int[ n + 1 ];
		rootPath[ 0 ] = 0;
		stack.Push( new BbNode( rootBound, rootMatrix, rootPath, 1, new HashSet<int> { 0 } ) );

		int explored     = 0;
		int improvements = 0;
		int pruned       = 0;
		int maxNodes     = bbSettings.MaxNodes > 0 ? bbSettings.MaxNodes : int.MaxValue; // unlimited by default

		while( stack.Count > 0 && explored < maxNodes )
		{
			var node = stack.Pop();
			++explored;

			// Progress report every 10,000 nodes
			if( explored % 10_000 == 0 )
				Debug.WriteLine( $"Progress: explored={explored}, stack={stack.Count}, upperBound={upperBound:F4}, improvements={improvements}, pruned={pruned}" );

			// Prune: this node's lower bound already meets or exceeds the best known tour
			if( node.Bound + MARGIN >= upperBound )
			{
				++pruned;
				continue;
			}

			int pathLen     = node.PathLen;
			int currentCity = node.Path[ pathLen - 1 ];

			// All n cities visited — close the tour back to city 0
			if( pathLen == n )
			{
				if( dist[ currentCity * n ] >= INF ) continue; // no return edge

				var completePath = new List<int>( n + 1 );
				for( int k = 0; k < pathLen; k++ ) completePath.Add( node.Path[ k ] );
				completePath.Add( 0 );

				// Re-compute from original distances, then apply 2-opt to tighten the upper bound quickly
				double tourCost = ComputeTourCost( completePath, dist, n );
				if( tourCost + MARGIN < upperBound )
				{
					var improved = base.Local2OptSearch( completePath );
					if( improved.Tour + MARGIN < upperBound )
					{
						upperBound = improved.Tour;
						++improvements;
						best = improved;
						Draw( best.Tour, explored, best.Path );
						Debug.WriteLine( $"*** NEW BEST: {improved.Tour:F4} (raw={tourCost:F4}, explored={explored})" );
					}
				}
				continue;
			}

			// Branch: evaluate all unvisited cities and collect viable children
			var children = new List<(double Bound, double[] Matrix, int City)>( n - pathLen );

			for( int next = 0; next < n; next++ )
			{
				if( node.Visited.Contains( next ) ) continue; // already in partial path

				double reducedEdgeCost = node.Matrix[ currentCity * n + next ];
				if( reducedEdgeCost >= INF ) continue; // edge not available

				double[] childMatrix = (double[])node.Matrix.Clone();
				bool     isLastEdge  = pathLen + 1 == n;
				ApplyEdge( childMatrix, n, currentCity, next, isLastEdge );
				double additionalReduction = ReduceMatrix( childMatrix, n );

				double childBound = node.Bound + reducedEdgeCost + additionalReduction;
				if( childBound + MARGIN >= upperBound ) continue; // prune

				children.Add( (childBound, childMatrix, next) );
			}

			// Push in descending bound order so the child with the lowest bound is on top
			children.Sort( ( a, b ) => b.Bound.CompareTo( a.Bound ) );

			foreach( var (childBound, childMatrix, next) in children )
			{
				var childPath        = (int[])node.Path.Clone();
				childPath[ pathLen ] = next;
				var childVisited     = new HashSet<int>( node.Visited ) { next };
				stack.Push( new BbNode( childBound, childMatrix, childPath, pathLen + 1, childVisited ) );
			}
		}

		Debug.WriteLine( $"B&B complete: explored={explored}, improvements={improvements}, pruned={pruned}, best={best.Tour:F4}, status={(explored >= maxNodes ? "BUDGET_EXHAUSTED" : "OPTIMAL")}" );
		return best;
	}

	#endregion


	#region Matrix Reduction ---------------------------------------------------

	/// <summary>
	/// Reduces the n×n cost matrix in-place: for every row subtract its minimum, then for
	/// every column subtract its minimum. Returns the total amount subtracted, which is a
	/// valid lower bound contribution (every Hamiltonian tour must pay at least this much).
	/// Rows or columns whose every entry is ∞ (no outgoing/incoming edge) contribute 0.
	/// </summary>
	static double ReduceMatrix( double[] m, int n )
	{		
		double total = 0.0;
				
		for( int i = 0; i < n; i++ ) // Row reduction
		{
			double rowMin = INF;
			for( int j = 0; j < n; j++ ) // Find the minimum value in this row
			{ 
				double v = m[ i * n + j ]; 
				if( v < rowMin ) rowMin = v; 
			}

			if( rowMin > 0.0 && rowMin < INF ) // Subtract the minimum from every entry in this row
			{
				total += rowMin;
				for( int j = 0; j < n; j++ ) 
				{ 
					double v = m[ i * n + j ]; 
					if( v < INF ) m[ i * n + j ] = v - rowMin; 
				}
			}
		}
				
		for( int j = 0; j < n; j++ ) // Column reduction
		{
			double colMin = INF;
			for( int i = 0; i < n; i++ ) // Find the minimum value in this column
			{ 
				double v = m[ i * n + j ]; 
				if( v < colMin ) colMin = v; 
			}

			if( colMin > 0.0 && colMin < INF ) // Subtract the minimum from every entry in this column
			{
				total += colMin;
				for( int i = 0; i < n; i++ ) // Subtract the minimum from every entry in this column
				{ 
					double v = m[ i * n + j ]; 
					if( v < INF ) m[ i * n + j ] = v - colMin; 
				}
			}
		}

		return total;
	}

	/// <summary>
	/// Updates the cost matrix when edge (from → to) is committed to the partial tour:
	/// <list type="bullet">
	///   <item>Sets row[from] entirely to ∞ — from has been departed and cannot be left again.</item>
	///   <item>Sets col[to]   entirely to ∞ — to has been entered and cannot be entered again.</item>
	///   <item>Sets matrix[to, 0] to ∞ unless <paramref name="isLastEdge"/> is <see langword="true"/>,
	///         preventing a sub-tour that would close back to city 0 before all cities are visited.</item>
	/// </list>
	/// </summary>
	static void ApplyEdge( double[] m, int n, int from, int to, bool isLastEdge )
	{
		for( int k = 0; k < n; k++ )
		{
			m[ from * n + k ] = INF; // forbid leaving 'from' again
			m[ k * n + to   ] = INF; // forbid entering 'to' again
		}
		
		if( !isLastEdge ) m[ to * n ] = INF; // Prevent sub-tour: forbid closing back to city 0 prematurely
	}

	#endregion


	#region Distance Table / Tour Cost ----------------------------------------

	/// <summary>
	/// Builds a flat n×n distance lookup table from the map's original edge weights.
	/// Diagonal entries (self-loops) are set to ∞.
	/// </summary>
	double[] BuildDistanceTable( int n )
	{
		double[] dist = new double[ n * n ];
		for( int i = 0; i < n; i++ )
		{
			for( int j = 0; j < n; j++ )
			{
				if( i == j ) { dist[ i * n + j ] = double.MaxValue / 2; continue; }
				double weight = base.map[ i, j ].Weight;
				if( !double.IsFinite( weight ) || weight < 0 )
					throw new ArgumentException( $"Invalid edge weight between cities {i} and {j}: {weight}." );
				dist[ i * n + j ] = weight;
			}
		}

		return dist;
	}

	/// <summary>
	/// Computes the total tour cost from the original (unreduced) distance table.
	/// </summary>
	static double ComputeTourCost( List<int> path, double[] dist, int n )
	{
		double cost = 0.0;
		for( int i = 0; i < path.Count - 1; i++ )
			cost += dist[ path[ i ] * n + path[ i + 1 ] ];
		
		return cost;
	}

	#endregion

}


/// <summary>
/// Configuration settings for the Branch and Bound algorithm.
/// </summary>
class BbSettings : TspConfigurationBase
{
	/// <summary>
	/// Maximum number of B&amp;B nodes to explore per epoch.
	/// Larger values yield better (or optimal) solutions at the cost of more computation.
	/// </summary>
	public int MaxNodes { get; set; }
}
