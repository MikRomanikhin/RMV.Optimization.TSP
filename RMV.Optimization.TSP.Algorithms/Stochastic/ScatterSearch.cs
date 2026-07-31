using RMV.Common.Configuration;

using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Scatter Search for TSP
/// </summary>
public class ScatterSearch( TspMap map ) : TspAlgorithmBase( map )
{
	ScatterSettings settings;
	List<TspResult> population = [];
	
	/// <summary>
	/// Configures the algorithm 
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<ScatterSettings>( "scatter" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the population and returns a clone of the best initial solution found.
	/// </summary>
	/// <remarks>
	/// The returned result is a deep copy of the best initial solution, ensuring that modifications
	/// to the result do not affect the internal population state.
	/// </remarks>
	/// <returns>A clone of the initial solution with the shortest tour from the generated population.</returns>
	protected override TspResult Initialize()
	{
		population = base.InitializePopulation( this.settings.Size );

		return population.MinBy( r => r.Tour )!.Clone();
	}

	/// <summary>
	/// Performs a single optimization epoch using the current population and returns the best solution found in this iteration.
	/// </summary>
	/// <remarks>
	/// Method combines solutions from the population, applies local search, and may mutate the population to maintain diversity.
	/// It is typically called repeatedly as part of an iterative optimization process.
	/// </remarks>
	/// <param name="best">The current best solution from the previous epoch. Used as a reference for improvement.</param>
	/// <returns>A TspResult representing the best solution found during this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var newSolution = CombineSolutions( population );

		var result = ParallelLocalSearch( newSolution );//Local2OptSearch( newSolution );

		result = ReplaceWorstSolution( population, result.Path );

		//if( population.Any() && population.Count % settings.Mutate == 0 ) population = SwapDuplicates( population );
		if( count % settings.Mutate == 0 ) population = SwapDuplicates( population );

		return result;
	}


	/// <summary>
	/// Swap duplicates in the population by random 2-opt swap
	/// </summary>	
	List<TspResult> SwapDuplicates( List<TspResult> population )
	{
		var (unique, duplicates) = Split( population );

		ChangeDuplicates( Random2OptSwap, duplicates );

		return [ .. unique.Concat( duplicates ).OrderBy( u => u.Tour ) ];
	}


	/// <summary>
	/// Applies a given mutation function to the paths of duplicate solutions in the population. 
	/// Method iterates through each duplicate solution and modifies its path using the provided function, 
	/// then recalculates the tour length for each modified solution to ensure that the population remains consistent with the new paths.
	/// </summary>
	/// <param name="func">The mutation function to apply to each duplicate solution's path.</param>
	/// <param name="duplicates">The list of duplicate solutions to be modified.</param>
	void ChangeDuplicates( Func<List<int>, List<int>> func, List<TspResult> duplicates )
	{
		foreach( var duplicate in duplicates )
		{
			duplicate.Path = func( duplicate.Path ); // alter the path of the duplicate solution

			duplicate.Tour = base.map.GetTourLength( duplicate.Path ); // Recalculate tour length after mutation			
		}
	}	


	/// <summary>
	/// Splits the population into unique and duplicate solutions
	/// </summary>	
	static (List<TspResult>, List<TspResult>) Split( List<TspResult> population )
	{
		var unique = new List<TspResult>();
		var duplicate = new List<TspResult>();

		var seen = new HashSet<TspResult>();

		foreach( var item in population )
		{
			if( !seen.Add( item ) )
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
	/// Crossover-like operation to combine two solutions into one
	/// </summary>
	static List<int> CombineSolutions( List<TspResult> population )
	{
		if( population.Count < 2 )
			throw new InvalidOperationException( "Population must have at least 2 solutions for crossover" );

		int index1 = Random.Shared.Next( population.Count );
		int index2 = Random.Shared.Next( population.Count );
				
		while( index1 == index2 ) // Ensure two different parents
		{
			index2 = Random.Shared.Next( population.Count );
		}

		var parent1 = population[ index1 ].Path;
		var parent2 = population[ index2 ].Path;

		var child = new List<int>( parent1.Count );
		var visited = new HashSet<int>();

		int length = parent1.Count;

		for( int i = 0; i < length; i++ ) // Iterate through both parents simultaneously
		{
			int c1 = parent1[ i ];
			int c2 = parent2[ i ];

			if( visited.Add( c1 ) )	child.Add( c1 );
			if( visited.Add( c2 ) )	child.Add( c2 );

			if( child.Count == length ) break;
		}

		if( child.Count < length )
			child.AddRange( Enumerable.Range( 0, length ).Except( visited ) );

		return child;
	}
	

	/// <summary>
	/// The worst solution in the population is replaced if the new solution is better
	/// </summary>	
	TspResult ReplaceWorstSolution( List<TspResult> population, List<int> newSolution )
	{
		ArgumentNullException.ThrowIfNull( population );
		ArgumentNullException.ThrowIfNull( newSolution );
		if( population.Count == 0 ) throw new InvalidOperationException( "Population cannot be empty" );

		var worstSolution = population.MaxBy( r => r.Tour );

		double newTour = base.map.GetTourLength( newSolution );

		if( newTour + MARGIN < worstSolution.Tour ) // If the new solution is better than the worst solution, replace it
		{
			population.Remove( worstSolution );

			population.Add( new TspResult( newTour, newSolution ) );
		}

		return population.MinBy( r => r.Tour );
	}
}

/// <summary>
/// Configuration Settings
/// </summary>
public class ScatterSettings : BeamSettings
{
	public int Mutate { get; set; } = 100; // Number of mutations to perform on duplicates
}
