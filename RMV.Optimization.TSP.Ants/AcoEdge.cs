namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// Edge values for ACO
/// </summary>
public class AcoEdge( int head, int tail, double weight, double pheromone, AcoSettings settings ) 
{

	#region Properties ---------------------------------------------------------

	public int Head { get; init; } = head;
	public int Tail { get; init; } = tail;
	public double Weight { get; init; } = weight;
	public double Pheromone { get; set; } = pheromone;

	readonly AcoSettings settings = settings;
	double initPheromone = pheromone;

	public double Chance => this.Pheromone * Math.Pow( 1.0 / this.Weight, this.settings.Beta );

	#endregion


	#region Update -------------------------------------------------------------

	/// <summary>
	/// Local update pheromone (ACS)
	/// </summary>	
	public void Update() => this.Pheromone = ( 1.0 - settings.P2 ) * this.Pheromone + settings.P2 * initPheromone;

	#endregion


	#region Evaporate ----------------------------------------------------------

	/// <summary>
	/// Evaporate pheromone (AS)
	/// </summary>
	//public void Evaporate() => this.Pheromone *= ( 1.0 - this.settings.Rho );
	public void Evaporate() => this.Pheromone = Math.Max( initPheromone, this.Pheromone * ( 1.0 - this.settings.Rho ) );

	#endregion


	#region Reset --------------------------------------------------------------

	/// <summary>
	/// Reset pheromone (MMA)
	/// </summary>	
	public void Reset( double amount ) => this.Pheromone = amount;

	#endregion


	#region Misc ---------------------------------------------------------------

	public override string ToString() => $"d={Weight:0.00} p={Pheromone:0.000} c={Chance:0.000}";

	//public double Pheromone
	//{
	//	get => this.pheromone;
	//	set => this.pheromone = Math.Max( initPheromone, value );
	//}
	//double pheromone = pheromone;

	#endregion

}
