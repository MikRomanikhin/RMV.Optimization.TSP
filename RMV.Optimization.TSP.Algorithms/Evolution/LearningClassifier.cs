using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Learning Classifier System (LCS) algorithm for TSP
/// Uses XCS-style accuracy-based learning with classifiers that represent heuristic rules
/// </summary>
public class LearningClassifier( TspMap map ) : TspAlgorithmBase( map )
{
	/// <summary>
	/// Represents a classifier rule in the LCS. Encodes heuristic weights for city selection
	/// </summary>
	class Classifier
	{
		/// <summary>
		/// Weight for distance heuristic (nearest neighbor)
		/// </summary>
		public double DistanceWeight { get; set; }

		/// <summary>
		/// Weight for angle heuristic (prefer convex hull)
		/// </summary>
		public double AngleWeight { get; set; }

		/// <summary>
		/// Weight for centrality heuristic (avoid edges)
		/// </summary>
		public double CentralityWeight { get; set; }

		/// <summary>
		/// Prediction of tour quality (lower is better)
		/// </summary>
		public double Prediction { get; set; }

		/// <summary>
		/// Prediction error - measures accuracy
		/// </summary>
		public double Error { get; set; }

		/// <summary>
		/// Fitness based on accuracy (inverse of error)
		/// </summary>
		public double Fitness { get; set; }

		/// <summary>
		/// Experience - number of times classifier has been used
		/// </summary>
		public int Experience { get; set; }

		/// <summary>
		/// Timestamp of last use
		/// </summary>
		public int TimeStamp { get; set; }

		public Classifier()
		{
			// Initialize with random weights
			DistanceWeight = Random.Shared.NextDouble();
			AngleWeight = Random.Shared.NextDouble();
			CentralityWeight = Random.Shared.NextDouble();

			// Normalize weights
			double sum = DistanceWeight + AngleWeight + CentralityWeight;
			DistanceWeight /= sum;
			AngleWeight /= sum;
			CentralityWeight /= sum;

			Prediction = 1000.0; // Initial prediction
			Error = 0.0;
			Fitness = 0.1; // Initial fitness
			Experience = 0;
			TimeStamp = 0;
		}

		/// <summary>
		/// Create classifier with specific weights
		/// </summary>
		public Classifier( double dist, double angle, double central )
		{
			DistanceWeight = dist;
			AngleWeight = angle;
			CentralityWeight = central;

			Prediction = 1000.0;
			Error = 0.0;
			Fitness = 0.1;
			Experience = 0;
			TimeStamp = 0;
		}

		/// <summary>
		/// Clone this classifier
		/// </summary>
		public Classifier Clone()
		{
			return new Classifier( DistanceWeight, AngleWeight, CentralityWeight ) {
				Prediction = this.Prediction,
				Error = this.Error,
				Fitness = this.Fitness,
				Experience = this.Experience,
				TimeStamp = this.TimeStamp
			};
		}
	}

	LcsSettings settings;
	readonly List<Classifier> classifiers = [];
	int timeStep = 0;

	/// <summary>
	/// Configures algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<LcsSettings>( "classifier" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the classifier population
	/// </summary>
	protected override TspResult? Initialize()
	{		
		CreateInitialClassifiers(); // Create initial diverse classifier population
				
		var initialTour = BuildTourWithClassifiers(); // Build initial tour using classifiers

		return initialTour;
	}

	/// <summary>
	/// Runs a single LCS epoch
	/// </summary>
	protected override TspResult RunEpoch( TspResult best )
	{
		timeStep++;

		var tour = BuildTourWithClassifiers(); // Build tour using current classifiers

		UpdateFitness( tour.Tour ); // Update classifier fitness based on tour quality

		if( timeStep % settings.GaThreshold == 0 )
		{
			DiscoverRules(); // Periodically run genetic algorithm for rule discovery
		}

		if( classifiers.Count > settings.MaxClassifiers )
		{
			DeleteClassifiers(); // Delete weak classifiers if population too large
		}

		return ParallelLocalSearch( tour.Path ); // Apply local search to improve
	}

