using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolutionary Programming algorithm for TSP
/// </summary>
/// <param name="map"></param>
public class EvolutionaryProgramming( TspMap map ) : TspAlgorithmBase( map )
{	
	EpSettings settings;
	List<TspResult> population = [];

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<EpSettings>( "ep" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult Initialize()
	{
		this.population = base.InitializePopulation( this.settings.Size );

		return this.population.MinBy( r => r.Tour );
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		population = Evolve( population, settings.Take );

		Reinsert( population ); // reinsert random tours to keep population size constant

		var result = population.MinBy( i => i.Tour ); // best solution in the current iteration

		return ParallelLocalSearch( result.Path ); // apply local search to improve the best solution
	}


	/// <summary>
	/// Evolves the population by mutating each individual
	/// </summary>	
	List<TspResult> Evolve( List<TspResult> population, int take ) =>
		[ .. population.Select( i => RandomSwap( i ) ).OrderBy( i => i.Tour ).Take( take ) ];
	

	/// <summary>
	/// Reinsert random tours to keep population size constant
	/// </summary>	
	void Reinsert( List<TspResult> population )
	{
		int count = population.Count;

		for( int i = count; i < settings.Size - count; i++ )
		{
			population.Add( base.InitializeTour() );
		}		
	}	

}

public class EpSettings : BeamSettings
{
	public int Take { get; set; } = 10; // number of individuals to take for next generation
}
