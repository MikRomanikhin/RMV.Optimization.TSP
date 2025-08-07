using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Building block for TSP Genetic Algorithms
/// </summary>
public class TspGeneticBase( TspMap map ) : TspAlgorithmBase( map )
{
	readonly TspMap map = map;

	#region Initialize ---------------------------------------------------------

	//protected List<TspResult> BuildPopulation( int size ) => [ .. Enumerable.Range( 0, size ).Select( _ => base.BuildRandomTour() ) ];

	#endregion


	#region Selection ----------------------------------------------------------

	protected static TspResult RouletteWheelSelection( List<TspResult> population )
	{
		double totalFitness = population.Sum( ind => ind.Fitness );

		double randomValue = Random.Shared.NextDouble() * totalFitness;

		double cumulativeFitness = 0.0;

		foreach( var individual in population )
		{
			cumulativeFitness += individual.Fitness;

			if( cumulativeFitness > randomValue ) return individual;
		}

		return population.First(); // Fallback
	}

	protected static TspResult TournamentSelection( List<TspResult> population, int tournamentSize )
	{
		var tournament = new List<TspResult>();

		for( int i = 0; i < tournamentSize; i++ )
		{
			tournament.Add( population[ Random.Shared.Next( population.Count ) ] );
		}

		return tournament.MinBy( i => i.Fitness );
	}

	protected static TspResult RankBasedSelection( List<TspResult> population )
	{
		var rankedPopulation = population.OrderBy( ind => ind.Fitness ).Select( ( ind, index ) => new { Individual = ind, Rank = index + 1 } ).ToList();

		double totalRank = rankedPopulation.Sum( r => r.Rank );

		double randomValue = Random.Shared.NextDouble() * totalRank;

		double cumulativeRank = 0.0;

		foreach( var ranked in rankedPopulation )
		{
			cumulativeRank += ranked.Rank;

			if( cumulativeRank > randomValue ) return ranked.Individual;
		}

		return rankedPopulation.First().Individual; // Fallback
	}

	#endregion


	#region Crossover ----------------------------------------------------------

	protected TspResult Crossover( TspResult parent1, TspResult parent2 )
	{
		int length = parent1.Path.Count;

		int start = Random.Shared.Next( length );
		int end = Random.Shared.Next( start, length );

		var child = parent1.Path.ToList().GetRange( start, end - start );

		child.AddRange( parent2.Path.Where( city => !child.Contains( city ) ).Select( city => city ) );

		return new TspResult( this.map.GetTourLength( child ), child );
	}

	//static int[] OrderedCrossover( int[] parent1, int[] parent2 )
	//{
	//	int length = parent1.Length;
	//	int[] offspring = new int[ length ];
	//	Array.Fill( offspring, -1 ); // Initialize offspring with -1 (placeholder)

	//	// Randomly select two crossover points
	//	Random random = new Random();
	//	int start = random.Next( 0, length );
	//	int end = random.Next( start, length );

	//	// Copy the segment from parent1 to offspring
	//	for( int i = start; i <= end; i++ )
	//	{
	//		offspring[ i ] = parent1[ i ];
	//	}

	//	// Fill the remaining positions from parent2 in order
	//	int currentIndex = ( end + 1 ) % length;
	//	for( int i = 0; i < length; i++ )
	//	{
	//		int candidate = parent2[ ( end + 1 + i ) % length ];
	//		if( !offspring.Contains( candidate ) )
	//		{
	//			offspring[ currentIndex ] = candidate;
	//			currentIndex = ( currentIndex + 1 ) % length;
	//		}
	//	}

	//	return offspring;
	//}

	protected TspResult OrderCrossover( TspResult parent1, TspResult parent2 )
	{
		int length = parent1.Path.Count;

		// Randomly select crossover points		
		int start = Random.Shared.Next( 0, length );
		int end = Random.Shared.Next( start, length );

		var child = new int[ length ];
		
		Array.Copy( parent1.Path.ToArray(), start, child, start, end - start + 1 ); // Initialize child with parent1's path		

		int index = ( end + 1 ) % length;

		for( int i = 0; i < length; i++ ) // Fill the remaining positions with genes from parent2 in order
		{
			int gene = parent2.Path[ ( end + 1 + i ) % length ];

			if( !child.Contains( gene ) )
			{
				child[ index ] = gene;

				index = ( index + 1 ) % length;
			}
		}

		return new TspResult( this.map.GetTourLength( child ), child );
	}	

	#endregion


	#region Mutation -----------------------------------------------------------

	protected TspResult Mutation( TspResult result, double rate ) =>	Random.Shared.NextDouble() < rate ? base.RandomSwap( result ) : result;

	#endregion
	
}

