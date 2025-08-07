namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP helper extention methods
/// </summary>
public static class Extenders
{
	/// <summary>
	/// Determines if Path contains Edge
	/// </summary>	
	public static bool Contains( this TspEdge edge, IList<int> path ) => path.Intersect( [ edge.Head, edge.Tail ] ).Count() == 2;
}
