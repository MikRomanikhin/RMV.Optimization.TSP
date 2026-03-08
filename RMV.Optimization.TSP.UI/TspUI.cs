using RMV.Optimization.TSP.Algorithms;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.UI;

public partial class TspUI : Form
{
	#region Initialize --------------------------------------------------------

	readonly TspController controller;
	readonly CancellationTokenSource cts = new();

	TspControllerState state = TspControllerState.Stopped;
	TspAlgorithmBase currentAlgorithm;

	TspMap map;

	public TspUI()
	{
		InitializeComponent();

		this.controller = new TspController( this );
	}

	#endregion


	#region Read TSP file ------------------------------------------------------

	/// <summary>
	/// Create map from file
	/// </summary>		
	void GetMapMenuItem_Click( object sender, EventArgs e )
	{
		OpenFileDialog dialog = new() { DefaultExt = "tsp", Filter = "TSP files (*.tsp)|*.tsp" };

		if( dialog.ShowDialog() == DialogResult.OK )
		{
			string[] csv = File.ReadAllLines( dialog.FileName );

			this.map = new TspMap {
				Name = csv[ 0 ].Split( ':' )[ 1 ],
				Comment = csv[ 1 ].Split( ":" )[ 1 ],
				Cities = int.Parse( csv[ 3 ].Split( ":" )[ 1 ] ),
				Nodes = []
			};

			mapControl.Path = [];
			mapControl.Map = GenerateNodes( csv );

			this.map.Initialize();

			this.controller.Map = this.map;
		}
	}

	/// <summary>
	/// Display the optimal path
	/// </summary>	
	void GetOptimalMenuItem_Click( object sender, EventArgs e )
	{
		OpenFileDialog dialog = new() { DefaultExt = "tour", Filter = "Tour files (*.tour)|*.tour" };

		if( dialog.ShowDialog() == DialogResult.OK )
		{
			string[] csv = File.ReadAllLines( dialog.FileName );

			string[] tmp = csv.Skip( Array.IndexOf( csv, "TOUR_SECTION" ) + 1 ).SkipLast( 2 ).ToArray();

			int[] path = Array.ConvertAll( tmp, new Converter<string, int>( StringToInt ) );

			mapControl.Path = [];
			mapControl.Optimal = Array.ConvertAll( [ .. path ], new Converter<int, ushort>( IntToUshort ) );
		}
	}

	#endregion


	#region GenerateNodes ------------------------------------------------------

	/// <summary>
	/// Generates 2D array of node coordinates from the provided CSV data and adds the corresponding nodes to the map.
	/// </summary>
	/// <remarks>
	/// Method expects the CSV data to be formatted such that each city's node information appears in
	/// consecutive entries, starting at a fixed offset. The generated coordinates are scaled based on the map's dimensions.
	/// </remarks>
	/// <param name="csv">An array of strings containing CSV-formatted data, where each relevant entry represents a city's node information.
	/// The array must contain at least as many entries as required to describe all cities.</param>
	/// <returns>A 2D array where each row contains the X and Y coordinates of a node, scaled to the map's display area.</returns>
	int[,] GenerateNodes( string[] csv )
	{
		int cities = this.map.Cities;

		var coord = new int[ cities, 2 ]; //coordinates array

		for( int i = 6; i < 6 + cities; i++ ) //build map nodes
		{
			var tmp = csv[ i ].Trim().Split( ' ' );

			int id = int.Parse( tmp[ 0 ] ) - 1;

			var node = new TspNode( double.Parse( tmp[ 1 ] ), double.Parse( tmp[ 2 ] ), id );

			this.map.Nodes.Add( node );
		}

		double minX = this.map.Nodes.MinX;
		double minY = this.map.Nodes.MinY;

		double scaleX = mapControl.MaxX / this.map.Nodes.MaxX;
		double scaleY = mapControl.MaxY / this.map.Nodes.MaxY;

		for( int i = 0; i < cities; i++ )
		{
			coord[ i, 0 ] = ( int )Math.Round( ( this.map.Nodes[ i ].X - minX ) * scaleX );
			coord[ i, 1 ] = ( int )Math.Round( ( this.map.Nodes[ i ].Y - minY ) * scaleY );
		}

		return coord;
	}

	#endregion


	#region Greedy -------------------------------------------------------------