	/// <summary>
	/// Creates initial diverse classifier population
	/// </summary>
	void CreateInitialClassifiers()
	{
		classifiers.Clear();

		// Create diverse initial population
		for( int i = 0; i < settings.MaxClassifiers; i++ )
		{
			classifiers.Add( new Classifier() );
		}
	}

	/// <summary>
	/// Builds a tour using classifier heuristics
	/// </summary>
	TspResult BuildTourWithClassifiers()
	{
		var path = new List<int>( Cities );
		var unvisited = new HashSet<int>( Enumerable.Range( 0, Cities ) );
				
		int current = Random.Shared.Next( Cities ); // Start from random city
		path.Add( current );
		unvisited.Remove( current );
				
		var classifier = SelectClassifier(); // Select classifier to use (match set simplified - use all classifiers)

		while( unvisited.Count > 0 ) // Build tour greedily using classifier heuristics
		{
			int next = SelectNextCity( current, unvisited, classifier );
			path.Add( next );
			unvisited.Remove( next );
			current = next;
		}

		return TspResult.Build( this.map, path );
	}

	/// <summary>
	/// Selects a classifier based on fitness (roulette wheel)
	/// </summary>
	Classifier SelectClassifier()
	{
		if( classifiers.Count == 0 ) return new Classifier(); // Covering - create new classifier
		
		double totalFitness = classifiers.Sum( c => c.Fitness );
		if( totalFitness <= 0 )	return classifiers[ Random.Shared.Next( classifiers.Count ) ];
		
		double randomValue = Random.Shared.NextDouble() * totalFitness;
		double cumulative = 0.0;

		foreach( var classifier in classifiers )
		{
			cumulative += classifier.Fitness;
			if( cumulative >= randomValue )
			{
				classifier.Experience++;
				classifier.TimeStamp = timeStep;
				return classifier;
			}
		}

		return classifiers[ ^1 ];
	}

	/// <summary>
	/// Selects next city using classifier heuristics
	/// </summary>
	int SelectNextCity( int current, HashSet<int> unvisited, Classifier classifier )
	{
		double bestScore = double.MaxValue;
		int bestCity = -1;

		foreach( int city in unvisited )
		{
			// Calculate heuristic score based on classifier weights
			double distance = map[ current, city ].Weight;
			double angle = CalculateAngleScore( current, city );
			double centrality = CalculateCentralityScore( city );

			// Weighted combination
			double score =	classifier.DistanceWeight * distance +	classifier.AngleWeight * angle +	classifier.CentralityWeight * centrality;

			if( score < bestScore )
			{
				bestScore = score;
				bestCity = city;
			}
		}

		return bestCity;
	}

	/// <summary>
	/// Calculate angle score (prefer convex hull)
	/// </summary>
	double CalculateAngleScore( int from, int to )
	{
		// Simple heuristic: prefer angles closer to 180 degrees (straight line)
		// This is a simplified version - could be more sophisticated
		return Math.Abs( map[ from, to ].Weight - map[ to, from ].Weight );
	}

	/// <summary>
	/// Calculate centrality score (distance from map center)
	/// </summary>
	/// <remarks>Simplified - just use city index as proxy</remarks>
	double CalculateCentralityScore( int city ) => Math.Abs( city - map.Cities / 2.0 ) / map.Cities;
	

	/// <summary>
	/// Updates classifier fitness based on tour quality (credit assignment)
	/// </summary>
	void UpdateFitness( double tourLength )
	{
		// Update recently used classifiers
		var recentClassifiers = classifiers.Where( c => c.TimeStamp == timeStep ).ToList();

		foreach( var classifier in recentClassifiers )
		{
			// Update prediction using moving average
			classifier.Prediction += settings.LearningRate * ( tourLength - classifier.Prediction );

			// Calculate prediction error
			double absError = Math.Abs( tourLength - classifier.Prediction );
			classifier.Error += settings.LearningRate * ( absError - classifier.Error );
						
			if( classifier.Error > 0 ) // Update fitness (inverse of error - higher accuracy = higher fitness)
			{
				double accuracy = 1.0 / ( classifier.Error + 1.0 );
				classifier.Fitness = accuracy;
			}
		}
	}

