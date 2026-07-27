using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Greedy Randomized Adaptive Search Procedure
/// </summary>
public class RandomizedAdaptiveSearch( TspMap map ) : TspAlgorithmBase( map )
{
	GraspSettings settings;

	/// <summary>
	/// Configures the algorithm settings
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<GraspSettings>( "grasp" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}
	

	/// <summary>
	/// Executes a single optimization epoch to improve the current TSP solution.
	/// </summary>
	/// <param name="best">The best solution found so far. Used as a reference for further optimization.</param>
	/// <returns>A new solution representing the result of the optimization epoch</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var path = BuildGreedySolution();

		return ParallelLocalSearch( path ); 
	}

	/// <summary>
	/// Constructs a path through all cities using a greedy randomized approach, starting from the initial city.
	/// </summary>
	/// <remarks>
	/// Selects the next city at each step based on the shortest available distance, with a degree of randomness
	/// controlled by the algorithm's settings. The resulting path may differ between invocations due to randomization. 
	/// The method does not guarantee an optimal solution, but provides a fast heuristic for generating feasible paths.
	/// </remarks>
	/// <returns>
	/// A list of city indices representing the constructed path. The list includes all cities, starting with the initial city (index 0).
	/// </returns>
	List<int> BuildGreedySolution()
	{
		int totalCities = base.Cities;
		List<int> path = new( totalCities ) { 0 }; // Pre-allocate with known capacity

		List<int> available = new( totalCities - 1 );
		for( int i = 1; i < totalCities; i++ )	available.Add( i );

		// Rent a buffer/array for candidate distances to avoid allocations in the while loop
		Span<(int Index, int City, double Distance)> candidates = new (int, int, double)[ totalCities ];
		int currentCity = 0;

		while( available.Any() )
		{
			// 1. Populate current distances without LINQ allocations, tracking original indices
			for( int i = 0; i < available.Count; i++ )
			{
				candidates[ i ] = (i, available[ i ], base.map[ currentCity, available[ i ] ].Weight);
			}

			// 2. Slice the span to only the active elements
			var activeCandidates = candidates[ ..available.Count ];

			// 3. Sort the slice in-place
			activeCandidates.Sort( ( a, b ) => a.Distance.CompareTo( b.Distance ) );

			// 4. Identify Restricted Candidate List (RCL) bound
			int takeCount = Math.Min( settings.Take, activeCandidates.Length );

			// 5. Select next city randomly from RCL or pick the best
			int selectedIndex = Random.Shared.NextDouble() > settings.Factor ? Random.Shared.Next( takeCount ) : 0;
			int nextCity = activeCandidates[ selectedIndex ].City;
			int indexInAvailable = activeCandidates[ selectedIndex ].Index;

			path.Add( nextCity );
			available.RemoveAt( indexInAvailable );
			currentCity = nextCity;
		}

		return path;
	}	


	/// <summary>
	/// Configuration Settings
	/// </summary>
	class GraspSettings : IlsSettings
	{
		public double Factor { get; set; }
		public int Take { get; set; }
	}
}
