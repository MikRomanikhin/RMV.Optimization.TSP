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
	public List<(int, int)> Velocity { get; set; } = [];

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

	/// <summary>
	/// Replaces position and updates personal best if improved
	/// </summary>
	public void SetPosition( IList<int> position, double cost )
	{
		this.Position = [ .. position ];
		this.Cost = cost;

		if( cost < this.BestCost )
		{
			this.BestCost = cost;
			this.BestPosition = [ .. position ];
		}
	}

	public void Update( IList<int> globalBest, TspMap map )
	{
		this.Velocity = this.UpdateVelocity( globalBest );

		// If velocity is empty, particle has converged — perturb to maintain diversity
		if( this.Velocity.Count == 0 )
		{
			Perturb();
		}

		this.Position = this.ApplyVelocity( this.Velocity );

		this.Cost = map.GetTourLength( this.Position );

		if( this.Cost < this.BestCost )
		{
			this.BestCost = this.Cost;
			this.BestPosition = [ .. this.Position ];
		}
	}

	List<(int, int)> UpdateVelocity( IList<int> globalBest )
	{
		var newVelocity = new List<(int, int)>();

		// Cognitive component: Difference between personal best and current position
		var cognitiveSwaps = GenerateSwaps( this.Position, this.BestPosition );

		foreach( var swap in cognitiveSwaps )
		{
			if( Random.Shared.NextDouble() < settings.Cognitive ) newVelocity.Add( swap );
		}

		// Social component: Difference between global best and current position
		var socialSwaps = GenerateSwaps( this.Position, globalBest );

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

	/// <summary>
	/// Adds random swaps when particle has fully converged to prevent stagnation
	/// </summary>
	void Perturb()
	{
		int n = this.Position.Count;
		int swapCount = Math.Max( 2, n / 10 ); // perturb ~10% of the path

		for( int s = 0; s < swapCount; s++ )
		{
			int i = Random.Shared.Next( n );
			int j = Random.Shared.Next( n );

			if( i != j ) this.Velocity.Add( (i, j) );
		}
	}


	List<int> ApplyVelocity( IList<(int, int)> velocity )
	{
		var newPosition = new List<int>( this.Position );

		foreach( var swap in velocity )
		{
			(newPosition[ swap.Item1 ], newPosition[ swap.Item2 ]) = (newPosition[ swap.Item2 ], newPosition[ swap.Item1 ]);
		}

		return newPosition;
	}


	/// <summary>
	/// Generates swap sequence to transform 'from' into 'to' using O(n) index lookup
	/// </summary>
	static List<(int, int)> GenerateSwaps( IList<int> from, IList<int> to )
	{
		int n = from.Count;
		var swaps = new List<(int, int)>();
		var temp = new int[ n ];
		var indexOf = new int[ n ]; // indexOf[city] = position of city in temp

		for( int i = 0; i < n; i++ )
		{
			temp[ i ] = from[ i ];
			indexOf[ from[ i ] ] = i;
		}

		for( int i = 0; i < n; i++ )
		{
			if( temp[ i ] != to[ i ] )
			{
				int j = indexOf[ to[ i ] ]; // O(1) lookup instead of IndexOf O(n)

				swaps.Add( (i, j) );

				// Update index tracking
				indexOf[ temp[ i ] ] = j;
				indexOf[ temp[ j ] ] = i;

				(temp[ i ], temp[ j ]) = (temp[ j ], temp[ i ]);
			}
		}

		return swaps;
	}

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