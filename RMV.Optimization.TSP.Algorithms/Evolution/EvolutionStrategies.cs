using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolution Strategies algorithm for TSP
/// </summary>
public class EvolutionStrategies( TspMap map ) : TspAlgorithmBase( map )
{
	/// <summary>
	/// Represents an individual in the ES population with its solution and mutation strength
	/// </summary>
	record Individual( TspResult Solution, double Sigma )
	{
		/// <summary>
		/// Creates a copy of this individual
		/// </summary>
		public Individual Copy() => new( Solution.Clone(), Sigma );
	}

	EsSettings settings;
	List<Individual> population = [];

	/// <summary>
	/// Generates a standard normal (Gaussian) random number using Box-Muller transform
	/// </summary>
	static double NextGaussian()
	{
		double u1 = 1.0 - Random.Shared.NextDouble(); // Uniform(0,1] to avoid log(0)
		double u2 = Random.Shared.NextDouble();
		return Math.Sqrt( -2.0 * Math.Log( u1 ) ) * Math.Cos( 2.0 * Math.PI * u2 );
	}

	/// <summary>
	/// Configures algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<EsSettings>( "es" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the population with random tours and initial sigma values
	/// </summary>
	/// <returns>The best solution from the initial population</returns>
	protected override TspResult Initialize()
	{
		int mu = this.settings.Size / 2; // μ parents
		this.population = [];

		for( int i = 0; i < mu; i++ )
		{
			var tour = base.InitializeTour();
			var individual = new Individual( tour, this.settings.InitialSigma );
			this.population.Add( individual );
		}

		return this.population.MinBy( ind => ind.Solution.Tour )!.Solution.Clone();
	}

	/// <summary>
	/// Recombines two parent individuals to create an offspring
	/// </summary>
	/// <param name="parent1">First parent</param>
	/// <param name="parent2">Second parent</param>
	/// <returns>Offspring individual with recombined tour and intermediate sigma</returns>
	Individual Recombine( Individual parent1, Individual parent2 )
	{
		// For TSP: use crossover from base class for tour recombination
		var offspringSolution = base.Crossover( parent1.Solution, parent2.Solution );

		// For sigma: use intermediate recombination (average of parent sigmas)
		double offspringSigma = ( parent1.Sigma + parent2.Sigma ) / 2.0;

		return new Individual( offspringSolution, offspringSigma );
	}

	/// <summary>
	/// Applies self-adaptive mutation to an individual
	/// </summary>
	/// <param name="individual">The individual to mutate</param>
	/// <returns>Mutated individual with updated sigma and solution</returns>
	Individual SelfAdaptiveMutate( Individual individual )
	{
		int n = this.Cities;

		// Learning rate: τ = 1/√(2n) as per standard ES
		double tau = 1.0 / Math.Sqrt( 2.0 * n );

		// Mutate sigma first (self-adaptation)
		double newSigma = individual.Sigma * Math.Exp( tau * NextGaussian() );

		// Clamp sigma to valid range
		newSigma = Math.Clamp( newSigma, this.settings.MinSigma, this.settings.MaxSigma );

		// Apply mutation to the tour based on the new sigma
		// Sigma controls the number of swap mutations to apply
		int numSwaps = Math.Max( 1, (int)Math.Round( newSigma * 2 ) );

		var mutatedPath = new List<int>( individual.Solution.Path );
		for( int i = 0; i < numSwaps; i++ )
		{
			// Perform random swap
			var (idx1, idx2) = IRandomSequence.GetPairInts( 0, mutatedPath.Count - 1 );
			(mutatedPath[ idx1 ], mutatedPath[ idx2 ]) = (mutatedPath[ idx2 ], mutatedPath[ idx1 ]);
		}

		var mutatedSolution = TspResult.Build( this.map, mutatedPath );
		return new Individual( mutatedSolution, newSigma );
	}

	/// <summary>
	/// Generates λ offspring from the current population using recombination and mutation
	/// </summary>
	/// <param name="lambda">Number of offspring to generate</param>
	/// <returns>List of offspring individuals</returns>
	List<Individual> GenerateOffspring( int lambda )
	{
		var offspring = new System.Collections.Concurrent.ConcurrentBag<Individual>();

		Parallel.For( 0, lambda, _ =>
		{
			Individual child;

			// Decide whether to apply recombination
			if( Random.Shared.NextDouble() < this.settings.RecombinationRate && this.population.Count >= 2 )
			{
				// Select two random parents
				int idx1 = Random.Shared.Next( this.population.Count );
				int idx2 = Random.Shared.Next( this.population.Count );
				while( idx2 == idx1 && this.population.Count > 1 )
				{
					idx2 = Random.Shared.Next( this.population.Count );
				}

				var parent1 = this.population[ idx1 ];
				var parent2 = this.population[ idx2 ];

				// Recombine parents to create offspring
				child = Recombine( parent1, parent2 );
			}
			else
			{
				// Clone a random parent
				var parent = this.population[ Random.Shared.Next( this.population.Count ) ];
				child = parent.Copy();
			}

			// Apply self-adaptive mutation
			child = SelfAdaptiveMutate( child );

			// Apply local search to improve the offspring
			var improvedSolution = ParallelLocalSearch( child.Solution.Path );
			child = new Individual( improvedSolution, child.Sigma );

			offspring.Add( child );
		} );

		return [ .. offspring ];
	}

	/// <summary>
	/// Selects the best μ individuals from the combined parent and offspring pool (μ + λ selection)
	/// </summary>
	/// <param name="parents">Current parent population</param>
	/// <param name="offspring">Newly generated offspring</param>
	/// <param name="mu">Number of individuals to select</param>
	/// <returns>The best μ individuals</returns>
	List<Individual> SelectBest( List<Individual> parents, List<Individual> offspring, int mu )
	{
		// Combine parents and offspring
		var combined = new List<Individual>( parents.Count + offspring.Count );
		combined.AddRange( parents );
		combined.AddRange( offspring );

		// Sort by fitness (tour length) and select the best μ
		return [ .. combined.OrderBy( ind => ind.Solution.Tour ).Take( mu ).Select( ind => ind.Copy() ) ];
	}

	/// <summary>
	/// Performs a single Evolution Strategies epoch
	/// </summary>
	/// <param name="best">The current best solution</param>
	/// <returns>The best solution found after this epoch</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		int mu = this.settings.Size / 2;  // μ parents
		int lambda = this.settings.Size;  // λ offspring

		// Generate λ offspring from current population
		var offspring = GenerateOffspring( lambda );

		// (μ + λ) selection: select best μ from parents + offspring
		this.population = SelectBest( this.population, offspring, mu );

		// Return the best individual from the new population
		return this.population.MinBy( ind => ind.Solution.Tour )!.Solution;
	}

}

/// <summary>
/// Configuration settings for Evolution Strategies algorithm
/// </summary>
public class EsSettings : BeamSettings
{
	/// <summary>
	/// Initial mutation strength (sigma). Typical range: 0.1 to 2.0
	/// </summary>
	public double InitialSigma { get; set; } = 1.0;

	/// <summary>
	/// Minimum mutation strength to prevent premature convergence
	/// </summary>
	public double MinSigma { get; set; } = 0.01;

	/// <summary>
	/// Maximum mutation strength to prevent excessive mutations
	/// </summary>
	public double MaxSigma { get; set; } = 3.0;

	/// <summary>
	/// Probability of applying recombination (crossover) to create offspring
	/// </summary>
	public double RecombinationRate { get; set; } = 0.7;
}
