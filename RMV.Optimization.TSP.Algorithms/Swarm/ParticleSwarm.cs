using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.PSO;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Particle Swarm Optimization for TSP
/// </summary>
public class ParticleSwarm( TspMap map ) : TspAlgorithmBase( map )
{
	PsoSettings settings;
	List<Particle> swarm;
	int stagnation = 0;

	/// <summary>
	/// Configures the current instance using the 'pso' section from the configuration manager.
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<PsoSettings>( "pso" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	/// <summary>
	/// Initializes the algorithm and constructs the initial tour for TSP solution.
	/// </summary>
	/// <remarks>
	/// Method prepares the internal state required for the algorithm to proceed by building a nearest-neighbor tour 
	/// and initializing the swarm. It should be called before performing further optimization steps.
	/// </remarks>
	/// <returns>TspResult representing the initial tour generated for the TSP, or null if initialization fails.</returns>
	protected override TspResult? Initialize()
	{
		var nearest = base.InitializeTour();

		this.swarm = InitializeSwarm( nearest );

		return nearest;
	}

	/// <summary>
	/// Performs a single optimization epoch, updating particle states and applying local search to improve the best solution.
	/// </summary>
	/// <remarks>
	/// Method updates all particles in the swarm in parallel and applies a local search when a new best solution is found. 
	/// If the algorithm stagnates for a prolonged period, it restarts the worst-performing particles to encourage exploration. 
	/// This method is typically called repeatedly as part of an iterative optimization process.
	/// </remarks>
	/// <param name="best">The current best solution found so far. Used as a reference for updating particle states during this epoch.</param>
	/// <returns>A new or updated best solution found during this epoch, or the input solution if no improvement was made.</returns>
	protected override TspResult? RunEpoch( TspResult best )
	{
		TspResult current = best;

		// Update all particles in parallel — each reads global best but writes only to its own state
		Parallel.ForEach( this.swarm, particle =>
		{
			particle.Update( current.Path, base.map );
		} );
				
		var iterBest = this.swarm.MinBy( p => p.Cost ); // Find iteration best

		bool newBestFound = iterBest.Cost + MARGIN < current.Tour;

		// Apply local search to the iteration best to refine it. If stagnating, search it anyway to help break out.
		if( newBestFound || ( this.stagnation > 0 && this.stagnation % 50 == 0 ) )
		{
			var improved = ParallelLocalSearch( [ .. iterBest.Position ] ); // Use robust parallel local search

			iterBest.SetPosition( improved.Path, improved.Tour );

			if( improved.Tour + MARGIN < current.Tour )
			{
				this.stagnation = 0;
				current = improved;
				newBestFound = true; // Confirmed post-local search
			}
		}

		// Restart worst particles when stagnating to inject diversity
		if( !newBestFound && ++this.stagnation > 0 && this.stagnation % settings.Stagnation == 0 ) this.swarm = RestartWorst(); 				

		return current;
	}

	/// <summary>
	/// Initializes a swarm of particles for the particle swarm optimization algorithm, seeding the first particle with the
	/// provided nearest-neighbour solution.
	/// </summary>
	/// <remarks>
	/// The size of the swarm is determined by the current settings. This method ensures that the initial
	/// swarm contains a diverse set of solutions, which can improve optimization performance.
	/// </remarks>
	/// <param name="nearest">The nearest-neighbour solution to use for initializing the first particle in the swarm. Cannot be null.</param>
	/// <returns>
	/// A list of particles representing the initialized swarm. The first particle is seeded with the 
	/// nearest-neighbour solution; the remaining particles are initialized with random tours.
	/// </returns>
	List<Particle> InitializeSwarm( TspResult nearest )
	{
		List<Particle> particles = [];

		// Seed first particle with nearest-neighbour solution
		particles.Add( new Particle( 0, nearest.Path, nearest.Tour ) );

		for( int i = 1; i < settings.Size; i++ )
		{
			// Give swarm slightly randomized start paths based on the NN guide to jumpstart learning
			var tour = i < settings.Size / 4 ? base.RandomSwap( nearest ) : base.map.BuildRandomTour();

			particles.Add( new Particle( i, tour.Path, tour.Tour ) );
		}

		return particles;
	}

	/// <summary>
	/// Replace the worst half of particles with fresh random tours to escape local optima
	/// </summary>
	List<Particle> RestartWorst()
	{		
		// Identify worst half by BestCost (highest cost = worst performance)
		var worstParticles = this.swarm.OrderByDescending( p => p.BestCost ).Take( this.swarm.Count / 2 ).Select( p => p.ID ).ToHashSet();

		
		for( int i = 0; i < this.swarm.Count; i++ ) // Restart worst particles in-place, maintaining ID order
		{
			if( worstParticles.Contains( this.swarm[ i ].ID ) )
			{
				var tour = base.map.BuildRandomTour();

				// Fully reset memory! Do not keep the old stagnated personal best!
				this.swarm[ i ] = new Particle( this.swarm[ i ].ID, tour.Path, tour.Tour );
			}
		}

		return this.swarm; // Return the swarm with worst particles replaced, maintaining order
	}
}