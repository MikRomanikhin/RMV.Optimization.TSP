using RMV.Optimization.TSP.Common;
using RMV.Optimization.TSP.Domain;

namespace RMV.Optimization.TSP.Algorithms;

/// <summary>
/// Beam Search algorithm for TSP
/// </summary>
public class BeamSearch( TspMap map ) : TspAlgorithmBase( map )//, ITspAsync
{
	BeamSettings settings;
	int nextStart = 0;

	protected override void Configure()
	{
		this.settings = ConfigManager.GetSection<BeamSettings>( "beam" );
		base.settings = this.settings as TspConfigurationBase ?? throw new ArgumentNullException( nameof( settings ) );
	}


	protected override TspResult? Initialize()
	{
		// Use city 0 beam search as the initial solution
		this.nextStart = 1;

		return RunBeamSearch( 0 );
	}

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
		var beam = new List<BeamState>( settings.Size ) { new( start ) };

		for( int step = 0; step < base.Cities - 1; step++ )
		{
			beam = ExpandBeam( beam );
		}

		// Complete tours and find best, then refine with local search
		var best = beam.Select( s => CompleteTour( s, start ) ).MinBy( r => r.Tour );

		return Parallel2OptSearch( [ .. best.Path ] );//LinKernighanSearch( [ .. best.Path ] );
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

			foreach( var (city, weight) in nearest )
			{
				nextBeam.Add( state.Extend( city, weight ) );
			}
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

			// Insert in sorted position, keep only top 'take'
			int pos = nearest.Count;

			for( int i = 0; i < nearest.Count; i++ )
			{
				if( w < nearest[ i ].weight ) { pos = i; break; }
			}

			if( pos < take )
			{
				nearest.Insert( pos, (city, w) );

				if( nearest.Count > take ) nearest.RemoveAt( take );
			}
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
		var newPath = new List<int>( this.Path.Count + 1 );
		newPath.AddRange( this.Path );
		newPath.Add( city );

		var newVisited = new HashSet<int>( this.Visited ) { city };

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
