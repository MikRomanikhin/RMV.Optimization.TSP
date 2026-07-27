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
		
	readonly List<TabuEntry> tabuList = [];
	readonly List<VisitedEntry> visitedList = [];

	int tabuTenure;
	int prohibPeriod = 1;
	int avgSize = 1;
	int lastChange = 0;

	/// <summary>
	/// Configures the Tabu Search algorithm by loading settings and initializing the tour.
	/// </summary>
	/// <remarks>
	/// Method is called during initialization to set up the algorithm's configuration. It retrieves the Tabu Search settings from the
	/// configuration manager and prepares the initial tour state. Override this method to customize the configuration process if necessary.
	/// </remarks>
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<TabuSettings>( "tabu" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );

		this.tabuTenure = settings.TabuTenure;

		this.current = base.InitializeTour();
	}

	/// <summary>
	/// Executes a single epoch
	/// </summary>
	/// <param name="best">The current best solution to use as the starting point for the epoch. Cannot be null.</param>
	/// <returns>A new TspResult instance representing the best solution found during this epoch.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		var candidateEntry = GetCandidateEntry( visitedList, this.current.Path );

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

			//if( first < best && first < current ) (current, bestMoveEdges) = first.Split();
			if( first < best ) (current, bestMoveEdges) = first.Split();
		}

		bestMoveEdges.ForEach( edge => UpdateTabu( tabuList, edge, count, settings.TabuListSize, tabuTenure ) );

		return current; // candidates.First();
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
	static bool IsTabu( TspEdge edge, List<TabuEntry> tabuList, int delta ) =>	
		tabuList.Any( e => e.Edge == edge && e.ProhibPeriod >= delta );


	/// <summary>
	/// Creates or updates tabu entry for the given edge
	/// </summary>	
	static void UpdateTabu( List<TabuEntry> tabuList, TspEdge edge, int iteration, int maxSize, int tabuTenure )
	{
		var entry = tabuList.FirstOrDefault( e => e.Edge.Equals( edge ) );

		if( entry == null )
		{
			if( tabuList.Count >= maxSize )
			{
				var oldest = tabuList.MinBy( e => e.Iteration );
				tabuList.Remove( oldest );
			}
			tabuList.Add( new TabuEntry( edge, iteration ) { ProhibPeriod = iteration + tabuTenure } );
		}
		else 
		{
			entry.Iteration = iteration;
			entry.ProhibPeriod = iteration + tabuTenure;
		}
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
	List<TspEdge> ToEdgeList( IList<int> path ) => [ .. Enumerable.Range( 0, this.Cities )
		.Select( i => i < this.Cities - 1 ? base.map[ path[ i ], path[ i + 1 ] ] : base.map[ path[ i ], path[ 0 ] ] ) ];

	/// <summary>
	/// Checks if two edge lists are equivalent
	/// </summary>	
	static bool Equivalent( List<TspEdge> first, List<TspEdge> second )
	{
		if( first.Count != second.Count ) return false;

		var secondSet = new HashSet<TspEdge>( second );

		return first.All( e => secondSet.Contains( e ) );
	}

	/// <summary>
/// Aggregates TspResult and Edges 
///</summary>
class TabooResult( double tour, List<int> path, List<TspEdge> edges ) : TspResult( tour, path )
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
