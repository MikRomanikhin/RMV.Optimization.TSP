namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// Methods generating random sequences
/// </summary>
public interface IRandomSequence
{
	/// <summary>
	/// Gets an integer array with values between minimum value (inclusive) and maximum value (exclusive).
	/// </summary>
	/// <returns>The integer array.</returns>
	/// <param name="length">The array length</param>
	/// <param name="min">Minimum value (inclusive).</param>
	/// <param name="max">Maximum value (exclusive).</param>
	public static int[] GetInts( int length, int min, int max ) =>
		Enumerable.Range( min, max - min ).OrderBy( x => Random.Shared.Next() ).Take( length ).ToArray();


	/// <summary>
	/// Generates a list of unique integers within a specified range
	/// </summary>		
	public static List<int> GetUniqueInts( int count, int min, int max )
	{
		if( count > ( max - min + 1 ) ) throw new ArgumentException( "Length exceeds the range of unique values." );

		return Enumerable.Range( min, max - min + 1 ).OrderBy( _ => Random.Shared.Next() ).Take( count ).ToList();
	}

	/// <summary>
	/// Generates a list of unique ordered integers within a specified range
	/// </summary>	
	public static List<int> GetUniqueOrderedInts( int length, int min, int max ) =>
		GetUniqueInts( length, min, max ).OrderBy( x => x ).ToList();
}
