using System.Runtime.InteropServices;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Iterated Local Search for TSP
/// </summary>
public class IteratedLocalSearch( TspMap map ) : TspAlgorithmBase( map ) 
{	
	/// <summary>
	/// Configures the algorithm settings
	/// </summary>	
	protected override void Configure()
	{		
		base.settings = ConfigManager.GetSection<IlsSettings>( "ils" ) ?? throw new ArgumentNullException( nameof( settings ) );		
	}

	/// <summary>
	/// Single optimization epoch running a perturbation followed by a local search.
	/// </summary>
	/// <param name="best">The current best solution to use as the starting point for the epoch. Cannot be null.</param>
	/// <returns>A new TspResult instance representing the best solution found during this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var path = Perturbate( best.Path );

		return ParallelLocalSearch( path );		
	}

	/// <summary>
	/// Perturbation using double bridge method.
	/// Recombines path segments as [0..a) + [c..d) + [b..c) + [a..b) + [d..n)
	/// </summary>
	static List<int> Perturbate( List<int> path )
	{
		int n = path.Count;
		if( n < 8 ) return [ .. path ]; // Double bridge needs at least 8 cities to be meaningful

		var cuts = IRandomSequence.GetUniqueInts( 4, 1, n - 1 );

		// Sort 4 cut points inline (faster than LINQ for small sets)
		int a = cuts[ 0 ], b = cuts[ 1 ], c = cuts[ 2 ], d = cuts[ 3 ];

		// Simple insertion sort for 4 elements
		if( a > b ) (a, b) = (b, a);
		if( c > d ) (c, d) = (d, c);
		if( b > c ) (b, c) = (c, b);
		if( a > b ) (a, b) = (b, a);
		if( c > d ) (c, d) = (d, c);

		var result = new List<int>( n );

		// Pre-allocate and use CollectionsMarshal for zero-copy segment operations
		CollectionsMarshal.SetCount( result, n );
		Span<int> dst = CollectionsMarshal.AsSpan( result );
		ReadOnlySpan<int> src = CollectionsMarshal.AsSpan( path );

		// Double bridge: [0..a) + [c..d) + [b..c) + [a..b) + [d..n)
		src[ ..a ].CopyTo( dst );
		src[ c..d ].CopyTo( dst[ a.. ] );
		src[ b..c ].CopyTo( dst[ ( a + d - c ).. ] );
		src[ a..b ].CopyTo( dst[ ( a + d - b ).. ] );
		src[ d.. ].CopyTo( dst[ d.. ] );

		return result;
	}
}

/// <summary>
/// Configuration Settings
/// </summary>
public class IlsSettings : TspConfigurationBase
{ }

