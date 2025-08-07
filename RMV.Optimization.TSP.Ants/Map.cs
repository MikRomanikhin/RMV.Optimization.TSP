
namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// Graph for solving TSP
/// </summary>
//public class Map
//{

//	#region Properties --------------------------------------------------------

//	public string Name { get; set; } = string.Empty;

//	public string Comment { get; set; } = string.Empty ;

//	public Method Method { get; set; } = Method.AntColonySystem;

//	public int Cities { get; set; }	

//	/// <summary>
//	/// Cities collection
//	/// </summary>
//	public List<Node> Nodes { get; set; } = [];

//	/// <summary>
//	/// Edges dictionary
//	/// </summary>
//	public Edges Edges = [];	
//	//public Dictionary<(int, int), AcoValues> Edges = [];	

//	/// <summary>
//	/// Nearest Neibour Tour
//	/// </summary>
//	public int NearestTour { get; set; }

//	/// <summary>
//	/// TSP path
//	/// </summary>
//	public IEnumerable<int> Path { get; private set; } = [];
	

//	#endregion


//	#region Initialize --------------------------------------------------------		

//	/// <summary>
//	/// Create Edges from Nodes
//	/// </summary>
//	public virtual void Initialize()
//	{
//		this.Cities = this.Nodes.Count;		

//		this.Edges = [];

//		for( int i = 0; i < this.Cities; i++ )
//		{
//			var from = this.Nodes[ i ];

//			for( int j = i + 1; j < this.Cities; j++ )
//			{				
//				var to = this.Nodes[ j ];
				
//				double dist = from.DistanceTo( to );

//				this.Edges.Add( (from.ID, to.ID), new Values { Distance = dist, Head = from.ID, Tail = to.ID  } );
//				this.Edges.Add( (to.ID, from.ID), new Values { Distance = dist, Head = to.ID, Tail = from.ID  } );
//			}
//		}		
//	}

//	#endregion


//	#region Nearest Neighbor --------------------------------------------------

//	/// <summary>
//	/// Nearest Neghbour tour and path
//	/// </summary>
//	public (double, int[]) GetNearestTour()
//	{
//		double tour = int.MaxValue;
//		var minpath = new int[ this.Cities ];

//		for( int city = 0; city < this.Cities; city++ )
//		{
//			(double tmp, int[] path) = GetNearest( city );

//			if( tmp < tour )
//			{
//				tour = tmp;
//				Array.Copy( path, minpath, this.Cities );	
//			}

//			tour = Math.Min( tour, tmp );
//		}

//		return (tour, minpath);
//	}

//	/// <summary>
//	/// Nearest Neghbour tour and path
//	/// </summary>
//	/// <param name="start">starting node</param>
//	/// <returns>tuple containing tour and path</returns>
//	(double, int[]) GetNearest( int start )
//	{
//		List<int> path = [ start ];

//		bool[] visited = new bool[ this.Cities ];
//		visited[ start ] = true;

//		double sum = 0;
//		int next = start;

//		for( int from = 0; from < this.Cities; from++ )
//		{
//			var edge = this.Edges.Where( e => e.Value.Head == next && visited[ e.Value.Tail ] == false )
//				.OrderBy( e => e.Value.Distance ).FirstOrDefault();   //.MinBy( e => e.Value.Distance );

//			if( edge.Value == null ) //last city
//			{
//				sum += this.Edges[ (next, start) ].Distance;

//				return (sum, path.ToArray());
//			}

//			sum += edge.Value.Distance;

//			next = edge.Value.Tail;

//			visited[ next ] = true;

//			path.Add( next );
//		}

//		throw new Exception( "OOPS!" );
//	}

//	#endregion


//	#region DFS ---------------------------------------------------------------

//	/// <summary>
//	/// DFS tour and path
//	/// </summary>	
//	public (double, int[]) GetDfs()
//	{
//		int current = 0;

//		bool[] visited = new bool[ this.Cities ];
//		visited[ current ] = true;

//		var path = new List<int> { current };

//		var taboo = new Dictionary<int, List<int>>();
//		for( int i = 0; i < this.Cities; i++ ) taboo[ i ] = [];

//		(double limit, _) = GetNearestTour();

//		double tour = 0;

//		DfsSearch( visited, path, taboo, ref limit, ref tour );

//		return (tour, path.ToArray());
//	}


//	/// <summary>
//	/// DP search
//	/// </summary>	
//	public void DfsSearch( bool[] visited, List<int> path, Dictionary<int, List<int>> taboo, ref double limit, ref double tour )
//	{
//		int current = path.Last(); //current node

//		var edges = this.Edges.Where( e => e.Value.Head == current && visited[ e.Value.Tail ] == false );// && !taboo[ current ].Contains( e.Value.Tail ) );

//		edges = edges.Where( e => !taboo[ current ].Contains( e.Value.Tail ) );	//filter adj nodes	

//		if( !edges.Any() ) //no more adj nodes
//		{
//			if( path.Count < this.Cities ) //tour is not completed, remove the last node
//			{
//				tour = RemoveNode( visited, path, taboo, tour, current );
//			}
//			else //last node - complete the tour
//			{
//				tour += this.Edges[ (current, 0) ].Distance;	//if( tour < limit ) limit = tour;
//			}
//		}
//		else //nodes to try 
//		{
//			foreach( var edge in edges ) //evaluate each adj node
//			{
//				int tail = edge.Value.Tail;	

//				tour += edge.Value.Distance;

//				if( tour > limit ) //bad node, 
//				{
//					tour -= edge.Value.Distance; //rollback tour

//					taboo[ current ].Add( tail ); //include in the taboo

//					if( !edges.Any() ) //all nodes are bad, remove current				
//					{
//						tour = RemoveNode( visited, path, taboo, tour, current );
//					}
//				}
//				else //good node, update path
//				{
//					path.Add( edge.Value.Tail );

//					taboo[ path[ ^1 ] ].AddRange( taboo[ path[ ^2 ] ] );
										
//					if( path.Count == this.Cities - 1 && tour < limit )
//						limit = tour;

//					visited[ tail ] = true;

//					DfsSearch( visited, path, taboo, ref limit, ref tour );
//				}
//			} //tour = RemoveNode( visited, path, taboo, tour, current );
//		}
//	}

//	/// <summary>
//	/// Remove parent node and update path and visited arrays
//	/// </summary>	
//	double RemoveNode( bool[] visited, List<int> path, Dictionary<int, List<int>> taboo, double tour, int current )
//	{
//		int last = path.Last(); //node to remove

//		visited[ last ] = true;

//		path.Remove( last );

//		taboo[ last ].Clear();

//		if( last < this.Cities - 1 ) //un-mark 
//		{
//			for( int i = last + 1; i < this.Cities; i++ )
//			{
//				visited[ i ] = false;
//			}
//		}

//		tour -= this.Edges[ (path.Last(), current) ].Distance;		

//		return tour;
//	}

//	#endregion
//}