	/// <summary>
	/// Genetic algorithm for rule discovery
	/// </summary>
	void DiscoverRules()
	{
		// Select parent classifiers based on fitness
		var parents = classifiers.OrderByDescending( c => c.Fitness ).Take( 20 ).ToList();
		if( parents.Count < 2 ) return;

		// Create offspring through crossover and mutation
		for( int i = 0; i < 10; i++ )
		{
			var parent1 = parents[ Random.Shared.Next( parents.Count ) ];
			var parent2 = parents[ Random.Shared.Next( parents.Count ) ];

			// Crossover
			Classifier offspring;
			if( Random.Shared.NextDouble() < settings.CrossoverRate )
			{
				double alpha = Random.Shared.NextDouble();
				offspring = new Classifier(
					alpha * parent1.DistanceWeight + ( 1 - alpha ) * parent2.DistanceWeight,
					alpha * parent1.AngleWeight + ( 1 - alpha ) * parent2.AngleWeight,
					alpha * parent1.CentralityWeight + ( 1 - alpha ) * parent2.CentralityWeight
				);
			}
			else
			{
				offspring = parent1.Clone();
			}

			// Mutation
			if( Random.Shared.NextDouble() < settings.MutationRate )
			{
				offspring.DistanceWeight += ( Random.Shared.NextDouble() - 0.5 ) * 0.2;
				offspring.AngleWeight += ( Random.Shared.NextDouble() - 0.5 ) * 0.2;
				offspring.CentralityWeight += ( Random.Shared.NextDouble() - 0.5 ) * 0.2;

				// Clamp to [0, 1] and normalize
				offspring.DistanceWeight = Math.Clamp( offspring.DistanceWeight, 0, 1 );
				offspring.AngleWeight = Math.Clamp( offspring.AngleWeight, 0, 1 );
				offspring.CentralityWeight = Math.Clamp( offspring.CentralityWeight, 0, 1 );

				double sum = offspring.DistanceWeight + offspring.AngleWeight + offspring.CentralityWeight;
				if( sum > 0 )
				{
					offspring.DistanceWeight /= sum;
					offspring.AngleWeight /= sum;
					offspring.CentralityWeight /= sum;
				}
			}

			classifiers.Add( offspring );
		}
	}

	/// <summary>
	/// Deletes weak classifiers when population is too large
	/// </summary>
	void DeleteClassifiers()
	{
		int toDelete = classifiers.Count - settings.MaxClassifiers;
		if( toDelete <= 0 ) return;

		// Remove classifiers with low fitness and high experience
		var candidates = classifiers.Where( c => c.Experience > settings.DeletionThreshold )
			.OrderBy( c => c.Fitness ).Take( toDelete ).ToList();

		candidates.ForEach( c => classifiers.Remove( c ) );
		//foreach( var classifier in candidates )
		//{
		//	classifiers.Remove( classifier );
		//}
	}
}

/// <summary>
/// Configuration settings for Learning Classifier System algorithm
/// </summary>
public class LcsSettings : TspConfigurationBase
{
	/// <summary>
	/// Maximum number of classifiers in the population
	/// </summary>
	public int MaxClassifiers { get; set; } = 200;

	/// <summary>
	/// Learning rate for updating predictions (beta)
	/// </summary>
	public double LearningRate { get; set; } = 0.2;

	/// <summary>
	/// Discount factor for fitness calculation
	/// </summary>
	public double DiscountFactor { get; set; } = 0.1;

	/// <summary>
	/// Threshold for triggering genetic algorithm
	/// </summary>
	public int GaThreshold { get; set; } = 25;

	/// <summary>
	/// Crossover probability
	/// </summary>
	public double CrossoverRate { get; set; } = 0.8;

	/// <summary>
	/// Mutation probability
	/// </summary>
	public double MutationRate { get; set; } = 0.04;

	/// <summary>
	/// Deletion threshold - remove classifiers with experience > this and low fitness
	/// </summary>
	public int DeletionThreshold { get; set; } = 20;

	/// <summary>
	/// Fraction of fitness for tournament selection in deletion
	/// </summary>
	public double DeletionFitnessFraction { get; set; } = 0.1;
}
