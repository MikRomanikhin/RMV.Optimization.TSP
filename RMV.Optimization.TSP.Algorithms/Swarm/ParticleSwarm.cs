using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.PSO;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Particle Swarm Optimization for TSP
/// </summary>
public class ParticleSwarm( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	PsoSettings settings;
	//readonly TspMap Map = map;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<PsoSettings>( "pso" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize() => base.InitializeTour() ?? BuildNearestTour();
	

	protected override TspResult? RunEpoch( TspResult best )
	{
		// Initialize swarm if not already present (store in a field if needed)
		// For stateless epoch, re-initialize each time
		var swarm = InitializeSwarm();

		TspResult current = best;

		foreach( var particle in swarm )
		{
			particle.Update( current.Path, base.map ); // Update particle based on global best

			if( particle.Cost < current.Tour ) // If this particle found a better solution, update global best
			{
				current = new TspResult( particle.Cost, new List<int>( particle.Position ) );
			}
		}

		return current;
	}
	


	List<Particle> InitializeSwarm()
	{					
		List<Particle> swarm = [];

		int count = 0;

		for( int i = 0; i < settings.Size; i++ )
		{
			var position = base.map.BuildRandomTour(); 			

			Particle particle = new( count++, position.Path, position.Tour );
		
			swarm.Add( particle );		
		}

		return swarm;
	}	
}

