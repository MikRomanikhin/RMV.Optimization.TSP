using RMV.Common.Configuration;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Beam Search algorithm for TSP
/// </summary>
public class BeamSearch( TspMap map ) : TspAlgorithmBase( map )
{
	BeamSettings settings;
	int nextStart = 0;

	/// <summary>
	/// Configures the algorithm by loading the beam search settings from the configuration manager
	/// </summary>	
	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "beam" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );

		this.settings.Size  = Math.Min( this.settings.Size, base.Cities ); //no point expanding beyond the number of cities
	}

	/// <summary>
	/// Initializes the algorithm and generates an initial solution for TSP problem using a beam search starting from city 0.
	/// </summary>
	/// <returns>A <see cref="TspResult"/> representing the initial solution, or <see langword="null"/> if no solution is found.</returns>
	protected override TspResult Initialize()
	{		
		this.nextStart = 1;

		return RunBeamSearch( 0 ); // Use city 0 beam search as the initial solution
	}

	/// <summary>
	/// Performs a single epoch and returns the better of the current result and the provided best
	/// result.
	/// </summary>
	/// <remarks>This method advances the internal search starting point for each epoch. If the internal starting
	/// index exceeds the number of cities, it wraps around to zero. The method compares the new result to the provided
	/// best result using the tour cost, with a margin applied, and returns the superior result.</remarks>
	/// <param name="best">The current best result to compare against. Must not be null.</param>
	/// <returns>A TspResult instance representing the better of the newly computed result and the provided best result.</returns>
	protected override TspResult RunEpoch( TspResult best )
	{
		if( this.nextStart >= base.Cities ) this.nextStart = 0;

		var result = RunBeamSearch( this.nextStart++ );

		return result.Tour + MARGIN < best.Tour ? result : best;
	}


	/// <summary>
	/// Beam Search algorithm with local search refinement
	/// </summary>
	TspResult RunBeamSearch( int start )
	{
		// Guard against very small maps: TSP requires at least 2 cities
		if( base.Cities < 2 ) return TspResult.Build( this.map, [ start ] );

		var beam = new List<BeamState>( settings.Size ) { new( start ) };

		for( int step = 0; step < base.Cities - 1; step++ )
		{
			beam = ExpandBeam( beam );

			// Guard against empty beam during expansion
			if( beam.Count == 0 ) return TspResult.Build( this.map, [ start ] );
		}

		// Guard against empty beam before MinBy
		if( beam.Count == 0 ) return TspResult.Build( this.map, [ start ] );

		// Complete tours and find best, then refine with local search
		var best = beam.Select( s => CompleteTour( s, start ) ).MinBy( r => r.Tour );

		return ParallelLocalSearch( best.Path );
	}

	/// <summary>
	/// Completes a partial tour by adding the return edge
	/// </summary>
	TspResult CompleteTour( BeamState state, int start )
	{
		double tour = state.Cost + map[ state.Path[ ^1 ], start ].Weight;

		return new TspResult( tour, state.Path );
	}

	/// <summary>
	/// Expands beam by considering only top candidates per state, avoiding full enumeration + sort of all possibilities
	/// </summary>
	List<BeamState> ExpandBeam( List<BeamState> beam )
	{
		var nextBeam = new List<BeamState>( settings.Size * 4 );

		foreach( var state in beam )
		{
			// Only expand the nearest candidates per state — no need to generate all
			var nearest = GetNearestAvailable( state );

			nearest.ForEach( item => nextBeam.Add( state.Extend( item.city, item.weight ) ) );
		}

		// Add one random expansion per state for diversity
		foreach( var state in beam )
		{
			int count = base.Cities - state.Visited.Count;

			if( count == 0 ) continue;

			int skip = Random.Shared.Next( count );

			// Pick a random unvisited city without allocating a list
			int randomCity = -1;
			int seen = 0;

			for( int city = 0; city < base.Cities; city++ )
			{
				if( !state.Visited.Contains( city ) )
				{
					if( seen == skip ) { randomCity = city; break; }
					seen++;
				}
			}

			if( randomCity > -1 )
			{
				double weight = map[ state.Path[ ^1 ], randomCity ].Weight;

				nextBeam.Add( state.Extend( randomCity, weight ) );
			}
		}

		nextBeam.Sort( ( a, b ) => a.Cost.CompareTo( b.Cost ) );

		return nextBeam.Count <= settings.Size ? nextBeam : nextBeam.GetRange( 0, settings.Size );
	}


	/// <summary>
	/// Returns the nearest unvisited cities for expansion, limited to beam size to avoid generating thousands of candidates
	/// </summary>
	List<(int city, double weight)> GetNearestAvailable( BeamState state )
	{
		int lastCity = state.Path[ ^1 ];
		int take = settings.Size;

		// Use a simple insertion into a bounded list to find top-N nearest
		var nearest = new List<(int city, double weight)>( take + 1 );

		for( int city = 0; city < base.Cities; city++ )
		{
			if( state.Visited.Contains( city ) ) continue;

			double w = map[ lastCity, city ].Weight;

			// Find sorted insertion position			
			int pos = nearest.FindIndex( item => w < item.weight );
			if( pos == -1 ) pos = nearest.Count;

			nearest.Insert( pos, (city, w) ); // Always insert at the correct sorted position, then trim if needed

			if( nearest.Count > take ) nearest.RemoveAt( take ); // Keep only top 'take' nearest candidates
		}

		return nearest;
	}

}

/// <summary>
/// Lightweight beam state — uses HashSet for O(1) visited checks and avoids repeated path copying during expansion
/// </summary>
sealed class BeamState
{
	public List<int> Path { get; }
	public HashSet<int> Visited { get; }
	public double Cost { get; }

	public BeamState( int startCity )
	{
		this.Path = [ startCity ];
		this.Visited = [ startCity ];
		this.Cost = 0;
	}

	BeamState( List<int> path, HashSet<int> visited, double cost )
	{
		this.Path = path;
		this.Visited = visited;
		this.Cost = cost;
	}

	/// <summary>
	/// Creates a new state by extending this state with a new city
	/// </summary>
	public BeamState Extend( int city, double edgeWeight )
	{
		// Pre-allocate with exact size needed to avoid resizing
		var newPath = new List<int>( this.Path.Count + 1 );
		newPath.AddRange( this.Path );
		newPath.Add( city );

		// Copy visited set and add new city
		var newVisited = new HashSet<int>( this.Visited ) { city	};

		return new BeamState( newPath, newVisited, this.Cost + edgeWeight );
	}
}

/// <summary>
/// Configuration Settings
/// </summary>
public class BeamSettings : TspConfigurationBase
{
	public int Size { get; set; }
}
