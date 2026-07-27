using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Differential Evolution algorithm for TSP
/// </summary>
public class DifferentialEvolution( TspMap map ) : TspAlgorithmBase( map )
{	
	DeSettings settings;
	List<TspResult> population = [];

	/// <summary>
	/// Configures the algorithm settings.
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<DeSettings>( "differential" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the population with random tours and seeds one top-tier solution
	/// </summary>
	protected override TspResult? Initialize()
	{
		population = base.InitializePopulation( this.settings.Size );

		// Seed one top-tier solution so DE has something to pull towards
		population[ 0 ] = base.BuildNearestTour();

		return population.MinBy( r => r.Tour )?.Clone() 
			?? throw new InvalidOperationException( "Failed to initialize population - population is empty" );
	}

	/// <summary>
	/// Runs a single epoch of the algorithm
	/// </summary>
	/// <param name="best">The best solution found so far</param>
	/// <returns>The best solution found in this epoch</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		List<TspResult> newPopulation = new( this.settings.Size );

		// Track tour lengths to detect duplicates efficiently (O(1) instead of O(n))
		var tourSet = new HashSet<double>();

		// Process target mutations in parallel to speed up large populations
		object lockObj = new();

		Parallel.ForEach( population, target => 
		{			
			var mutant = Mutate( population, target );  // Mutate to create trial vector

			// DE crossover (binomial)
			var trialPath = CrossoverDE( target.Path, mutant.Path );
			var trial = TspResult.Build( base.map, trialPath );

			trial = ParallelLocalSearch( trial.Path ); // Local search the trial to smooth combinatorial edges

			lock( lockObj )
			{
				Update( newPopulation, tourSet, trial < target ? trial : target );
			}
		} );

		population = CleanDuplicates( newPopulation ); // Clean up duplicates if the population starts to converge/stagnate

		return population.MinBy( r => r.Tour )!; // Return the best solution from the current population
	}


	/// <summary>
	/// Generates a mutant vector using three random donor tours In discrete TSP, a widely accepted
	/// approximation of X1 + F(X2 - X3) is combinatorial path relinking or ordered insertion.
	/// </summary>	
	TspResult Mutate( List<TspResult> pop, TspResult target )
	{
		int size = pop.Count;

		// Get three unique indices, distinct from target if possible
		var points = IRandomSequence.GetUniqueInts( 3, 0, size - 1 );

		var donor1 = pop[ points[ 0 ] ];
		var donor2 = pop[ points[ 1 ] ];
		var donor3 = pop[ points[ 2 ] ];

		// Discrete mutant via greedy edge/position assembly
		var mutantPath = DiscreteMutation( donor1.Path, donor2.Path, donor3.Path );

		return TspResult.Build( base.map, mutantPath );
	}

	/// <summary>
	/// Approximates A + F * (B - C) for permutations by blending donor1 with features common to donor2 and donor3.
	/// For discrete TSP, Factor acts as a probability of incorporating genes from donors B/C rather than a scaling factor.
	/// </summary>
	List<int> DiscreteMutation( IList<int> donor1, IList<int> donor2, IList<int> donor3 )
	{
		int length = donor1.Count;
		var mutant = new int[ length ];
		var added = new HashSet<int>();

		for( int i = 0; i < length; i++ )
		{
			// Factor controls whether we take from donor1 (Base) or inject features from (B-C)
			// In discrete space, this is a probability rather than a continuous scaling factor
			if( Random.Shared.NextDouble() < settings.Factor )
			{
				// Attempt to pull a city from B or C that isn't already used
				int candidate = donor2[ i ];
				if( !added.Contains( candidate ) )
				{
					mutant[ i ] = candidate;
					added.Add( candidate );
					continue;
				}

				candidate = donor3[ i ];
				if( !added.Contains( candidate ) )
				{
					mutant[ i ] = candidate;
					added.Add( candidate );
					continue;
				}
			}

			// Base vector donor1 fallback
			if( !added.Contains( donor1[ i ] ) )
			{
				mutant[ i ] = donor1[ i ];
				added.Add( donor1[ i ] );
			}
			else
			{				
				mutant[ i ] = -1; // Mark as -1 to be repaired
			}
		}

		// Repair missing cities (filling holes with defensive bounds checking)
		var missing = donor1.Where( c => !added.Contains( c ) ).ToList();

		int missingIdx = 0;

		for( int i = 0; i < length; i++ )
		{
			if( mutant[ i ] == -1 )
			{
				// Defensive bounds check - should not happen with correct logic, but prevents crashes
				mutant[ i ] = missingIdx < missing.Count ? missing[ missingIdx++ ] : donor1[ i ];
			}
		}

		return [ .. mutant ];
	}

	/// <summary>
	/// Binomial Crossover: mixes target with mutant based on Crossover Rate (CR) ensuring the result is valid.
	/// </summary>
	List<int> CrossoverDE( IList<int> target, IList<int> mutant )
	{
		int length = target.Count;
		var trial = new int[ length ];
		var used = new HashSet<int>();

		// Guarantee at least one element comes from the mutant
		int fixedIndex = Random.Shared.Next( length );

		for( int i = 0; i < length; i++ )
		{
			if( i == fixedIndex || Random.Shared.NextDouble() < settings.Rate )
			{
				if( !used.Contains( mutant[ i ] ) )
				{
					trial[ i ] = mutant[ i ];
					used.Add( mutant[ i ] );
				}
				else
				{
					trial[ i ] = -1;
				}
			}
			else
			{
				if( !used.Contains( target[ i ] ) )
				{
					trial[ i ] = target[ i ];
					used.Add( target[ i ] );
				}
				else
				{
					trial[ i ] = -1;
				}
			}
		}

		// Repair with defensive bounds checking
		var missing = target.Where( c => !used.Contains( c ) ).ToList();
		int missingIdx = 0;

		for( int i = 0; i < length; i++ )
		{
			if( trial[ i ] == -1 )
			{
				// Defensive bounds check to prevent index out of range
				trial[ i ] = missingIdx < missing.Count ? missing[ missingIdx++ ] : target[ i ];
			}
		}

		return [ .. trial ];
	}

	/// <summary>
	/// Update population and prevent exact duplicates from dominating the pool
	/// Uses HashSet for O(1) duplicate detection instead of O(n) linear search
	/// </summary>	
	void Update( List<TspResult> nextGen, HashSet<double> tourSet, TspResult current )
	{
		// Round to 4 decimal places to handle floating-point precision
		double roundedTour = Math.Round( current.Tour, 4 );

		if( tourSet.Add( roundedTour ) )
			nextGen.Add( current );
		else
			nextGen.Add( base.InitializeTour() ); // inject diversity 
	}


	/// <summary>
	/// Replace duplicates in the population by random tour
	///</summary>
	List<TspResult> CleanDuplicates( List<TspResult> population )
	{
		int count = population.Count;

		var result = population.Distinct().OrderBy( c => c.Tour ).ToList();

		while( result.Count < count ) result.Add( this.map.BuildRandomTour() );

		return result;
	}

}

/// <summary>
/// Configuration settings for Differential Evolution algorithm
/// </summary>
public class DeSettings : BeamSettings
{
	/// <summary>
	/// Mutation factor (F) - in discrete TSP, acts as probability of taking genes from donor vectors.
	/// Typical range: 0.4 to 1.0
	/// </summary>
	public double Factor { get; set; } = 0.8;

	/// <summary>
	/// Crossover rate (CR) - probability of inheriting a gene from the mutant vector during crossover.
	/// Typical range: 0.7 to 1.0
	/// </summary>
	public double Rate { get; set; } = 0.9;
}
