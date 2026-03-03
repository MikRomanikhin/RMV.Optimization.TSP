using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// Ants colony
/// </summary>
public class Colony : List<Ant>
{

	#region Properties ---------------------------------------------------------	
		
	public Ant Current { get; private set; }  /// Best ant for the current iteration

	/// <summary>
	/// Best for all epochs
	/// </summary>
	public Ant Best { get; set; }	

	#endregion


	#region Initialize ---------------------------------------------------------

	readonly AcoEdges Edges; //edges collection
	readonly int Cities; //total cities
	readonly AcoSettings Settings; // Configuration parameters	

	public Colony( AcoSettings settings, int cities, AcoEdges edges )//, Ant best )
	{
		this.Settings = settings;
		this.Cities = cities;		
		this.Edges = edges;
		//this.Best = best;

		Initialize();
	}

	/// <summary>
	/// Create ants
	/// </summary>
	void Initialize()
	{
		base.Clear();

		for( int i = 0; i < this.Settings.Size; i++ )
		{
			base.Add( new Ant( this.Cities, i ) );
		}		
	}

	#endregion


	#region Evaluate -----------------------------------------------------------	

	/// <summary>
	/// Evaluates the best ant
	/// </summary>
	public TspResult Evaluate()
	{
		this.Current = this.MinBy( a => a.Tour );

		if( this.Best == null || this.Current.Tour + MARGIN < this.Best.Tour ) this.Best = this.Current.Clone() as Ant;		

		return new TspResult( this.Current.Tour, this.Current.Path );
	}	

	const double MARGIN = 0.0001;

	#endregion


	#region Deposit ------------------------------------------------------------

	/// <summary>
	/// Pheromone deposit AS
	/// </summary>
	public void Deposit() => Parallel.ForEach( this, this.Edges.Deposit );

	/// <summary>
	/// Rank-based pheromone deposit AS with elitist best-ant reinforcement
	/// </summary>
	public void Deposit( int eliteCount ) => this.Edges.Deposit( this, eliteCount, this.Best );

	#endregion


	#region Restart ------------------------------------------------------------

	/// <summary>
	/// Reinitialize the ant population before starting another tour
	/// </summary>
	public void Restart() => this.ForEach( a => a.Reset() );				

	#endregion

	#region obsolete
	//void Reset( int start )
	//{
	//	//Parallel.ForEach( this.ants, a => a.Reset() );
	//	this.ants.ForEach( a => a.Reset( start ) );
	//}
	/// <summary>
	/// Move AS
	/// </summary>
	//public void MoveAS()
	//{
	//	this.ForEach( a => a.PathAco( this.Edges ) ); //Parallel.ForEach( this.ants, StepAS );		
	//}

	/// <summary>
	/// Move ACS
	/// </summary>
	//public void MoveACS()
	//{
	//	this.ForEach( a => a.PathAcs( this.Edges, this.Settings, this.NearestTour ) ); //Parallel.ForEach( this.ants, StepACS );		
	//}

	///// <summary>
	///// Tour AS
	///// </summary>
	//void MoveAS()
	//{
	//	for( int city = 0; city < this.Cities - 2; city++ )
	//	{
	//		base.ForEach( StepAS );
	//		//Parallel.ForEach( this.ants, StepAS );
	//	}
	//}

	///// <summary>
	///// AS Next city move for the target ant
	///// </summary>
	///// <param name="ant">target ant</param>
	//void StepAS( Ant ant )
	//{
	//	int next = ProbSelect( ant );

	//	ant.Step( next );
	//}

	///// <summary>
	///// Greedy ACS city selection
	///// </summary>	
	//int GreedySelect( Ant ant )
	//{
	//	int city = -1;
	//	double max = 0;

	//	for( int to = 0; to < this.Cities; to++ )
	//	{
	//		if( !ant.Visited[ to ] )
	//		{
	//			double choice = this.AcoEdges[ (ant.Point, to) ].Choice;

	//			if( choice >= max )
	//			{
	//				max = choice;
	//				city = to;
	//			}
	//		}
	//	}

	//	return city;
	//}


	///// <summary>
	///// Selects next city by probability
	///// </summary>
	//int ProbSelect( Ant ant )
	//{
	//	var (rand, selection) = BuildWheel( ant );

	//	double p = selection[ 0 ];

	//	int to = 0;

	//	while( p < rand )
	//	{
	//		if( ++to == this.Cities ) to = 0;

	//		p += selection[ to ];
	//	}

	//	return to;
	//}

	///// <summary>
	///// Roulette wheel selection
	///// </summary>
	///// <param name="ant">target ant</param>
	///// <returns>tuple</returns>
	//(double, double[]) BuildWheel( Ant ant )
	//{
	//	var selection = new double[ this.Cities ];

	//	int from = ant.Point;

	//	double denom = GetDenominator( from, ant.Visited );

	//	double sum = 0;

	//	for( int to = 0; to < this.Cities; to++ )
	//	{
	//		if( ant.Visited[ to ] )
	//		{
	//			selection[ to ] = 0;
	//		}
	//		else
	//		{
	//			selection[ to ] = this.AcoEdges[ (from, to) ].Choice / denom;

	//			sum += selection[ to ];
	//		}
	//	}

	//	return (GetRand( sum ), selection);
	//}

	//double GetDenominator( int from, bool[] visited )
	//{
	//	double sum = 0.0;

	//	for( int to = 0; to < this.Cities; to++ )
	//	{
	//		if( to != from && !visited[ to ] ) sum += this.AcoEdges[ (from, to) ].Choice;
	//	}

	//	return sum;
	//}

	//static readonly Random random = new();

	//static double GetRand( double limit )
	//{
	//	double rnd = random.NextDouble();

	//	while( rnd > limit ) rnd = random.NextDouble();

	//	return rnd;
	//}
	#endregion
}
