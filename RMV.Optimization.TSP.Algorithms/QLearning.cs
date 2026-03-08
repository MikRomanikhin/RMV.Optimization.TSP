using Microsoft.Extensions.Configuration;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Q-Learning algorithm for solving the Traveling Salesman Problem (TSP).
/// </summary>
public class QLearning( TspMap map ) : TspAlgorithmBase( map )
{
	QLearningSettings settings;	
	double[,] qTable;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<QLearningSettings>( "q-learning" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );		
	}
	

	/// <summary>
	/// Initializes the Q-Table and returns a random initial tour.
	/// </summary>
	protected override TspResult? Initialize()
	{
		InitializeQTable();

		return base.Initialize();
	}

	/// <summary>
	/// Initializes the Q-table with values based on the distances between cities.
	/// </summary>
	/// <remarks>
	/// Assigns higher initial Q-values to city pairs with shorter distances, which can encourage
	/// exploration of shorter routes in reinforcement learning algorithms such as Q-learning. 	
	/// </remarks>
	void InitializeQTable()
	{
		qTable = new double[ base.Cities, base.Cities ];
		
		for( int i = 0; i < base.Cities; i++ ) // SEED Q-TABLE: Give short distances huge initial Q-Values
		{
			for( int j = 0; j < base.Cities; j++ )
			{
				if( i != j ) // Add small epsilon to avoid divide by zero if cities overlap
				{					
					qTable[ i, j ] = settings.Reward / ( base.map[ i, j ].Weight + settings.OptimalityGap );
				}
			}
		}
	}

	/// <summary>
	/// Performs a single Q-Learning epoch: Trains the table multiple times and evaluates the best known path.
	/// </summary>
	protected override TspResult RunEpoch( TspResult best )
	{
		EvaporateQTable();

		var epochBest = best.Clone();

		for( int i = 0; i < settings.Episodes; i++ )
		{
			var episodeResult = TrainQTable( qTable );

			if( episodeResult.Tour < epochBest.Tour )
			{
				epochBest = episodeResult;
			}
		}

		var greedyResult = FindBestGreedyPath( qTable );

		if( greedyResult.Tour < epochBest.Tour )
		{
			epochBest = greedyResult;
		}

		return ParallelLocalSearch( epochBest.Path );
	}
	
	/// <summary>
	/// Evaporates the Q-values in the Q-table to simulate the decay of learned knowledge over time.
	/// </summary>
	void EvaporateQTable()
	{
		for( int i = 0; i < base.Cities; i++ )
		{
			for( int j = 0; j < base.Cities; j++ )
			{
				if( i != j )
				{
					qTable[ i, j ] *= settings.Evaporation;
				}
			}
		}
	}

	/// <summary>
	/// Finds the best greedy path by evaluating all possible starting cities using the provided Q-table.
	/// </summary>
	/// <remarks>This method evaluates the greedy path starting from each city and returns the one with
	/// the minimal total cost. The input Q-table is not modified.</remarks>
	/// <param name="qTable">Q-table as 2D array, where each element specifies the cost or weight between pairs of cities.</param>
	/// <returns>TspResult containing the lowest-cost tour and its corresponding path.</returns>
	TspResult FindBestGreedyPath( double[,] qTable )
	{
		TspResult best = new( double.MaxValue, [] );

		for( int start = 0; start < base.Cities; start++ )
		{
			var result = FindOptimalPath( qTable, start );

			if( result.Tour < best.Tour ) best = result;
		}

		return best;
	}

	/// <summary>
	/// Trains the Q-table and returns the complete path generated during this episode.
	/// </summary>
	/// <param name="qTable">Q-table as 2D array, where each element specifies the cost or weight between pairs of cities.</param>
	/// <returns>TspResult containing the tour length and the path generated during this episode.</returns>
	TspResult TrainQTable( double[,] qTable )
	{
		int currentCity = Random.Shared.Next( base.Cities );

		List<int> path = new( base.Cities ) { currentCity };
		HashSet<int> visited = [ currentCity ];

		while( visited.Count < base.Cities )
		{
			int nextCity = ChooseNextCity( currentCity, visited, qTable );
			visited.Add( nextCity );

			double reward = settings.Reward / ( base.map[ currentCity, nextCity ].Weight + settings.OptimalityGap );
			double maxFutureQ = GetMaxQ( nextCity, visited, qTable );

			qTable[ currentCity, nextCity ] +=
				settings.Learning * ( reward + settings.Discount * maxFutureQ - qTable[ currentCity, nextCity ] );

			path.Add( nextCity );
			currentCity = nextCity;
		}

		double tour = base.map.GetTourLength( path );
		double bonus = settings.TourReward / ( tour + settings.OptimalityGap );

		for( int i = 0; i < path.Count; i++ )
		{
			int from = path[ i ];
			int to = path[ ( i + 1 ) % path.Count ];

			qTable[ from, to ] += settings.Learning * bonus;
		}

		return new TspResult( tour, path );
	}
	

	/// <summary>
	/// Finds the optimal path using the provided Q-table.
	/// </summary>
	/// <param name="qTable">A two-dimensional array representing the Q-values for transitions between cities.</param>
	/// <returns>A TspResult containing the optimal path and its length.</returns>
	TspResult FindOptimalPath( double[,] qTable, int startCity )
	{
		List<int> path = [ startCity ];
		HashSet<int> visited = [ startCity ];
		int currentCity = startCity;

		while( visited.Count < base.Cities )
		{
			int nextCity = -1;
			double bestScore = double.MinValue;

			for( int i = 0; i < base.Cities; i++ )
			{
				if( visited.Contains( i ) ) continue;

				double q = qTable[ currentCity, i ];
				double heuristic = settings.Reward / ( base.map[ currentCity, i ].Weight + settings.OptimalityGap );
				double score = settings.QWeight * q + settings.HeuristicWeight * heuristic;

				if( score > bestScore )
				{
					bestScore = score;
					nextCity = i;
				}
			}

			path.Add( nextCity );
			visited.Add( nextCity );
			currentCity = nextCity;
		}

		return new TspResult( base.map.GetTourLength( path ), path );
	}


	/// <summary>
	/// Chooses the next city to visit based on the Q-learning policy.
	/// </summary>
	/// <param name="currentCity">The index of the current city.</param>
	/// <param name="visited">A set containing the indices of cities that have already been visited.</param>
	/// <param name="qTable">A two-dimensional array representing the Q-values for transitions between cities.</param>
	/// <returns>The index of the next city to visit.</returns>
	int ChooseNextCity( int currentCity, HashSet<int> visited, double[,] qTable )
	{
		List<(int City, double Score)> candidates = [];

		for( int i = 0; i < base.Cities; i++ )
		{
			if( visited.Contains( i ) ) continue;

			double q = qTable[ currentCity, i ];
			double heuristic = settings.Reward / ( base.map[ currentCity, i ].Weight + settings.OptimalityGap );

			double score = settings.QWeight * q + settings.HeuristicWeight * heuristic;

			candidates.Add( (i, Math.Max( score, MARGIN )) );
		}

		if( candidates.Count == 1 ) return candidates[ 0 ].City;

		if( Random.Shared.NextDouble() < settings.Exploration )
		{
			double total = candidates.Sum( c => c.Score );
			double pick = Random.Shared.NextDouble() * total;
			double sum = 0;

			foreach( var candidate in candidates )
			{
				sum += candidate.Score;

				if( sum >= pick ) return candidate.City;
			}

			return candidates[ ^1 ].City;
		}

		return candidates.MaxBy( c => c.Score ).City;
	}	


	/// <summary>
	/// Returns the maximum Q-value for transitions from the specified city to any unvisited city.
	/// </summary>
	/// <param name="city">The index of the current city from which to evaluate possible transitions.</param>
	/// <param name="visited">A set containing the indices of cities that have already been visited.</param>
	/// <param name="qTable">A two-dimensional array representing the Q-values for transitions between cities.</param>
	/// <returns>
	/// The highest Q-value for transitions from the specified city to any city not in the visited set. 
	/// Returns 0 if all possible transitions have been visited.
	/// </returns>
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
	
}

