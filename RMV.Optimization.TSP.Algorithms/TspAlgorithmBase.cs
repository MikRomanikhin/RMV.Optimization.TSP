using System.Diagnostics;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Parent class for TSP algorithms
/// </summary>
public abstract class TspAlgorithmBase 
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
				pauseEvent.Wait( token ?? CancellationToken.None ); // pause support

				if( token?.IsCancellationRequested == true ) return; // ensure the method returns a Task-compatible type

				TspResult current = RunEpoch( best );
								
				if( current == null ) continue;  // Skip if current or best is null			

				if( best != null && current < best )
				{
					best = current.Clone();
					noChanges = 0;
					Draw( best.Tour, count, best.Path );
				}
				else if( best == null ) // Initialize best if it wasn't set by Initialize()
				{					
					best = current.Clone();
					Draw( best.Tour, count, best.Path );
				}

				if( ++count % settings.Redraw == 0 ) Draw( best.Tour, count );
			}

			if( best != null ) Draw( best.Tour, ++count, best.Path );

		}, token ?? CancellationToken.None ).ConfigureAwait( false ); // ensure proper async behavior

		this.timer.Stop();

		return best;
	}

	protected virtual TspResult Initialize() => this.map.BuildRandomTour();
	protected TspResult InitializeTour() => this.map.BuildRandomTour();
	protected List<TspResult> InitializePopulation( int size ) => [ .. Enumerable.Range( 0, size ).Select( _ => InitializeTour() ) ];


	/// <summary>
	/// Executes a single epoch of the algorithm, generating a new TSP result based on the current best solution.
	/// </summary>
	/// <param name="best">The current best solution from the previous epoch.</param>
	/// <returns>A TspResult representing the best solution found during this epoch.</returns>
	protected virtual TspResult RunEpoch( TspResult best ) { return new TspResult( double.MaxValue, [] ); }

	#endregion


	#region Draw ---------------------------------------------------------------

	public EventHandler<DrawEventArgs> OnDraw;
	void TriggerDraw( DrawEventArgs ea ) => OnDraw?.Invoke( null, ea );

	protected void Draw( double tour, int count, IEnumerable<int>? path = null )
	{
		string time = $"{timer.Elapsed:hh\\:mm\\:ss}";//:{timer.Elapsed.Milliseconds}";

		TriggerDraw( new DrawEventArgs( tour, count, time, path ) );
	}

	#endregion


	#region Local Search Selection ---------------------------------------------

		/// <summary>
		/// Selects a local search algorithm to apply in parallel
		/// </summary>
		/// <param name="path">The path to optimize</param>
		/// <returns>The optimized TSP result</returns>	
	protected TspResult ParallelLocalSearch( List<int> path )
	{
		if( path == null || path.Count < 3 ) return TspResult.Build( this.map, path );

		return Random.Shared.Next( 7 ) switch 
		{
			0 or 1 or 2 or 3 => Parallel2OptSearch( path ),
			4 => Parallel2p5OptSearch( path ),
			5 => Parallel3OptSearch( path ),
			_ => ParallelLinKernighanSearch( path )
		};
	}

	#endregion


	#region 2-opt Search -------------------------------------------------------

	/// <summary>
	/// Local Search algorithm
	/// </summary>
	/// <param name="path">target path</param>
	/// <remarks>iteratively reverses segments in the path to improve the tour length</remarks>
	protected TspResult Local2OptSearch( List<int> path )
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
	/// Reverses the order of nodes between indices i and j in the path
	/// </summary>	
	protected static List<int> TwoOptSwap( List<int> path, int i, int j )
	{
		var copy = new List<int>( path );

		copy.Reverse( i, j - i + 1 );

		return copy;
	}

	/// <summary>
	/// Parallel local 2-opt search for TSP using delta evaluations
	/// </summary>	
	/// <remarks>: tries to improve the tour by removing two edges and reconnecting the nodes</remarks>
	protected TspResult Parallel2OptSearch( List<int> path )
	{
		int[] tourArray = [ .. path ];
		int cities = tourArray.Length;
		double bestTour = this.map.GetTourLength( path );

		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1, bestJ = -1;

			Parallel.For( 0, cities - 2, i => {
				int a = tourArray[ i ];
				int b = tourArray[ i + 1 ];
				double w_ab = this.map[ a, b ].Weight;

				// j must start at i+2, otherwise we are selecting adjacent edges (which share a node)
				for( int j = i + 2; j < cities; j++ )
				{
					// If i == 0 and j == cities - 1, we are reversing the entire path minus the endpoints,
					// which represents the same logical tour. We can skip it.
					if( i == 0 && j == cities - 1 ) continue;

					int c = tourArray[ j ],  d = tourArray[ ( j + 1 ) % cities ];
					double w_cd = this.map[ c, d ].Weight;

					// 2-opt delta: cost of removed edges vs cost of added edges
					double delta = w_ab + w_cd - ( this.map[ a, c ].Weight + this.map[ b, d ].Weight );

					if( delta > MARGIN )
					{
						lock( lockObj )
						{
							if( delta > bestDelta )
							{
								bestDelta = delta;
								bestI = i;
								bestJ = j;
							}
						}
					}
				}
			} );

			if( bestDelta > MARGIN ) // Apply the best move in place
			{				
				ReverseSegment( tourArray, bestI + 1, bestJ );
				bestTour -= bestDelta; // Update tour track naturally
				improved = true;
			}
		}
				
		for( int i = 0; i < cities; i++ ) // Update the original passed list
		{
			path[ i ] = tourArray[ i ];
		}

		return new TspResult( bestTour, path );
	}

	#endregion


	#region Local 2.5-opt Search -----------------------------------------------	

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


	/// <summary>
	/// Parallel local 2.5-opt search for TSP: tries to improve the tour by removing two edges and inserting a node
	/// </summary>
	protected TspResult Parallel2p5OptSearch( List<int> path )
	{
		int[] tourArray = [ .. path ];
		int cities = tourArray.Length;

		double bestTour = this.map.GetTourLength( path );
		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1,  bestJ = -1,  bestK = -1;
			bool isTwoOpt = false;

			// Explore all 2-opt and 2.5-opt moves in parallel
			Parallel.For( 0, cities - 2, i => {
				int a = tourArray[ i ],  b = tourArray[ i + 1 ];
				double w_ab = this.map[ a, b ].Weight;

				for( int j = i + 2; j < cities; j++ )
				{
					int c = tourArray[ j ],  d = tourArray[ ( j + 1 ) % cities ];
					double w_cd = this.map[ c, d ].Weight;

					// 1. Calculate 2-Opt Delta
					double delta2Opt = this.map[ a, c ].Weight + this.map[ b, d ].Weight - w_ab - w_cd;

					if( delta2Opt < -MARGIN )
					{
						lock( lockObj )
						{
							if( delta2Opt < bestDelta )
							{
								bestDelta = delta2Opt;
								bestI = i;	bestJ = j;
								isTwoOpt = true;
							}
						}
					}

					// 2. Calculate 2.5-Opt Delta (Node Shifting/Insertion)
					for( int k = 0; k < cities; k++ )
					{
						if( k == i || k == i + 1 || k == j ) continue;

						int targetNode = tourArray[ k ];

						// Node k is extracted and placed between i and i+1
						// We break edges (k-1, k), (k, k+1) and (i, i+1)
						// We form edges (k-1, k+1), (i, k), and (k, i+1)

						int k_prev = tourArray[ ( k - 1 + cities ) % cities ];
						int k_next = tourArray[ ( k + 1 ) % cities ];

						double delta2p5 =
							// Add new connections
							this.map[ a, targetNode ].Weight + this.map[ targetNode, b ].Weight + this.map[ k_prev, k_next ].Weight
							// Subtract broken connections
							- w_ab - this.map[ k_prev, targetNode ].Weight - this.map[ targetNode, k_next ].Weight;

						if( delta2p5 < -MARGIN )
						{
							lock( lockObj )
							{
								if( delta2p5 < bestDelta )
								{
									bestDelta = delta2p5;
									bestI = i;        // destination insertion index
									bestK = k;        // the node that is moved
									isTwoOpt = false;
								}
							}
						}
					}
				}
			} );
						
			if( bestDelta < -MARGIN ) // Apply the best move
			{
				if( isTwoOpt )				
					ReverseSegment( tourArray, bestI + 1, bestJ );				
				else				
					ShiftNode( tourArray, bestK, bestI );				

				bestTour += bestDelta;
				improved = true;
			}
		}
				
		for( int i = 0; i < cities; i++ ) // Copy back to original reference
		{
			path[ i ] = tourArray[ i ];
		}

		return new TspResult( bestTour, path );
	}

	/// <summary>
	/// Moves the node at index 'source' to the location directly after 'destination'.
	/// Modifies the array in place dynamically sliding the contents left or right.
	/// </summary>
	static void ShiftNode( int[] tour, int source, int destination )
	{
		int node = tour[ source ];

		if( source < destination )
		{
			// Node moves right: shift elements between source+1 and destination to the left
			for( int x = source; x < destination; x++ )
			{
				tour[ x ] = tour[ x + 1 ];
			}

			tour[ destination ] = node;
		}
		else if( source > destination )
		{
			// Node moves left: shift elements between destination+1 and source-1 to the right
			for( int x = source; x > destination + 1; x-- )
			{
				tour[ x ] = tour[ x - 1 ];
			}

			tour[ destination + 1 ] = node;
		}
	}	

	#endregion


	#region Local/Parallel 3-opt Search ----------------------------------------

	/// <summary>
	/// Local 3-opt search for TSP: tries to improve the tour by removing three edges and reconnecting the nodes
	/// </summary>	
	protected TspResult Local3OptSearch( List<int> path )
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

	/// <summary>
	/// Parallel 3-opt search for TSP using delta evaluations.
	/// </summary>
	protected TspResult Parallel3OptSearch( List<int> path )
	{
		var currentPath = new List<int>( path );
		int cities = currentPath.Count;
		double bestTour = this.map.GetTourLength( currentPath );

		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1, bestJ = -1, bestK = -1;
			int bestCase = -1;

			Parallel.For( 0, cities - 2, i => {
				int a = currentPath[ i ];
				int b = currentPath[ i + 1 ];
				double w_ab = this.map[ a, b ].Weight;

				for( int j = i + 1; j < cities - 1; j++ )
				{
					int c = currentPath[ j ];
					int d = currentPath[ j + 1 ];
					double w_cd = this.map[ c, d ].Weight;

					for( int k = j + 1; k < cities; k++ )
					{
						int e = currentPath[ k ];
						int f = currentPath[ ( k + 1 ) % cities ];
						double w_ef = this.map[ e, f ].Weight;

						// Cost of the 3 edges being removed
						double removedCost = w_ab + w_cd + w_ef;

						// Case 1: 2-opt between (a,b) and (c,d) - handled by 2-opt usually, but included here
						double d1 = removedCost - ( this.map[ a, c ].Weight + this.map[ b, d ].Weight + w_ef );
						// Case 2: 2-opt between (c,d) and (e,f)
						double d2 = removedCost - ( w_ab + this.map[ c, e ].Weight + this.map[ d, f ].Weight );
						// Case 3: 2-opt between (a,b) and (e,f)
						double d3 = removedCost - ( this.map[ a, e ].Weight + w_cd + this.map[ b, f ].Weight );
						// Case 4: 3-opt pure (swap segments, no reverse) a-d-e-b-c-f
						double d4 = removedCost - ( this.map[ a, d ].Weight + this.map[ e, b ].Weight + this.map[ c, f ].Weight );
						// Case 5: 3-opt a-d-e-c-b-f
						double d5 = removedCost - ( this.map[ a, d ].Weight + this.map[ e, c ].Weight + this.map[ b, f ].Weight );
						// Case 6: 3-opt a-e-d-b-c-f
						double d6 = removedCost - ( this.map[ a, e ].Weight + this.map[ d, b ].Weight + this.map[ c, f ].Weight );
						// Case 7: 3-opt a-c-b-e-d-f
						double d7 = removedCost - ( this.map[ a, c ].Weight + this.map[ b, e ].Weight + this.map[ d, f ].Weight );

						// Find the max delta among the 7 combinations
						double maxDelta = Math.Max( Math.Max( Math.Max( d1, d2 ), Math.Max( d3, d4 ) ), Math.Max( Math.Max( d5, d6 ), d7 ) );

						if( maxDelta > MARGIN )
						{
							int caseType = 1;
							if( maxDelta == d2 ) caseType = 2;
							else if( maxDelta == d3 ) caseType = 3;
							else if( maxDelta == d4 ) caseType = 4;
							else if( maxDelta == d5 ) caseType = 5;
							else if( maxDelta == d6 ) caseType = 6;
							else if( maxDelta == d7 ) caseType = 7;

							lock( lockObj )
							{
								if( maxDelta > bestDelta )
								{
									bestDelta = maxDelta;
									bestI = i;
									bestJ = j;
									bestK = k;
									bestCase = caseType;
								}
							}
						}
					}
				}
			} );

			if( bestDelta > MARGIN )
			{
				Apply3OptSwap( currentPath, bestI, bestJ, bestK, bestCase );
				bestTour -= bestDelta; // Update tour track naturally
				improved = true;
			}
		}

		// Update the original passed list
		for( int idx = 0; idx < cities; idx++ )
		{
			path[ idx ] = currentPath[ idx ];
		}

		return new TspResult( this.map.GetTourLength( path ), path );
	}

	/// <summary>
	/// Reconnects the path based on the selected 3-opt or 2-opt configuration.
	/// </summary>
	static void Apply3OptSwap( List<int> path, int i, int j, int k, int caseType )
	{
		var copy = new List<int>( path ); // Create a temporary copy to correctly resolve out-of-place index moves

		int n = path.Count;
		int index = 0;

		switch( caseType )
		{
			case 1:
				ReverseSegment( path, i + 1, j );
				break;
			case 2:
				ReverseSegment( path, j + 1, k );
				break;
			case 3:
				ReverseSegment( path, i + 1, k );
				break;
			case 4: // a-d-e-b-c-f (Swap segments without reversing)
				for( int x = 0; x <= i; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = j + 1; x <= k; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = i + 1; x <= j; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = k + 1; x < n; x++ ) path[ index++ ] = copy[ x % n ];
				break;
			case 5: // a-d-e-c-b-f  (Swap segments and reverse second)
				for( int x = 0; x <= i; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = j + 1; x <= k; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = j; x >= i + 1; x-- ) path[ index++ ] = copy[ x % n ];
				for( int x = k + 1; x < n; x++ ) path[ index++ ] = copy[ x % n ];
				break;
			case 6: // a-e-d-b-c-f (Swap segments and reverse first)
				for( int x = 0; x <= i; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = k; x >= j + 1; x-- ) path[ index++ ] = copy[ x % n ];
				for( int x = i + 1; x <= j; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = k + 1; x < n; x++ ) path[ index++ ] = copy[ x % n ];
				break;
			case 7: // a-c-b-e-d-f (Double reverse, no segment swap)
				for( int x = 0; x <= i; x++ ) path[ index++ ] = copy[ x % n ];
				for( int x = j; x >= i + 1; x-- ) path[ index++ ] = copy[ x % n ];
				for( int x = k; x >= j + 1; x-- ) path[ index++ ] = copy[ x % n ];
				for( int x = k + 1; x < n; x++ ) path[ index++ ] = copy[ x % n ];
				break;
		}
	}	

	#endregion


	#region Lin-Kernighan Search -----------------------------------------------

	/// <summary>
	/// Local 2-opt Search for TSP (First Improvement)
	/// Currently named LinKernighanSearch but implements 2-opt.
	/// </summary>
	/// <remarks>iteratively reverses segments in the path to improve the tour length</remarks>	
	public TspResult LinKernighanSearch( List<int> path )
	{
		// 1. Work with a raw array to avoid interface/virtual method dispatch overhead
		int[] tourArray = [ .. path ];
		int cities = tourArray.Length;

		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int i = 0; i < cities - 1; i++ )
			{
				int a = tourArray[ i ];
				int b = tourArray[ i + 1 ];
				double weight_ab = this.map[ a, b ].Weight;

				for( int j = i + 1; j < cities; j++ )
				{
					int c = tourArray[ j ];
					int d = tourArray[ ( j + 1 ) % cities ]; // Modulo only on the outer edge
										
					double delta = this.map[ a, c ].Weight + this.map[ b, d ].Weight - weight_ab - this.map[ c, d ].Weight;

					if( delta < -MARGIN ) // Reverse the segment between i+1 and j
					{
						ReverseSegment( tourArray, i + 1, j );
						improved = true;

						// In a "First Improvement" strategy, you can restart from the inner loop
						// or let the outer loop continue. Breaking out restarts the search.
						// break; // Optional: break here if you want to restart outer loop immediately
					}
				}
			}
		}

		// Optional: Update the original IList if needed, or simply return the new array.
		for( int i = 0; i < cities; i++ )
		{
			path[ i ] = tourArray[ i ];
		}

		return new TspResult( this.map.GetTourLength( path ), path );
	}

	/// <summary>
	/// Calculates the change in tour length if the segment between indices i and j is reversed.
	/// </summary>
	/// <param name="path">The current tour path.</param>
	/// <param name="i">The starting index of the segment to reverse.</param>
	/// <param name="j">The ending index of the segment to reverse.</param>
	/// <returns>The change in tour length if the segment is reversed.</returns>
	double GetDelta( IList<int> path, int i, int j )
	{
		int a = path[ i ], b = path[ ( i + 1 ) % this.Cities ], c = path[ j ], d = path[ ( j + 1 ) % this.Cities ];

		return this.map[ a, c ].Weight + this.map[ b, d ].Weight - this.map[ a, b ].Weight - this.map[ c, d ].Weight;
	}

	/// <summary>
	/// Reverses the order of elements in a specified segment of the array in place.
	/// </summary>
	/// <remarks>Only the elements between <paramref name="start"/> and <paramref name="end"/>, inclusive, are
	/// reversed. The operation modifies the original array.</remarks>
	/// <param name="path">The array whose segment will be reversed. Cannot be null.</param>
	/// <param name="start">The zero-based starting index of the segment to reverse. Must be >= 0 and <= to <paramref name="end"/>.</param>
	/// <param name="end">The zero-based ending index of the segment to reverse. Must be >= to 'start' and < the length of 'path'</param>
	static void ReverseSegment( int[] path, int start, int end )
	{
		while( start < end )
		{
			(path[ start ], path[ end ]) = (path[ end ], path[ start ]);
			start++;
			end--;
		}
	}

	/// <summary>
	/// Reverses the order of elements in a specified segment of the provided list in place.
	/// </summary>
	/// <remarks>Only the elements between <paramref name="start"/> and <paramref name="end"/>, inclusive, are reversed. 
	/// The operation modifies the original list.</remarks>
	/// <param name="path">The list whose segment will be reversed. Cannot be null.</param>
	/// <param name="start">The zero-based index at which the segment to reverse begins. Must be >= to 0 and <= to <paramref name="end"/>.</param>
	/// <param name="end">The zero-based index at which the segment to reverse ends. Must be >= to <paramref name="start"/> and < path.Count</param>
	static void ReverseSegment( IList<int> path, int start, int end )
	{		
		while( start < end ) // Kept for backward compatibility with other methods using IList<int>
		{
			(path[ start ], path[ end ]) = (path[ end ], path[ start ]);
			start++;
			end--;
		}
	}

	/// <summary>
	/// Parallel "Any-Improvement" 2-opt Search for TSP.
	/// Uses parallelization but stops early as soon as any thread finds a valid improvement.
	/// </summary>	
	public TspResult ParallelLinKernighanSearch( List<int> path )
	{
		var tourArray = new List<int>( path );
		int cities = tourArray.Count;
		double bestTour = this.map.GetTourLength( path );

		bool improved = true;
		object lockObj = new();

		while( improved )
		{
			improved = false;

			double bestDelta = 0;
			int bestI = -1, bestJ = -1;

			// We pass 'loopState' so we can signal an early exit
			Parallel.For( 0, cities - 2, ( i, loopState ) => 
			{
				int a = tourArray[ i ];
				int b = tourArray[ i + 1 ];
				double w_ab = this.map[ a, b ].Weight;

				for( int j = i + 2; j < cities; j++ )
				{
					// If another thread already found an improvement, stop evaluating
					if( loopState.IsStopped ) break;

					if( i == 0 && j == cities - 1 ) continue; // Skip full loop reversal

					int c = tourArray[ j ];
					int d = tourArray[ ( j + 1 ) % cities ];
					double w_cd = this.map[ c, d ].Weight;

					// Evaluate delta locally
					double delta = w_ab + w_cd - ( this.map[ a, c ].Weight + this.map[ b, d ].Weight );

					if( delta > MARGIN )
					{
						lock( lockObj )
						{
							// Since multiple threads might hit this lock at the same time before 
							// IsStopped propagates, we ensure we grab the biggest delta found so far
							if( delta > bestDelta )
							{
								bestDelta = delta;
								bestI = i;
								bestJ = j;

								// Signal all other threads to abort their loops
								loopState.Stop();
							}
						}
					}
				}
			} );
			
			if( bestDelta > MARGIN ) // If any thread found an improvement, apply it sequentially and loop again
			{
				ReverseSegment( tourArray, bestI + 1, bestJ );
				bestTour -= bestDelta;
				improved = true;
			}
		}

		for( int i = 0; i < cities; i++ )
		{
			path[ i ] = tourArray[ i ];
		}

		return new TspResult( bestTour, path );
	}	

	#endregion


	#region Swap ---------------------------------------------------------------	

	/// <summary>
	/// Swaps two cities in the path and returns the action and the delta of the tour length after the swap
	/// </summary>
	/// <remarks>Based on https://github.com/Inspiaaa/TSP-Simulated-Annealing/blob/master/Scripts/SimulatedAnnealing.cs</remarks>	
	protected (Action, double) Swap( IList<int> path )
	{
		return Random.Shared.Next( 6 ) switch 
		{							
			0 => GetDeltaAfterSwap( path ), //swap random cities													  
			1 or 2 => GetDeltaAfterTransport( path ), // This operation only works for more than 3 cities																	
			_ => GetDeltaAfterReverse( path ), // Twice as likely as it is more powerful			
		};
	}
	

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

	protected List<int> Swapit( List<int> path )
	{
		return Random.Shared.Next( 4 ) switch {
			0 => RandomSwap( path ),
			1 => Random2OptSwap( path ),
			2 => ThreeOptSwap( path, 0, 1, 2 ), // Example indices, adjust as needed
			_ => OrOptSwap( path )
		};

	}

	#region RandomSwap ---------------------------------------------------------

	/// <summary>
	/// Randomly swaps two cities in the path to create a new path
	/// </summary>
	/// <param name="path">The path to modify</param>
	protected static List<int> RandomSwap( IList<int> path )
	{
		var copy = new List<int>( path );

		var indexes = IRandomSequence.GetUniqueInts( 2, 0, path.Count - 1 );

		int i = indexes[ 0 ];
		int j = indexes[ 1 ];

		(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);

		return copy;
	}

	/// <summary>
	/// Randomly swaps two cities in the TSP result to create a new result
	/// </summary>
	/// <param name="result">The TSP result to modify</param>
	/// <returns>A new TSP result with two cities swapped</returns>
	protected TspResult RandomSwap( TspResult result )
	{
		var copy = new List<int>( result.Path );

		(int i, int j) = IRandomSequence.GetPairInts( 0, result.Path.Count - 1 );

		(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);

		return TspResult.Build( this.map, copy );
	}

	#endregion

	#region TwoOptSwap ---------------------------------------------------------	


	/// <summary>
	/// Randomly selects two indices in the path and reverses the segment between them
	/// </summary>	
	protected static List<int> Random2OptSwap( List<int> path )
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
		List<int> copy = [ .. path ];

		copy.Reverse( i + 1, j - i ); // Reverse the segment between i+1 and j

		copy.Reverse( j + 1, k - j ); // Reverse the segment between j+1 and k

		return copy;
	}

	#endregion


	#region OrOptSwap ----------------------------------------------------------

	/// <summary>
	/// Or-Opt local search for TSP: moves segments of length 1, 2, or 3 to a new position if it improves the tour.
	/// Optimized to avoid O(n²) Except() calls by using direct index manipulation.
	/// </summary>
	/// <param name="path">Initial path to optimize (will be modified in-place)</param>
	/// <param name="maxSegmentLength">Maximum segment length to move (1, 2, or 3)</param>
	/// <returns>Improved TspResult</returns>
	//protected static TspResult OrOptSwap( List<int> path, int maxSegmentLength = 3 )
	//{
	//	int cities = path.Count;
	//	double tour = this.map.GetTourLength( path );

	//	bool improved = true;

	//	while( improved )
	//	{
	//		improved = false;

	//		for( int segmentLength = 1; segmentLength <= maxSegmentLength; segmentLength++ )
	//		{
	//			if( segmentLength >= cities ) continue; // Don't move the whole tour or more

	//			for( int i = 0; i < cities && !improved; i++ )
	//			{
	//				// Extract segment indices (handles wrap-around)
	//				var segmentIndices = new List<int>( segmentLength );
	//				for( int k = 0; k < segmentLength; k++ )
	//				{
	//					segmentIndices.Add( ( i + k ) % cities );
	//				}

	//				// Try all possible insertion positions
	//				for( int insertPos = 0; insertPos < cities; insertPos++ )
	//				{
	//					// Skip if trying to insert back at original position
	//					if( insertPos >= i && insertPos < ( i + segmentLength ) % cities ) continue;

	//					// Build new path: remove segment and insert at new position
	//					var newPath = BuildReorderedPath( path, segmentIndices, insertPos );

	//					double newTour = this.map.GetTourLength( newPath );

	//					if( newTour + MARGIN < tour ) // Found an improvement
	//					{
	//						tour = newTour;
	//						path = newPath;
	//						improved = true;
	//						break; // Restart with new path
	//					}
	//				}
	//			}
	//		}
	//	}

	//	return new TspResult( tour, path );
	//}
	protected List<int> OrOptSwap( List<int> path, int maxSegmentLength = 3 )
	{
		double tour = this.map.GetTourLength( path );

		bool improved = true;

		while( improved )
		{
			improved = false;

			for( int segmentLength = 1; segmentLength <= maxSegmentLength; segmentLength++ )
			{
				if( segmentLength >= this.Cities ) continue; // Don't move the whole tour or more

				for( int i = 0; i < this.Cities && !improved; i++ )
				{
					// Extract segment indices (handles wrap-around)
					var segmentIndices = new List<int>( segmentLength );
					for( int k = 0; k < segmentLength; k++ )
					{
						segmentIndices.Add( ( i + k ) % this.Cities );
					}

					// Try all possible insertion positions
					for( int insertPos = 0; insertPos < this.Cities; insertPos++ )
					{
						// Skip if trying to insert back at original position
						if( insertPos >= i && insertPos < ( i + segmentLength ) % this.Cities ) continue;

						// Build new path: remove segment and insert at new position
						var newPath = BuildReorderedPath( path, segmentIndices, insertPos );

						double newTour = this.map.GetTourLength( newPath );

						if( newTour + MARGIN < tour ) // Found an improvement
						{
							tour = newTour;
							path = newPath;
							improved = true;
							break; // Restart with new path
						}
					}
				}
			}
		}

		return path;//new TspResult( tour, path );
	}

	/// <summary>
	/// Builds a new path by removing segment at specified indices and reinserting at new position.
	/// Avoids inefficient LINQ Except() operation.
	/// </summary>
	static List<int> BuildReorderedPath( List<int> path, List<int> segmentIndices, int insertPos )
	{
		var newPath = new List<int>( path.Count );
		var segmentSet = new HashSet<int>( segmentIndices );
		var segment = new List<int>( segmentIndices.Count );

		int posCounter = 0;
				
		for( int i = 0; i < path.Count; i++ ) // Extract segment values and remaining cities
		{
			if( segmentSet.Contains( i ) )
			{
				segment.Add( path[ i ] );
			}
			else
			{
				if( posCounter == insertPos )	newPath.AddRange( segment );				
				newPath.Add( path[ i ] );
				posCounter++;
			}
		}
				
		if( insertPos >= posCounter ) // Insert segment at end if insertPos >= remaining cities
		{
			newPath.AddRange( segment );
		}

		return newPath;
	}

	#endregion	


	#region Nearest Neighbour --------------------------------------------------

	/// <summary>
	/// Nearest Neighbour helper
	/// </summary>
	protected TspResult BuildNearestTour()
	{
		var best = new TspResult( double.MaxValue, new List<int>( this.Cities ) );

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
	TspResult GetNearest( int start )
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


	#region Selection ----------------------------------------------------------

	/// <summary>
	/// Selects a single individual from the population using roulette wheel (fitness-proportionate) selection.
	/// </summary>
	/// <remarks>
	/// Roulette wheel selection increases the likelihood of selecting individuals with higher Fitness values.</remarks>
	/// <param name="population">
	/// The list of individuals to select from. Each individual's selection probability is proportional to its Fitness value.
	/// </param>
	/// <returns>TspResult representing the selected individual from the population.</returns>
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

	/// <summary>
	/// Selects a single TspResult from the given population using tournament selection with the specified tournament size.
	/// </summary>
	/// <remarks>
	/// This is a common method in genetic algorithms for selecting individuals based on fitness. The method randomly selects 
	/// a subset of the population and returns the individual with the lowest fitness value. The selection is stochastic and may
	/// select the same individual multiple times if the tournament size is less than the population size.
	/// </remarks>
	/// <param name="population">The list of TspResult instances representing the current population from which to select.</param>
	/// <param name="tournamentSize">The number of individuals to randomly select for the tournament.</param>
	/// <returns>TspResult with the best (minimum) fitness value among the randomly selected tournament participants.</returns>
	protected static TspResult TournamentSelection( List<TspResult> population, int tournamentSize )
	{
		var tournament = new List<TspResult>();

		for( int i = 0; i < tournamentSize; i++ )
		{
			tournament.Add( population[ Random.Shared.Next( population.Count ) ] );
		}

		return tournament.MinBy( i => i.Fitness );
	}

	/// <summary>
	/// Selects a single individual from the given population using rank-based selection, where individuals
	/// with higher fitness ranks have a greater probability of being chosen.
	/// </summary>
	/// <remarks>
	/// Assigns selection probabilities according to the relative rank of each individual's fitness, rather than the raw
	/// fitness value. This helps maintain diversity and prevents highly fit individuals from dominating the selection process. 
	/// The method assumes that lower fitness values represent better solutions.
	/// </remarks>
	/// <param name="population">The list of individuals representing the current population.</param>
	/// <returns>A single individual selected from the population based on rank-based probability.</returns>
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
	/// Performs ordered crossover (OX) between two parent TspResult instances to produce a child TspResult.
	/// </summary>
	/// <param name="parent1">The first parent TspResult.</param>
	/// <param name="parent2">The second parent TspResult.</param>
	/// <returns>TspResult representing the child produced from the crossover.</returns>
	protected TspResult Crossover( TspResult parent1, TspResult parent2 )
	{
		int length = parent1.Path.Count;

		(int start, int end) = IRandomSequence.GetPairInts( 0, length - 1 ); // crossover points	

		var child = new List<int>( new int[ length ] );

		Array.Copy( parent1.Path.ToArray(), start, child.ToArray(), start, end - start + 1 ); // Initialize child with parent1's path		

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


	#region Mutation -----------------------------------------------------------

	/// <summary>
	/// Mutates an individual using one of several mutation operators
	/// </summary>
	protected TspResult Mutate( TspResult individual, double rate )
	{
		if( Random.Shared.NextDouble() < rate ) return individual; // No mutation with probability (1 - rate)
																								 
		return Random.Shared.Next( 4 ) switch  // Select random mutation operator for diversity
		{
			0 => RandomSwap( individual ),        // Swap two cities
			1 => InversionMutation( individual ), // Reverse a segment
			2 => InsertionMutation( individual ), // Move a city
			_ => ScrambleMutation( individual )   // Shuffle a segment
		};
	}

	/// <summary>
	/// Inversion mutation: reverse a random segment of the tour
	/// </summary>
	TspResult InversionMutation( TspResult individual )
	{
		var path = new List<int>( individual.Path );
		if( path.Count < 3 ) return individual;

		var (start, end) = IRandomSequence.GetPairInts( 0, path.Count - 1 );
		path.Reverse( start, end - start + 1 );

		return TspResult.Build( this.map, path );
	}

	/// <summary>
	/// Insertion mutation: remove a city and insert it elsewhere
	/// </summary>
	TspResult InsertionMutation( TspResult individual )
	{
		var path = new List<int>( individual.Path );
		if( path.Count < 3 ) return individual;

		int removeIndex = Random.Shared.Next( path.Count );
		int city = path[ removeIndex ];
		path.RemoveAt( removeIndex );

		int insertIndex = Random.Shared.Next( path.Count );
		path.Insert( insertIndex, city );

		return TspResult.Build( this.map, path );
	}

	/// <summary>
	/// Scramble mutation: shuffle a random segment of the tour
	/// </summary>
	TspResult ScrambleMutation( TspResult individual )
	{
		var path = new List<int>( individual.Path );
		if( path.Count < 3 ) return individual;

		var (start, end) = IRandomSequence.GetPairInts( 0, path.Count - 1 );
		int length = end - start + 1;

		// Shuffle the segment
		var segment = path.GetRange( start, length );
		for( int i = 0; i < length; i++ )
		{
			int j = Random.Shared.Next( i, length );
			(segment[ i ], segment[ j ]) = (segment[ j ], segment[ i ]);
		}
				
		for( int i = 0; i < length; i++ ) // Replace the segment
		{
			path[ start + i ] = segment[ i ];
		}

		return TspResult.Build( this.map, path );
	}


	#endregion

}
