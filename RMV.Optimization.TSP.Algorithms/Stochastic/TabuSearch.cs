using Microsoft.Extensions.Configuration;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Taboo Search for TSP
/// </summary>
public class TabuSearch( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{
	TabuSettings settings;

	readonly TspMap Map = map;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<TabuSettings>( "taboo" );
	}

	#region obsolete
	//static T Iif<T>( bool condition, T truePart, T falsePart ) => condition ? truePart : falsePart;
	///// <summary>
	///// Taboo Search async wrapper
	///// </summary>	
	//public async Task<TspResult> RunAsync()
	//{
	//	base.timer.Start();

	//	TspResult best = base.BuildRandomTour();

	//	await Task.Run( () => 
	//	{
	//		int count = 0;
	//		int noChanges = 0;						

	//		Queue<(int, int)> tabuList = new();

	//		while( noChanges++ < settings.Limit )
	//		{
	//			#region obsolete
	//			//var neighborhood = GenerateNeighborhood( current.Path );

	//			//// Filter out tabu moves
	//			//var filtered = neighborhood.Where( n => !tabuList.Contains( (n.Path[ 0 ], n.Path[ 1 ]) ) ).ToList();

	//			//best = filtered.MinBy( n => n.Tour ); // Select the best move

	//			//if( best?.Path != null )
	//			//{
	//			//	current = best.Clone() as TspResult;					

	//			//	AddToTabuList( tabuList, current.Path[ 0 ], current.Path[ 1 ] );					

	//			//	if( current < best )
	//			//	{
	//			//		best = current.Clone() as TspResult;

	//			//		noChanges = 0;

	//			//		base.Draw( best.Tour, count, best.Path );
	//			//	}
	//			//}
	//			//List<int> bestCandidate = null;
	//			//int bestCandidateCost = int.MaxValue;
	//			#endregion

	//			var candidate = new TspResult( double.MaxValue, null );

	//			for( int i = 0; i < base.Cities - 1; i++ )
	//			{
	//				for( int j = i + 1; j < base.Cities; j++ )
	//				{
	//					if( !IsTabu( tabuList, best, i, j ) )
	//					{
	//						var current = SwapCities( best.Path, i, j );							

	//						if( current < candidate ) candidate = current.Clone();
	//					}
	//				}
	//			}

	//			if( candidate < best )
	//			{
	//				best = candidate.Clone();

	//				AddToTabuList( tabuList, best.Path[ 0 ], best.Path[ 1 ] ); // Add swap to tabu list

	//				base.Draw( best.Tour, count, best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
	//		}

	//		base.Draw( best.Tour, count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}

	//List<TspResult> GenerateNeighborhood( IList<int> path )
	//{
	//	List<TspResult> neighborhood = [];

	//	for( int i = 1; i < path.Count - 1; i++ )
	//	{
	//		for( int j = i + 1; j < path.Count; j++ )
	//		{
	//			List<int> newPath = new( path );

	//			(newPath[ i ], newPath[ j ]) = (newPath[ j ], newPath[ i ]);

	//			double newCost = this.Map.GetTourLength( newPath );

	//			neighborhood.Add( new TspResult( newCost, newPath) );
	//		}
	//	}

	//	return neighborhood;
	//}

	//void AddToTabuList( Queue<(int, int)> tabuList, int city1, int city2 )
	//{
	//	if( tabuList.Count > settings.Size )	tabuList.Dequeue();		

	//	tabuList.Enqueue( (city1, city2) );
	//}

	//static bool IsTabu( Queue<(int, int)> tabuList, TspResult result, int i, int j ) =>
	//	tabuList.Contains( (result.Path[ i ], result.Path[ j ]) ) || tabuList.Contains( (result.Path[ j ], result.Path[ i ]) );

	//TspResult SwapCities( IList<int> path, int i, int j )
	//{
	//	List<int> newPath = new( path );

	//	(newPath[ i ], newPath[ j ]) = (newPath[ j ], newPath[ i ]); // Swap

	//	double newCost = this.Map.GetTourLength( newPath );

	//	return new TspResult( newCost, newPath );
	//}
	#endregion


	/// <summary>
	/// Taboo Search async wrapper
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		base.timer.Start();

		var current = this.Map.BuildRandomTour();

		var best = current.Clone();

		base.Draw( best.Tour, 0, best.Path );

		await Task.Run( () => 
		{
			int count = 0;
			int noChanges = 0;

			List<IList<int>> tabuList = [];

			while( noChanges++ < settings.Limit )
			{
				var candidates = BuildCandidateList( current, tabuList );

				var bestCandidate = candidates.First();

				if( bestCandidate < current )
				{
					current = bestCandidate;

					if( bestCandidate < best ) best = bestCandidate;

					UpdateTaboo( tabuList, bestCandidate.Path );

					noChanges = 0;

					base.Draw( best.Tour, count, best.Path );
				}

				if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
			}

			base.Draw( best.Tour, count, best.Path );
		} );

		base.timer.Stop();

		return best;
	}


	/// <summary>
	/// Builds a list of candidate solutions by generating random 2-opt moves from the current best solution
	/// </summary>	
	List<TspResult> BuildCandidateList( TspResult current, List<IList<int>> tabu ) =>
		[ .. Enumerable.Range( 0, settings.CandidateSize ).Select( _ => GenerateCandidate( current, tabu ) ).OrderBy( x => x.Tour ) ];


	/// <summary>
	/// Generates a candidate solution by applying a stochastic 2-opt move to the best solution found so far
	/// </summary>	
	TspResult GenerateCandidate( TspResult best, List<IList<int>> tabuList )
	{
		var perm = Random2OptSwap( best.Path );

		while( IsTabu( perm, tabuList ) ) perm = Random2OptSwap( best.Path );

		return new TspResult { Path = perm, Tour = this.Map.GetTourLength( perm ) };
	}

	/// <summary>
	/// Determines if path is in the tabu list
	/// </summary>	
	static bool IsTabu( List<int> path, List<IList<int>> tabuList ) => tabuList.Any( t => t.SequenceEqual( path ) );


	/// <summary>
	/// Updates the tabu list with the best candidate solution found in the current iteration
	/// </summary>	
	void UpdateTaboo( List<IList<int>> tabuList, IList<int> path )
	{
		tabuList.Add( path );

		while( tabuList.Count > settings.TabuListSize ) tabuList.RemoveAt( 0 );
	}
}


	#region obsolete
	//static List<int> ReactiveTabuSearch( int[,] distanceMatrix, int numCities, int maxIterations, int tabuTenure )
	//{
	//	List<int> currentSolution = GenerateInitialSolution( numCities );

	//	List<int> bestSolution = new( currentSolution );

	//	int bestCost = CalculateCost( currentSolution, distanceMatrix );

	//	Queue<(int, int)> tabuList = new();

	//	for( int iteration = 0; iteration < maxIterations; iteration++ )
	//	{
	//		var neighbors = GenerateNeighbors( currentSolution );

	//		List<int> bestNeighbor = [];

	//		int bestNeighborCost = int.MaxValue;

	//		foreach( var neighbor in neighbors )
	//		{
	//			int cost = CalculateCost( neighbor, distanceMatrix );

	//			if( cost < bestNeighborCost && !tabuList.Contains( (neighbor[ 0 ], neighbor[ 1 ]) ) )
	//			{
	//				bestNeighbor = neighbor;
	//				bestNeighborCost = cost;
	//			}
	//		}

	//		if( bestNeighbor != null )
	//		{
	//			currentSolution = bestNeighbor;

	//			if( bestNeighborCost < bestCost )
	//			{
	//				bestSolution = new List<int>( bestNeighbor );
	//				bestCost = bestNeighborCost;
	//			}

	//			tabuList.Enqueue( (currentSolution[ 0 ], currentSolution[ 1 ]) );
	//			if( tabuList.Count > tabuTenure )
	//			{
	//				tabuList.Dequeue();
	//			}
	//		}
	//	}

	//	return bestSolution;
	//}

	//static List<int> GenerateInitialSolution( int numCities )
	//{
	//	var solution = Enumerable.Range( 0, numCities ).ToList();
	//	solution = solution.OrderBy( x => random.Next() ).ToList();
	//	return solution;
	//}
	//static List<List<int>> GenerateNeighbors( List<int> solution )
	//{
	//	var neighbors = new List<List<int>>();

	//	for( int i = 0; i < solution.Count - 1; i++ )
	//	{
	//		for( int j = i + 1; j < solution.Count; j++ )
	//		{
	//			var neighbor = new List<int>( solution );

	//			(neighbor[ i ], neighbor[ j ]) = (neighbor[ j ], neighbor[ i ]);

	//			neighbors.Add( neighbor );
	//		}
	//	}

	//	return neighbors;
	//}
	
	//TspResult ReactiveTabuSearch( int maxIterations )
	//{
	//	var currentSolution = this.Map.BuildRandomTour();

	//	var bestSolution = currentSolution.Clone();		

	//	Queue<(int, int)> tabuList = new();

	//	for( int iteration = 0; iteration < maxIterations; iteration++ )
	//	{
	//		var neighbors = GenerateNeighbors( currentSolution );

	//		var bestNeighbor = new TspResult( double.MaxValue, [] );			

	//		foreach( var neighbor in neighbors )
	//		{
	//			if( neighbor < bestNeighbor && !tabuList.Contains( neighbor.Path ) )//[ 0 ], neighbor[ 1 ]) ) )
	//			{
	//				bestNeighbor = neighbor.Clone();					
	//			}
	//		}

	//		if( bestNeighbor.Path.Any() )
	//		{
	//			currentSolution = bestNeighbor;

	//			if( bestNeighbor < bestSolution ) bestSolution = bestNeighbor.Clone();

	//			tabuList.Enqueue( (currentSolution[ 0 ], currentSolution[ 1 ]) );

	//			if( tabuList.Count > this.settings.TabuListSize ) tabuList.Dequeue();				
	//		}
	//	}

	//	return bestSolution;
	//}

	//List<TspResult> GenerateNeighbors( TspResult solution )
	//{
	//	var neighbors = new List<TspResult>();

	//	int count = solution.Path.Count;

	//	for( int i = 0; i < count - 1; i++ )
	//	{
	//		for( int j = i + 1; j < count; j++ )
	//		{
	//			var path = new List<int>( solution.Path );

	//			(path[ i ], path[ j ]) = (path[ j ], path[ i ]);

	//			neighbors.Add( new TspResult { Path = path, Tour = this.Map.GetTourLength( path ) } );
	//		}
	//	}

	//	return neighbors;
	//}
	
	//static List<int> StochasticTwoOpt( IList<int> path )
	//{
	//	var perm = new List<int>( path );

	//	int count = perm.Count;

	//	int c1 = Random.Shared.Next( count );
	//	int c2 = Random.Shared.Next( count );

	//	List<int> exclude = [ c1, Iif( c1 == 0, count - 1, c1 - 1 ), Iif( c1 == count - 1, 0, c1 + 1 ) ];

	//	while( exclude.Contains( c2 ) ) c2 = Random.Shared.Next( count );

	//	if( c2 < c1 ) (c1, c2) = (c2, c1);

	//	perm.Reverse( c1, c2 - c1 ); // Reverse the segment between c1 and c2		

	//	return perm;
	//}	
	//static (int[], List<IList<int>>) StochasticTwoOpt( IList<int> path )
	//{
	//	var perm = new List<int>( path );

	//	int count = perm.Count;

	//	int c1 = Random.Shared.Next( count );
	//	int c2 = Random.Shared.Next( count );

	//	List<int> exclude = [ c1, Iif( c1 == 0, count - 1, c1 - 1 ), Iif( c1 == count - 1, 0, c1 + 1 ) ];

	//	while( exclude.Contains( c2 ) ) c2 = Random.Shared.Next( count );

	//	if( c2 < c1 ) (c1, c2) = (c2, c1);

	//	perm.Reverse( c1, c2 - c1 ); // Reverse the segment between c1 and c2		

	//	return (perm, new List<IList<int>> { new int[] {
	//		path[ Iif( c1 == 0, count - 1, c1 - 1 ) ], path[ c1 ] }, new int[] { path[ c2 ], path[ Iif( c2 == 0, count - 1, c2 - 1 ) ] }
	//	});
	//}
	//static (int[], List<int[]>) StochasticTwoOpt( int[] parent )
	//{
	//	Random random = new();
	//	int[] perm = ( int[] )parent.Clone();

	//	int c1 = random.Next( perm.Length );
	//	int c2 = random.Next( perm.Length );

	//	List<int> exclude = [ c1, Iif( c1 == 0, perm.Length - 1, c1 - 1 ), Iif( c1 == perm.Length - 1, 0, c1 + 1 ) ];

	//	while( exclude.Contains( c2 ) ) c2 = random.Next( perm.Length );

	//	if( c2 < c1 ) (c1, c2) = (c2, c1);

	//	Array.Reverse( perm, c1, c2 - c1 );

	//	return (perm, new List<int[]> { new int[] { parent[ Iif( c1 == 0, parent.Length - 1, c1 - 1 ) ], parent[ c1 ] }, new int[] { parent[ c2 ], parent[ Iif( c2 == 0, parent.Length - 1, c2 - 1 ) ] } });
	//}

	//static bool IsTabu( int[] permutation, List<int[]> tabuList )
	//{
	//	for( int i = 0; i < permutation.Length; i++ )
	//	{
	//		int c2 = permutation[ Iif( i == permutation.Length - 1, 0, i + 1 ) ];

	//		if( tabuList.Any( t => t.SequenceEqual( [ permutation[ i ], c2 ] ) ) ) return true;
	//		//foreach( var forbiddenEdge in tabuList )
	//		//{
	//		//	if( forbiddenEdge.SequenceEqual( [permutation[ i ], c2] ) )	return true;				
	//		//}
	//	}

	//	return false;
	//}

	//static (Dictionary<string, object>, List<int[]>) GenerateCandidate( Dictionary<string, object> best, List<int[]> tabuList, int[][] cities )
	//{
	//	var (perm, edges) = StochasticTwoOpt( ( int[] )best[ "vector" ] );

	//	while( IsTabu( perm, tabuList ) )
	//	{
	//		(perm, edges) = StochasticTwoOpt( ( int[] )best[ "vector" ] );
	//	}

	//	var candidate = new Dictionary<string, object> { { "vector", perm }, { "cost", Cost( perm, cities ) } };

	//	return (candidate, edges);
	//}

	//public static Dictionary<string, object> Search( int[][] cities, int tabuListSize, int candidateListSize, int maxIter )
	//{
	//	var current = new Dictionary<string, object>
	//	{
	//			{ "vector", RandomPermutation(cities) },
	//			{ "cost", Cost((int[])current["vector"], cities) }
	//	};

	//	var best = current;
	//	var tabuList = new List<int[]>();

	//	for( int iter = 0; iter < maxIter; iter++ )
	//	{
	//		var candidates = new List<(Dictionary<string, object>, List<int[]>)>();

	//		for( int i = 0; i < candidateListSize; i++ )
	//		{
	//			candidates.Add( GenerateCandidate( current, tabuList, cities ) );
	//		}

	//		candidates = candidates.OrderBy( x => ( double )x.Item1[ "cost" ] ).ToList();

	//		var bestCandidate = candidates[ 0 ].Item1;
	//		var bestCandidateEdges = candidates[ 0 ].Item2;

	//		if( ( double )bestCandidate[ "cost" ] < ( double )current[ "cost" ] )
	//		{
	//			current = bestCandidate;

	//			if( ( double )bestCandidate[ "cost" ] < ( double )best[ "cost" ] ) best = bestCandidate;

	//			tabuList.AddRange( bestCandidateEdges );

	//			while( tabuList.Count > tabuListSize )	tabuList.RemoveAt( 0 );				
	//		}
	//		//Console.WriteLine( $" > iteration {iter + 1}, best={( double )best[ "cost" ]}" );
	//	}

	//	return best;
	//}
	//static double Euc2D( int[] c1, int[] c2 ) => Math.Round( Math.Sqrt( Math.Pow( c1[ 0 ] - c2[ 0 ], 2 ) + Math.Pow( c1[ 1 ] - c2[ 1 ], 2 ) ) );

	//static double Cost( int[] permutation, int[][] cities )
	//{
	//	double distance = 0;

	//	for( int i = 0; i < permutation.Length; i++ )
	//	{
	//		int c2 = permutation[ Iif( i == permutation.Length - 1, 0, i + 1 ) ];

	//		distance += Euc2D( cities[ permutation[ i ] ], cities[ c2 ] );
	//	}

	//	return distance;
	//}

	//static int[] RandomPermutation( int[][] cities )
	//{		
	//	var perm = Enumerable.Range( 0, cities.Length ).ToArray();

	//	for( int i = 0; i < perm.Length; i++ )
	//	{
	//		int r = Random.Shared.Next( i, perm.Length );

	//		(perm[ r ], perm[ i ]) = (perm[ i ], perm[ r ]);
	//	}

	//	return perm;
	//}

	//bool IsTabu( Queue<(int, int)> tabuList, int city1, int city2 ) => tabuList.Contains( (city1, city2) ) || tabuList.Contains( (city2, city1) );
	#endregion


/// <summary>
/// Configuration Settings
/// </summary>
public class TabuSettings : TspConfigurationBase
{
	[ConfigurationKeyName( "cand-size" )]
	public int CandidateSize { get; set; } // Number of candidates to generate in each iteration

	[ConfigurationKeyName( "taboo-size" )]
	public int TabuListSize { get; set; } // Size of the tabu list
}
