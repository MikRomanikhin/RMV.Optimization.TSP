using System;

namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// Edges collection for ACO
/// </summary>
public class AcoEdges : Dictionary<(int, int), AcoEdge>
{

	#region General ------------------------------------------------------------

	/// <summary>
	/// Indexer
	/// </summary>	
	public AcoEdge this[ int head, int tail ] => base.ContainsKey( (head, tail) ) ? base[ (head, tail) ] : base[ (tail, head) ];	

	public void Add( AcoEdge edge ) => base.Add( (edge.Head, edge.Tail), edge );

	public AcoSettings Settings { get; set; }	

	#endregion


	#region Path ---------------------------------------------------------------

	/// <summary>
	/// End-to-end tour for ACO ant
	/// </summary>
	/// /// <param name="ant">target ant</param>
	public void BuildPathAs( Ant ant )
	{
		for( int city = 0; city < ant.Cities - 2; city++ )
		{
			int nextCity = ProbSelect( ant );

			ant.Move( nextCity );
		}

		ant.Tour = Evaluate( ant );
	}


	/// <summary>
	/// End-to-end tour for ACS ant
	/// </summary>
	/// <param name="ant">target ant</param>
	public void BuildPathAcs( Ant ant )
	{
		for( int city = 0; city < ant.Cities - 2; city++ )
		{
			int current = ant.CurrentCity;

			int nextCity = ( Random.Shared.NextDouble() < this.Settings.Greedy ) ? GreedySelect( ant ) : ProbSelect( ant );

			ant.Move( nextCity );

			this[ current, nextCity ].Update(); //local pheromone update after traversal
		}

		ant.Tour = Evaluate( ant );
	}
	

	/// <summary>
	/// Calculate tour length for the ant
	/// </summary>	
	/// <param name="ant">target ant</param>
	double Evaluate( Ant ant ) => Enumerable.Range( 0, ant.Cities ).Sum( i => i < ant.Cities - 1
		? this[ ant.Path[ i ], ant.Path[ i + 1 ] ].Weight 
		: this[ ant.Path[ i ], ant.Path[ 0 ] ].Weight );

	#endregion


	#region Select -------------------------------------------------------------	

	/// <summary>
	/// Greedy ACS city selection — picks best from candidate list
	/// </summary>
	int GreedySelect( Ant ant ) => GetCandidates( ant ).MaxBy( next => this[ ant.CurrentCity, next ].Chance );

	/// <summary>
	/// Greedy ACS city selection
	/// </summary>
	//int GreedySelect( Ant ant ) => ant.Available.MaxBy( next => this[ ant.CurrentCity, next ].Chance );


	/// <summary>
	/// Selects next city for target ant by probability
	/// </summary>
	int ProbSelect( Ant ant )
	{
		(int node, double chance)[] selection = BuildWheel( ant );

		double rand = Random.Shared.NextDouble();

		double cumulative = 0;

		for( int i = 0; i < selection.Length; i++ )
		{
			cumulative += selection[ i ].chance;

			if( cumulative >= rand ) return selection[ i ].node;
		}

		return selection[ ^1 ].node; // fallback to last candidate
	}
	

	/// <summary>
	/// Builds Roulette Wheel selection probability
	/// </summary>
	(int, double)[] BuildWheel( Ant ant )
	{
		var candidates = GetCandidates( ant );

		double denom = candidates.Sum( city => this[ ant.CurrentCity, city ].Chance );

		if( denom <= 0 ) return [ .. candidates.Select( city => (city, 1.0 / candidates.Count()) ) ];

		return [ .. candidates.Select( city => (city, this[ ant.CurrentCity, city ].Chance / denom) ) ];
	}
	

	/// <summary>
	/// Finds list of cities candidates
	/// </summary>
	IEnumerable<int> GetCandidates( Ant ant )
	{
		if( this.Settings.Neighbours < 1 || this.Settings.Neighbours >= ant.Available.Count )	return ant.Available;

		// Use nearest-neighbour candidate list to reduce search space
		var nearest = ant.Available.Select( n => this[ ant.CurrentCity, n ] ).OrderBy( e => e.Weight )
			.Take( this.Settings.Neighbours ).Select( e => e.Tail == ant.CurrentCity ? e.Head : e.Tail );

		// If candidate list is too restrictive, fall back to all available cities
		return nearest.Any() ? nearest : ant.Available;
	}			

