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

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "classifier" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize()
	{
		population = base.InitializePopulation( this.settings.Size );

		return population.MinBy( r => r.Tour )!.Clone();
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		population = Evolve( population );

		//Reinsert( population ); // reinsert random tours to keep population size constant

		var result = population.MinBy( i => i.Tour ); // best solution in the current iteration

		return Parallel2OptSearch( result.Path ); // apply local search to improve the best solution
	}


	/// <summary>
	/// Evolves the population using a simple genetic algorithm approach
	/// </summary>	
	List<TspResult> Evolve( List<TspResult> population )
	{
		List<TspResult> newPopulation = [];

		for( int i = 0; i < population.Count; i++ )
		{
			var parent1 = Select( population );
			var parent2 = Select( population );

			var child = base.Crossover( parent1, parent2 );

			var mutated = RandomSwap( child ); //Mutate( child );

			newPopulation.Add( mutated < child ? mutated : child );
		}

		return newPopulation;
	}

	static TspResult Select( List<TspResult> population ) => RouletteWheelSelection( population );

	#region obsolete
	/// <summary>
	/// Evolutionary Programming async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	base.timer.Start();

	//	int count = 0;
	//	int noChanges = 0;

	//	var population = BuildInitialPopulation();

	//	var best = Map.BuildRandomTour();

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			population = Evolve( population );

	//			//Reinsert( population ); // reinsert random tours to keep population size constant

	//			var result = population.MinBy( i => i.Tour ); // best solution in the current iteration

	//			//result = Local2OptSearch( result.Path ); // apply local search to improve the best solution

	//			if( result < best )
	//			{
	//				population.Add( result ); // add the improved solution back to the population
	//				population.Remove( population.MaxBy( i => i.Tour ) ); // remove worse solutions to keep population size constant

	//				best = result.Clone();

	//				noChanges = 0;

	//				base.Draw( best.Tour, count, best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
	//		}

	//		base.Draw( best.Tour, ++count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return new TspResult( best.Tour, best.Path );
	//}
	/// <summary>
	/// Ordered crossover between two parents
	/// </summary>	
	//TspResult Crossover( IList<int> parent1, IList<int> parent2 )
	//{
	//	int length = parent1.Count;

	//	List<int> child = Enumerable.Repeat( -1, length ).ToList();

	//	// Select random crossover points
	//	int start = Random.Shared.Next( 0, length );
	//	int end = Random.Shared.Next( start, length );

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

	//	return new TspResult( this.Map.GetTourLength( child ), child );
	//}

	//protected static TspResult RouletteWheelSelection( List<TspResult> population )
	//{
	//	double totalFitness = population.Sum( ind => ind.Fitness );

	//	double randomValue = Random.Shared.NextDouble() * totalFitness;

	//	double cumulativeFitness = 0.0;

	//	foreach( var individual in population )
	//	{
	//		cumulativeFitness += individual.Fitness;

	//		if( randomValue < cumulativeFitness ) return individual;
	//	}

	//	return population.First(); // Fallback
	//}

	//protected static TspResult TournamentSelection( List<TspResult> population, int tournamentSize )
	//{
	//	var tournament = new List<TspResult>();

	//	for( int i = 0; i < tournamentSize; i++ )
	//	{
	//		tournament.Add( population[ Random.Shared.Next( population.Count ) ] );
	//	}

	//	return tournament.MinBy( i => i.Fitness );
	//}

	//protected static TspResult RankBasedSelection( List<TspResult> population )
	//{
	//	var rankedPopulation = population.OrderBy( ind => ind.Fitness ).Select( ( ind, index ) => new { Individual = ind, Rank = index + 1 } ).ToList();

	//	double totalRank = rankedPopulation.Sum( r => r.Rank );

	//	double randomValue = Random.Shared.NextDouble() * totalRank;

	//	double cumulativeRank = 0.0;

	//	foreach( var ranked in rankedPopulation )
	//	{
	//		cumulativeRank += ranked.Rank;

	//		if( cumulativeRank >= randomValue ) return ranked.Individual;
	//	}

	//	return rankedPopulation.First().Individual; // Fallback
	//}	
	//TspResult Crossover( TspResult parent1, TspResult parent2 )
	//{
	//	int start = Random.Shared.Next( base.Cities );
	//	int end = Random.Shared.Next( base.Cities );

	//	HashSet<int> segment = new( parent1.Path[ start..end ] );

	//	List<int> child = new( parent1.Path.Count );

	//	foreach( int city in parent2.Path )
	//	{
	//		if( !segment.Contains( city ) ) child.Add( city );
	//	}

	//	child.InsertRange( start, parent1[ start..end ] );

	//	return new TspResult( child.ToArray();
	//}


	//static List<int[]> Evolve( List<int[]> population, double[,] distances )
	//{
	//	List<int[]> newPopulation = new List<int[]>();
	//	for( int i = 0; i < population.Count; i++ )
	//	{
	//		int[] parent1 = SelectParent( population, distances );
	//		int[] parent2 = SelectParent( population, distances );
	//		int[] child = Crossover( parent1, parent2 );

	//		Mutate( child );

	//		newPopulation.Add( child );
	//	}

	//	return newPopulation;
	//}

	//static int[] SelectParent( List<int[]> population, double[,] distances )
	//{
	//	return population.OrderBy( route => CalculateRouteDistance( route, distances ) ).First();
	//}

	//static int[] Crossover( int[] parent1, int[] parent2 )
	//{
	//	int start = random.Next( parent1.Length );
	//	int end = random.Next( start, parent1.Length );

	//	HashSet<int> segment = new( parent1[ start..end ] );
	//	List<int> child = new( parent1.Length );

	//	foreach( int city in parent2 )
	//	{
	//		if( !segment.Contains( city ) ) child.Add( city );			
	//	}

	//	child.InsertRange( start, parent1[ start..end ] );

	//	return child.ToArray();
	//}

	//static void Mutate( int[] route )
	//{
	//	if( random.NextDouble() < 0.1 )
	//	{
	//		int index1 = random.Next( route.Length );
	//		int index2 = random.Next( route.Length );
	//		(route[ index1 ], route[ index2 ]) = (route[ index2 ], route[ index1 ]);
	//	}
	//}
	#endregion
}

