using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.Algorithms;

namespace RMV.Optimization.TSP.UI;

/// <summary>
/// Primary Controller
/// </summary>
class TspController( Form1 form )
{	
	public TspMap Map { get; set; }
	//TspConfigurationBase settings;

	TspAlgorithmBase Algorithm { get; set; }

	readonly Form1 form = form ?? throw new ArgumentNullException( nameof( form ) );

	public TspAlgorithmBase BuildAlgorithm( TspAlgorithm algorithm )
	{
		this.Map.Algorithm = algorithm;

		this.Algorithm = algorithm switch 
		{
			TspAlgorithm.Random => new RandomSearch( this.Map ),
			TspAlgorithm.NearestNeighbour => new NearestNeighbour( this.Map ),
			TspAlgorithm.ShortestEdge => new ShortestEdge( this.Map ),
			TspAlgorithm.FarthestInsert => new LongestEdge( this.Map ),
			TspAlgorithm.Beam => new BeamSearch( this.Map ),
			//TspAlgorithm.Pilot => new K_NearestSearch( this.Map ),
			
			TspAlgorithm.IteratedLocal => new IteratedLocalSearch( this.Map ),
			TspAlgorithm.GuidedLocal => new GuidedLocalSearch( this.Map ),
			TspAlgorithm.VariableNeiborhood => new VarialbleNeiborhoodSearch( this.Map ),
			TspAlgorithm.GRASP => new RandomizedAdaptiveSearch( this.Map ),
			TspAlgorithm.Scatter => new ScatterSearch( this.Map ),
			TspAlgorithm.Taboo => new ReactiveTabuSearch( this.Map ),
			
			TspAlgorithm.Annealing => new SimulatedAnnealing( this.Map ),
			TspAlgorithm.Genetic => new GeneticAlgorithm( this.Map ),
			TspAlgorithm.ES => new EvolutionStrategies( this.Map ),
			TspAlgorithm.DE => new DifferentialEvolution( this.Map ),
			TspAlgorithm.EP => new EvolutionaryProgramming( this.Map ),
			TspAlgorithm.Classifier => new LearningClassifier( this.Map ),
			
			TspAlgorithm.AntSystem => new AntColonySearch( this.Map ),
			TspAlgorithm.AntColonySystem => new AntColonySearch( this.Map ),
			TspAlgorithm.MaxMinAnt => new AntColonySearch( this.Map ),
			TspAlgorithm.PSO => new ParticleSwarm( this.Map ),
			TspAlgorithm.QLearning => new QLearning( this.Map ),
			_ => throw new ArgumentException( $"Unknown algorithm {algorithm}" ),
		};

		return this.Algorithm;
	}

	/// <summary>
	/// Configures the particular TSP algorithm
	/// </summary>	
	public async Task Run( TspAlgorithm algorithm, CancellationToken? token )
	{
		this.Map.Algorithm = algorithm;

		#region obsolete
		//TspAlgorithmBase tspAlgorithm = algorithm switch 
		//{
		//	TspAlgorithm.Random => new RandomSearch( this.Map ),
		//	TspAlgorithm.NearestNeighbour => new NearestNeighbour( this.Map ),
		//	TspAlgorithm.ShortestEdge => new ShortestEdge( this.Map ),			
		//	TspAlgorithm.FarthestInsert => new LongestEdge( this.Map ),
		//	TspAlgorithm.Beam => new BeamSearch( this.Map ),
		//	TspAlgorithm.Pilot => new K_NearestSearch( this.Map ),

		//	TspAlgorithm.IteratedLocal => new IteratedLocalSearch( this.Map ),
		//	TspAlgorithm.GuidedLocal => new GuidedLocalSearch( this.Map ),
		//	TspAlgorithm.VariableNeiborhood => new VarialbleNeiborhoodSearch( this.Map ),
		//	TspAlgorithm.GRASP => new RandomizedAdaptiveSearch( this.Map ),
		//	TspAlgorithm.Scatter => new ScatterSearch( this.Map ),
		//	TspAlgorithm.Taboo => new TabuSearch( this.Map ),
		//	TspAlgorithm.Reactive => new ReactiveTabuSearch( this.Map ),

		//	TspAlgorithm.Annealing => new SimulatedAnnealing( this.Map ),

		//	TspAlgorithm.Genetic => new GeneticAlgorithm( this.Map ),
		//	TspAlgorithm.ES => new EvolutionStrategies( this.Map ),
		//	TspAlgorithm.DE => new DifferentialEvolution( this.Map ),
		//	TspAlgorithm.EP => new EvolutionaryProgramming( this.Map ),
		//	TspAlgorithm.Classifier => new LearningClassifier( this.Map ),

		//	TspAlgorithm.AntSystem => new AntColonySearch( this.Map ),
		//	TspAlgorithm.AntColonySystem => new AntColonySearch( this.Map ),
		//	TspAlgorithm.MaxMinAnt => new AntColonySearch( this.Map ),
		//	TspAlgorithm.PSO => new ParticleSwarm( this.Map ),

		//	TspAlgorithm.QLearning => new QLearning( this.Map ),

		//	_ => throw new ArgumentException( $"Unknown algorithm {algorithm}" ),
		//};
		#endregion		

		this.Algorithm.OnDraw += HandleDraw;		

		try
		{
			await ( ( ITspAsync )this.Algorithm ).RunAsync( token );
		}
		catch( OperationCanceledException )
		{
			this.form.UpdateStatusLabel( "Operation cancelled by user" );			
		}		
	}

	void HandleDraw( object? sender, DrawEventArgs ea ) => OnDraw?.Invoke( null, ea );

	/// <summary>
	/// Event handler for drawing the TSP map
	/// </summary>
	public static EventHandler<DrawEventArgs> OnDraw;	
}
