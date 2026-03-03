using System.ComponentModel;

using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// ACO Map
/// </summary>
public class AcoMap 
{

	#region Properties --------------------------------------------------------			

	/// <summary>
	/// Best for all epochs
	/// </summary>
	public Ant Best 
	{ 
		get => this.Colony.Best;
		set => this.Colony.Best = value; 
	}	

	#endregion


	#region Initialize ---------------------------------------------------------

	public AcoMap( TspMap map, AcoSettings settings )
	{
		this.Settings = settings;

		//this.Name = map.Name;
		this.Cities = map.Cities;
		this.Nodes = map.Nodes;
		this.Algorithm = map.Algorithm;

		Initialize();
	}
	

	readonly TspAlgorithm Algorithm; //ACO algorithm
	readonly AcoSettings Settings; // Configuration parameters
	readonly int Cities = 0; // Total cities

	readonly TspNodes Nodes = []; // Nodes collection	
	readonly AcoEdges Edges = []; // Edges dictionary
	
	Colony Colony;   // Ants collection	

	/// <summary>
	/// Initialize Map and Ants
	/// </summary>	
	void Initialize()
	{	
		BuildEdges();

		BuildAnts();		
	}

	/// <summary>
	/// Create Edges from Nodes
	/// </summary>
	void BuildEdges()
	{
		this.Edges.Settings = this.Settings;
		
		double pheromone = InitPheromone();

		for( int i = 0; i < this.Cities; i++ )
		{
			var from = this.Nodes[ i ];

			for( int j = i + 1; j < this.Cities; j++ )
			{
				var to = this.Nodes[ j ];				

				this.Edges.Add( new AcoEdge( from.ID, to.ID, from.DistanceTo( to ), pheromone, Settings ) );				
			}
		}		
	}

	double InitPheromone()
	{
		return this.Algorithm switch 
		{
			TspAlgorithm.AntSystem => this.Cities / this.Settings.Nearest,//1.0 / this.Cities, //this.Settings.Size / this.Settings.Nearest,

			TspAlgorithm.AntColonySystem => 1.0 / ( this.Cities * this.Settings.Nearest ),

			TspAlgorithm.MaxMinAnt => 1.0 / ( ( 1.0 - this.Settings.Rho ) * this.Settings.Nearest ),

			_ => throw new ArgumentException( $"Unknown algorithm {this.Algorithm}" ),
		};
	}

	void BuildAnts() => this.Colony = new Colony( this.Settings, this.Cities, this.Edges );
		

	/// <summary>
	/// Indexer, finds edge by head and tail nodes
	/// </summary>	
	public AcoEdge this[ int head, int tail ] => this.Edges[ head, tail ];

	#endregion


	#region RunEpoch -----------------------------------------------------------

	/// <summary>
	/// ACO one iteration
	/// </summary>
	public TspResult RunEpoch( TspResult best )
	{
		Restart();

		return this.Algorithm switch 
		{
			TspAlgorithm.AntSystem => RunAS(),

			TspAlgorithm.AntColonySystem => RunACS(),

			//TspAlgorithm.MaxMinAnt => RunMMA( noChanges ),

			_ => throw new InvalidEnumArgumentException( $"Unknown method:{Algorithm}" ),
		};
	}

	/// <summary>
	/// Ant System Tour
	/// </summary>
	TspResult RunAS()
	{
		MoveAS();

		var result = Evaluate();

		Evaporate();

		Deposit();

		Smooth();

		return result;
	}

	/// <summary>
	/// Ant Colony System Tour
	/// </summary>
	TspResult RunACS()
	{
		MoveACS();

		var result = Evaluate();

		UpdateACS();

		return result;
	}

	#endregion


	#region Move ---------------------------------------------------------------

	/// <summary>
	/// Move AS
	/// </summary>
	void MoveAS() => Parallel.ForEach( this.Colony,  this.Edges.BuildPathAs  );
		//this.Colony.ForEach(  this.Edges.BuildPathAs  );


	/// <summary>
	/// Move ACS
	/// </summary>
	void MoveACS() => //Parallel.ForEach( this.Colony, this.Edges.BuildPathAcs );
		this.Colony.ForEach(  this.Edges.BuildPathAcs  );		

	#endregion


	#region Evaluate -----------------------------------------------------------

	/// <summary>
	/// Calculates tour length and evaluates the best
	/// </summary>
	TspResult Evaluate() => this.Colony.Evaluate();	

	#endregion


	#region Evaporate ----------------------------------------------------------

	/// <summary>
	/// Pheromone evaporation (Ant System)
	/// </summary>
	void Evaporate() => this.Edges.Evaporate();

	#endregion


	#region Deposit ------------------------------------------------------------

	/// <summary>
	/// Pheromone deposit (Ant System)
	/// </summary>
	void Deposit() => this.Colony.Deposit( this.Settings.Elite );

