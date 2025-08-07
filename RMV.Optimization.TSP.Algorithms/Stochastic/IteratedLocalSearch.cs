using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Iterated Local Search for TSP
/// </summary>
public class IteratedLocalSearch( TspMap map ) : TspAlgorithmBase( map ) //, ITspAsync
{	

	protected override void Configure()
	{		
		base.settings = ConfigManager.GetSection<IlsSettings>( "ils" ) ?? throw new ArgumentNullException( nameof( settings ) );		
	}

	#region obsolete
	/// <summary>
	/// ILS async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	int count = 0;
	//	int noChanges = 0;

	//	base.timer.Start();

	//	var best = base.BuildRandomTour();

	//	await Task.Run( () => 
	//	{
	//		while( noChanges++ < settings.Limit )
	//		{
	//			var path = Perturbate( [ .. best.Path ] ); //RandomSwap( best.Path ); //

	//			var result = Local2OptSearch( path );	//	Local3OptSearch( path );		

	//			if( result < best )
	//			{
	//				best = result.Clone();

	//				noChanges = 0;

	//				base.Draw( best.Tour, count, best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );				
	//		}

	//		base.Draw( best.Tour, ++count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}
	#endregion

	protected override TspResult? Initialize() => base.InitializeTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var path = Perturbate( best.Path );

		switch( Random.Shared.Next( 2 ) )
		{
			case 0:  
			case 1: 
			case 2: return Parallel2OptSearch( path ); 

			case 3: 
			case 4: return Parallel2p5OptSearch( path );
			case 5: return Parallel3OptSearch( path );
			default: throw new InvalidOperationException( "Invalid random choice" );
		}		
	}

	/// <summary>
	/// Perturbation using double bridge method
	/// </summary>
	static List<int> Perturbate( IList<int> path )
	{
		int n = path.Count;
		if( n < 8 ) return [ .. path ];// Double bridge needs at least 8 cities to be meaningful
					
		var cuts = IRandomSequence.GetUniqueInts( 4, 1, n - 1 ); // Select 4 unique, sorted cut points in [1, n-1)	
		int a = cuts[ 0 ], b = cuts[ 1 ], c = cuts[ 2 ], d = cuts[ 3 ];

		// Double bridge recombination: [0..a) + [c..d) + [b..c) + [a..b) + [d..n)
		var result = new List<int>( n );
		//result.AddRange( path.Take( a ) ); // [0..a)
		//result.AddRange( path.Skip( c ).Take( d - c ) );
		//result.AddRange( path.Skip( b ).Take( c - b ) );
		//result.AddRange( path.Skip( a ).Take( b - a ) );
		//result.AddRange( path.Skip( d ) );
		for( int i = 0; i < a; i++ ) result.Add( path[ i ] );
		for( int i = c; i < d; i++ ) result.Add( path[ i ] );
		for( int i = b; i < c; i++ ) result.Add( path[ i ] );
		for( int i = a; i < b; i++ ) result.Add( path[ i ] );
		for( int i = d; i < n; i++ ) result.Add( path[ i ] );

		return result;
	}	
	

	#region obsolete
	//static List<int> Perturbate( int[] path )
	//{
	//	int n = path.Length;

	//	int pos1 = GetRandom( n );
	//	int pos2 = pos1 + GetRandom( n );
	//	int pos3 = pos2 + GetRandom( n );

	//	return [ .. path[ 0..pos1 ], .. path[ pos3..n ], .. path[ pos2..pos3 ], .. path[ pos1..pos2 ] ];
	//}
	//static int GetRandom( int n ) => 1 + Random.Shared.Next( n / 4 );
	//List<int> Perturbate( IList<int> path )
	//{		
	//	var copy = new List<int>( path );
	//	int i = Random.Shared.Next( this.Cities );
	//	int j = Random.Shared.Next( this.Cities );
	//	while( i == j ) j = Random.Shared.Next( this.Cities );		
	//	(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);
	//	return copy;
	//}

	//TspResult LocalSearch( int[] path )
	//{
	//	double oldTour= base.map.GetTour( path );
	//	double newTour = 0;

	//	List<int> newPath;
	//	int count = 0;

	//	while( true )
	//	{
	//		newPath = RandomTwoOpt( path );
	//		newTour = base.map.GetTour( [ .. newPath ] );

	//		count = newTour < oldTour ? 0 : count + 1;			

	//		if( count > settings.Limit ) break;			
	//	}

	//	return new TspResult { Tour = newTour, Path = newPath };
	//}

	//static List<int> RandomTwoOpt( int[] path )
	//{
	//	int n = path.Length;

	//	var newPath = new List<int>( path );

	//	int c1 = Random.Shared.Next( n );
	//	int c2 = Random.Shared.Next( n );

	//	List<int> exclude = [ c1 ];
	//	exclude.Add( c1 == 0 ? path[ ^1 ] : c1 - 1 );
	//	exclude.Add( c1 == n - 1 ? 0 : c1 + 1 );

	//	while( exclude.Contains( c2 ) )  c2 = Random.Shared.Next( n );

	//	if( c2 < c1 ) (c1, c2) = ( c2, c1 );

	//	newPath[ c1..c2 ].Reverse();	

	//	return newPath;
	//}


	//TspResult TwoOptSwap( IEnumerable<int> path, int i, int j )
	//{
	//	var newPath = new List<int>( path );

	//	newPath.Reverse( i, j - i + 1 );

	//	var result = new TspResult { Path = [ .. newPath ] };
	//	result.UpdateTour( base.map );

	//	return result;
	//}

	//TspResult LocalSearch( TspResult result )
	//{
	//	bool improved = true;

	//	while( improved )
	//	{
	//		improved = false;

	//		for( int i = 0; i < result.Path.Count - 1; i++ )
	//		{
	//			for( int j = i + 1; j < result.Path.Count; j++ )
	//			{
	//				var tmp = TwoOptSwap( result.Path, i, j );

	//				if( tmp < result )
	//				{
	//					result = ( TspResult )tmp.Clone();
	//					improved = true;
	//				}
	//			}
	//		}
	//	}

	//	return result;
	//}

	//TspResult TwoOptSwap( IEnumerable<int> path, int i, int j )
	//{
	//	var newPath = new List<int>( path );

	//	newPath.Reverse( i, j - i + 1 );

	//	var result = new TspResult { Path = [ .. newPath ] };
	//	result.UpdateTour( base.map );

	//	return result;
	//}

	//TspResult Perturb( TspResult path )
	//{
	//	int n = path.Path.Count;

	//	int i = Random.Shared.Next( n );
	//	int j = Random.Shared.Next( n );

	//	var copy = path.Clone() as TspResult;

	//	(copy.Path[ i ], copy.Path[ j ]) = (copy.Path[ j ], copy.Path[ i ]);
	//	copy.UpdateTour( base.map );

	//	return copy;
	//}
	//static List<int> Perturbate( IList<int> path )
	//{
	//	int n = path.Count;

	//	int i = Random.Shared.Next( n );
	//	int j = Random.Shared.Next( n );

	//	var copy = new List<int>( path );		

	//	(copy[ i ], copy[ j ]) = (copy[ j ], copy[ i ]);		

	//	return copy;
	//}
	#endregion
}

/// <summary>
/// Configuration Settings
/// </summary>
public class IlsSettings : TspConfigurationBase
{ }

