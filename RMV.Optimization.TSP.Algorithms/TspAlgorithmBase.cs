using System.Diagnostics;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Parent class for TSP algorithms
/// </summary>
public abstract class TspAlgorithmBase : ITspAsync
{

	#region Constructor --------------------------------------------------------

	public TspAlgorithmBase( TspMap map ) 
	{
		this.map = map;		

		Configure();
	}

	protected virtual void Configure() { }

	#endregion


	#region Properties ---------------------------------------------------------

	protected int count = 0; // Number of epochs

	protected readonly TspMap map;
	protected int Cities => map.Cities;

	protected readonly Stopwatch timer = new();

	protected const double MARGIN = 0.0001;

	public TspConfigurationBase settings;

	protected readonly ManualResetEventSlim pauseEvent = new( true );
	public void Pause() => pauseEvent.Reset();
	public void Resume() => pauseEvent.Set();

	#endregion


	#region RunAsync -----------------------------------------------------------

	/// <summary>
	/// Generic workflow for TSP algorithms
	/// </summary>	
	public async Task<TspResult> RunAsync( CancellationToken? token = null )
	{
		this.timer.Start();
				
		int noChanges = 0;

		TspResult best = Initialize();

		if( best != null ) Draw( best.Tour, ++count, best.Path );

		await Task.Run( () => 
		{
			while( noChanges++ < settings.Limit )
			{
				pauseEvent.Wait( token ?? CancellationToken.None ); //pause support

				if( token?.IsCancellationRequested == true ) return; // ensure the method returns a Task-compatible type

				TspResult current = RunEpoch( best );

				//if( best != null )
				//{
					if( current < best )
					{
						best = current.Clone();
						noChanges = 0;
						Draw( best.Tour, count, best.Path );
					}

					if( ++count % settings.Redraw == 0 ) Draw( best.Tour, count );
				//}
			}

			Draw( best.Tour, ++count, best.Path );

		}, token ?? CancellationToken.None ).ConfigureAwait( false ); //Provide a default CancellationToken if null and ensure proper async behavior

		this.timer.Stop();

		return best;
	}

	protected virtual TspResult? Initialize() => default;
	protected TspResult? InitializeTour() => this.map.BuildRandomTour();
	protected List<TspResult> InitializePopulation( int size ) => [ .. Enumerable.Range( 0, size ).Select( _ => InitializeTour() ) ];
	protected virtual TspResult? RunEpoch( TspResult best ) { return default; }

	#endregion


	#region Draw ---------------------------------------------------------------

	public EventHandler<DrawEventArgs> OnDraw;
	void TriggerDraw( DrawEventArgs ea ) => OnDraw?.Invoke( null, ea );

	protected void Draw( double tour, int count, IEnumerable<int>? path = null )
	{
		string time = $" {timer.Elapsed.Minutes}m:{timer.Elapsed.Seconds}c";//:{timer.Elapsed.Milliseconds}";

		TriggerDraw( new DrawEventArgs( tour, count, time, path ) );
	}

	#endregion

	
	#region RandomSwap ---------------------------------------------------------

	/// <summary>
	/// Randomly swaps two cities in the path to create a new path
	/// </summary>	
	protected static List<int> RandomSwap( IList<int> path )
	{
		var copy = new List<int>( path );

		var indexes = IRandomSequence.GetUniqueInts( 2, 0, path.Count - 1 );

		int i = indexes[ 0 ];
		int j = indexes[ 1 ];

		(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);

		return copy;
	}

	protected TspResult RandomSwap( TspResult result )
	{
		var copy = new List<int>( result.Path );

		(int i, int j) = IRandomSequence.GetPairInts( 0, result.Path.Count - 1 );

		(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);

		return TspResult.Build( this.map, copy );
	}

	//protected TspResult RandomSwap( TspResult result )
	//{
	//	var copy = new List<int>( result.Path );

	//	var indexes = IRandomSequence.GetUniqueInts( 2, 0, result.Path.Count - 1 );

	//	int i = indexes[ 0 ];
	//	int j = indexes[ 1 ];

	//	(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);

	//	return new TspResult( this.map.GetTourLength( copy ), copy );
	//}

	#endregion


	#region Local 2-opt Search -------------------------------------------------

