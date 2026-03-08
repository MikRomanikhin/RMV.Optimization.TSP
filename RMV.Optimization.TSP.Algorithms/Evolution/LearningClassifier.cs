using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Learning Classifier algorithm for TSP
/// </summary>
public class LearningClassifier( TspMap map ) : TspAlgorithmBase( map )
{	
	BeamSettings settings;
	List<TspResult> population = [];
	List<TspResult> nextPopulation = []; // Pre-allocated buffer to reduce GC pressure

	/// <summary>
	/// Configures algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "classifier" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}


	/// <summary>
	/// Initializes the population and returns a clone of the best initial solution found.
	/// </summary>
	protected override TspResult? Initialize()
	{
		population = base.InitializePopulation( this.settings.Size );
		nextPopulation = new List<TspResult>( this.settings.Size );

		return population.MinBy( r => r.Tour )!.Clone();
	}

	/// <summary>
	/// Performs a single optimization epoch, evolving the current population and applying local search to improve
	/// </summary>
	protected override TspResult RunEpoch( TspResult best )
	{
		Evolve();

		var result = population.MinBy( i => i.Tour )!; // best solution in the current iteration

		return ParallelLocalSearch( result.Path ); // apply local search to improve the best solution
	}

	/// <summary>
	/// Evolves the population using a simple genetic algorithm approach with Elitism
	/// </summary>	
	void Evolve()
	{
		nextPopulation.Clear();

		// 1. Elitism: preserve the absolute best individual without mutating it
		var currentBest = population.MinBy( p => p.Tour )!;
		nextPopulation.Add( currentBest );

		// 2. Fill the rest of the population
		while( nextPopulation.Count < population.Count )
		{
			var parent1 = Select( population );
			var parent2 = Select( population );

			var child = base.Crossover( parent1, parent2 );
			var mutated = RandomSwap( child );

			// Keep the best between the raw child and mutated child
			nextPopulation.Add( mutated < child ? mutated : child );
		}

		// 3. Swap buffers to avoid allocating a new list next epoch
		(population, nextPopulation) = (nextPopulation, population);
	}
	

	static TspResult Select( List<TspResult> population ) => RouletteWheelSelection( population );

}

