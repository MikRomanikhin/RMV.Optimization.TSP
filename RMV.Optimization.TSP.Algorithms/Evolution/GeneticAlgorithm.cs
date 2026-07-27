using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Genetic Algorithm for solving the Traveling Salesman Problem (TSP)
/// </summary>
public class GeneticAlgorithm( TspMap map ) : TspAlgorithmBase( map )
{
	GeneticSettings settings;
	List<TspResult> population = [];
	List<(TspResult individual, int rank)> rankedCache = [];
	
	/// <summary>
	/// Configures the algorithm settings	
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<GeneticSettings>( "genetic" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the population and returns a clone of the best initial solution found.
	/// </summary>
	/// <remarks>
	/// The returned result is a deep copy of the best solution, ensuring that modifications 
	/// to the result do not affect the internal population state.
	/// </remarks>
	/// <returns>A clone of the initial solution with the shortest tour from the generated population.</returns>
	protected override TspResult Initialize()
	{
		population = base.InitializePopulation( this.settings.MaxSize );

		return population.MinBy( r => r.Tour )!.Clone();
	}

	/// <summary>
	/// Performs a single evolutionary epoch
	/// </summary>
	/// <remarks>
	/// Method applies parallelized crossover, mutation, and local search to efficiently evolve the/ population. 
	/// The population size and mutation rate are determined by the current settings. The method is thread-safe.
	/// </remarks>
	/// <param name="best">The current best solution found so far. Used as a reference for generating the next population.</param>
	/// <returns>The best solution found in the new population after completing the epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		this.population.Sort( ( a, b ) => a.Tour.CompareTo( b.Tour ) );

		if( base.count % settings.Redraw == 0 )
		{
			this.population = HandleDuplicates( this.population, Random2OptSwap );
			this.population.Sort( ( a, b ) => a.Tour.CompareTo( b.Tour ) );
		}

		// Cache ranked population once to avoid re-sorting on every selection
		this.rankedCache = [ .. this.population.OrderBy( ind => ind.Fitness ).Select( ( ind, index ) => (ind, index + 1) ) ];

		var newPopulation = this.population.Take( this.settings.MinSize ).ToList();
		int childrenToGenerate = this.settings.MaxSize - this.settings.MinSize;

		// 1. Thread-safe collection for parallel breeding
		var children = new ConcurrentBag<TspResult>();

		// 2. Parallelize crossover and mutation
		Parallel.For( 0, childrenToGenerate, _ => 
		{
			// Use cached ranks for fast selection (no re-sorting)
			var parent1 = LocalRankBasedSelection();
			var parent2 = LocalRankBasedSelection();

			var child = base.Crossover( parent1, parent2 );
			child = base.Mutate( child, this.settings.MutationRate );

			// 3. MEMETIC BOOST: Apply local search to the mutated child
			// If ParallelLocalSearch causes thread starvation inside a Parallel.For, 
			// use standard Single-Thread Local Search/2-Opt here if available in your base class.
			child = ParallelLocalSearch( child.Path );

			children.Add( child );
		} );

		newPopulation.AddRange( children );
		this.population = newPopulation;

		return this.population.MinBy( r => r.Tour )!;
	}


	/// <summary>
	/// Performs rank-based selection using pre-computed cached ranks (called within parallel loops).
	/// Avoids redundant sorting by using the rankedCache computed once per epoch.
	/// </summary>
	TspResult LocalRankBasedSelection()
	{
		double totalRank = this.rankedCache.Sum( r => r.rank );
		double randomValue = Random.Shared.NextDouble() * totalRank;
		double cumulativeRank = 0.0;

		foreach( var (individual, rank) in this.rankedCache )
		{
			cumulativeRank += rank;
			if( cumulativeRank > randomValue ) return individual;
		}

		return this.rankedCache.First().individual; // Fallback
	}

	/// <summary>
	/// Alter duplicates in the population
	/// </summary>	
	List<TspResult> HandleDuplicates( List<TspResult> population, Func<List<int>, List<int>> func )
	{
		var (unique, duplicates) = Split( population );

		if( duplicates.Count == 0 ) return population;

		ChangeDuplicates( duplicates, func );

		unique.AddRange( duplicates );

		return unique;
	}
	
	/// <summary>
	/// Splits collection into unique and duplicate solutions based on Tour length (Phenotype) 
	/// </summary>	
	static (List<TspResult>, List<TspResult>) Split( List<TspResult> results )
	{
		var unique = new List<TspResult>( results.Count );
		var duplicate = new List<TspResult>();

		// Track rounded tour lengths to safely evaluate floating-point phenotypic duplicates
		var seenLengths = new HashSet<long>();

		foreach( var item in results )
		{
			// Convert tour to a fast integer scale for hashing (adjust multiplier based on coordinate precision)
			long tourKey = ( long )( item.Tour * 1000 );

			if( !seenLengths.Add( tourKey ) )
			{
				duplicate.Add( item );
			}
			else
			{
				unique.Add( item );
			}
		}

		return (unique, duplicate);
	}	

	/// <summary>
	/// Alter the path of duplicate solutions by applying a Func
	/// </summary>	
	void ChangeDuplicates( List<TspResult> duplicates, Func<List<int>, List<int>> func )
	{
		foreach( var duplicate in duplicates )
		{
			duplicate.Path = func( duplicate.Path ); // alter the path of the duplicate solution
			duplicate.Tour = this.map.GetTourLength( duplicate.Path ); // Recalculate tour length after mutation			
		}
	}
}

/// <summary>
/// Genetic algorithm configuration settings
/// </summary>
public class GeneticSettings : TspConfigurationBase
{
	/// <summary>
	/// Population size
	/// </summary>
	public int MinSize { get; set; }
	public int MaxSize { get; set; }	

	/// <summary>
	/// Mutation rate
	/// </summary>
	[ConfigurationKeyName( "mut-rate" )]
	[Range( 0.0, 0.5, ErrorMessage = "Mutation rate must be between 0 and 0.5" )]
	public double MutationRate { get; set; }	
}
