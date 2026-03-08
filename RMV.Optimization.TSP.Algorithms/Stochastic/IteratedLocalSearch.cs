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
	/// Perturbation using double bridge method
	/// </summary>
	static List<int> Perturbate( IList<int> path )
	{
		int n = path.Count;
		if( n < 8 ) return [ .. path ]; // Double bridge needs at least 8 cities to be meaningful

		var cuts = IRandomSequence.GetUniqueInts( 4, 1, n - 1 ); // Select 4 unique, sorted cut points in [1, n-1)	
		int a = cuts[ 0 ], b = cuts[ 1 ], c = cuts[ 2 ], d = cuts[ 3 ];

		var result = new List<int>( n );

		// Fast path for List<int> utilizing memory block copying
		if( path is List<int> list )
		{
			CollectionsMarshal.SetCount( result, n );
			Span<int> dst = CollectionsMarshal.AsSpan( result );
			ReadOnlySpan<int> src = CollectionsMarshal.AsSpan( list );

			src[ ..a ].CopyTo( dst );
			src[ c..d ].CopyTo( dst[ a.. ] );
			src[ b..c ].CopyTo( dst[ ( a + d - c ).. ] );
			src[ a..b ].CopyTo( dst[ ( a + d - b ).. ] );
			src[ d.. ].CopyTo( dst[ d.. ] );
		}
		else
		{
			// Fallback for generic IList
			for( int i = 0; i < a; i++ ) result.Add( path[ i ] );
			for( int i = c; i < d; i++ ) result.Add( path[ i ] );
			for( int i = b; i < c; i++ ) result.Add( path[ i ] );
			for( int i = a; i < b; i++ ) result.Add( path[ i ] );
			for( int i = d; i < n; i++ ) result.Add( path[ i ] );
		}

		return result;
	}
	//static List<int> Perturbate( IList<int> path )
	//{
	//	int n = path.Count;
	//	if( n < 8 ) return [ .. path ];// Double bridge needs at least 8 cities to be meaningful

	//	var cuts = IRandomSequence.GetUniqueInts( 4, 1, n - 1 ); // Select 4 unique, sorted cut points in [1, n-1)	
	//	int a = cuts[ 0 ], b = cuts[ 1 ], c = cuts[ 2 ], d = cuts[ 3 ];

	//	// Double bridge recombination: [0..a) + [c..d) + [b..c) + [a..b) + [d..n)
	//	var result = new List<int>( n );
	//	//result.AddRange( path.Take( a ) ); // [0..a)
	//	//result.AddRange( path.Skip( c ).Take( d - c ) );
	//	//result.AddRange( path.Skip( b ).Take( c - b ) );
	//	//result.AddRange( path.Skip( a ).Take( b - a ) );
	//	//result.AddRange( path.Skip( d ) );
	//	for( int i = 0; i < a; i++ ) result.Add( path[ i ] );
	//	for( int i = c; i < d; i++ ) result.Add( path[ i ] );
	//	for( int i = b; i < c; i++ ) result.Add( path[ i ] );
	//	for( int i = a; i < b; i++ ) result.Add( path[ i ] );
	//	for( int i = d; i < n; i++ ) result.Add( path[ i ] );

	//	return result;
	//}	
}

/// <summary>
/// Configuration Settings
/// </summary>
public class IlsSettings : TspConfigurationBase
{ }