	/// <summary>
	/// Pheromone smoothing — reduces gap between max and min pheromone
	/// to counteract stagnation in AS
	/// </summary>
	void Smooth()
	{
		double max = this.Edges.Values.Max( e => e.Pheromone );

		if( max <= 0 ) return;

		double avg = this.Edges.Values.Average( e => e.Pheromone );
		double ratio = max / avg;

		if( ratio > 10.0 ) // pheromone concentration too extreme
		{
			double factor = 0.9;

			foreach( var edge in this.Edges.Values )
			{
				edge.Pheromone = factor * edge.Pheromone + ( 1.0 - factor ) * avg;
			}
		}
	}

	/// <summary>
	/// Global pheromone update (Ant Colony System)
	/// Always reinforces global best — this is the canonical ACS rule
	/// </summary>
	void UpdateACS() => this.Edges.Update( this.Best );


	/// <summary>
	/// Global pheromone update (Ant Colony System)
	/// Gradually shifts from iteration-best to global-best as solutions converge
	/// </summary>
	//void UpdateACS()
	//{
	//	double ratio = this.Best.Tour > 0 ? this.Colony.Current.Tour / this.Best.Tour : 1.0;

	//	// When current ≈ best (ratio → 1.0), prefer global best to intensify
	//	// When current >> best (ratio >> 1), prefer iteration best to explore
	//	var best = ratio < 1.05 ? this.Best : this.Colony.Current;

	//	this.Edges.Update( best );
	//}

	/// <summary>
	/// Global pheromone update (Ant Colony System)
	/// </summary>
	//void UpdateACS()
	//{
	//	var best = Random.Shared.Next( 5 ) < 2 ? this.Best : this.Colony.Current;

	//	this.Edges.Update( best );
	//}

	/// <summary>
	/// Global pheromone update (MAX-MIN Ant System)
	/// </summary>
	void UpdateMMA()
	{
		var best = Random.Shared.Next( 5 ) < 1 ? this.Best : this.Colony.Current;

		double max = 1.0 / ( ( 1.0 - this.Settings.Rho ) * best.Tour );
		double min = ( max * ( 1.0 - this.Settings.P ) ) / ( ( this.Cities * 0.5 - 1.0 ) * this.Settings.P );
		//double min = ( max * ( 1.0 - this.Settings.P * this.Cities ) ) / ( ( this.Cities * 0.5 - 1.0 ) * this.Settings.P * this.Cities );

		this.Edges.Update( best, min, max );
	}

	#endregion


	#region Restart ------------------------------------------------------------

	/// <summary>
	/// Reinitialize the ant population to start another tour around the graph
	/// </summary>
	void Restart() => this.Colony.Restart();

	#endregion


	#region Reset --------------------------------------------------------------

	/// <summary>
	/// Reset pheromone (MMA)
	/// </summary>	
	void Reset()
	{
		double amount = 1.0 / ( ( 1.0 - this.Settings.Rho ) * this.Best.Tour );

		this.Edges.Reset( amount );
	}

	#endregion


	#region obsolete
	//public bool RunEpoch( int noChanges )
	//{
	//	Restart();

	//	return this.Algorithm switch 
	//	{
	//		TspAlgorithm.AntSystem => RunAS(),

	//		TspAlgorithm.AntColonySystem => RunACS(),

	//		TspAlgorithm.MaxMinAnt => RunMMA( noChanges ),

	//		_ => throw new InvalidEnumArgumentException( $"Unknown method:{Algorithm}" ),
	//	};
	//}
	/// <summary>
	/// Max-Min Ant System Tour
	/// </summary>
	//TspResult RunMMA( int noChanges )
	//{
	//	bool improved = false;

	//	if( noChanges > Settings.Stagnation )
	//	{
	//		Reset();
	//		improved = true;
	//	}

	//	MoveAS();

	//	improved = improved | Evaluate();

	//	UpdateMMA();

	//	return improved;
	//}

	///// <summary>
	///// Ant System Tour
	///// </summary>
	//bool RunAS()
	//{
	//	MoveAS();

	//	bool improved = Evaluate();

	//	Evaporate();

	//	Deposit();

	//	return improved;
	//}

	///// <summary>
	///// Ant Colony System Tour
	///// </summary>
	//bool RunACS()
	//{
	//	MoveACS();

	//	bool improved = Evaluate();		

	//	UpdateACS();

	//	return improved;
	//}

	///// <summary>
	///// Max-Min Ant System Tour
	///// </summary>
	//bool RunMMA( int noChanges )
	//{
	//	bool improved = false;

	//	if( noChanges > Settings.Stagnation )
	//	{
	//		Reset();
	//		improved = true;
	//	}

	//	MoveAS();

	//	improved = improved | Evaluate();		

	//	UpdateMMA();

	//	return improved;
	//}

	#endregion
}
