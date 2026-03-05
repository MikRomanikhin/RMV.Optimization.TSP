using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.Algorithms;

namespace RMV.Optimization.TSP.UI;

/// <summary>
/// Controller class for managing TSP algorithms and interactions with UI 
/// </summary>
class TspController( Form1 form )
{	
	public TspMap Map { get; set; }
	//TspConfigurationBase settings;

	TspAlgorithmBase Algorithm { get; set; }

	readonly Form1 form = form ?? throw new ArgumentNullException( nameof( form ) );

	/// <summary>
	/// Creates and configures a new instance of a TSP algorithm based on the specified algorithm type.
	/// </summary>
	/// <remarks>
	/// The created algorithm instance is associated with the current map. 
	/// Subsequent calls with different algorithm types will replace the previously created algorithm instance.
	/// </remarks>
	/// <param name="algorithm">The type of Traveling Salesman Problem (TSP) algorithm to instantiate</param>
	/// <returns>A new instance of a TSP algorithm corresponding to the specified algorithm type.</returns>	
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
			TspAlgorithm.Pilot => new PilotSearch( this.Map ),			

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
	/// Runs the specified TSP algorithm asynchronously, handling user cancellation if requested.
	/// </summary>
	/// <remarks>
	/// If the operation is cancelled via the provided cancellation token, the status label is updated to indicate cancellation. 
	/// The algorithm is assigned to the map before execution, and drawing events are handled during the run.
	/// </remarks>
	/// <param name="algorithm">The TSP algorithm to execute. This algorithm will be assigned to the map and executed asynchronously.</param>
	/// <param name="token">An optional cancellation token that can be used to cancel the operation</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public async Task Run( TspAlgorithm algorithm, CancellationToken? token )
	{
		this.Map.Algorithm = algorithm;	

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

	/// <summary>
	/// Handles a draw event by invoking the associated draw event handler, if one is registered.
	/// </summary>	
	void HandleDraw( object? sender, DrawEventArgs ea ) => OnDraw?.Invoke( null, ea );


	/// <summary>
	/// Event handler for drawing the TSP map
	/// </summary>
	public static EventHandler<DrawEventArgs> OnDraw;	
}
