using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;
using RMV.Optimization.TSP.ACO;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Ant Colony Optimization for TSP
/// </summary>
public class AntColonySearch( TspMap map ) : TspAlgorithmBase( map )
{	
	AcoMap Map;
	AcoSettings settings;	

	/// <summary>
	/// Configures the current instance by loading and assigning the required settings section.
	/// </summary>
	/// <remarks>
	/// Method overrides the base configuration logic to retrieve the 'aco' section from the configuration manager and
	/// assign it to the settings properties. Ensure that the configuration contains a valid 'aco' section compatible with the expected type.
	/// </remarks>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<AcoSettings>( "aco" );		
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );			
	}

	/// <summary>
	/// Initializes the ant colony optimization process and runs the first epoch to establish a baseline solution.
	/// </summary>
	/// <remarks>
	/// Method prepares the internal state for subsequent optimization by constructing the initial
	/// tour and setting up the ant colony map. It should be called before performing further optimization steps.
	/// </remarks>
	/// <returns>TspResult representing the result of the initial epoch, or null if initialization fails</returns>
	protected override TspResult? Initialize()
	{
		var result = base.BuildNearestTour();
		this.settings.Nearest = result.Tour;

		this.Map = new AcoMap( this.map, this.settings );
				
		return this.Map.RunEpoch( null );  // Run first epoch to establish ant baseline
	}	

	/// <summary>
	/// Runs a single epoch of the ant colony optimization process using the provided best solution as a reference.
	/// </summary>
	/// <param name="best">The current best solution to guide the optimization process.</param>
	/// <returns>The result of the epoch, potentially updating the best solution.</returns>
	protected override TspResult RunEpoch( TspResult best ) => this.Map.RunEpoch( best );	
	
}
