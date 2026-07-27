using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolutionary Programming algorithm for TSP
/// </summary>
/// <param name="map"></param>
public class EvolutionaryProgramming( TspMap map ) : TspAlgorithmBase( map )
{	
	EpSettings settings;
	List<TspResult> population = [];

	/// <summary>
	/// Configures the algorithm settings.
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<EpSettings>( "ep" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the population and returns the best initial solution found.
	/// </summary>
	/// <remarks>Method selects the best result from the newly initialized population based on tour length.</remarks>
	/// <returns>TspResult representing the initial solution with the shortest tour in the population.</returns>
	protected override TspResult Initialize()
	{
		this.population = base.InitializePopulation( this.settings.Size );

		return this.population.MinBy( r => r.Tour );
	}

	/// <summary>
	/// Runs a single epoch of the algorithm
	/// </summary>
	/// <param name="best">The best solution found so far</param>
	/// <returns>The best solution found in this epoch</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		this.population = Evolve( this.population, this.settings );

		Reinsert( this.population ); // reinsert random tours to keep population size constant

		var result = this.population.MinBy( i => i.Tour ) 
			?? throw new InvalidOperationException( "Population became empty during evolution" );

		return ParallelLocalSearch( result.Path ); // apply local search to improve the best solution
	}	


	/// <summary>
	/// Evolves the population by mutating each individual with diverse mutation operators
	/// </summary>	
	List<TspResult> Evolve( List<TspResult> population, EpSettings settings ) =>
		[ .. population.Select( i => base.Mutate( i, settings.Rate ) ).OrderBy( i => i.Tour ).Take( settings.Take ) ];
	

	/// <summary>
	/// Reinsert random tours to keep population size constant
	/// </summary>	
	void Reinsert( List<TspResult> population )
	{
		int count = population.Count;

		// Fixed: loop should go to settings.Size, not Size - count
		for( int i = count; i < settings.Size; i++ )
		{
			population.Add( base.InitializeTour() );
		}		
	}

}

/// <summary>
/// Configuration settings for Evolutionary Programming algorithm
/// </summary>
public class EpSettings : BeamSettings
{
	/// <summary>
	/// Number of best individuals to select after mutation (survival selection).
	/// Must be less than Size. Remaining slots are filled with random tours for diversity.
	/// Typical range: 10-30% of Size (e.g., Take=20 for Size=100)
	/// </summary>
	public int Take { get; set; } = 10;

	public double Rate { get; set; } = 0.1; // Mutation rate (probability of mutation)
}
