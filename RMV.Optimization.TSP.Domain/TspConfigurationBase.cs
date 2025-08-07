namespace RMV.Optimization.TSP.Domain;

public class TspConfigurationBase
{
	/// <summary>
	/// Iterations to redraw
	/// </summary>
	public int Redraw { get; set; }

	/// <summary>
	/// Total iterations limit
	/// </summary>
	public int Limit { get; set; }
}
