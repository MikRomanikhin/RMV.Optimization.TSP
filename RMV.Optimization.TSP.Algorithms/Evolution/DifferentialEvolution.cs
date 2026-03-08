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

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<DeSettings>( "differential" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize()
	{
		population = base.InitializePopulation( this.settings.Size );

		// Seed one top-tier solution so DE has something to pull towards
		population[ 0 ] = base.BuildNearestTour();

		return population.MinBy( r => r.Tour )!.Clone();
	}


	protected override TspResult RunEpoch( TspResult best )
	{
		List<TspResult> newPopulation = new( this.settings.Size );

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
				Update( newPopulation, trial < target ? trial : target );
			}
		} );
				
		population = CleanDuplicates( newPopulation ); // Clean up duplicates if the population starts to converge/stagnate

		return population.First(); // best solution in the current iteration
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
	/// </summary>
	List<int> DiscreteMutation( IList<int> donor1, IList<int> donor2, IList<int> donor3 )
	{
		int length = donor1.Count;
		var mutant = new int[ length ];
		var added = new HashSet<int>();

		for( int i = 0; i < length; i++ )
		{
			// F controls whether we take from donor1 (Base) or inject features from (B-C)
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

		// Repair missing cities (filling holes)
		var missing = donor1.Where( c => !added.Contains( c ) ).ToList();

		int missingIdx = 0;

		for( int i = 0; i < length; i++ )
		{
			if( mutant[ i ] == -1 )	mutant[ i ] = missing[ missingIdx++ ];			
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

		// Repair
		var missing = target.Where( c => !used.Contains( c ) ).ToList();
		int missingIdx = 0;

		for( int i = 0; i < length; i++ )
		{
			if( trial[ i ] == -1 )
			{
				trial[ i ] = missing[ missingIdx++ ];
			}
		}

		return [ .. trial ];
	}

	/// <summary>
	/// Update population and prevent exact duplicates from dominating the pool
	/// </summary>	
	void Update( List<TspResult> nextGen, TspResult current )
	{
		if( !nextGen.Exists( p => Math.Abs( p.Tour - current.Tour ) < MARGIN ) )
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
	public double Factor { get; set; } = 0.8; // Mutation factor
	public double Rate { get; set; } = 0.9; // Crossover rate
}
