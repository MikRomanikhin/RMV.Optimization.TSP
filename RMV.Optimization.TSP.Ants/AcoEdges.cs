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
			int nextCity = ( Random.Shared.NextDouble() < this.Settings.Greedy ) ? GreedySelect( ant ) : ProbSelect( ant );			

			this[ ant.CurrentCity, nextCity ].Update(); //local pheromone update

			ant.Move( nextCity );
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
	/// Greedy ACS city selection
	/// </summary>
	int GreedySelect( Ant ant ) => ant.Available.MaxBy( next => this[ ant.CurrentCity, next ].Chance );
	

	/// <summary>
	/// Selects next city for target ant by probability
	/// </summary>	
	int ProbSelect( Ant ant )
	{
		(int node, double chance)[] selection = BuildWheel( ant );

		double rand = Random.Shared.NextDouble();

		int index = 0;

		double p = selection[ index ].chance;
		int city = selection[ index ].node;

		while( p < rand )
		{
			if( ++index > selection.Length - 1 ) index = 0;

			p += selection[ index ].chance;
		}

		return selection[ index ].node;
	}

	/// <summary>
	/// Builds Roulette Wheel selection probability
	/// </summary>		
	(int, double)[] BuildWheel( Ant ant )
	{
		var candidates = GetCandidates( ant );

		double denom = candidates.Sum( city => this[ ant.CurrentCity, city ].Chance );

		return candidates.Select( city => ( city, this[ ant.CurrentCity, city ].Chance / denom ) ).ToArray();
	}	

	/// <summary>
	/// Finds list of cities candidates
	/// </summary>
	IEnumerable<int> GetCandidates( Ant ant ) =>	ant.Available.Select( n => this[ ant.CurrentCity, n ] ).OrderBy( e => e.Weight )
		.Take( this.Settings.Neighbours ).Select( e => e.Tail == ant.CurrentCity ? e.Head : e.Tail );		

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

		for( int i = 0; i < ant.Cities - 1; i++ )
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
	/// </summary>	
	public void Update( Ant ant, double min, double max )
	{
		double p1 = 1.0 - this.Settings.P1;
		double p2 = this.Settings.P1 / ant.Tour;

		for( int i = 0; i < ant.Cities; i++ )
		{
			var edge = i < ant.Cities - 1 ? this[ ant.Path[ i ], ant.Path[ i + 1 ] ] : this[ ant.Path[ i ], ant.Path[ 0 ] ];

			double amount = p1 * edge.Pheromone + p2;

			edge.Pheromone = Math.Clamp( amount, min, max );
		}		
	}

	#endregion


	#region Reset --------------------------------------------------------------

	/// <summary>
	/// Reset pheromone (MMA)
	/// </summary>	
	public void Reset( double amount ) => Parallel.ForEach( this.Values, v => v.Reset( amount ) );

	#endregion


	#region obsolete
	//static double RandomDouble( double limit ) => Random.Shared.NextDouble() * limit;
	//int GreedySelect( Ant ant ) => Enumerable.Range( 0, ant.Cities ).Where( i => ant.IsAvailable( i ) ).MaxBy( i => this[ ant.CurrentCity, i ].Choice );
	//int GreedySelect( Ant ant ) => Enumerable.Range( 0, ant.Cities ).Where( i => !ant.Visited[ i ] ).MaxBy( i => this[ ant.CurrentCity, i ].Choice );
	//double GetDenominator( Ant ant ) => 
	//	Enumerable.Range( 0, ant.Cities ).Where( city => !ant.Visited[ city ] && city != ant.CurrentCity ).Sum( city => this[ ant.CurrentCity, city ].Choice );
	//double GetDenominator( Ant ant ) => Enumerable.Range( 0, ant.Cities ).Where( i => ant.IsAvailable( i ) ).Sum( i => this[ ant.CurrentCity, i ].Choice );
	//int ProbSelect( Ant ant )
	//{
	//	var selection = BuildWheel( ant );	
	//	double rand = Random.Shared.NextDouble();
	//	int index = 0;
	//	double p = selection[ index ].Item2;
	//	int city = selection[ index ].Item1;
	//	while( p < rand )
	//	{
	//		if( ++index > selection.Length - 1 ) index = 0;			
	//		p += selection[ index ].Item2;
	//	}
	//	return selection[ index ].Item1;
	//}
	//Tuple<int, double>[] BuildWheel( Ant ant )	
	//{
	//	var candidates = GetCandidates( ant );
	//	double denom = candidates.Sum( city => this[ ant.CurrentCity, city ].Chance );
	//	return candidates.Select( city => Tuple.Create( city, this[ ant.CurrentCity, city ].Chance / denom ) ).ToArray();
	//}	
	//int ProbSelect( Ant ant )
	//{	
	//	var (rand, selection) = BuildWheel( ant );
	//	double p = selection[ 0 ];
	//	int city = 0;
	//	while( p < rand )
	//	{
	//		if( ++city == ant.Cities ) city = 0;
	//		p += selection[ city ];
	//	}
	//	return city;
	//}
	//double[] BuildWheel( Ant ant )
	//{
	//	var selection = new double[ ant.Cities ];	
	//	double denom = GetDenominator( ant );		
	//	for( int to = 0; to < ant.Cities; to++ )
	//	{
	//		if( !ant.Visited[ to ] ) selection[ to ] = this[ ant.CurrentCity, to ].Choice / denom;			
	//	}
	//	return selection;
	//}	
	//int GreedySelect( Ant ant )
	//{
	//	int city = -1;
	//	double max = 0;
	//	for( int to = 0; to < ant.Cities; to++ )
	//	{
	//		if( ant.Visited[ to ] ) continue;
	//		double choice = this[ ant.CurrentCity, to ].Choice;
	//		if( choice > max )
	//		{
	//			max = choice;
	//			city = to;
	//		}
	//	}
	//	return city;
	//}
	//double GetDenominator( int from, Ant ant )
	//{
	//	double sum = 0.0;
	//	for( int to = 0; to < ant.Cities; to++ )
	//	{
	//		if( to != from && !ant.Visited[ to ] ) sum += this[ from, to ].Choice;
	//	}
	//	return sum;
	//}	
	//double Evaluate( Ant ant )
	//{
	//	double tour = 0;
	//	for( int i = 1; i < ant.Cities; i++ )
	//	{
	//		tour += this[ ant.Path[ i - 1 ], ant.Path[ i ] ].Weight;
	//	}
	//	tour += GetLastEdge( ant ).Weight; //last node - start node		
	//	return tour;
	//}
	//public void DepositAcs( Ant ant )
	//{
	//	double p1 = 1.0 - this.Settings.P1;
	//	double p2 = this.Settings.P1 / ant.Tour;
	//	Parallel.For( 0, ant.Cities - 1, i => {
	//		var edge =  this[ ant.Path[ i ], ant.Path[ i + 1 ] ];
	//		edge.Pheromone += ( p1 * edge.Pheromone + p2 );
	//	} );
	//	var last = GetLastEdge( ant );
	//	last.Pheromone += ( p1 * last.Pheromone + p2 ); 
	//}
	//public void DepositAcs( Ant ant )
	//{
	//	for( int city = 0; city < ant.Cities - 1; city++ ) 
	//	{		
	//		var edge = this[ ant.Path[ city ], ant.Path[ city + 1 ] ];
	//		edge.Pheromone += ( ( 1.0 - this.Settings.P1 ) * edge.Pheromone + this.Settings.P1 / ant.Tour );//edge.Weight );
	//	}
	//	var last = GetLastEdge( ant );
	//	last.Pheromone += ( ( 1.0 - this.Settings.P1 ) * last.Pheromone + this.Settings.P1 / ant.Tour ); //last.Weight ); ;
	//}
	#endregion
}
