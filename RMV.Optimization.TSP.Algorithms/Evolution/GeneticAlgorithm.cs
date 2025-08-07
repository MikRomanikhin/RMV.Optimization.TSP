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
	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<GeneticSettings>( "genetic" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult Initialize()
	{
		population = base.InitializePopulation( this.settings.MaxSize );

		return population.MinBy( r => r.Tour )!.Clone();
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		this.population = base.count % settings.Redraw == 0 
			? HandleDuplicates( this.population, Random2OptSwap ) 
			: [ .. this.population.OrderBy( r => r.Tour ) ];

		var newPopulation = this.population.Take( this.settings.MinSize ).ToList();

		while( newPopulation.Count < this.settings.MaxSize )
		{
			var parent1 = RankBasedSelection( this.population );
			var parent2 = RankBasedSelection( this.population );

			var child = base.Crossover( parent1, parent2 );

			Mutation( child, this.settings.MutationRate );

			newPopulation.Add( child );
		}

		this.population = newPopulation.OrderBy( r => r.Tour ).ToList();

		return this.population.First();
	}	


	#region Selection ----------------------------------------------------------

	//static TspResult RouletteWheelSelection( List<TspResult> population )
	//{
	//	double totalFitness = population.Sum( ind => ind.Fitness );

	//	double randomValue = Random.Shared.NextDouble() * totalFitness;

	//	double cumulativeFitness = 0.0;

	//	foreach( var individual in population )
	//	{
	//		cumulativeFitness += individual.Fitness;

	//		if( cumulativeFitness > randomValue ) return individual;
	//	}

	//	return population.First(); // Fallback
	//}

	//static TspResult TournamentSelection( List<TspResult> population, int tournamentSize )
	//{
	//	var tournament = new List<TspResult>();

	//	for( int i = 0; i < tournamentSize; i++ )
	//	{
	//		tournament.Add( population[ Random.Shared.Next( population.Count ) ] );
	//	}

	//	return tournament.MinBy( i => i.Fitness );
	//}

	//static TspResult RankBasedSelection( List<TspResult> population )
	//{
	//	var rankedPopulation = population.OrderBy( ind => ind.Fitness ).Select( ( ind, index ) => new { Individual = ind, Rank = index + 1 } ).ToList();

	//	double totalRank = rankedPopulation.Sum( r => r.Rank );

	//	double randomValue = Random.Shared.NextDouble() * totalRank;

	//	double cumulativeRank = 0.0;

	//	foreach( var ranked in rankedPopulation )
	//	{
	//		cumulativeRank += ranked.Rank;

	//		if( cumulativeRank > randomValue ) return ranked.Individual;
	//	}

	//	return rankedPopulation.First().Individual; // Fallback
	//}

	#endregion


	#region Crossover ----------------------------------------------------------

	//TspResult Crossover( TspResult parent1, TspResult parent2 )
	//{
	//	int length = parent1.Path.Count;

	//	int start = Random.Shared.Next( length );
	//	int end = Random.Shared.Next( start, length );

	//	var child = parent1.Path.ToList().GetRange( start, end - start );

	//	child.AddRange( parent2.Path.Where( city => !child.Contains( city ) ).Select( city => city ) );

	//	return TspResult.Build( base.map, child );
	//}

	/// <summary>
	/// Ordered Crossover (OX1) for TSP
	/// </summary>	
	//TspResult Crossover( TspResult parent1, TspResult parent2 )
	//{
	//	int length = parent1.Path.Count;
							
	//	(int start, int end) = IRandomSequence.GetPairInts( 0, length - 1 ); // crossover points	

	//	var child = new int[ length ];

	//	Array.Copy( parent1.Path.ToArray(), start, child, start, end - start + 1 ); // Initialize child with parent1's path		

	//	int index = ( end + 1 ) % length;

	//	for( int i = 0; i < length; i++ ) // Fill the remaining positions with genes from parent2 in order
	//	{
	//		int gene = parent2.Path[ ( end + 1 + i ) % length ];

	//		if( !child.Contains( gene ) )
	//		{
	//			child[ index ] = gene;

	//			index = ( index + 1 ) % length;
	//		}
	//	}

	//	return TspResult.Build( base.map, child );
	//}

	#endregion


	#region Mutation -----------------------------------------------------------

	TspResult Mutation( TspResult result, double rate ) => Random.Shared.NextDouble() < rate ? base.RandomSwap( result ) : result;

	#endregion
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

