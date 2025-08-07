using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.PSO;

/// <summary>
/// Particle for TSP PSO
/// </summary>
public class Particle 
{
	public IList<int> Position { get; set; } = [];
	public IList<int> BestPosition { get; set; } = [];
	public double Cost { get; set; } = double.MaxValue;
	public double BestCost { get; set; } = double.MaxValue;
	public List<(int,int)> Velocity { get; set; } = [];

	int ID;

	static readonly PsoSettings settings = ConfigManager.GetSection<PsoSettings>( "pso" );


	public Particle( int id, IList<int> position, double cost )
	{
		this.ID = id;
		this.Position = position;
		this.BestPosition = [ .. position ];
		this.Cost = cost;
		this.BestCost = cost;
		this.Velocity = [];
	}

	public void Update( IList<int> position, TspMap map )
	{		
		this.Velocity = this.UpdateVelocity( position ); //update velocity based on global best
				
		this.Position = this.ApplyVelocity( this.Velocity ); //update position by applying velocity

		this.Cost = map.GetTourLength( this.Position ); //evaluate new position
		
		if( this.Cost < this.BestCost ) //update personal best
		{
			this.BestCost = this.Cost;
			this.BestPosition = [ .. this.Position ];
		}
	}

	List<(int, int)> UpdateVelocity( IList<int> position )
	{
		var newVelocity = new List<(int, int)>();

		// Cognitive component: Difference between personal best and current position
		var cognitiveSwaps = GenerateSwaps( this.Position, this.BestPosition );

		foreach( var swap in cognitiveSwaps )
		{
			if( Random.Shared.NextDouble() < settings.Cognitive ) newVelocity.Add( swap );
		}

		// Social component: Difference between global best and current position
		var socialSwaps = GenerateSwaps( this.Position, position );

		foreach( var swap in socialSwaps )
		{
			if( Random.Shared.NextDouble() < settings.Social ) newVelocity.Add( swap );
		}

		// Apply inertia weight to retain part of the previous velocity
		foreach( var swap in this.Velocity )
		{
			if( Random.Shared.NextDouble() < settings.Inertia ) newVelocity.Add( swap );
		}

		return newVelocity;
	}	


	List<int> ApplyVelocity( IList<(int,int)> velocity )
	{
		var newPosition = new List<int>( this.Position );

		foreach( var swap in velocity ) // Apply each swap in the velocity to the current position
		{
			(newPosition[ swap.Item1 ], newPosition[ swap.Item2 ]) = (newPosition[ swap.Item2 ], newPosition[ swap.Item1 ]);			
		}

		return newPosition;
	}



	static List<(int, int)> GenerateSwaps( IList<int> from, IList<int> to )
	{
		if( from.Count != to.Count ) throw new ArgumentException( "Permutations must have the same length." );

		var swaps = new List<(int, int)>();
		var temp = new List<int>( from );

		for( int i = 0; i < temp.Count; i++ )
		{
			if( temp[ i ] != to[ i ] )
			{
				int index = temp.IndexOf( to[ i ] );

				if( index == -1 ) throw new ArgumentException( "Target permutation contains elements not in source." );

				swaps.Add( (i, index) );

				(temp[ i ], temp[ index ]) = (temp[ index ], temp[ i ]); // Swap in temp to keep future indices correct
			}
		}

		return swaps;
	}

	#region obsolete
	//List<(int, int)> GenerateSwaps( IList<int> path )
	//{
	//	var swaps = new List<(int, int)>();
	//	var temp = new List<int>( this.Position );

	//	for( int i = 0; i < this.Position.Count; i++ )
	//	{
	//		if( temp[ i ] != path[ i ] )
	//		{
	//			int index = temp.IndexOf( path[ i ] );

	//			swaps.Add( (i, index) );

	//			(temp[ i ], temp[ index ]) = (temp[ index ], temp[ i ]); // Swap the elements in the temporary list				
	//		}
	//	}

	//	return swaps;
	//}


	//public List<int> ApplyVelocity( IList<int> position, IList<int> velocity )
	//{
	//	var newPosition = new List<int>( position );

	//	foreach( var swap in velocity ) // Apply each swap in the velocity to the current position
	//	{
	//		(newPosition[ swap.Item1 ], newPosition[ swap.Item2 ]) = (newPosition[ swap.Item2 ], newPosition[ swap.Item1 ]); 
	//		//int temp = newPosition[ swap.Item1 ];
	//		//newPosition[ swap.Item1 ] = newPosition[ swap.Item2 ];
	//		//newPosition[ swap.Item2 ] = temp;
	//	}

	//	return newPosition;
	//}
	//static List<(int, int)> GenerateSwaps( IList<int> from, IList<int> to )
	//{
	//	var swaps = new List<(int, int)>();
	//	var temp = new List<int>( from );
	//	for( int i = 0; i < from.Count; i++ )
	//	{
	//		if( temp[ i ] != to[ i ] )
	//		{
	//			int swapIndex = temp.IndexOf( to[ i ] );
	//			swaps.Add( (i, swapIndex) );
	//			(temp[ i ], temp[ swapIndex ]) = (temp[ swapIndex ], temp[ i ]); // Swap the elements in the temporary list				
	//		}
	//	}
	//	return swaps;
	//}

	/// <summary>
	/// Update the particle's position and velocity based on the global best position
	///</summary>
	///<param name="position"> The global best position to update the particle's velocity</param>
	//public void Update( TspMap map, IList<int> position )
	//{
	//	this.UpdateVelocity( position );

	//	this.UpdatePosition( map );
	//}

	/// <summary>
	/// Update the velocity of the particle based on the global best position
	/// </summary>	
	//void UpdateVelocity( IList<int> gbPosition ) => this.Velocity = gbPosition.Except( base.Path ).ToList();
	//void UpdateVelocity( IList<int> position ) => this.Velocity = position.Except( this.Position ).ToList();


	/// <summary>
	/// Update the position of the particle based on its velocity
	/// </summary>	
	//void UpdatePosition( TspMap map )
	//{
	//	this.Velocity.Where( move => !this.Position.Contains( move ) ).ToList().ForEach( move => this.Position.Add( move ) );

	//	double cost = map.GetTourLength( this.Position );

	//	if( cost < this.BestCost )
	//	{
	//		this.BestCost = cost;
	//		this.BestPosition = new List<int>( this.Position );
	//	}
	//}
	#endregion

	public override string ToString() => $"id:{ID} cost:{BestCost:0.#} path:[{string.Join( ',', Position )}]";
	
}

/// <summary>
/// Configuration Settings
/// </summary>
public class PsoSettings : TspConfigurationBase
{
	public int Size { get; set; }

	public double Inertia { get; set; }

	public double Cognitive { get; set; }

	public double Social { get; set; }
}
