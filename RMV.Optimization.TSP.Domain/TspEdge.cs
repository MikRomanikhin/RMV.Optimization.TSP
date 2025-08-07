namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP map Edge
/// </summary>
public class TspEdge( int head, int tail, double weight ) : IEquatable<TspEdge>
{
	public int Head { get; set; } = head; 
	public int Tail { get; set; } = tail;
	
	public int Next { get; set; }

	public double Weight { get; set; } = weight;

	public double Penalty { get; set; }

	//public bool Visited { get; set; }
	//public bool IsAvailable => this.Visited == false;
	
	public void SetNext( int node ) => this.Next = this.Head == node ? this.Tail : this.Head;
	public int GetNext( int node ) => this.Head == node ? this.Tail : this.Head;

	public bool Contains( int node ) => this.Head == node || this.Tail == node;

	//public bool ContainsAll( IList<int> path ) => path.Contains( this.Head ) && path.Contains( this.Tail );	

	public override string ToString() => $"{Head}-{Tail} weight:{Weight:0.00}";

	public bool Equals( TspEdge? other ) => other is not null &&
		( ( this.Head == other.Head && this.Tail == other.Tail ) || ( this.Head == other.Tail && this.Tail == other.Head ) );	
}



/// <summary>
/// Edges collection
/// </summary>
public class TspEdges : Dictionary<(int,int),TspEdge>
{
	/// <summary>
	/// Search edge by head and tail
	/// </summary>		
	public TspEdge this[ int head, int tail ] => base.ContainsKey( (head, tail) ) ? base[ (head, tail) ] : base[ (tail, head) ];	
}

//public class Edges : HashSet<TspEdge>
//{
//	/// <summary>
//	/// Search edge by head and tail
//	/// </summary>		
//	public TspEdge this[ int head, int tail ] => base.TryGetValue( e => e[head, tail] ) ? base[ (head, tail) ] : base[ (tail, head) ];
//}
