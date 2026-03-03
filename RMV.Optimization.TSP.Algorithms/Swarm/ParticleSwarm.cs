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

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<PsoSettings>( "pso" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize()
	{
		var nearest = base.BuildNearestTour(); //base.InitializeTour()

		this.swarm = InitializeSwarm( nearest );

		return nearest;
	}

	protected override TspResult? RunEpoch( TspResult best )
	{
		TspResult current = best;

		// Update all particles in parallel — each reads global best but writes only to its own state
		Parallel.ForEach( this.swarm, particle =>
		{
			particle.Update( current.Path, base.map );
		} );

		// Find iteration best
		var iterBest = this.swarm.MinBy( p => p.Cost );

		if( iterBest.Cost + MARGIN < current.Tour )
		{
			// Apply local search only when a new best is found
			var improved = Parallel2OptSearch( [ .. iterBest.Position ] );//LinKernighanSearch( [ .. iterBest.Position ] );

			iterBest.SetPosition( improved.Path, improved.Tour );

			this.stagnation = 0;

			current = improved;
		}
		else
		{
			this.stagnation++;

			// Restart worst particles when stagnating to inject diversity
			if( this.stagnation > 0 && this.stagnation % 500 == 0 )
			{
				RestartWorst();
			}
		}

		return current;
	}

	List<Particle> InitializeSwarm( TspResult nearest )
	{
		List<Particle> particles = [];

		// Seed first particle with nearest-neighbour solution
		particles.Add( new Particle( 0, nearest.Path, nearest.Tour ) );

		for( int i = 1; i < settings.Size; i++ )
		{
			var tour = base.map.BuildRandomTour();

			particles.Add( new Particle( i, tour.Path, tour.Tour ) );
		}

		return particles;
	}

	/// <summary>
	/// Replace the worst half of particles with fresh random tours to escape local optima
	/// </summary>
	void RestartWorst()
	{
		var sorted = this.swarm.OrderBy( p => p.BestCost ).ToList();

		int half = sorted.Count / 2;

		for( int i = half; i < sorted.Count; i++ )
		{
			var tour = base.map.BuildRandomTour();

			sorted[ i ].SetPosition( tour.Path, tour.Tour );
			sorted[ i ].Velocity = [];
		}
	}
}