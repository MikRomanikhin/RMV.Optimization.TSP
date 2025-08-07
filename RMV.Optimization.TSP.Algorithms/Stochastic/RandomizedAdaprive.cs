using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Greedy Randomized Adaptive Search Procedure
/// </summary>
public class RandomizedAdaptiveSearch( TspMap map ) : TspAlgorithmBase( map )
{
	GraspSettings settings;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<GraspSettings>( "grasp" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize() => base.BuildNearestTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var path = BuildGreedySolution();// best.Path );

		return Parallel2OptSearch( path );  //var result = OrOptSwap( path );
	}
	

	List<int> BuildGreedySolution()
	{
		List<int> path = [ 0 ]; // Start from city 0
		HashSet<int> available = [ .. Enumerable.Range( 1, base.Cities - 1 ) ];

		while( available.Count > 0 )
		{
			int currentCity = path.Last();

			var candidates = available.Select( city => new { City = city, Distance = base.map[ currentCity, city ].Weight } )
											  .OrderBy( x => x.Distance ).Take( settings.Take ).ToList();

			int nextCity = Random.Shared.NextDouble() > settings.Factor 
				? candidates[ Random.Shared.Next( candidates.Count ) ].City : candidates[ 0 ].City;

			path.Add( nextCity );
			available.Remove( nextCity );
		}		

		return path;
	}


	#region obsolete
	//List<int> BuildGreedySolution()
	//{
	//	List<int> path = [];
	//	HashSet<int> unvisited = new( Enumerable.Range( 0, base.Cities ) );

	//	int currentCity = Random.Shared.Next( base.Cities );
	//	path.Add( currentCity );
	//	unvisited.Remove( currentCity );

	//	while( unvisited.Any() )
	//	{
	//		List<int> candidates = [ .. unvisited ];

	//		candidates.Sort( ( a, b ) => this.Map[ currentCity, a ].Weight.CompareTo( this.Map[ currentCity, b ].Weight ) );

	//		int maxCandidates = Math.Max( 1, candidates.Count / 3 ); // Adjust greediness

	//		int nextCity = candidates[ Random.Shared.Next( maxCandidates ) ];

	//		path.Add( nextCity );
	//		unvisited.Remove( nextCity );
	//		currentCity = nextCity;
	//	}

	//	return path;
	//}

	//List<int> BuildGreedySolution( IList<int> path )
	//{
	//	var candidate = new List<int>( path.Take( Random.Shared.Next( 1, path.Count ) ) ).Distinct().ToList();		

	//	var allCities = Enumerable.Range( 0, path.Count );

	//	while( candidate.Count < path.Count )
	//	{
	//		var candidates = allCities.Except( candidate ).ToList();

	//		if( candidates.Count == 1 )
	//		{
	//			candidate.Add( candidates[ 0 ] );
	//			break;
	//		}

	//		List<double> costs = [];
	//		for( int i = 0; i < candidates.Count; i++ )
	//		{
	//			costs.Add( this.Map[ path[ ^1 ], path[ i ] ].Weight );
	//		}

	//		//var costs = GetCosts( candidates, path ); //candidates.Select( i => this.Map[ path[ ^1 ], path[ i ] ].Weight ).ToList();

	//		//var costs = Enumerable.Range( 0, candidates.Count ).Select( i => this.Map[ path[ ^1 ], candidates[ i ] ].Weight ).ToList();

	//		double maxCost = costs.Max();
	//		double minCost = costs.Min();
	//		double delta = minCost + settings.Factor * ( maxCost - minCost );

	//		//var rcl = Enumerable.Range( 0, costs.Count ).Where( i => costs[ i ] < delta ).ToList();

	//		var rcl = new List<int>();

	//		for( int i = 0; i < costs.Count; i++ )
	//		{
	//			if( costs[ i ] < delta ) rcl.Add( candidates[ i ] );
	//		}

	//		if( rcl.Count > 0 ) candidate.Add( rcl[ Random.Shared.Next( rcl.Count ) ] );
	//	}

	//	return candidate;
	//}

	//List<double> GetCosts( List<int> candidates, IList<int> path ) => candidates.Select( i => this.Map[ path[ ^1 ], path[ i ] ].Weight ).ToList();

