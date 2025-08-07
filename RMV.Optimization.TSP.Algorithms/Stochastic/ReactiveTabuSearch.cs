using Microsoft.Extensions.Configuration;

using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Taboo Search for TSP
/// </summary>
public class ReactiveTabuSearch( TspMap map ) : TspAlgorithmBase( map )
{
	TabuSettings settings;
	TspResult current;

	//List<TspResult> population = [];
	List<TabuEntry> tabuList = [];
	List<VisitedEntry> visitedList = [];

	int tabuTenure;
	int prohibPeriod = 1;
	int avgSize = 1;
	int lastChange = 0;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<TabuSettings>( "taboo" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );

		this.tabuTenure = settings.TabuTenure;
	}

	protected override TspResult Initialize()
	{
		this.current = base.InitializeTour();
		//population = [ current.Clone() ];			

		return current;
	}

	protected override TspResult RunEpoch( TspResult best )
	{
		var candidateEntry = GetCandidateEntry( visitedList, current.Path );

		if( candidateEntry != null ) //update existing entry
		{
			int repetitionInterval = count - candidateEntry.Iteration;

			candidateEntry.Iteration = count;
			candidateEntry.Visits++;

			if( repetitionInterval < 2 * ( this.Cities - 1 ) )
			{
				avgSize = ( int )Math.Round( 0.1 * ( count - candidateEntry.Iteration ) + 0.9 * avgSize );

				prohibPeriod = ( int )( prohibPeriod * settings.IncreaseFactor );

				lastChange = count;
			}
		}

		if( count - lastChange > avgSize ) //Reduce the prohib period if no changes for a while
		{
			prohibPeriod = Math.Max( ( int )( prohibPeriod * settings.DecreaseFactor ), 1 );

			lastChange = count;
		}

		List<TabooResult> candidates = BuildCandidates( current );

		var (tabu, admis) = SortNeighborhood( candidates, tabuList, count - prohibPeriod );

		if( admis.Count < 2 )
		{
			prohibPeriod = this.Cities - 2;
			lastChange = count;
		}

		var result = admis.Any() ? admis.First() : tabu.First();

		(current, var bestMoveEdges) = result.Split();

		if( tabu.Any() )
		{
			var first = tabu.First();

			if( first < best && first < current ) (current, bestMoveEdges) = first.Split();
		}

		bestMoveEdges.ForEach( edge => UpdateTabu( tabuList, edge, count ) );

		return candidates.First();
	}


	/// <summary>
	/// Sort the neighborhood of candidates into tabu and admissible solutions based on the tabu list and delta value
	/// </summary>	
	static (List<TabooResult>, List<TabooResult>) SortNeighborhood( List<TabooResult> candidates, List<TabuEntry> tabuList, int delta )
	{
		var tabu = new List<TabooResult>();
		var admiss = new List<TabooResult>();

		foreach( var cand in candidates )
		{			
			if( IsTabu( cand.Edges[ 0 ], tabuList, delta ) || IsTabu( cand.Edges[ 1 ], tabuList, delta ) )
				tabu.Add( cand );
			else
				admiss.Add( cand );
		}

		return (tabu, admiss);
	}

	/// <summary>
	/// Checks if the given edge is tabu based on the tabu list and delta value
	/// </summary>	
	static bool IsTabu( TspEdge edge, List<TabuEntry> tabuList, int delta ) =>	tabuList.Any( e => e.Edge == edge && e.ProhibPeriod >= delta );


	/// <summary>
	/// Creates or updates tabu entry for the given edge
	/// </summary>	
	static void UpdateTabu( List<TabuEntry> tabuList, TspEdge edge, int iteration )
	{
		var entry = tabuList.FirstOrDefault( e => e.Edge.Equals( edge ) );

		if( entry == null )					
			tabuList.Add( new TabuEntry( edge, iteration ) );							
		else 
			entry.Iteration = iteration;
	}
	

	/// <summary>
	/// Generates a list of candidate solutions based on the best solution found so far
	/// </summary>	
	List<TabooResult> BuildCandidates( TspResult current ) => 
		Enumerable.Range( 0, this.settings.CandidateSize ).Select( _ => BuildCandidate( current ) ).OrderBy( c => c.Tour ).ToList();
	

	/// <summary>
	/// Generates a single candidate solution
	/// </summary>	
	TabooResult BuildCandidate( TspResult best )
	{
		var (path, edges) = StochasticTwoOpt( best.Path );		

		return new TabooResult( base.map.GetTourLength( path ), path, edges );
	}

	/// <summary>
	/// Generates a candidate solution using stochastic 2-opt
	/// </summary>
	(List<int>, List<TspEdge>) StochasticTwoOpt( IList<int> path )
	{
		int count = path.Count;

		int c1 = Random.Shared.Next( count - 1 );    // [0, n-2]
		int c2 = Random.Shared.Next( c1 + 1, count ); // [i+1, n-1]

		var perm = new List<int>( path );		

		perm.Reverse( c1, c2 - c1 + 1 ); //

		int prevC1 = c1 == 0 ? count - 1 : c1 - 1;
		int prevC2 = c2 - 1;

		return (perm, [ base.map[ path[ prevC1 ], path[ c1 ] ], base.map[ path[ prevC2 ], path[ c2 ] ] ]);
	}
		

	/// <summary>
	/// Gets a candidate entry from the visited list based on the current path
	/// </summary>	
	VisitedEntry? GetCandidateEntry( List<VisitedEntry> visitedList, IList<int> path )
	{
		var edgeList = ToEdgeList( path );

		if( !visitedList.Any() ) 
		{
			visitedList.Add( new VisitedEntry( edgeList, 0 ) );
			return null; // No previous entries, return null
		}

		foreach( var entry in visitedList )
		{
			if( Equivalent( entry.EdgeList, edgeList ) ) return entry;
		}

		return null;
	}

	/// <summary>
	/// Converts a path to a list of edges
	/// </summary>	
	List<TspEdge> ToEdgeList( IList<int> path ) => Enumerable.Range( 0, this.Cities )
		.Select( i => i < this.Cities - 1 ? base.map[ path[ i ], path[ i + 1 ] ] : base.map[ path[ i ], path[ 0 ] ] ).ToList();

	/// <summary>
	/// Checks if two edge lists are equivalent
	/// </summary>	
	static bool Equivalent( List<TspEdge> first, List<TspEdge> second ) => first.All( e => second.Contains( e ) );

	/// <summary>
	/// Aggregates TspResult and Edges 
	///</summary>
	class TabooResult( double tour, IList<int> path, List<TspEdge> edges ) : TspResult( tour, path )
	{		
		public List<TspEdge> Edges { get; set; } = edges;

		public ( TspResult, List<TspEdge>) Split() => (new TspResult( this.Tour, this.Path ), this.Edges);		
	}

	/// <summary>
	/// Tabu entry for storing edge and iterations
	/// </summary>	
	class TabuEntry( TspEdge edge, int iter )
	{
		public TspEdge Edge { get; set; } = edge;
		public int Iteration { get; set; } = iter;
		//public int Visits { get; set; } = 1; // Number of visits to this edge
		public int ProhibPeriod { get; set; } = 1; // Prohibition period

		public override string ToString() => $"Edge:{this.Edge} @ {this.Iteration}";
	}

	/// <summary>
	/// Visited entry for storing visited edges and their iterations
	/// </summary>	
	class VisitedEntry( List<TspEdge> edgeList, int iteration )
	{
		public List<TspEdge> EdgeList { get; set; } = edgeList;
		public int Iteration { get; set; } = iteration;
		public int Visits { get; set; } = 1;
		public override string ToString() => $"Edges:{string.Join( ",", this.EdgeList )} @ {this.Iteration}";
	}

	#region obsolete
	//protected override TspResult RunEpoch( TspResult best )
	//	{
	//		// Generate candidate moves (e.g., all 2-opt neighbors)
	//		var candidates = new List<TspResult>();

	//		for( int i = 0; i < map.Cities - 1; i++ )
	//		{
	//			for( int j = i + 1; j < map.Cities; j++ )
	//			{
	//				var newPath = TwoOptSwap( best.Path, i, j );

	//				var key = string.Join( ",", newPath );

	//				if( !tabuList.Contains( key ) )
	//				{
	//					double newTour = map.GetTourLength( newPath );
	//					candidates.Add( new TspResult( newTour, newPath ) );
	//				}
	//			}
	//		}

	//		// Select the best candidate
	//		var next = candidates.OrderBy( r => r.Tour ).FirstOrDefault() ?? best.Clone();

	//		// Update tabu list
	//		tabuList.Add( string.Join( ",", next.Path ) );

	//		// Remove oldest entry (simple FIFO)
	//		if( tabuList.Count > tabuTenure ) tabuList.Remove( tabuList.First() );

	//		return next;
	//	}


	/// <summary>
	/// Taboo Search async wrapper
	/// </summary>	
	//public async Task<TspResult> RunAsync(CancellationToken token )
	//{
	//	base.timer.Start();

	//	var current = base.map.BuildRandomTour();
	//	var best = current.Clone();

	//	base.Draw( best.Tour, 0, best.Path );

	//	await Task.Run( () => 
	//	{
	//		int count = 0;
	//		int noChanges = 0;

	//		var tabuList = new List<TabuEntry>();
	//		var visitedList = new List<VisitedEntry>();

	//		int prohibPeriod = 1;
	//		int avgSize = 1;
	//		int lastChange = 0;

	//		while( noChanges++ < settings.Limit )
	//		{
	//			var candidateEntry = GetCandidateEntry( visitedList, current.Path );

	//			if( candidateEntry != null ) //update existing entry
	//			{
	//				int repetitionInterval = count - candidateEntry.Iteration;

	//				candidateEntry.Iteration = count;
	//				candidateEntry.Visits++;

	//				if( repetitionInterval < 2 * ( this.Cities - 1 ) )
	//				{
	//					avgSize = ( int )Math.Round( 0.1 * ( count - candidateEntry.Iteration ) + 0.9 * avgSize );

	//					prohibPeriod = ( int )( prohibPeriod * settings.IncreaseFactor );

	//					lastChange = count;
	//				}
	//			}

	//			if( count - lastChange > avgSize ) //Reduce the prohib period if no changes for a while
	//			{
	//				prohibPeriod = Math.Max( ( int )( prohibPeriod * settings.DecreaseFactor ), 1 );

	//				lastChange = count;
	//			}

	//			List<TabooResult> candidates = BuildCandidates( current );

	//			var (tabu, admis) = SortNeighborhood( candidates, tabuList, count - prohibPeriod );

	//			if( admis.Count < 2 )
	//			{
	//				prohibPeriod = this.Cities - 2;
	//				lastChange = count;
	//			}

	//			var result = admis.Any() ? admis.First() : tabu.First();

	//			(current, var bestMoveEdges) = result.Split(); 

	//			if( tabu.Any() )
	//			{
	//				var first = tabu.First();

	//				if( first < best && first < current ) (current, bestMoveEdges) = first.Split();					
	//			}

	//			bestMoveEdges.ForEach( edge => UpdateTabu( tabuList, edge, count ) );

	//			var bestCandidate = candidates.First();

	//			if( bestCandidate < best )
	//			{
	//				best = new TspResult( bestCandidate.Tour, bestCandidate.Path );

	//				noChanges = 0;

	//				base.Draw( best.Tour, count, best.Path );
	//			}

	//			if( ++count % settings.Redraw == 0 ) base.Draw( best.Tour, count );
	//		}

	//		base.Draw( best.Tour, count, best.Path );
	//	} );

	//	base.timer.Stop();

	//	return best;
	//}	
	#endregion

}

/// <summary>
/// Configuration Settings
/// </summary>
public class TabuSettings : TspConfigurationBase
{
	[ConfigurationKeyName( "cand-size" )]
	public int CandidateSize { get; set; } // Number of candidates to generate in each iteration

	[ConfigurationKeyName( "tabu-size" )]
	public int TabuListSize { get; set; } // Size of the tabu list

	[ConfigurationKeyName( "tabu-tenure" )]
	public int TabuTenure { get; set; } = 10; // Initial tabu tenure

	public double IncreaseFactor { get; set; } = 1.2; // Factor to increase the tabu tenure
	public double DecreaseFactor { get; set; } = 0.8; // Factor to decrease the tabu tenure
}