	/// <summary>
	/// Local Search algorithm
	/// </summary>
	/// <param name="path">target path</param>
	/// <remarks>iteratively reverses segments in the path to improve the tour length</remarks>
	protected TspResult Local2OptSearch( IList<int> path )
	{
		double tour = this.map.GetTourLength( path );

		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int i = 0; i < this.Cities - 2; i++ )
			{
				for( int j = i + 2; j < this.Cities; j++ )
				{
					var newPath = TwoOptSwap( path, i, j );

					double newTour = this.map.GetTourLength( newPath );

					if( newTour + MARGIN < tour )
					{
						tour = newTour;
						path = newPath;
						improved = true;
					}
				}
			}
		}

		return new TspResult( tour, path );
	}

	/// <summary>
	/// Parallel local 2-opt search for TSP
	/// </summary>	
	/// <remarks>: tries to improve the tour by removing two edges and reconnecting the nodes</remarks>
	protected TspResult Parallel2OptSearch( IList<int> path )
	{
		double tour = this.map.GetTourLength( path );

		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1, bestJ = -1;
			List<int> bestPath = [];		

			Parallel.For( 0, this.Cities - 1, i =>
			{
				for( int j = i + 1; j < this.Cities; j++ )
				{
					var newPath = TwoOptSwap( path, i, j );
					double newTour = this.map.GetTourLength( newPath );

					double delta = tour - newTour;

					if( delta > MARGIN )
					{
						lock( lockObj )
						{
							if( delta > bestDelta )
							{
								bestDelta = delta;
								(bestI, bestJ) = (i, j);
								bestPath = newPath;
							}
						}
					}
				}
			} );

			if( bestI != -1 && bestJ != -1 )
			{
				path = TwoOptSwap( path, bestI, bestJ );
				tour = this.map.GetTourLength( path );
				improved = true;
			}			
		}

		return new TspResult( tour, path );
	}

	#endregion


	#region Local 2.5-opt Search -----------------------------------------------

	#region obsolete
	/// <summary>
	/// Local 2.5-opt search for TSP: tries to improve the tour by removing two edges and inserting a node
	/// </summary>	
	//protected TspResult Local2Point5OptSearch( IList<int> path )
	//{
	//	var copy = new List<int>( path );

	//	bool improvement = true;

	//	while( improvement )
	//	{
	//		improvement = false;

	//		// 2-opt improvement
	//		for( int i = 0; i < copy.Count - 2; i++ )
	//		{
	//			for( int j = i + 2; j < copy.Count; j++ )
	//			{
	//				if( CalculateDelta( copy, i, j ) < -MARGIN )
	//				{
	//					ReverseSegment( copy, i + 1, j );
	//					improvement = true;
	//					goto RestartSearch;
	//				}
	//			}
	//		}

	//		// 2.5-opt node insertion
	//		for( int i = 0; i < copy.Count - 2; i++ )
	//		{
	//			for( int j = i + 2; j < copy.Count; j++ )
	//			{
	//				for( int k = 0; k < copy.Count; k++ )
	//				{
	//					if( k == i || k == j || k == j + 1 ) continue;
	//					//if( j < 0 || j >= copy.Count ) continue;

	//					int node = copy[ j ];
	//					int insertPos = k > j ? k - 1 : k;

	//					if( insertPos < 0 || insertPos > copy.Count - 1 ) continue;

	//					// Defensive: ensure indices for CalculateInsertionDelta are valid
	//					//if( i < 0 || i >= copy.Count - 1 || insertPos < 0 || insertPos >= copy.Count ) continue;

	//					if( CalculateInsertionDelta( copy, i, j, k ) < -MARGIN )
	//					{
	//						copy.RemoveAt( j );
	//						copy.Insert( insertPos, node );
	//						improvement = true;
	//						goto RestartSearch;
	//					}
	//				}
	//			}
	//		}

	//		break; // If no improvement, break out of the loop

	//	RestartSearch:;
	//	}

	//	return new TspResult( this.map.GetTourLength( copy ), copy );
	//}

	//double CalculateInsertionDelta( List<int> path, int i, int j, int k )
	//{
	//	int n = path.Count;

	//	if( i < 0 || i >= n - 1 || j < 0 || j >= n || k < 0 || k >= n )
	//		throw new ArgumentOutOfRangeException( nameof( path ), $"Out of range:i={i},j={j},k={k}" );

	//	int a = path[ i ], b = path[ ( i + 1 ) % n ], d = path[ k ];

	//	return this.map[ a, d ].Weight + this.map[ d, b ].Weight - this.map[ a, b ].Weight;
	//}
	#endregion

	/// <summary>
	/// Local 2.5-opt search for TSP: tries to improve the tour by removing two edges and inserting a node
	/// </summary>	
	protected TspResult Local2Point5OptSearch( IList<int> path )
	{
		var copy = new List<int>( path );

		bool improvement = true;

		while( improvement )
		{
			improvement = false;

			for( int i = 0; i < copy.Count - 2; i++ )
			{
				for( int j = i + 2; j < copy.Count; j++ ) // Perform 2-opt swap
				{									
					if( CalculateDelta( copy, i, j ) < -MARGIN )
					{
						ReverseSegment( copy, i + 1, j );
						improvement = true;
					}
										
					for( int k = 0; k < copy.Count; k++ ) // Perform 2.5-opt node insertion
					{
						if( k != i && k != i + 1 && k != j )
						{							
							if( CalculateInsertionDelta( copy, i, k ) < -MARGIN )
							{
								InsertNode( copy, j, k );
								improvement = true;
							}
						}
					}
				}
			}
		}

		return TspResult.Build( this.map, copy ); 
	}

	/// <summary>
	/// Parallel local 2.5-opt search for TSP: tries to improve the tour by removing two edges and inserting a node
	/// </summary>
	protected TspResult Parallel2p5OptSearch( IList<int> path )
	{
		var copy = new List<int>( path );

		while( true )
		{
			double bestDelta = 0;
			int bestI = -1, bestJ = -1, bestK = -1;
			bool isTwoOpt = false;

			// 2-opt: find best move in parallel
			Parallel.For( 0, copy.Count - 2, i => 
			{
				for( int j = i + 2; j < copy.Count; j++ )
				{
					double delta = CalculateDelta( copy, i, j );

					if( delta < -MARGIN )
					{
						lock( copy )
						{
							if( delta < bestDelta )
							{
								bestDelta = delta;
								bestI = i;
								bestJ = j;
								bestK = -1;
								isTwoOpt = true;
							}
						}
					}
																
					for( int k = 0; k < copy.Count; k++ ) // 2.5-opt: find best node insertion
					{
						if( k == i || k == i + 1 || k == j ) continue;

						delta = CalculateInsertionDelta( copy, i, k );

						if( delta < -MARGIN )
						{
							lock( copy )
							{
								if( delta < bestDelta )
								{
									bestDelta = delta;
									bestI = i;
									bestJ = j;
									bestK = k;
									isTwoOpt = false;
								}
							}
						}
					}
				}
			} );

			if( bestDelta > -MARGIN ) break;

			if( isTwoOpt ) // Apply best move
			{
				ReverseSegment( copy, bestI + 1, bestJ );
			}
			else
			{
				InsertNode( copy, bestJ, bestK );
			}
		}

		return TspResult.Build( this.map, copy );
	}

	/// <summary>
	/// Calculates the delta for a 2-opt swap in the path
	/// </summary>	
	double CalculateDelta( List<int> path, int i, int j )
	{
		int a = path[ i ], b = path[ i + 1 ], c = path[ j ], d = path[ ( j + 1 ) % path.Count ];

		return this.map[ a, c ].Weight + this.map[ b, d ].Weight - this.map[ a, b ].Weight - this.map[ c, d ].Weight;
	}

	/// <summary>
	/// Calculates the delta for inserting a node into the path
	/// </summary>	
	double CalculateInsertionDelta( List<int> path, int i, int k )
	{
		int a = path[ i ], b = path[ i + 1 ], d = path[ k ];

		return this.map[ a, d ].Weight + this.map[ d, b ].Weight - this.map[ a, b ].Weight;
	}

	/// <summary>
	/// Inserts a node into the path at position k, removing it from position j
	/// </summary>	
	static void InsertNode( List<int> tour, int j, int k )
	{
		int node = tour[ j ];
		tour.RemoveAt( j );
		tour.Insert( k, node );
	}

	#endregion


	#region Local/Parallel 3-opt Search ----------------------------------------

	/// <summary>
	/// Local 3-opt search for TSP: tries to improve the tour by removing three edges and reconnecting the nodes
	/// </summary>	
	protected TspResult Local3OptSearch( IList<int> path )
	{
		double best = this.map.GetTourLength( path );

		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int i = 1; i < path.Count - 3; i++ )
			{
				for( int j = i + 1; j < path.Count - 2; j++ )
				{
					for( int k = j + 1; k < path.Count - 1; k++ )
					{
						var paths = Generate3OptSwaps( path, i, j, k );

						foreach( var newPath in paths )
						{
							double tour = this.map.GetTourLength( newPath );

							if( tour < best )
							{
								best = tour;
								path = newPath;
								improved = true;
								break;
							}
						}

						if( improved ) break;
					}

					if( improved ) break;
				}

				if( improved ) break;
			}
		}

		return new TspResult( best, path ); ;
	}

	protected TspResult Parallel3OptSearch( IList<int> path )
	{
		var currentPath = new List<int>( path );
		double bestTour = this.map.GetTourLength( currentPath );

		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1, bestJ = -1, bestK = -1;
			List<int>? bestNewPath = null;

			Parallel.For( 1, currentPath.Count - 3, i =>
			{
				for( int j = i + 1; j < currentPath.Count - 2; j++ )
				{
					for( int k = j + 1; k < currentPath.Count - 1; k++ )
					{
						var swaps = Generate3OptSwaps( currentPath, i, j, k );

						foreach( var newPath in swaps )
						{
							double newTour = this.map.GetTourLength( newPath );
							double delta = bestTour - newTour;

							if( delta > MARGIN )
							{
								lock( lockObj )
								{
									if( delta > bestDelta )
									{
										bestDelta = delta;
										bestI = i;
										bestJ = j;
										bestK = k;
										bestNewPath = new List<int>( newPath );
									}
								}
							}
						}
					}
				}
			} );

			if( bestNewPath != null )
			{
				currentPath = bestNewPath;
				bestTour = this.map.GetTourLength( currentPath );
				improved = true;
			}
		}

		return new TspResult( bestTour, currentPath );
	}



	/// <summary>
	/// Generates all possible 3-opt swaps for the given tour and indices i, j, k
	/// </summary>	
	static List<List<int>> Generate3OptSwaps( IList<int> tour, int i, int j, int k )
	{
		List<List<int>> newTours = [];

		// Case 1: Reverse segment [i+1, j]
		List<int> case1 = [ .. tour ];
		case1.Reverse( i + 1, j - i );
		newTours.Add( case1 );

		// Case 2: Reverse segment [j+1, k]
		List<int> case2 = [ .. tour ];
		case2.Reverse( j + 1, k - j );
		newTours.Add( case2 );

		// Case 3: Reverse both segments [i+1, j] and [j+1, k]
		List<int> case3 = [ .. tour ];
		case3.Reverse( i + 1, j - i );
		case3.Reverse( j + 1, k - j );
		newTours.Add( case3 );

		return newTours;
	}	

	#endregion


	#region Lin-Kernighan Search -----------------------------------------------

	/// <summary>
	/// Local Lin-Kernighan Search for TSP
	/// </summary>
	/// <remarks>iteratively reverses segments in the path to improve the tour length</remarks>	
	public TspResult LinKernighanSearch( IList<int> path )
	{		
		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int i = 0; i < this.Cities - 1; i++ )
			{
				for( int j = i + 1; j < this.Cities; j++ )
				{
					double delta = GetDelta( path, i, j );

					if( delta < 0 ) // Reverse the segment between i+1 and j
					{
						ReverseSegment( path, i + 1, j );

						improved = true;						
					}
				}
			}		
		}

		return new TspResult( this.map.GetTourLength( path ), path );
	}

	double GetDelta( IList<int> path, int i, int j )
	{		
		int a = path[ i ], b = path[ ( i + 1 ) % this.Cities ], c = path[ j ], d = path[ ( j + 1 ) % this.Cities ];
		
		return this.map[ a, c ].Weight + this.map[ b, d ].Weight - this.map[ a, b ].Weight - this.map[ c, d ].Weight;
	}

	static void ReverseSegment( IList<int> path, int start, int end )
	{
		//int mid = ( end - start + 1 ) / 2;

		//for( int offset = 0; offset < mid; offset++ )
		//{
		//	(path[ start + offset ], path[ end - offset ]) = (path[ end - offset ], path[ start + offset ]);
		//}

		while( start < end )
		{
			(path[ start ], path[ end ]) = (path[ end ], path[ start ]); // Swap elements			
			start++;
			end--;
		}
	}

	#endregion


	#region Swap ---------------------------------------------------------------	

	/// <summary>
	/// Swaps two cities in the path and returns the action and the delta of the tour length after the swap
	/// </summary>
	/// <remarks>Based on ideas from https://github.com/Inspiaaa/TSP-Simulated-Annealing/blob/master/Scripts/SimulatedAnnealing.cs</remarks>	
	public (Action, double) Swap( IList<int> path )
	{
		return Random.Shared.Next( 6 ) switch 
		{							
			0 => GetDeltaAfterSwap( path ), //swap random cities													  
			1 or 2 => GetDeltaAfterTransport( path ), // This operation only works for more than 3 cities																	
			3 or 4 or 5 => GetDeltaAfterReverse( path ), // Twice as likely as it is more powerful.
			_ => throw new ArgumentException( "OOPS!" ),
		};
	}

	#region Swap -----------------------------------------------------

	(Action, double) GetDeltaAfterSwap( IList<int> path )
	{
		int indexA = RandomIndex();
		int indexB = RandomIndex();

		void action() => SwapCities( path, indexA, indexB );
		double delta = GetAfterSwap( path, indexA, indexB );

		return (action, delta);
	}

	int RandomIndex() => Random.Shared.Next( this.Cities );
	int WrapIndex( int index ) => ( ( index % this.Cities ) + this.Cities ) % this.Cities;

	static void SwapCities( IList<int> path, int i, int j ) => (path[ i ], path[ j ]) = (path[ j ], path[ i ]);

	double GetAfterSwap( IList<int> path, int indexA, int indexB )
	{
		int indexBeforeA = WrapIndex( indexA - 1 );
		int posBeforeA = path[ indexBeforeA ];
		int posA = path[ indexA ];
		int indexAfterA = WrapIndex( indexA + 1 );
		int posAfterA = path[ indexAfterA ];

		int indexBeforeB = WrapIndex( indexB - 1 );
		int posBeforeB = path[ indexBeforeB ];
		int posB = path[ indexB ];
		int indexAfterB = WrapIndex( indexB + 1 );
		int posAfterB = path[ indexAfterB ];

		double delta = -this.map[ posBeforeA, posA ].Weight - this.map[ posA, posAfterA ].Weight - this.map[ posBeforeB, posB ].Weight - this.map[ posB, posAfterB ].Weight;

		// Positions of predecessors/successors may change due to the swap
		posBeforeA = indexBeforeA == indexB ? posA : posBeforeA;
		posAfterA = indexAfterA == indexB ? posA : posAfterA;

		posBeforeB = indexBeforeB == indexA ? posB : posBeforeB;
		posAfterB = indexAfterB == indexA ? posB : posAfterB;

		delta += this.map[ posBeforeA, posB ].Weight + this.map[ posB, posAfterA ].Weight + this.map[ posBeforeB, posA ].Weight + this.map[ posA, posAfterB ].Weight;

		return delta;
	}

	#endregion

	#region Transport ------------------------------------------------

	(Action, double) GetDeltaAfterTransport( IList<int> path )
	{
		int startIndex = RandomIndex();
		int count = Random.Shared.Next( 1, this.Cities / 4 );

		// Note: count+distance must be LESS than the number of cities for the GetDistanceDeltaAfterTransport
		int distance = Random.Shared.Next( 1, this.Cities - count );

		void action() => TransportRange( path, startIndex, count, distance );

		double delta = GetAfterTransport( path, startIndex, count, distance );

		return (action, delta);
	}

	void TransportRange( IList<int> path, int startIndex, int count, int distance )
	{
		var citiesToMove = new int[ count ];

		for( int i = 0; i < count; i++ )
		{
			citiesToMove[ i ] = path[ WrapIndex( startIndex + i ) ];
		}

		for( int i = 0; i < distance; i++ )// Move the right segment to the left
		{
			path[ WrapIndex( startIndex + i ) ] = path[ WrapIndex( startIndex + i + count ) ];
		}

		for( int i = 0; i < count; i++ ) // Move the previous left segment to the right.
		{
			path[ WrapIndex( startIndex + distance + i ) ] = citiesToMove[ i ];
		}
	}

	double GetAfterTransport( IList<int> path, int startIndex, int count, int distance )
	{
		int leftSegmentStartIndex = startIndex;
		int leftSegmentEndIndex = WrapIndex( startIndex + count - 1 );
		int indexBeforeLeftSegment = WrapIndex( startIndex - 1 );

		int posBeforeLeftSegment = path[ indexBeforeLeftSegment ];
		int leftSegmentStart = path[ leftSegmentStartIndex ];
		int leftSegmentEnd = path[ leftSegmentEndIndex ];

		int rightSegmentStartIndex = WrapIndex( leftSegmentEndIndex + 1 );
		int rightSegmentEndIndex = WrapIndex( rightSegmentStartIndex + distance - 1 );
		int indexAfterRightSegment = WrapIndex( rightSegmentEndIndex + 1 );

		int rightSegmentStart = path[ rightSegmentStartIndex ];
		int rightSegmentEnd = path[ rightSegmentEndIndex ];
		int posAfterRightSegment = path[ indexAfterRightSegment ];

		double delta = -this.map[ posBeforeLeftSegment, leftSegmentStart ].Weight - this.map[ leftSegmentEnd, rightSegmentStart ].Weight - 
			this.map[ rightSegmentEnd, posAfterRightSegment ].Weight;

		delta += this.map[ posBeforeLeftSegment, rightSegmentStart ].Weight + this.map[ rightSegmentEnd, leftSegmentStart ].Weight + 
			this.map[ leftSegmentEnd, posAfterRightSegment ].Weight;

		return delta;
	}

	#endregion

	#region Reverse --------------------------------------------------

	(Action, double) GetDeltaAfterReverse( IList<int> path )
	{
		int reverseStartIndex = RandomIndex();
		int reverseCount = Random.Shared.Next( 1, this.Cities / 2 );

		void action() => ReverseRange( path, reverseStartIndex, reverseCount );
		double delta = GetAfterReverse( path, reverseStartIndex, reverseCount );

		return (action, delta);
	}

	void ReverseRange( IList<int> cities, int startIndex, int count )
	{
		for( int i = 0; i <= count / 2; i++ )
		{
			int left = WrapIndex( startIndex + i );
			int right = WrapIndex( startIndex + count - i );

			SwapCities( cities, left, right );
		}
	}

	double GetAfterReverse( IList<int> path, int startIndex, int count )
	{
		int endIndex = WrapIndex( startIndex + count );

		var beforeStart = path[ WrapIndex( startIndex - 1 ) ];
		var startPosition = path[ startIndex ];
		var endPosition = path[ endIndex ];
		var afterEnd = path[ WrapIndex( endIndex + 1 ) ];

		// When reversing a range of cities, the distances between the individual cities remain the same.
		// The only thing that changes are the distances between the start and end positions to their predecessor and successor, respectively.

		return -this.map[ beforeStart, startPosition ].Weight - this.map[ endPosition, afterEnd ].Weight + 
			this.map[ beforeStart, endPosition ].Weight + this.map[ startPosition, afterEnd ].Weight;
	}

	#endregion

	#endregion


	#region TwoOptSwap ---------------------------------------------------------

	/// <summary>
	/// Reverses the order of nodes between indices i and j in the path
	/// </summary>	
	protected static List<int> TwoOptSwap( IList<int> path, int i, int j )
	{
		var copy = new List<int>( path );

		copy.Reverse( i, j - i + 1 );

		return copy;
	}


	/// <summary>
	/// Randomly selects two indices in the path and reverses the segment between them
	/// </summary>	
	protected static List<int> Random2OptSwap( IList<int> path )
	{
		int n = path.Count;	//if( n < 4 ) return new List<int>( path ); // Not enough cities to swap

		int i = Random.Shared.Next( n - 1 );    // i in [0, n-2]
		int j = Random.Shared.Next( i + 1, n ); // j in [i+1, n-1]

		var copy = new List<int>( path );

		copy.Reverse( i, j - i + 1 ); // Reverse segment [i, j] inclusive

		return copy;
	}
	

	/// <summary>
	/// Improves a given tour by removing three edges and reconnecting the nodes
	/// </summary>	
	protected static List<int> ThreeOptSwap( List<int> path, int i, int j, int k )
	{
		List<int> copy = new( path );

		copy.Reverse( i + 1, j - i ); // Reverse the segment between i+1 and j

		copy.Reverse( j + 1, k - j ); // Reverse the segment between j+1 and k

		return copy;
	}



	#endregion


	#region OrOptSwap ----------------------------------------------------------

	/// <summary>
	/// Or-Opt local search for TSP: moves segments of length 1, 2, or 3 to a new position if it improves the tour.
	/// </summary>
	/// <param name="best">Initial TspResult (will not be modified)</param>
	/// <param name="maxSegmentLength">Maximum segment length to move (1, 2, or 3)</param>
	/// <returns>Improved TspResult</returns>
	protected TspResult OrOptSwap( IList<int> path, int maxSegmentLength = 3 )
	{
		double tour = this.map.GetTourLength( path );

		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int segmentLength = 1; segmentLength <= maxSegmentLength; segmentLength++ )
			{
				if( segmentLength >= this.Cities ) continue; // Don't move the whole tour or more

				for( int i = 0; i < this.Cities; i++ )
				{
					// Build the segment to move (wraps around)
					var segment = Enumerable.Range( 0, segmentLength ).Select( k => path[ ( i + k ) % this.Cities ] ).ToList();

					var rest = path.Except( segment ).ToList();  // Build the rest of the tour without the segment			   

					improved = TryInsert( ref path, ref tour, i, segment, rest ); // Try all possible insertion positions

					if( improved ) goto NextIteration; // Restart search after improvement					
				}
			}
		NextIteration:;
		}

		return new TspResult( tour, path );
	}

	bool TryInsert( ref IList<int> path, ref double tour, int i, List<int> segment, List<int> rest )
	{
		for( int insertPos = 0; insertPos <= rest.Count; insertPos++ )
		{
			if( insertPos == i ) continue; // Don't insert back at the original position

			var newPath = new List<int>( rest );

			newPath.InsertRange( insertPos, segment );

			double newTour = this.map.GetTourLength( newPath );

			if( newTour + MARGIN < tour )
			{
				tour = newTour;
				path = newPath;

				return true; //goto NextIteration; 
			}
		}

		return false;
	}

	#endregion	


	#region Nearest Neighbour --------------------------------------------------

	/// <summary>
	/// Nearest Neighbour helper
	/// </summary>
	protected TspResult BuildNearestTour()
	{
		var best = new TspResult( double.MaxValue, new int[ this.Cities ] );

		for( int city = 0; city < this.Cities; city++ )
		{
			var result = GetNearest( city );

			if( result < best ) best = result;
		}

		return best;
	}

	/// <summary>
	/// Nearest Neighbour algorithm for starting node
	/// </summary>
	/// <param name="start">starting node</param>
	protected TspResult GetNearest( int start )
	{
		List<int> path = [ start ];

		var available = Enumerable.Range( 0, this.Cities ).Except( path ).ToList();

		double tour = 0;

		while( available.Any() )
		{
			int nearest = available.MinBy( city => this.map[ path[ ^1 ], city ].Weight );

			tour += this.map[ path[ ^1 ], nearest ].Weight;

			path.Add( nearest );
			available.Remove( nearest );
		}

		tour += this.map[ path[ 0 ], path[ ^1 ] ].Weight;

		return new TspResult( tour, path );
	}

	#endregion


	#region Duplicates ---------------------------------------------------------

	/// <summary>
	/// Replace duplicates in the population by random tour
	///</summary>
	protected List<TspResult> CleanDuplicates( List<TspResult> population )
	{
		int count = population.Count;

		var result = population.Distinct().OrderBy( c => c.Tour ).ToList();

		while( result.Count < count ) result.Add( this.map.BuildRandomTour() );

		return result;
	}

	/// <summary>
	/// Alter duplicates in the population
	/// </summary>	
	protected List<TspResult> HandleDuplicates( List<TspResult> population, Func<IList<int>, IList<int>> func )
	{
		var (unique, duplicates) = Split( population );

		if( !duplicates.Any() ) return [ .. unique.OrderBy( u => u.Tour ) ]; // No duplicates to swap

		ChangeDuplicates( duplicates, func );

		return [ .. unique.Concat( duplicates ).OrderBy( u => u.Tour ) ];
	}	

	/// <summary>
	/// Splits collection into unique and duplicate solutions
	/// </summary>	
	static (List<TspResult>, List<TspResult>) Split( List<TspResult> results )
	{
		var unique = new List<TspResult>();
		var duplicate = new List<TspResult>();

		var seen = new HashSet<TspResult>();

		foreach( var item in results )
		{
			if( !seen.Add( item ) )
			{
				duplicate.Add( item );
			}
			else
			{
				unique.Add( item );
			}
		}

		return (unique, duplicate);
	}

	/// <summary>
	/// Alter the path of duplicate solutions by applying a Func
	/// </summary>	
	void ChangeDuplicates( List<TspResult> duplicates, Func<IList<int>, IList<int>> func )
	{
		foreach( var duplicate in duplicates )
		{
			duplicate.Path = func( duplicate.Path ); // alter the path of the duplicate solution
			duplicate.Tour = this.map.GetTourLength( duplicate.Path ); // Recalculate tour length after mutation			
		}
	}

	#endregion


	#region Selection ----------------------------------------------------------

	protected static TspResult RouletteWheelSelection( List<TspResult> population )
	{
		double totalFitness = population.Sum( ind => ind.Fitness );

		double randomValue = Random.Shared.NextDouble() * totalFitness;

		double cumulativeFitness = 0.0;

		foreach( var individual in population )
		{
			cumulativeFitness += individual.Fitness;

			if( cumulativeFitness > randomValue ) return individual;
		}

		return population.First(); // Fallback
	}

	protected static TspResult TournamentSelection( List<TspResult> population, int tournamentSize )
	{
		var tournament = new List<TspResult>();

		for( int i = 0; i < tournamentSize; i++ )
		{
			tournament.Add( population[ Random.Shared.Next( population.Count ) ] );
		}

		return tournament.MinBy( i => i.Fitness );
	}

	protected static TspResult RankBasedSelection( List<TspResult> population )
	{
		var rankedPopulation = population.OrderBy( ind => ind.Fitness ).Select( ( ind, index ) => new { Individual = ind, Rank = index + 1 } ).ToList();

		double totalRank = rankedPopulation.Sum( r => r.Rank );

		double randomValue = Random.Shared.NextDouble() * totalRank;

		double cumulativeRank = 0.0;

		foreach( var ranked in rankedPopulation )
		{
			cumulativeRank += ranked.Rank;

			if( cumulativeRank > randomValue ) return ranked.Individual;
		}

		return rankedPopulation.First().Individual; // Fallback
	}

	#endregion


	#region Crossover ----------------------------------------------------------

	/// <summary>
	/// Ordered crossover for TSP
	/// </summary>	
	protected TspResult Crossover( TspResult parent1, TspResult parent2 )
	{
		int length = parent1.Path.Count;

		(int start, int end) = IRandomSequence.GetPairInts( 0, length - 1 ); // crossover points	

		var child = new int[ length ];

		Array.Copy( parent1.Path.ToArray(), start, child, start, end - start + 1 ); // Initialize child with parent1's path		

		int index = ( end + 1 ) % length;

		for( int i = 0; i < length; i++ ) // Fill the remaining positions with genes from parent2 in order
		{
			int gene = parent2.Path[ ( end + 1 + i ) % length ];

			if( !child.Contains( gene ) )
			{
				child[ index ] = gene;

				index = ( index + 1 ) % length;
			}
		}

		return TspResult.Build( this.map, child );
	}

	#endregion

	#region obsolete
	//static void BuildSwap( IList<int> tour, int i, int j, List<List<int>> newTours )
	//{
	//	List<int> path = new( tour );

	//	path.Reverse( i, j );

	//	newTours.Add( path );
	//}
	#endregion
}
