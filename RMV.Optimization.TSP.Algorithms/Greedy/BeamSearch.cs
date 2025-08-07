using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Beam Search algorithm for TSP
/// </summary>
public class BeamSearch( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	BeamSettings settings;	

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "beam" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}

	protected override TspResult? Initialize() => base.InitializeTour();

	protected override TspResult RunEpoch( TspResult best )
	{
		var current = best.Clone(); // start with the best so far solution

		for( int city = 0; city < base.Cities; city++ )
		{
			var result = RunBeamSearch( city );

			if( result < current ) current = result;		
		}

		return current; // return the best found solution in this epoch
	}


	/// <summary>
	/// Beam Search algorithm 
	/// </summary>
	TspResult RunBeamSearch( int start )
	{
		var beam = new List<TspResult> { new( 0, [ start ] ) };

		for( int step = 0; step < base.Cities - 1; step++ )
		{			
			beam = PopulateBeam( beam );	// Select the best candidates for the next beam			
		}

		// Complete the tour by returning to the start city
		beam.ForEach( s => s.Tour += map[ s.Path[ ^1 ], start ].Weight );

		return beam.MinBy( s => s.Tour );
	}

	List<TspResult> PopulateBeam( List<TspResult> beam )
	{
		var nextBeam = CreateBeam( beam );

		for( int i = 0; i < beam.Count - 2; i++ ) // Hybridization: combine top solutions to create new candidates
		{
			for( int j = i + 2; j < beam.Count; j++ )
			{				
				var result = base.Crossover( beam[ i ], beam[ j ] );
				nextBeam.Add( result );
			}
		}

		return nextBeam.OrderBy( s => s.Tour ).Take( settings.Size ).ToList();
	}

	List<TspResult> CreateBeam( List<TspResult> beam )
	{
		var nextBeam = new List<TspResult>();

		foreach( var state in beam ) // Expand current beam
		{
			var available = Enumerable.Range( 0, base.Cities ).Except( state.Path );

			foreach( int city in available )
			{
				var newPath = new List<int>( state.Path ) { city };
				double newCost = map[ state.Path[ ^1 ], city ].Weight + state.Tour;

				nextBeam.Add( new TspResult( newCost, newPath ) );
			}
		}

		return nextBeam;
	}

	#region obsolete
	///<summary>
	/// Combines two TSP paths into a new candidate using order-based crossover
	///</summary>
	//static List<int> CombineSolutions( IList<int> parent1, IList<int> parent2 )
	//{
	//	int length = parent1.Count;

	//	// Randomly select crossover points
	//	var range = IRandomSequence.GetUniqueInts( 2, 0, length - 1 );
	//	int start = range[ 0 ];//Random.Shared.Next( 0, length );
	//	int end = range[ 1 ];//Random.Shared.Next( start, length );

	//	var child = new int[ length ];
	//	var visited = new HashSet<int>();

	//	for( int i = start; i <= end; i++ ) // Copy the segment from parent1
	//	{
	//		child[ i ] = parent1[ i ];
	//		visited.Add( parent1[ i ] );
	//	}		

	//	int index = ( end + 1 ) % length;

	//	for( int i = 0; i < length; i++ ) // Fill the remaining positions with genes from parent2 in order
	//	{
	//		int gene = parent2[ ( end + 1 + i ) % length ];

	//		if( !visited.Contains( gene ) )
	//		{
	//			child[ index ] = gene;
	//			visited.Add( gene );
	//			index = ( index + 1 ) % length;
	//		}
	//	}

	//	return [ .. child ];
	//}


	/// <summary>
	/// Combines two TSP paths into a new candidate using order-based crossover.
	/// Ensures the result is a valid permutation of all cities.
	/// </summary>
	//static List<int> CombineSolutions( IList<int> parent1, IList<int> parent2 )
	//{
	//	int n = parent1.Count;

	//	var child = new List<int>( n );
	//	var visited = new HashSet<int>();

	//	// Copy the first half from parent1
	//	int half = n / 2;
	//	for( int i = 0; i < half; i++ )
	//	{
	//		child.Add( parent1[ i ] );
	//		visited.Add( parent1[ i ] );
	//	}

	//	// Fill the rest from parent2 in order, skipping duplicates
	//	for( int i = 0; i < n; i++ )
	//	{
	//		int city = parent2[ i ];
	//		if( !visited.Contains( city ) )
	//		{
	//			child.Add( city );
	//			visited.Add( city );
	//		}
	//	}

	//	return child;
	//}
	//TspResult RunBeamSearch( int start )
	//{
	//	var beam = new List<TspResult> { new( 0, [ start ] ) };

	//	for( int step = 0; step < base.Cities - 1; step++ )
	//	{
	//		beam = beam.SelectMany( state => Enumerable.Range( 0, base.Cities ).Except( state.Path )
	//					  .Select( city => new TspResult( state.Tour + map[ state.Path[ ^1 ], city ].Weight, [.. state.Path,  city] ) ) )
	//					  .OrderBy( s => s.Tour ).Take( settings.Size ).ToList();
	//	}

	//	beam.ForEach( s => s.Tour += base.map[ s.Path[ ^1 ], start ].Weight );

	//	return beam.MinBy( s => s.Tour );
	//}
	/// <summary>
	/// Beam Search async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	var best = new TspResult( double.MaxValue, new int[ base.Cities ] );

	//	base.timer.Start();

	//	await Task.Run( () => 
	//	{
	//		int count = 0;

	//		for( int city = 0; city < base.Cities; city++ )
	//		{
	//			var result = RunBeamSearch( city );

	//			if( result < best ) //tour length check
	//			{
	//				best = result;

	//				base.Draw( best.Tour, ++count, best.Path );
	//			}
	//		}
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}
	//TspResult RunBeamSearch( int start )
	//{
	//	List<TspResult> beam = [ new( 0, [ start ] ) ];

	//	for( int step = 0; step < base.Cities - 1; step++ )
	//	{
	//		List<TspResult> nextBeam = [];

	//		foreach( var state in beam )
	//		{
	//			var available = Enumerable.Range( 0, base.Cities ).Except( state.Path );

	//			foreach( int city in available )
	//			{
	//				var newPath = new List<int>( state.Path ) { city };

	//				double newCost = state.Tour + this.Map[ state.Path[ ^1 ], city ].Weight;

	//				nextBeam.Add( new TspResult( newCost, newPath ) );
	//			}
	//		}

	//		// Keep only the top `beamWidth` states with the lowest cost
	//		beam = nextBeam.OrderBy( s => s.Tour ).Take( settings.Size ).ToList();
	//	}

	//	beam.ForEach( s => s.Tour += this.Map[ s.Path[ ^1 ], start ].Weight ); // Complete the tour		

	//	return beam.MinBy( s => s.Tour ); // Return the best solution in the beam
	//}	

	/// <summary>
	/// N-Nearest Neighbor TSP: At each step, keep N nearest candidates and branch.
	/// </summary>
	/// <param name="start">Starting city index</param>
	/// <param name="N">Number of nearest neighbors to consider at each step</param>
	//protected TspResult RunBeamSearch( int start )
	//{
	//	// Each state is a partial path and its cost
	//	var beam = new List<TspResult> { new( 0, [start] ) };

	//	for( int step = 0; step < this.Cities - 1; step++ )
	//	{
	//		var nextBeam = new List<TspResult>();

	//		foreach( var state in beam ) // Find N nearest unvisited cities from the current city
	//		{											
	//			var unvisited = Enumerable.Range( 0, this.Cities ).Except( state.Path );

	//			var nearest = unvisited.Select( city => new { city, weight = this.Map[ state.Path[ ^1 ], city ].Weight } )
	//				 .OrderBy( x => x.weight ).Take( settings.Size );

	//			foreach( var candidate in nearest )
	//			{
	//				var newPath = new List<int>( state.Path ) { candidate.city };

	//				double newCost = state.Tour + candidate.weight;

	//				nextBeam.Add( new TspResult( newCost, newPath ) );
	//			}
	//		}

	//		// Optionally, keep only the best N states to limit growth (beam width)
	//		beam = nextBeam.OrderBy( s => s.Tour ).Take( settings.Size ).ToList();
	//	}

	//	return beam.Select( b => new TspResult( b.Tour + this.Map[ b.Path[ ^1 ], b.Path[ 0 ] ].Weight, b.Path ) ).MinBy( b => b.Tour );		
	//}
	#endregion

}

/// <summary>
/// Configuration Settings
/// </summary>
public class BeamSettings : TspConfigurationBase
{
	public int Size { get; set; }
}
