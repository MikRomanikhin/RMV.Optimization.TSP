using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Variable Neiborhood Search
/// </summary>	
public class VarialbleNeiborhoodSearch( TspMap map ) : TspAlgorithmBase( map )
{	
	protected override void Configure()
	{
		base.settings = ConfigManager.GetSection<TspConfigurationBase>( "vns" ) ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize() => base.InitializeTour();//BuildNearestTour();

	/// <summary>
	/// Runs a single epoch of the Variable Neighborhood Search algorithm
	/// </summary>
	/// <param name="best">The best solution found so far</param>
	/// <returns>The best solution found in this epoch</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var dup = best.Clone(); // start with the best found solution from the previous epoch
		
		int neighborhood = 0;

		while( ++neighborhood < 3 )
		{
			var shakenTour = Shake( dup.Path, neighborhood );//Shake( best.Path, neighborhood );

			var current = ParallelLocalSearch( shakenTour ); //ParallelLinKernighanSearch( shakenTour );				

			if( current < dup )
			{
				dup = current.Clone();
				neighborhood = 0; // restart from first neighborhood						
			}
		}

		return dup; // return the best found solution in this epoch
	}


	/// <summary>
	/// Shakes the given path by applying a neighborhood operation
	/// </summary>	
	/// <param name="path">The current path to be shaken</param>
	/// <param name="neighborhood">The neighborhood index indicating the type of shake to apply</param>
	/// <returns>A new path resulting from the shake operation</returns>
	static List<int> Shake( List<int> path, int neighborhood )
	{
		var newTour = new List<int>( path );
		int cities = path.Count;
		
		if( cities < 2 ) return newTour; // Guard against small tours: operations require minimum city count

		switch( neighborhood )
		{
			case 1:  // Swap two cities		//newTour = RandomSwap( newTour );
				int i = Random.Shared.Next( cities );
				int j = Random.Shared.Next( cities );
				while( i == j ) j = Random.Shared.Next( cities );
				(newTour[ i ], newTour[ j ]) = (newTour[ j ], newTour[ i ]);
				break;

			case 2:  // 2-opt		//newTour = Random2OptSwap( newTour );
				int a = Random.Shared.Next( cities - 1 );
				int b = Random.Shared.Next( a + 1, cities );
				newTour.Reverse( a, b - a + 1 );
				break;
		}

		return newTour;
	}
	
}
