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

		return population.MinBy( r => r.Tour )!.Clone();
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		List<TspResult> newPopulation = [];

		foreach( var target in population )
		{
			var mutant = Mutate( population, target );

			var trial = base.Crossover( target, mutant );

			Update( newPopulation, trial < target ? trial : target );   											
		}

		population = [ .. newPopulation.OrderBy( p => p.Tour ) ];

		var result = population.First(); // best solution in the current iteration

		return Parallel2OptSearch( result.Path );
	}
	

	/// <summary>
	/// Update population and prevent duplicates
	/// </summary>	
	void Update( List<TspResult> population, TspResult current )
	{
		if( !population.Contains( current ) )		
			population.Add( current );					
		else 
			population.Add( base.InitializeTour() ); //add a new random tour to the population
	}

	/// <summary>
	/// Mutates the target tour using three random donor tours from the population
	/// </summary>	
	TspResult Mutate( List<TspResult> population, TspResult target )
	{
		int size = population.Count;

		var points = IRandomSequence.GetUniqueInts( 3, 0, size ); // Get three unique indices

		var donor1 = population[ points[ 0 ] ];
		var donor2 = population[ points[ 1 ] ];
		var donor3 = population[ points[ 2 ] ];

		return Mutate( target.Path, donor1.Path, donor2.Path, donor3.Path );
	}

	/// <summary>
	/// Mutates the target tour using three donor tours
	/// </summary>	
	TspResult Mutate( IList<int> target, IList<int> donor1, IList<int> donor2, IList<int> donor3 )
	{								
		var mutated = new List<int>( target ); // Mutation: donor1 + F * (donor2 - donor3)

		for( int i = 0; i < target.Count; i++ )
		{
			if( Random.Shared.NextDouble() < settings.Factor )
				mutated[ i ] = donor1[ i ] + ( int )settings.Factor * ( donor2[ i ] - donor3[ i ] );		
		}
		
		return RepairTour( mutated, target ); // Repair step to ensure valid TSP tour (no duplicates)
	}

	/// <summary>
	/// Repair the mutated tour to ensure it's a valid permutation
	/// </summary>	
	TspResult RepairTour( List<int> mutated, IList<int> current )
	{
		var missing = current.Except( mutated ).ToList();

		var duplicates = mutated.GroupBy( x => x ).Where( g => g.Count() > 1 ).Select( g => g.Key ).ToList();

		int index = 0;

		for( int i = 0; i < mutated.Count; i++ )
		{
			int city = mutated[ i ];

			if( duplicates.Contains( city ) )
			{
				mutated[ i ] = missing[ index++ ];

				duplicates.Remove( city );
			}
		}

		return TspResult.Build( base.map, mutated );
	}

	#region obsolete
	/// <summary>
	/// Ordered crossover between two parents
	/// </summary>	
	//TspResult Crossover( IList<int> parent1, IList<int> parent2 )
	//{
	//	int length = parent1.Count;

	//	List<int> child = Enumerable.Repeat( -1, length ).ToList();

	//	(int start, int end) = IRandomSequence.GetPairInts( 0, length ); // Get random crossover points

	//	// Select random crossover points
	//	//int start = Random.Shared.Next( 0, length );
	//	//int end = Random.Shared.Next( start, length );

	//	for( int i = start; i <= end; i++ ) // Copy segment from parent1 to child
	//	{
	//		child[ i ] = parent1[ i ];
	//	}

	//	int index = 0;

	//	for( int i = 0; i < length; i++ ) // Fill remaining positions from parent2
	//	{
	//		if( !child.Contains( parent2[ i ] ) )
	//		{
	//			while( child[ index ] != -1 ) index++;

	//			child[ index ] = parent2[ i ];
	//		}
	//	}

	//	return TspResult.Build( base.map, child );
	//}
	#endregion
}

/// <summary>
/// Configuration settings for Differential Evolution algorithm
/// </summary>
public class DeSettings : BeamSettings
{
	public double Factor { get; set; } = 0.8; // Mutation factor
	public double Rate { get; set; } = 0.9; // Crossover rate
}
