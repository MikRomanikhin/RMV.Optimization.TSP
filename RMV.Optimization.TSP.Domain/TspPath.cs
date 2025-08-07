namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// Single Path 
///</summary>
public class TspPath : List<int>
{	
	//public int ID { get; set; }
	public double Tour { get; set; }		
	public bool IsVisited( int node ) => this.Contains( node );
}

#region obsolete
//public class TspPath// : List<int>
//{
//	public TspPath( int cities )
//	{
//		this.Path = new int[ cities ];
//		//this.Visited = new bool[ cities ];
//	}

//	int position = 0;

//	//public int ID { get; set; }
//	public double Tour { get; set; }

//	public int[] Path { get; set; }
//	//public bool[] Visited { get; set; }

//	public void Add( int node )
//	{		
//		this.Path[ position ] = node;
//		//this.Visited[ position++ ] = true;
//	}
//	public bool IsVisited( int node ) => this.Path.Contains( node );	
//}
#endregion

///<summary>
/// Path for K-Nearest
///</summary>
public class TspPaths : List<TspPath>
{
	//public new TspPath this[ int id ] => base.Find( p => p.ID == id );

	//public TspPath GetShortest => base..Order( p => p. )

}