	/// <summary>
	/// Nearest Neighbor path
	/// </summary>		
	async void NearestNeighbourMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.NearestNeighbour );

	/// <summary>
	/// Shortest Edge insert
	/// </summary>
	async void ShortestEdgeMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.ShortestEdge );

	//void NnWorker_ProgressChanged( object? sender, ProgressChangedEventArgs e )
	//{
	//	ProgressBar.Value = e.ProgressPercentage;
	//}

	/// <summary>
	/// Farthest Edge Insert
	/// </summary>	
	async void FarthestInsertMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.FarthestInsert );

	/// <summary>
	/// Beam Search
	/// </summary>	
	async void BeamSearchMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Beam );


	/// <summary>
	/// Pilot search
	/// </summary>		
	async void PilotMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Pilot );

	#endregion


	#region Simulated annealing ------------------------------------------------

	/// <summary>
	/// Simulated Annealing
	/// </summary>	
	async void Annealing_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Annealing );

	#endregion


	#region Stochastic ---------------------------------------------------------

	/// <summary>
	/// Random path
	/// </summary>		
	async void RandomSearchMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Random );

	/// <summary>
	/// Iterated Local Search
	/// </summary>	
	async void IteratedLocalSearch_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.IteratedLocal );

	/// <summary>
	/// Guided Local Search
	/// </summary>	
	async void GuidedLocalSearch_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.GuidedLocal );

	/// <summary>
	/// Variable Neighborhood Search
	/// </summary>	
	async void VariableNeighborhoodSearch_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.VariableNeiborhood );

	/// <summary>
	/// Randomized Adaptive Search
	/// </summary>	
	async void RandomizedAdaptiveSearch_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.GRASP );

	/// <summary>
	/// Scatter Search
	/// </summary>	
	async void ScatterSearchMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Scatter );

	/// <summary>
	/// Taboo Search
	/// </summary>	
	async void TabooSearchMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Taboo );		

	#endregion


	#region Swarm --------------------------------------------------------------

	/// <summary>
	/// Ant System
	/// </summary>	
	async void AntSystemMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.AntSystem );

	/// <summary>
	/// Ant Colony System
	/// </summary>	
	async void AntColonySystemMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.AntColonySystem );

	/// <summary>
	/// Max-Min Ant System
	/// </summary>	
	async void MinMaxAntMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.MaxMinAnt );

	/// <summary>
	/// Particle Swarm Optimization
	/// </summary>	
	async void PsoMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.PSO );

	#endregion


	#region Evolution ----------------------------------------------------------

	/// <summary>
	/// Genetic Algorithm
	/// </summary>	
	async void GeneticAlgorithmMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Genetic );

	/// <summary>
	/// Evolution Strategies
	/// </summary>	
	async void EvolutionStrategiesMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.ES );

	/// <summary>
	/// Differential Evolution
	/// </summary>	
	async void DifferentialEvolutionMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.DE );

	/// <summary>
	/// Evolutionary Programming
	/// </summary>	
	async void EvolutionaryProgrammingMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.EP );

	/// <summary>
	/// Learning Classifier 
	/// </summary>	
	async void LearningClassifierMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.Classifier );

	#endregion


	#region Q-Learning ---------------------------------------------------------

	/// <summary>
	/// Q-Learning
	/// </summary>	
	async void LearningMenuItem_Click( object sender, EventArgs e ) => await Execute( TspAlgorithm.QLearning );


	#endregion


	#region Stop/Pause/Resume --------------------------------------------------

	/// <summary>
	/// Handle cancellation
	/// </summary>	
	void buttonStop_Click( object sender, EventArgs e )
	{
		this.cts.Cancel();

		this.state = TspControllerState.Stopped;
		this.buttonPause.Enabled = false;
	}

	enum TspControllerState { Running, Paused, Stopped }

	/// <summary>
	/// Pause/Resume the algorithm execution
	/// </summary> 	
	void buttonPause_Click( object sender, EventArgs e )
	{
		switch( this.state )
		{
			case TspControllerState.Running:
				this.currentAlgorithm?.Pause();
				this.state = TspControllerState.Paused;
				this.buttonPause.Text = "Resume";
				break;

			case TspControllerState.Paused:
				this.currentAlgorithm?.Resume();
				this.state = TspControllerState.Running;
				this.buttonPause.Text = "Pause";
				break;
		}
	}


	#endregion


	#region Misc --------------------------------------------------------------

	ushort IntToUshort( int data ) => ( ushort )data;
	int StringToInt( string data ) => int.Parse( data );

	/// <summary>
	/// Handles Controller calls
	/// </summary>	
	async Task Execute( TspAlgorithm algorithmType, CancellationToken? token = null )
	{
		this.state = TspControllerState.Running;

		TspController.OnDraw += HandleDrawEvent;

		this.currentAlgorithm = this.controller.BuildAlgorithm( algorithmType );

		await controller.Run( algorithmType, token ?? cts.Token );

		TspController.OnDraw -= HandleDrawEvent;
	}

	/// <summary>
	/// Update status label
	/// </summary>	
	public void UpdateStatusLabel( string text )
	{
		this.StatusLabel.Text = text;

		Task.Run( () => this.statusStrip.Update() );
	}

	void HandleDrawEvent( object sender, DrawEventArgs ea )
	{
		if( ea.Path != null )
		{
			mapControl.Path = Array.ConvertAll( [ .. ea.Path ], new Converter<int, ushort>( IntToUshort ) );
		}

		UpdateStatusLabel( $"Tour={ea.Tour:N2} Iterations={ea.Counter} Time={ea.Time}" );

		//nnWorker.ReportProgress( ea.Counter * 100 / this.map.Cities );		
	}

	#endregion
	
}