	//IList<int> BuildGreedySolution1( IList<int> path )
	//{
	//	var candidate = new List<int>();
	//	for( int i = 0; i < path.Count; i++ )
	//	{
	//		candidate.Add( Random.Shared.Next( path.Count ) );
	//	}

	//	candidate = candidate.Distinct().ToList();

	//	var allCities = Enumerable.Range( 0, path.Count );

	//	while( candidate.Count < path.Count )
	//	{
	//		var candidates = allCities.Except( candidate ).ToList();

	//		if( candidates.Count == 1)
	//		{
	//			candidate.Add( candidates[ 0 ] );
	//			break;
	//		}

	//		List<double> costs = [];

	//		for( int i = 0; i < candidates.Count; i ++ )
	//		{
	//			costs.Add( this.Map[ path[ ^1 ], path[ i ] ].Weight );
	//		}			

	//		var maxCost = costs.Max();
	//		var minCost = costs.Min();

	//		var rcl = new List<int>();

	//		for( int i = 0; i < costs.Count; i++ )
	//		{		
	//			if( costs[ i ] < ( minCost + settings.Factor * ( maxCost - minCost ) ) )
	//			{
	//				rcl.Add( candidates[ i ] );
	//			}
	//		}

	//		if( rcl.Count > 0 ) candidate.Add( rcl[ Random.Shared.Next( rcl.Count ) ] );			
	//	}

	//	return candidate;
	//}
	//public List<int> ConstructGreedySolution( List<(double, double)> points )
	//{
	//	List<int> available = Enumerable.Range( 0, points.Count ).ToList();

	//	var tour = base.BuildRandomTour();//[ available[ new Random().Next( 0, available.Count ) ] ];

	//	available.Remove( tour[ 0 ] );

	//	while( available.Count > 0 )
	//	{
	//		int nextPoint = available.OrderBy( x => EuclideanDistance( points[ tour.Last() ], points[ x ] ) ).First();

	//		tour.Add( nextPoint );

	//		available.Remove( nextPoint );
	//	}

	//	return tour;
	//}

	//IList<int> BuildGreedySolution( IList<int> path )
	//{
	//	var unvisited = Enumerable.Range( 0, path.Count ).ToList();

	//	IList<int> tour = [ unvisited[ Random.Shared.Next( unvisited.Count ) ] ];

	//	unvisited.Remove( tour[ 0 ] );

	//	while( unvisited.Any() )
	//	{
	//		int next = GetNearestCity( tour.Last(), unvisited );  

	//		tour.Add( next );

	//		unvisited.Remove( next );

	//		base.map.MarkVisited( tour );
	//	}

	//	base.map.ResetVisited();

	//	return tour;
	//}

	//int GetNearestCity( int node, IList<int> path )
	//{
	//	var edge = base.map.FindEdges( node, path ).MinBy( e => e.Weight );

	//	edge.SetNext( node );

	//	return edge.Next;
	//}
	//TspResult Swap( IList<int> path )
	//{
	//	var copy = new List<int>( path );
	//	double tour = map.GetTour( path );

	//	int count = 0;

	//	while( true )
	//	{
	//		(Action accept, double delta) = base.Swap( copy );

	//		if( delta < 0 )
	//		{
	//			tour += delta;
	//			accept!();
	//			count = 0;
	//		}

	//		if( ++count > 1000 ) 
	//			break;
	//	}

	//	return new TspResult { Tour = tour, Path = copy };
	//}

	//IList<int> BuildGreedySolution( IList<int> path )
	//{
	//	var copy = new List<int>( path );

	//	List<int> tour = [];
	//	int currentCity = copy[ 0 ]; // Starting city

	//	tour.Add( currentCity );
	//	copy.Remove( currentCity );

	//	while( copy.Any() ) // Select the nearest city
	//	{			
	//		int nextCity = FindNearestCity( currentCity, copy );			

	//		tour.Add( nextCity );

	//		copy.Remove( nextCity );

	//		currentCity = nextCity;

	//		base.map.MarkVisited( tour );
	//	}

	//	base.map.ResetVisited();

	//	return tour;
	//}
	//
	#endregion

	/// <summary>
	/// Configuration Settings
	/// </summary>
	class GraspSettings : IlsSettings
	{
		public double Factor { get; set; }
		public int Take { get; set; }
	}
}