	#endregion


	#region Evaporate ----------------------------------------------------------

	/// <summary>
	/// Evaporation
	/// </summary>	
	public void Evaporate() => Parallel.ForEach( this.Values, v => v.Evaporate() );

	#endregion


	#region Deposit ------------------------------------------------------------

	/// <summary>
	/// Pheromone deposit (Ant System)
	/// </summary>	
	public void Deposit( Ant ant )
	{
		double amount = this.Settings.Q / ant.Tour;

		for( int i = 0; i < ant.Cities; i++ )
		{
			var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];

			edge.Pheromone += amount;
		}
	}
	

	/// <summary>
	/// Rank-based pheromone deposit with elitist best-ant reinforcement
	/// </summary>
	/// <param name="ants">colony ants</param>
	/// <param name="eliteCount">number of top ants that deposit pheromone</param>
	/// <param name="bestAnt">global best ant for extra reinforcement (optional)</param>
	public void Deposit( IEnumerable<Ant> ants, int eliteCount, Ant bestAnt = null )
	{
		var ranked = ants.OrderBy( a => a.Tour ).Take( eliteCount ).ToList();

		for( int rank = 0; rank < ranked.Count; rank++ )
		{
			var ant = ranked[ rank ];
			double weight = ( double )( eliteCount - rank ) / eliteCount;
			double amount = weight * this.Settings.Q / ant.Tour;

			DepositOnPath( ant, amount );
		}
				
		if( bestAnt != null ) // Elitist reinforcement: global best ant deposits extra pheromone
		{
			double bestAmount = this.Settings.Q / bestAnt.Tour;

			DepositOnPath( bestAnt, bestAmount );
		}
	}

	/// <summary>
	/// Deposits pheromone along an ant's complete tour path
	/// </summary>
	void DepositOnPath( Ant ant, double amount )
	{
		for( int i = 0; i < ant.Cities; i++ )
		{
			var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];

			edge.Pheromone += amount;
		}
	}	

	/// <summary>
	/// Pheromone update (Ant Colony System)
	/// </summary>
	/// <param name="ant">best ant (local or global)</param>
	public void Update( Ant ant )
	{
		double p1 = 1.0 - this.Settings.P1;
		double p2 = this.Settings.P1 / ant.Tour;

		for( int i = 0; i < ant.Cities; i++ )
		{
			var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];

			edge.Pheromone = p1 * edge.Pheromone + p2;
		}

		//Parallel.For( 0, ant.Cities, i => {
		//	var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];
		//	edge.Pheromone += ( p1 * edge.Pheromone + p2 );
		//} );
	}

	/// <summary>
	/// Pheromone update (Max-Min Ant System)
	/// 1. Evaporate ALL edges
	/// 2. Deposit only on best ant's path
	/// 3. Clamp all pheromone values to [min, max]
	/// </summary>	
	public void Update( Ant ant, double min, double max )
	{
		double rho = this.Settings.Rho;
		double deposit = 1.0 / ant.Tour;

		// Step 1: Evaporate all edges
		foreach( var edge in this.Values )
		{
			edge.Pheromone *= ( 1.0 - rho );
		}

		// Step 2: Deposit on best ant's path only
		for( int i = 0; i < ant.Cities; i++ )
		{
			var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];

			edge.Pheromone += deposit;
		}

		// Step 3: Clamp all edges to [min, max]
		foreach( var edge in this.Values )
		{
			edge.Pheromone = Math.Clamp( edge.Pheromone, min, max );
		}
	}

	#endregion


	#region Reset --------------------------------------------------------------

	/// <summary>
	/// Reset pheromone (MMA)
	/// </summary>	
	public void Reset( double amount ) => Parallel.ForEach( this.Values, v => v.Reset( amount ) );

	#endregion
	
}
