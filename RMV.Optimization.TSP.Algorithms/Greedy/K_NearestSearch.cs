using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// K-Nearest Neighbour TSP algorithm
/// </summary>
/// <param name="map">TSP map</param>
public class K_NearestSearch( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	KNearestSettings settings;
	int nextStart = 0;


	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<KNearestSettings>( "k-nearest" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize()
	{
		this.nextStart = 1;

		return RunKNearest( 0 );
	}


	protected override TspResult RunEpoch( TspResult best )
	{
		if( this.nextStart >= base.Cities ) this.nextStart = 0;

		var result = RunKNearest( this.nextStart++ );

		return result.Tour + MARGIN < best.Tour ? result : best;
	}

	/// <summary>
	/// K-Nearest Neighbour algorithm starting from a given city.
	/// Recursively expands the k nearest unvisited cities at each step.
	/// </summary>
	/// <param name="start">starting city</param>	
	TspResult RunKNearest( int start )
	{
		var visited = new HashSet<int> { start };
		List<int> path = [ start ];

		var best = new TspResult( double.MaxValue, [] );

		Expand( path, visited, 0, start, best );

		return best;
	}

	/// <summary>
	/// Recursively expands the path by choosing the k nearest unvisited cities
	/// </summary>
	void Expand( List<int> path, HashSet<int> visited, double cost, int start, TspResult best )
	{
		if( path.Count == this.Cities )
		{
			double tour = cost + this.map[ path[ ^1 ], start ].Weight; // complete the tour

			if( tour + MARGIN < best.Tour )
			{
				best.Tour = tour;
				best.Path = new List<int>( path );
			}

			return;
		}

		// Prune: if current cost already exceeds best known tour, skip this branch
		if( cost >= best.Tour ) return;

		int lastCity = path[ ^1 ];

		// Take k nearest unvisited cities
		var nearest = Enumerable.Range( 0, this.Cities ).Where( c => !visited.Contains( c ) )
			.OrderBy( c => this.map[ lastCity, c ].Weight ).Take( settings.Take );

		foreach( int city in nearest )
		{
			double edgeCost = this.map[ lastCity, city ].Weight;

			path.Add( city );
			visited.Add( city );

			Expand( path, visited, cost + edgeCost, start, best );

			// Backtrack
			path.RemoveAt( path.Count - 1 );
			visited.Remove( city );
		}
	}

	#region obsolete
	/// <summary>
	/// K-Nearest Neighbour algorithm
	/// </summary>
	/// <param name="start">starting node</param>	
	//protected TspResult RunKNearest( TspResult result )
	//{
	//	var path = result.Path; // Copy the current path
	//	double tour = result.Tour; // Current tour length

	//	var available = Enumerable.Range( 0, this.Cities ).Except( path ).ToList();

	//	while( available.Any() )
	//	{
	//		var nearest = available.OrderBy( city => this.map[ path[ ^1 ], city ].Weight ).Take( settings.Size );

	//		foreach( int city in nearest )
	//		{
	//			tour += this.map[ path[ ^1 ], city ].Weight;
	//			path.Add( city );

	//			available.Remove( city );

	//			result = RunKNearest( new TspResult( tour, path ) ); // Recursive call to continue building the path

	//			if( result.Path.Count == this.Cities ) return result; // If we have a complete path, return it				
	//		}
	//	}

	//	tour += this.map[ path[ 0 ], path[ ^1 ] ].Weight;

	//	return new TspResult( tour, path );
	//}
	#endregion
}

/// <summary>
/// Configuration Settings
/// </summary>
public class KNearestSettings : TspConfigurationBase
{
	public int Take { get; set; }
}