public class QLearningSettings : TspConfigurationBase
{
	/// <summary>
	/// Learning rate
	/// </summary>	
	public double Learning { get; set; } = 0.5;

	/// <summary>
	/// Exploration rate for choosing between exploration and exploitation during training.
	/// </summary>
	public double Exploration { get; set; } = 0.5;

	/// <summary>
	/// Discount factor for future rewards in the Q-learning update rule.
	/// </summary>	
	public double Discount { get; set; } = 0.9;

	/// <summary>
	/// Number of training episodes to run during the learning process.
	/// </summary>	
	public int Episodes { get; set; } = 100;

	/// <summary>
	/// Scale factor applied to reward values during learning.
	/// </summary>
	/// <remarks>
	/// Adjust this value to control the magnitude of rewards, which can help stabilize or accelerate the learning process.
	/// </remarks>
	[ConfigurationKeyName( "reward" )]
	public double Reward { get; set; } = 1000.0;

	/// <summary>
	/// Acceptable optimality gap for early stopping when a known optimal solution is available.
	/// </summary>
	/// <remarks>
	/// The optimality gap specifies the relative difference, as a decimal fraction, between the current
	/// solution and the known optimal value at which the algorithm may terminate early. For example, a value of 0.01
	/// allows early stopping when the solution is within 1% of the optimal value. This property is relevant only if an
	/// optimal solution is known or can be estimated.
	/// </remarks>
	[ConfigurationKeyName( "gap" )]
	public double OptimalityGap { get; set; } = 0.01; // 1% gap to known optimal solution (if available) for early stopping

	/// <summary>
	/// Evaporation rate applied during each iteration.
	/// </summary>
	/// <remarks>
	/// Determines how much of a resource or value is reduced over time. Typical values are between 0 and 1, where values
	/// closer to 1 result in slower evaporation. Adjust this property to control the persistence of accumulated values across iterations.
	/// </remarks>	
	public double Evaporation { get; set; } = 0.995;

	/// <summary>
	/// Quality weight value used for content negotiation or prioritization.
	/// </summary>
	/// <remarks>
	/// Represents the relative preference or importance of an item, such as in HTTP content negotiation
	/// where higher values indicate greater preference. The default value is 0.7.
	/// </remarks>
	[ConfigurationKeyName( "q-weight" )]
	public double QWeight { get; set; } = 0.7;
	
	/// <summary>
	/// Heuristic weight value used for content negotiation or prioritization.
	/// </summary>
	/// <remarks>
	/// Represents the relative preference or importance of an item, such as in HTTP content negotiation
	/// where higher values indicate greater preference. The default value is 0.3.
	/// </remarks>
	[ConfigurationKeyName( "heuristic-weight" )]
	public double HeuristicWeight { get; set; } = 0.3;

	/// <summary>
	/// Reward value assigned for completing a tour.
	/// </summary>
	[ConfigurationKeyName( "tour-reward" )]
	public double TourReward { get; set; } = 5000.0;
}
