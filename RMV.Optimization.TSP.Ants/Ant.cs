namespace RMV.Optimization.TSP.ACO;

/// <summary>
/// Single ant functionality
/// </summary>
public class Ant : ICloneable	
{	

	#region Initialize ---------------------------------------------------------

	/// <summary>
	/// "Regular" constructor
	/// </summary>
	/// <param name="size">problem size</param>
	/// <param name="id">ant ID</param>
	public Ant( int size, int id )
	{
		this.Cities = size;
		this.ID = id;

		this.Path = [];
		this.Available = Enumerable.Range( 0, this.Cities ).ToHashSet();		
	}

	/// <summary>
	/// Initialize best ant
	/// </summary>
	public Ant( double tour, List<int> path )
	{
		this.ID = -999;
		this.Tour = tour;
		this.Path = path;
	}

	/// <summary>
	/// Initialize best ant
	/// </summary>
	//public Ant()
	//{
	//	this.ID = -999;
	//	this.Tour = int.MaxValue;
	//	this.Path = [];
	//}

	#endregion


	#region Properties ---------------------------------------------------------

	public int Cities { get; init; }
	
	public int CurrentCity => this.Path.Last();
	//public int FirstCity => this.Path.First();

	int ID; 

	/// <summary>
	/// Path
	/// </summary>
	public List<int> Path { get; set; } = [];

	/// <summary>
	/// Non-visited cities
	/// </summary>
	public HashSet<int> Available { get; private set; } = [];

	public bool IsAvailable( int city ) => this.Available.Contains( city );

	/// <summary>
	/// Tour length
	/// </summary>
	public double Tour { get; set; } = int.MaxValue;	

	#endregion	


	#region Move ---------------------------------------------------------------

	/// <summary>
	/// Next city move
	/// </summary>
	public void Move( int nextCity )
	{
		this.Path.Add( nextCity );
		this.Available.Remove( nextCity );				

		if( this.Available.Count == 1 )
		{
			int city = this.Available.First();

			this.Path.Add( city );
			this.Available.Remove( city );			
		}		
	}	

	#endregion


	#region Restart ------------------------------------------------------------

	/// <summary>
	/// Reset to start another tour
	/// </summary>	
	public void Reset()
	{
		this.Tour = int.MaxValue;
		
		this.Available = Enumerable.Range( 0, this.Cities ).ToHashSet();
		this.Path.Clear();

		int city = Random.Shared.Next( this.Cities );

		this.Path.Add( city );
		this.Available.Remove( city );
	}	

	#endregion	


	#region Interfaces ---------------------------------------------------------

	public override string ToString() => $"id={this.ID}, tour={this.Tour:0.000} path={GetPath()}";

	/// <summary>
	/// Path console formatting
	/// </summary>	
	string GetPath() => string.Join( ",", this.Path );


	/// <summary>
	/// IClonable implementation
	/// </summary>	
	public object Clone()
	{
		var ant = new Ant( this.Cities, this.ID )
		{
			Path = new List<int>( this.Path ),
			//Path = this.Path.Clone() as int[],
			//ant.pathID = this.pathID;
			Tour = this.Tour
		};

		return ant;
	}

	#endregion


	#region obsolete
	/// <summary>
	/// Deposit pheromone
	/// </summary>	
	//public void Deposit( AcoEdges edges, double Q )
	//{
	//	double amount = Q / this.Tour;

	//	for( int i = 1; i < this.cities; i++ )
	//	{
	//		edges[ this.Path[ i - 1 ], this.Path[ i ] ].Pheromone += amount;
	//	}

	//	edges[ this.Path[ 0 ], this.Path[ ^1 ] ].Pheromone += amount;
	//}
	//public void Reset()
	//{
	//	this.Tour = int.MaxValue;
	//	this.pathID = 0;

	//	Array.Fill( this.Visited, false );
	//	Array.Fill( this.Path, -1 );

	//	this.Path[ 0 ] = this.CurrentCity = Random.Shared.Next( this.Cities );
	//	this.Visited[ this.CurrentCity ] = true;
	//}	
	//internal void Reset( int start )
	//{
	//	this.Tour = int.MaxValue;
	//	this.pathID = 0;

	//	Array.Fill( this.Visited, false );
	//	Array.Fill( this.Path, -1 );

	//	this.Path[ 0 ] = this.Point = start;		
	//	this.Visited[ this.Point ] = true;
	//}
	//public void Step( int nextCity )
	//{
	//	this.Visited[ nextCity ] = true;
	//	this.Path[ ++this.pathID ] = this.CurrentCity = nextCity;
	//	if( this.pathID == this.Cities - 2 ) //penultimate city
	//	{
	//		nextCity = Array.IndexOf( this.Visited, false );
	//		this.Visited[ nextCity ] = true;
	//		this.Path[ ++this.pathID ] = this.CurrentCity = nextCity;			
	//	}
	//}
	/// <summary>
	/// IEquatable implementation
	/// </summary>	
	//public bool Equals( Ant? that ) => this.ID == that?.ID && this.Tour > that.Tour;
	//public override bool Equals( object obj ) => Equals( obj as Ant );
	//public override int GetHashCode() => this.ID.GetHashCode() + this.Tour.GetHashCode();

	/// <summary>
	/// End-to-end path for ACO ant
	/// </summary>	
	//public void PathAco( AcoEdges edges )
	//{
	//	for( int city = 0; city < this.cities - 2; city++ )
	//	{
	//		int next = ProbSelect( edges );

	//		Step( next ); 
	//	}

	//	this.Tour = Evaluate( edges );
	//}


	/// <summary>
	/// End-to-end path for ACS ant
	/// </summary>	
	//public void PathAcs( AcoEdges edges, AcoSettings settings, double tour )
	//{
	//	for( int city = 0; city < this.cities - 2; city++ )
	//	{
	//		int next = ( Random.Shared.NextDouble() < settings.Greedy ) ? GreedySelect( edges ) : ProbSelect( edges );

	//		edges[ this.currentCity, next ].Update( settings, tour ); //local pheromone update

	//		Step( next );			
	//	}

	//	this.Tour = Evaluate( edges );
	//}

	/// <summary>
	/// Calculate tour length
	/// </summary>	
	//double Evaluate( AcoEdges edges )
	//{
	//	double tour = 0;

	//	for( int i = 1; i < this.cities; i++ )
	//	{
	//		tour += edges[ this.Path[ i - 1 ], this.Path[ i ] ].Weight;
	//	}

	//	tour += edges[ this.Path[ 0 ], this.Path[ ^1 ] ].Weight; //last node - start node

	//	return tour;
	//}
	#endregion
}