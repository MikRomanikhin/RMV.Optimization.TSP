using Microsoft.Extensions.Configuration;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Q-Learning algorithm for solving the Traveling Salesman Problem (TSP).
/// </summary>
public class QLearning( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{
	QLearningSettings settings;
	readonly TspMap Map = map;	

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<QLearningSettings>( "q-learning" );
	}

	/// <summary>
	/// Q-Learning async wrapper
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		base.timer.Start();

		double[,] qTable = new double[ base.Cities, base.Cities ];

		int count = 0;
		int noChanges = 0;

		var best = Map.BuildRandomTour(); // Initialize with a random tour

		await Task.Run( () => 
		{
			while( noChanges++ < settings.Limit )
			{
				TrainQTable( qTable );

				var result = FindOptimalPath( qTable ); // Find the optimal path using the Q-table

				if( result < best )
				{
					best = result.Clone();
					noChanges = 0;

					base.Draw( best.Tour, count, best.Path );
				}

				if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
			}

			base.Draw( best.Tour, ++count, best.Path );
		} );

		base.timer.Stop();

		return new TspResult( best.Tour, best.Path );
	}

	TspResult FindOptimalPath( double[,] qTable )
	{
		List<int> path = [];
		int currentCity = 0; // Start from city 0
		path.Add( currentCity );

		HashSet<int> visited = [ currentCity ];

		while( visited.Count < base.Cities )
		{
			int nextCity = ChooseNextCity( currentCity, visited, qTable );

			path.Add( nextCity );
			visited.Add( nextCity );

			currentCity = nextCity;
		}

		return new TspResult( this.Map.GetTourLength( path ), path );
	}

	void TrainQTable( double[,] qTable )
	{
		int currentCity = Random.Shared.Next( base.Cities );

		HashSet<int> visited = [ currentCity ];
		//List<int> path = [ currentCity ];

		while( visited.Count < base.Cities )
		{
			int nextCity = ChooseNextCity( currentCity, visited, qTable );

			double reward = -this.Map[ currentCity, nextCity ].Weight;

			double maxFutureQ = GetMaxQ( nextCity, visited, qTable );

			qTable[ currentCity, nextCity ] +=
				 this.settings.LearningRate * ( reward + this.settings.DiscountFactor * maxFutureQ - qTable[ currentCity, nextCity ] );

			visited.Add( nextCity );
			//path.Add( nextCity );
			currentCity = nextCity;
		}

		//return new TspResult( this.Map.GetTourLength( path ), path );
	}


	int ChooseNextCity( int currentCity, HashSet<int> visited, double[,] qTable )
	{
		if( Random.Shared.NextDouble() < this.settings.ExplorationRate ) // Explore: Choose a random unvisited city
		{			
			List<int> unvisited = [];

			for( int i = 0; i < base.Cities; i++ )
			{
				if( !visited.Contains( i ) ) unvisited.Add( i );
			}

			return unvisited[ Random.Shared.Next( unvisited.Count ) ];
		}
		else // Exploit: Choose the city with the highest Q-value
		{			
			double maxQ = double.MinValue;
			int bestCity = -1;

			for( int i = 0; i < base.Cities; i++ )
			{
				if( !visited.Contains( i ) && qTable[ currentCity, i ] > maxQ )
				{
					maxQ = qTable[ currentCity, i ];
					bestCity = i;
				}
			}

			return bestCity;
		}
	}

	double GetMaxQ( int city, HashSet<int> visited, double[,] qTable )
	{
		double maxQ = double.MinValue;

		for( int i = 0; i < base.Cities; i++ )
		{
			if( !visited.Contains( i ) && qTable[ city, i ] > maxQ )
			{
				maxQ = qTable[ city, i ];
			}
		}

		return maxQ == double.MinValue ? 0 : maxQ;
	}

	//List<int> FindOptimalPath( double[,] qTable )
	//{
	//	List<int> path = [];
	//	int currentCity = 0; // Start from city 0
	//	path.Add( currentCity );

	//	HashSet<int> visited = [ currentCity ];		

	//	while( visited.Count < base.Cities )
	//	{
	//		int nextCity = ChooseNextCity( currentCity, visited, qTable );

	//		path.Add( nextCity );
	//		visited.Add( nextCity );

	//		currentCity = nextCity;
	//	}

	//	return path;
	//}

	//void TrainQTable( double[,] qTable )
	//{
	//	for( int episode = 0; episode < episodes; episode++ )
	//	{
	//		int currentCity = Random.Shared.Next( base.Cities );

	//		HashSet<int> visited = [ currentCity ];

	//		while( visited.Count < base.Cities )
	//		{
	//			int nextCity = ChooseNextCity( currentCity, visited, qTable );

	//			double reward = -this.Map[ currentCity, nextCity ].Weight;

	//			double maxFutureQ = GetMaxQ( nextCity, visited, qTable );

	//			qTable[ currentCity, nextCity ] +=
	//				 this.settings.LearningRate * ( reward + this.settings.DiscountFactor * maxFutureQ - qTable[ currentCity, nextCity ] );

	//			visited.Add( nextCity );
	//			currentCity = nextCity;
	//		}
	//	}
	//}
}

public class QLearningSettings : TspConfigurationBase
{
	/// <summary>
	/// Learning rate
	/// </summary>
	[ConfigurationKeyName( "learning" )]
	public double LearningRate { get; set; } = 0.5;

	[ConfigurationKeyName( "exploration" )]
	public double ExplorationRate { get; set; } = 0.5;

	/// <summary>
	/// Discount factor.
	/// </summary>
	[ConfigurationKeyName( "discount" )]
	public double DiscountFactor { get; set; } = 0.9;
}
