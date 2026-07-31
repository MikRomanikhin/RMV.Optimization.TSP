using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Simulated Annealing algorithm for TSP
/// </summary>
public class SimulatedAnnealing( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	AnnealingSettings settings;
	TspResult current;
	double temperature; // Local temperature state instead of modifying settings (thread-safety, reset capability)

	/// <summary>
	/// Configures the annealing algorithm by loading settings from the configuration section and initializing the base
	/// configuration.
	/// </summary>
	/// <remarks>This method overrides the base configuration logic to ensure that annealing-specific settings are
	/// loaded and validated. The configuration section name used is "annealing". If the configuration is missing or of an
	/// unexpected type, an exception is thrown to prevent misconfiguration.</remarks>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<AnnealingSettings>("annealing");		
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );		
	}

	/// <summary>
	/// Initializes the algorithm by constructing an initial tour using the nearest neighbor heuristic and resetting temperature.
	/// </summary>
	/// <remarks>The returned tour represents the starting solution for the algorithm. Subsequent optimization steps
	/// may modify this tour. The method uses the nearest neighbor approach to build the initial tour, which may affect the
	/// quality of the starting solution. Temperature is reset here for each new algorithm run.</remarks>
	/// <returns>A clone of the initial tour as a <see cref="TspResult"/> instance, or "null" if initialization fails.</returns>
	protected override TspResult? Initialize()
	{
		current = base.BuildNearestTour();

		// Reset temperature for new algorithm run (critical for reusability)
		this.temperature = this.settings.Temperature;

		return current.Clone();
	}

	/// <summary>
	/// Performs a single optimization epoch and returns the improved result if found.
	/// </summary>
	/// <param name="best">The current best solution to compare against. Cannot be null.</param>
	/// <returns>A new instance of the improved solution if a better result is found; otherwise, the original best solution.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		GetAnnealing( current );

		if( current < best )
		{
			return current.Clone();
		}

		return best;
	}	

	/// <summary>
	/// Performs a single SA step on the provided TSP result, potentially updating the tour and temperature based on acceptance criteria.
	/// </summary>
	/// <remarks>This method applies the simulated annealing acceptance rule to determine whether a candidate swap should be accepted, 
	/// and updates the temperature according to the configured decay rate. The input result is modified in place. Uses local temperature
	/// state for thread-safety and consistency across multiple algorithm runs.</remarks>
	/// <param name="result">The current result of the traveling salesman problem, including the tour path and cost. 
	/// This object will be updated if a swap is accepted.</param>
	void GetAnnealing( TspResult result )
	{			
		(Action accept, double delta) = base.Swap( result.Path );

		// Metropolis criterion: accept improving moves always, accept worse moves with temperature-dependent probability
		if( delta < 0 || Math.Exp( -delta / this.temperature ) > Random.Shared.NextDouble() )
		{
			result.Tour += delta;
			accept!();
		}

		// Decay temperature for next iteration (geometric cooling schedule)
		this.temperature *= this.settings.Decay;
	}
}

/// <summary>
/// Configuration Settings
/// </summary>
class AnnealingSettings : TspConfigurationBase
{
	public double Decay { get; set; }
	public double Temperature { get; set; }	
}

