namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP map
/// </summary>
public class TspMap
{

	#region Properties ---------------------------------------------------------

	public string Name { get; set; } = string.Empty;

	public string Comment { get; set; } = string.Empty;

	public int Cities { get; set; }	

	/// <summary>
	/// Nodes collection
	/// </summary>
	public TspNodes Nodes { get; set; } = [];

	/// <summary>
	/// Edges collection
	/// </summary>
	public TspEdges Edges { get; set; } = [];

	public TspAlgorithm Algorithm { get; set; }

	/// <summary>
	/// Indexer, finds edge by head and tail nodes
	/// </summary>	
	public TspEdge this[ int head, int tail ] => this.Edges[ head, tail ];

	#endregion


	#region BuildEdges ---------------------------------------------------------

	/// <summary>
	/// Create Edges from Nodes
	/// </summary>
	public void Initialize()
	{
		this.Edges = [];

		for( int i = 0; i < this.Cities; i++ )
		{
			var from = this.Nodes[ i ];

			for( int j = i + 1; j < this.Cities; j++ )
			{
				var to = this.Nodes[ j ];

				this.Edges.Add( (from.ID, to.ID), new TspEdge( from.ID, to.ID, from.DistanceTo( to ) ) );
			}
		}
	}

	#endregion


	#region GetTourLength/BuildRandomTour/BuildRandomPopulation ----------------

	/// <summary>
	/// Calculates tour length for the given path
	/// </summary>	
	public double GetTourLength( List<int> path ) => Enumerable.Range( 0, this.Cities )//.AsParallel()
		.Sum( i => i < this.Cities - 1 ? this[ path[ i ], path[ i + 1 ] ].Weight : this[ path[ i ], path[ 0 ] ].Weight );



	/// <summary>
	/// Builds random Path and Tour
	/// </summary>	
	public TspResult BuildRandomTour()
	{
		var path = BuildRandomPath();

		double tour = GetTourLength( path );

		return new TspResult( tour, path );
	}

	/// <summary>
	/// Builds random path
	/// </summary>	
	List<int> BuildRandomPath() => [ .. Enumerable.Range( 0, this.Cities ).OrderBy( _ => Random.Shared.Next() ) ];

	/// <summary>
	/// Builds random population of tours
	/// </summary>	
	//public List<TspResult> BuildPopulation( int size ) => [ .. Enumerable.Range( 0, size ).Select( _ => BuildRandomTour() ) ];	

	#endregion


	#region obsolete
	/// <summary>
	/// Finds closest node (if any) to the node
	/// </summary>	
	//public int GetNearestNode( int node, bool[] visited )
	//{
	//	int nearest = -1;
	//	double min = double.MaxValue;
	//	for( int next = 0; next < this.Cities; next++ )
	//	{
	//		if( !visited[ next ] && this[ node, next ].Weight < min )
	//		{
	//			nearest = next;
	//			min = this[ node, next ].Weight;
	//		}
	//	}
	//	visited[ nearest ] = true;
	//	return nearest;
	//}
	//public TspEdge? GetNearestEdge( int node, IList<int> path )
	//{		
	//	var edges = this.Edges.Values.Where( e => e.IsAvailable && e.Contains( node ) );

	//	edges.Where( e => e.Contains( path ) ).Count( e => e.Visited == true );

	//	return edges.Where( e => e.IsAvailable ).MinBy( e => e.Weight );		
	//}

	/// <summary>
	/// Finds closest edge (if any) to the node
	/// </summary>
	/// <param name="node">target node</param>	
	/// <returns>closest edge or null</returns>
	//public TspEdge? GetNearestEdge( int node ) => this.Edges.GetNearest( node );

	//public List<int> GetNearest( int startCity )
	//{
	//	int n = distances.GetLength( 0 );
	//	bool[] visited = new bool[ n ];
	//	List<int> tour = new List<int> { startCity };
	//	visited[ startCity ] = true;

	//	int currentCity = startCity;

	//	for( int i = 1; i < n; i++ )
	//	{
	//		int nearestCity = -1;
	//		double shortestDistance = double.MaxValue;

	//		for( int nextCity = 0; nextCity < n; nextCity++ )
	//		{
	//			if( !visited[ nextCity ] && distances[ currentCity, nextCity ] < shortestDistance )
	//			{
	//				nearestCity = nextCity;
	//				shortestDistance = distances[ currentCity, nextCity ];
	//			}
	//		}

	//		visited[ nearestCity ] = true;
	//		tour.Add( nearestCity );
	//		currentCity = nearestCity;
	//	}

	//	// Return to the starting city to complete the tour
	//	tour.Add( startCity );

	//	return tour;
	//}

	/// <summary>
	/// Retrieves collection of not visited edges containing node
	/// </summary>
	/// <param name="node">target node</param>
	/// <param name="path">current path</param>
	/// <returns>updated path</returns>
	//public IEnumerable<TspEdge> FindEdges( int node, IList<int> path )
	//{
	//	List<TspEdge> buffer = [];

	//	var edges = this.Edges.Values.Where( e => e.IsAvailable && e.Contains( node ) );

	//	edges.Where( e => e.Contains( path ) ).Count( e => e.Visited == true );

	//	foreach( var edge in edges )
	//	{
	//		if( edge.Contains( path ) )
	//		{
	//			edge.Visited = true;
	//			continue;
	//		}

	//		edge.SetNext( node );

	//		buffer.Add( edge );
	//	}

	//	return buffer;
	//}
	//public IEnumerable<TspEdge> FindEdges( int node, IList<int> path )
	//{
	//	List<TspEdge> buffer = [];

	//	var edges = this.Edges.Values.Where( e => e.Visited == false && e.Contains( node ) );

	//	foreach( var edge in edges )
	//	{
	//		if( edge.Contains( path ) )
	//		{
	//			edge.Visited = true;
	//			continue;
	//		}

	//		edge.SetNext( node );

	//		buffer.Add( edge );
	//	}

	//	return buffer;
	//}			

	#endregion
}
