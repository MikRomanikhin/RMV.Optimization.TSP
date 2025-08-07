namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP map Node
/// </summary>
/// <param name="x">x coordinate.</param>
/// <param name="y">y coordinate.</param>
/// <param name="n">city ID</param>
public class TspNode( double x, double y, int n )
{
	public int ID { get; set; } = n; // city ID

	public double X { get; set; } = x; // X coordinate												
	public double Y { get; set; } = y; // Y coordinate		

	public double DistanceTo( TspNode that ) //=> Math.Sqrt( Math.Pow( that.X - this.X, 2 ) + Math.Pow( that.Y - this.Y, 2 ) );	
	{
		double x = Math.Abs( this.X - that.X );
		double y = Math.Abs( this.Y - that.Y );

		return Math.Sqrt( ( x * x ) + ( y * y ) );
	}
}

/// <summary>
/// TSP nodes collection
/// </summary>
public class TspNodes : List<TspNode>
{
	//readonly List<TspNode> nodes = [];
	//public int Count => this.nodes.Count;
	//public void Add( TspNode node ) => this.nodes.Add( node );
	//public void Reset() => base.ForEach( n => n.Visited = false );
	//public TspNode this[ int id ] => base.Find( n => n.ID == id );

	public double MinX => this.Min( n => n.X );
	public double MinY => this.Min( n => n.Y );
	public double MaxX => this.Max( n => n.X );
	public double MaxY => this.Max( n => n.Y );
}
