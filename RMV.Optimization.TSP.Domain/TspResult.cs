namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP algorithm result
/// </summary>
public class TspResult: IEquatable<TspResult> //: ICloneable
{
	public TspResult() { }

	public TspResult( double tour, List<int> path )
	{
		this.Tour = tour;
		this.Path = path ?? throw new ArgumentNullException( nameof( path ), "Path cannot be null" );
	}

	/// <summary>
	/// Builds new TspResult from the given map and path
	/// </summary>	
	public static TspResult Build( TspMap map, List<int> path ) => new( map.GetTourLength( path ), path );
	

	#region Properties ---------------------------------------------------------

	public double Tour { get; set; } //= tour;

	public double Fitness => Math.Round( Tour, 2 );

	public bool HasFitness => this.Fitness > 0; //if fitness is 0, then the tour is not valid

	public List<int> Path { get; set; }// = path;

	#endregion


	const double MARGIN = 0.0001;
	public static bool operator <( TspResult a, TspResult b ) => a.Tour + MARGIN < b.Tour;
	public static bool operator >( TspResult a, TspResult b ) => a.Tour > b.Tour + MARGIN;
		
	public TspResult Clone() => new( this.Tour, [ .. this.Path ] );

	public override string ToString() => $"Tour:{this.Tour:0.##} Path:[{string.Join( ',', this.Path )}]";	

	#region IList --------------------------------------------------------------

	public void Add( int item ) => this.Path.Add( item );	
	public void Clear() => this.Path.Clear();	
	public bool Contains( int item ) => Path.Contains( item );

	public bool Equals( TspResult other )
	{
		if( other == null ) return false;
		if( ReferenceEquals( this, other ) ) return true;

		// Compare paths: same cities in same order
		return this.Path.SequenceEqual( other.Path );
	}

	public override bool Equals( object obj ) => Equals( obj as TspResult );

	public override int GetHashCode()
	{
		unchecked // Hash based on path contents for duplicate detection
		{
			int hash = 17;
			foreach( var city in this.Path )
			{
				hash = hash * 31 + city.GetHashCode();
			}

			return hash;
		}
	}

	//public void CopyTo( int[] array, int arrayIndex ) => Path.CopyTo( array, arrayIndex );
	//public bool Remove( int item ) => _list.Remove( item );	
	//public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();	
	//IEnumerator IEnumerable.GetEnumerator() => ( ( IEnumerable )_list ).GetEnumerator();

	#endregion
}


/// <summary>
/// Custom draw arguments
/// </summary>
public class DrawEventArgs( double tour, int counter, string time, IEnumerable<int>? path = null ) : EventArgs
{
	public double Tour { get; init; } = tour;
	public IEnumerable<int> Path { get; init; } = path;
	public int Counter { get; init; } = counter; //iterations counter
	public string Time { get; init; } = time;
	//public string Message { get; init; } = message;
} 
