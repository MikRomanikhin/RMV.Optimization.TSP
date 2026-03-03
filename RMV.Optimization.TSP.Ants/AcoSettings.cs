using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.ACO;

public class AcoSettings : TspConfigurationBase
{
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Colony size
	/// </summary>
	public int Size { get; set; }

	/// <summary>
	/// Elite level value
	/// </summary>
	public int Elite { get; set; }

	public double Alpha { get; set; } = 1;
	public double Beta { get; set; }	

	/// <summary>
	/// Evaporation intensity
	/// </summary>
	public double Rho { get; set; }

	/// <summary>
	/// ACS local update parameter
	/// </summary>
	public double P2 { get; set; }

	/// <summary>
	/// ACS global update parameter
	/// </summary>
	public double P1 { get; set; }

	/// <summary>
	/// Deposit constant AS
	/// </summary>
	public double Q { get; set; }

	/// <summary>
	/// Total cities
	/// </summary>
	//public int Cities { get; set; }

	/// <summary>
	/// Nearest neighbour tour
	/// </summary>
	public double Nearest { get; set; }
	
	public int Neighbours { get; set; }

	/// <summary>
	/// ACS greedy selection bound
	/// </summary>
	public double Greedy { get; set; }

	/// <summary>
	/// MMAS parameter
	/// </summary>
	public double P { get; set; }

	/// <summary>
	/// Stagnation limit
	/// </summary>
	public int Stagnation { get; set; }	
}
