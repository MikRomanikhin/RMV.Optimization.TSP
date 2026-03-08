using RMV.Optimization.TSP.Common;
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
	/// Meth1od combines solutions from the population, applies local search, and may mutate the population to maintain diversity.
	/// It is typically called repeatedly as part of an iterative optimization process.
	/// </remarks>
	/// <param name="best">The current best solution from the previous epoch. Used as a reference for improvement.</param>
	/// <returns>A TspResult representing the best solution found during this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var newSolution = CombineSolutions( population );

		var result = ParallelLocalSearch( newSolution );//Local2OptSearch( newSolution );

		result = ReplaceWorstSolution( population, result.Path );

		if( population.Any() && population.Count % settings.Mutate == 0 ) population = SwapDuplicates( population );

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

	///<summary
	/// Mutate duplicates in the population by random swapping of cities in the path
	///</summary>
	List<TspResult> MutateDuplicates( List<TspResult> population )
	{
		var (unique, duplicates) = Split( population );

		ChangeDuplicates( RandomSwap, duplicates );

		return [ .. unique.Concat( duplicates ).OrderBy( u => u.Tour ) ];
	}

	/// <summary>
	/// Replace duplicates in the population by random tour
	///</summary>
	List<TspResult> CleanDuplicates( List<TspResult> population )
	{
		int count = population.Count;

		var result = population.Distinct().OrderBy( c => c.Tour ).ToList();

		while( result.Count < count ) result.Add( base.map.BuildRandomTour() );

		return result;
	}

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
		int index1 = Random.Shared.Next( population.Count );
		int index2 = Random.Shared.Next( population.Count );

		if( index1 == index2 ) index2 = ( index2 + 1 ) % population.Count; // Ensure two different parents

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

		return child;
	}
	

	/// <summary>
	/// The worst solution in the population is replaced if the new solution is better
	/// </summary>	
	TspResult ReplaceWorstSolution( List<TspResult> population, List<int> newSolution )
	{
		ArgumentNullException.ThrowIfNull( population );
		ArgumentNullException.ThrowIfNull( newSolution );

		var worstSolution = population.MaxBy( r => r.Tour );

		double newTour = base.map.GetTourLength( newSolution );

		if( newTour < worstSolution.Tour ) // If the new solution is better than the worst solution, replace it
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
