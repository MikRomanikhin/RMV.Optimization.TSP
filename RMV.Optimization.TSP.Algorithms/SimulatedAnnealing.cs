using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Simulated Annealing algorithm for TSP
/// </summary>
public class SimulatedAnnealing( TspMap map ) : TspAlgorithmBase( map ), ITspAsync
{
	AnnealingSettings settings;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<AnnealingSettings>("annealing");		
	}

	/// <summary>
	/// SA async wrapper
	/// </summary>	
	public async Task<TspResult> RunAsync(CancellationToken token )
	{
		base.timer.Start();

		int count = 0;
		int noChanges = 0;

		var best = base.BuildNearestTour();//base.BuildRandomTour();
		
		var result = best.Clone();

		await Task.Run( () => 
		{
			while( noChanges++ < settings.Limit )
			{
				GetAnnealing( result );				

				if( result < best )
				{
					best = result.Clone();

					noChanges = 0;

					base.Draw( best.Tour, count, best.Path );
				}			

				if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
			}

			base.Draw( best.Tour, ++count, best.Path );			
		} );		

		base.timer.Stop();

		return best;
	}

	/// <summary>
	/// Simulated Annealing
	/// </summary>	
	void GetAnnealing( TspResult result )
	{		
		(Action accept, double delta) = base.Swap( result.Path );

		if( delta < 0 || Math.Exp( -delta / settings.Temperature ) > Random.Shared.NextDouble() )
		{
			result.Tour += delta;
			accept!();
		}

		settings.Temperature *= settings.Decay;
	}

	#region obsolete
	//void GetAnnealing( TspResult result )
	//{
	//	Action? acceptSolution = null;
	//	double distanceChange = 0;		

	//	switch( Random.Shared.Next( 6 ) )
	//	{
	//		case 0: //swap random cities
	//			int swapIndexA = base.map.RandomIndex();
	//			int swapIndexB = base.map.RandomIndex();

	//			acceptSolution = () => base.map.SwapCities( result.Path, swapIndexA, swapIndexB );
	//			distanceChange = GetDeltaAfterSwap( result.Path, swapIndexA, swapIndexB );
	//			break;

	//		case 1:  // This operation only works for more than 3 cities
	//		case 2:
	//			int startIndex = RandomIndex();
	//			int count = Random.Shared.Next( 1, this.Cities / 4 );

	//			// Note: count+distance must be LESS than the number of cities for the GetDistanceDeltaAfterTransport
	//			int distance = Random.Shared.Next( 1, this.Cities - count );

	//			acceptSolution = () => TransportRange( result.Path, startIndex, count, distance );
	//			distanceChange = GetDeltaAfterTransport( result.Path, startIndex, count, distance );
	//			break;

	//		case 3: // Twice as likely as it is more powerful.
	//		case 4:
	//		case 5:
	//			int reverseStartIndex = RandomIndex();
	//			int reverseCount = Random.Shared.Next( 1, this.Cities / 2 );

	//			acceptSolution = () => ReverseRange( result.Path, reverseStartIndex, reverseCount );
	//			distanceChange = GetDeltaAfterReverse( result.Path, reverseStartIndex, reverseCount );
	//			break;
	//	}

	//	if( distanceChange < 0 || Math.Exp( -distanceChange / settings.Temperature ) > Random.Shared.NextDouble() )
	//	{
	//		result.Tour += distanceChange;
	//		acceptSolution!();
	//	}

	//	settings.Temperature *= settings.Decay;		
	//}

	//int RandomIndex() => Random.Shared.Next( this.Cities );
	//int WrapIndex( int index ) => ( ( index % this.Cities ) + this.Cities ) % this.Cities;

	//static void SwapCities( IList<int> path, int i, int j ) => (path[ i ], path[ j ]) = (path[ j ], path[ i ]);

	//double GetDeltaAfterSwap( IList<int> path, int indexA, int indexB )
	//{
	//	int indexBeforeA = WrapIndex( indexA - 1 );
	//	int posBeforeA = path[ indexBeforeA ];
	//	int posA = path[ indexA ];
	//	int indexAfterA = WrapIndex( indexA + 1 );
	//	int posAfterA = path[ indexAfterA ];

	//	int indexBeforeB = WrapIndex( indexB - 1 );
	//	int posBeforeB = path[ indexBeforeB ];
	//	int posB = path[ indexB ];
	//	int indexAfterB = WrapIndex( indexB + 1 );
	//	int posAfterB = path[ indexAfterB ];

	//	double delta = -this.map[ posBeforeA, posA ].Weight - this.map[ posA, posAfterA ].Weight -
	//		this.map[ posBeforeB, posB ].Weight - this.map[ posB, posAfterB ].Weight;

	//	// Positions of predecessors/successors may change due to the swap
	//	posBeforeA = indexBeforeA == indexB ? posA : posBeforeA;
	//	posAfterA = indexAfterA == indexB ? posA : posAfterA;

	//	posBeforeB = indexBeforeB == indexA ? posB : posBeforeB;
	//	posAfterB = indexAfterB == indexA ? posB : posAfterB;

	//	delta += this.map[ posBeforeA, posB ].Weight + this.map[ posB, posAfterA ].Weight +
	//		this.map[ posBeforeB, posA ].Weight + this.map[ posA, posAfterB ].Weight;

	//	return delta;
	//}

	//void TransportRange( IList<int> cities, int startIndex, int count, int distance )
	//{
	//	var citiesToMove = new int[ count ];

	//	for( int i = 0; i < count; i++ )
	//	{
	//		citiesToMove[ i ] = cities[ WrapIndex( startIndex + i ) ];
	//	}

	//	for( int i = 0; i < distance; i++ )// Move the right segment to the left
	//	{
	//		cities[ WrapIndex( startIndex + i ) ] = cities[ WrapIndex( startIndex + i + count ) ];
	//	}

	//	for( int i = 0; i < count; i++ ) // Move the previous left segment to the right.
	//	{
	//		cities[ WrapIndex( startIndex + distance + i ) ] = citiesToMove[ i ];
	//	}
	//}

	//double GetDeltaAfterTransport( IList<int> cities, int startIndex, int count, int distance )
	//{
	//	int leftSegmentStartIndex = startIndex;
	//	int leftSegmentEndIndex = WrapIndex( startIndex + count - 1 );
	//	int indexBeforeLeftSegment = WrapIndex( startIndex - 1 );

	//	int posBeforeLeftSegment = cities[ indexBeforeLeftSegment ];
	//	int leftSegmentStart = cities[ leftSegmentStartIndex ];
	//	int leftSegmentEnd = cities[ leftSegmentEndIndex ];

	//	int rightSegmentStartIndex = WrapIndex( leftSegmentEndIndex + 1 );
	//	int rightSegmentEndIndex = WrapIndex( rightSegmentStartIndex + distance - 1 );
	//	int indexAfterRightSegment = WrapIndex( rightSegmentEndIndex + 1 );

	//	int rightSegmentStart = cities[ rightSegmentStartIndex ];
	//	int rightSegmentEnd = cities[ rightSegmentEndIndex ];
	//	int posAfterRightSegment = cities[ indexAfterRightSegment ];

	//	double delta = -this.map[ posBeforeLeftSegment, leftSegmentStart ].Weight - this.map[ leftSegmentEnd, rightSegmentStart ].Weight -
	//						 this.map[ rightSegmentEnd, posAfterRightSegment ].Weight;

	//	delta += this.map[ posBeforeLeftSegment, rightSegmentStart ].Weight + this.map[ rightSegmentEnd, leftSegmentStart ].Weight +
	//				this.map[ leftSegmentEnd, posAfterRightSegment ].Weight;

	//	return delta;
	//}

	//void ReverseRange( IList<int> cities, int startIndex, int count )
	//{
	//	for( int i = 0; i <= count / 2; i++ )
	//	{
	//		int left = WrapIndex( startIndex + i );
	//		int right = WrapIndex( startIndex + count - i );

	//		SwapCities( cities, left, right );
	//	}
	//}

	//double GetDeltaAfterReverse( IList<int> cities, int startIndex, int count )
	//{
	//	int endIndex = WrapIndex( startIndex + count );

	//	var beforeStart = cities[ WrapIndex( startIndex - 1 ) ];
	//	var startPosition = cities[ startIndex ];
	//	var endPosition = cities[ endIndex ];
	//	var afterEnd = cities[ WrapIndex( endIndex + 1 ) ];

	//	// When reversing a range of cities, the distances between the individual cities remain the same.
	//	// The only thing that changes are the distances between the start and end positions to their predecessor and successor, respectively.

	//	return -this.map[ beforeStart, startPosition ].Weight - this.map[ endPosition, afterEnd ].Weight +
	//			  this.map[ beforeStart, endPosition ].Weight + this.map[ startPosition, afterEnd ].Weight;
	//}
	#endregion
}

/// <summary>
/// Configuration Settings
/// </summary>
class AnnealingSettings : TspConfigurationBase
{
	public double Decay { get; set; }
	public double Temperature { get; set; }	
}

