using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Evolutionary Programming algorithm for TSP
/// </summary>
/// <param name="map"></param>
public class EvolutionaryProgramming( TspMap map ) : TspAlgorithmBase( map )
{	
	EpSettings settings;
	List<TspResult> population = [];

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<EpSettings>( "ep" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult Initialize()
	{
		this.population = base.InitializePopulation( this.settings.Size );

		return this.population.MinBy( r => r.Tour );
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		population = Evolve( population, settings.Take );

		Reinsert( population ); // reinsert random tours to keep population size constant

		var result = population.MinBy( i => i.Tour ); // best solution in the current iteration

		return Parallel2OptSearch( result.Path ); // apply local search to improve the best solution
	}


	/// <summary>
	/// Evolves the population by mutating each individual
	/// </summary>	
	List<TspResult> Evolve( List<TspResult> population, int take ) =>
		[ .. population.Select( i => RandomSwap( i ) ).OrderBy( i => i.Tour ).Take( take ) ];
	

	/// <summary>
	/// Reinsert random tours to keep population size constant
	/// </summary>	
	void Reinsert( List<TspResult> population )
	{
		int count = population.Count;

		for( int i = count; i < settings.Size - count; i++ )
		{
			population.Add( base.InitializeTour() );
		}		
	}

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
	//			population = Evolve( population, settings.Take );

	//			Reinsert( population ); // reinsert random tours to keep population size constant

	//			var result = population.MinBy( i => i.Tour ); // best solution in the current iteration

	//			result = Local2OptSearch( result.Path ); // apply local search to improve the best solution

	//			if( result < best )
	//			{
	//				population.Add( result ); // add the improved solution back to the population

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
	//(List<int> Route, int Distance) Solve()
	//{
	//	var population = BuildInitialPopulation();

	//	for( int generation = 0; generation < _generations; generation++ )
	//	{
	//		population = EvolvePopulation( population );
	//	}

	//	var bestIndividual = population.OrderBy( ind => CalculateDistance( ind ) ).First();
	//	return (bestIndividual, CalculateDistance( bestIndividual ));
	//}
	//private List<List<int>> InitializePopulation()
	//{
	//	var population = new List<List<int>>();
	//	int cities = _distances.GetLength( 0 );
	//	for( int i = 0; i < _populationSize; i++ )
	//	{
	//		var route = Enumerable.Range( 0, cities ).OrderBy( _ => _random.Next() ).ToList();
	//		population.Add( route );
	//	}
	//	return population;
	//}

	//List<List<int>> EvolvePopulation( List<List<int>> population )
	//{
	//	var newPopulation = new List<List<int>>();

	//	foreach( var individual in population )
	//	{
	//		var mutated = Mutate( individual );
	//		newPopulation.Add( mutated );
	//	}

	//	return newPopulation.OrderBy( ind => CalculateDistance( ind ) ).Take( _populationSize ).ToList();
	//}

	//private List<int> Mutate( List<int> route )
	//{
	//	var mutated = new List<int>( route );
	//	int index1 = _random.Next( mutated.Count );
	//	int index2 = _random.Next( mutated.Count );

	//	// Swap two cities
	//	(mutated[ index1 ], mutated[ index2 ]) = (mutated[ index2 ], mutated[ index1 ]);

	//	return mutated;
	//}

	//private int CalculateDistance( List<int> route )
	//{
	//	int totalDistance = 0;

	//	for( int i = 0; i < route.Count - 1; i++ )
	//	{
	//		totalDistance += _distances[ route[ i ], route[ i + 1 ] ];
	//	}

	//	totalDistance += _distances[ route.Last(), route.First() ]; // Return to start
	//	return totalDistance;
	//}
	#endregion

}

public class EpSettings : BeamSettings
{
	public int Take { get; set; } = 10; // number of individuals to take for next generation
}
