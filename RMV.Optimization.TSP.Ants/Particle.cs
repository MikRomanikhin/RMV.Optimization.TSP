using RMV.Common.Configuration;

using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.PSO;

/// <summary>
/// Particle for TSP PSO
/// </summary>
public class Particle( int id, List<int> position, double cost )
{
	public int ID => id;
	public List<int> Position { get; set; } = position;
	public List<int> BestPosition { get; set; } = [ .. position ];
	public double Cost { get; set; } = cost;
	public double BestCost { get; set; } = cost;
	public List<(int, int)> Velocity { get; set; } = [];	

	static readonly PsoSettings settings = ConfigManager.GetSection<PsoSettings>( "pso" );

	/// <summary>
	/// Replaces position and updates personal best if improved
	/// </summary>
	public void SetPosition( List<int> position, double cost )
	{
		this.Position = [ .. position ];
		this.Cost = cost;

		if( cost < this.BestCost )
		{
			this.BestCost = cost;
			this.BestPosition = [ .. position ];
		}
	}

	/// <summary>
	/// Updates the particle's position and cost based on its velocity and the provided global best solution.
	/// </summary>
	/// <remarks>
	/// If the particle's velocity becomes empty, indicating convergence, the particle is perturbed to maintain diversity 
	/// in the swarm. This method updates the particle's best-known position and cost if an improved solution is found.
	/// </remarks>
	/// <param name="globalBest">
	/// A list of city indices representing the current global best tour. Used to guide the particle's movement toward the best-known solution.
	/// </param>
	/// <param name="map">
	/// The map containing the distance information for the traveling salesman problem. Used to evaluate the cost of the particle's current tour.
	/// </param>
	public void Update( List<int> globalBest, TspMap map )
	{
		this.Velocity = UpdateVelocity( globalBest );

		// If velocity is empty, particle has converged — perturb to maintain diversity
		if( this.Velocity.Count == 0 )
		{
			Perturb();
		}

		this.Position = ApplyVelocity( this.Velocity );

		this.Cost = map.GetTourLength( this.Position );

		if( this.Cost < this.BestCost )
		{
			this.BestCost = this.Cost;
			this.BestPosition = [ .. this.Position ];
		}
	}

	/// <summary>
	/// Updates the particle's velocity based on cognitive, social, and inertia components.
	/// </summary>
	/// <remarks>
	/// The updated velocity incorporates influences from the particle's own best-known position (cognitive component), 
	/// the global best position (social component), and a portion of the previous velocity (inertia). The probability of 
	/// including each component is determined by the corresponding settings. This method is typically used in discrete PSO 
	/// algorithms where velocity is represented as a sequence of swaps.
	/// </remarks>
	/// <param name="globalBest">
	/// The global best position found by any particle in the swarm. Used to compute the social component of the velocity update.
	/// </param>
	/// <returns>
	/// A list of swap operations representing the new velocity. Each tuple specifies a swap to be applied to the particle's current position.
	/// </returns>
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

	/// <summary>
	/// Applies a sequence of swap operations (velocity) to the particle's current position, resulting in a new position.
	/// </summary>
	/// <param name="velocity">A list of swap operations to apply. Each tuple specifies a swap to be performed on the current position.</param>
	/// <returns>A new list of city indices representing the particle's position after applying the swaps.</returns>
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

	public int Stagnation { get; set; }
}