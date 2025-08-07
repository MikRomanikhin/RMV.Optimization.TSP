using System.Globalization;

namespace RMV.Optimization.TSP.Common;

/// <summary>
/// Initializes a new instance of the <see cref="IntRange"/> structure.
/// </summary>	
/// <param name="min">Minimum value of the range.</param>
/// <param name="max">Maximum value of the range.</param>	
public struct IntRange( int min, int max )
{
	public int Min { get; set; } = min;
	public int Max { get; set; } = max;

	/// <summary>
	/// Length of the range (deffirence between maximum and minimum values).
	/// </summary>
	public readonly int Length => Max - Min;

	/// <summary>
	/// Check if the specified value is inside of the range.
	/// </summary>
	/// <param name="x">Value to check.</param>
	/// <returns><b>True</b> if the specified value is inside of the range or <b>false</b> otherwise.</returns>	 
	public readonly bool IsInside( int x ) => ( x >= this.Min ) && ( x <= this.Max );
	

	/// <summary>
	/// Check if the specified range is inside of the range.
	/// </summary>	
	/// <param name="range">Range to check.</param>	
	/// <returns><b>True</b> if the specified range is inside of the range or <b>false</b> otherwise.</returns>	
	public readonly bool IsInside( IntRange range ) => IsInside( range.Min ) && IsInside( range.Max );


	/// <summary>
	/// Check if the specified range overlaps with this range.
	/// </summary>	
	/// <param name="range">Range to check for overlapping.</param>	
	/// <returns><b>True</b> if the specified range overlaps with the range or <b>false</b> otherwise.</returns>	
	public readonly bool IsOverlapping( IntRange range ) => 
		IsInside( range.Min ) || IsInside( range.Max ) || range.IsInside( this.Min ) || range.IsInside( this.Max );
	

	/// <summary>
	/// Implicit conversion to <see cref="Range"/>.
	/// </summary>	
	/// <param name="range">Integer range to convert to single precision range.</param>	
	/// <returns>Returns new single precision range which min/max values are implicitly converted
	/// to floats from min/max values of the specified integer range.</returns>	
	public static implicit operator Range( IntRange range ) => new( range.Min, range.Max );
	

	/// <summary>
	/// Equality operator - checks if two ranges have equal min/max values.
	/// </summary>	 
	/// <param name="range1">First range to check.</param>
	/// <param name="range2">Second range to check.</param>
	/// <returns>Returns <see langword="true"/> if min/max values of specified ranges are equal.</returns>	
	public static bool operator ==( IntRange range1, IntRange range2 ) => ( range1.Min == range2.Min ) && ( range1.Max == range2.Max );
	

	/// <summary>
	/// Inequality operator - checks if two ranges have different min/max values.
	/// </summary>
	/// <param name="range1">First range to check.</param>
	/// <param name="range2">Second range to check.</param>	
	/// <returns>Returns <see langword="true"/> if min/max values of specified ranges are not equal.</returns>	
	public static bool operator !=( IntRange range1, IntRange range2 ) => !( range1 == range2 );
	

	/// <summary>
	/// Check if this instance of <see cref="Range"/> equal to the specified one.
	/// </summary>	 
	/// <param name="obj">Another range to check equalty to.</param>	
	/// <returns>Return <see langword="true"/> if objects are equal.</returns>	
	public override readonly bool Equals( object obj ) => ( obj is IntRange range ) && ( this == range );
	

	/// <summary>
	/// Get hash code for this instance.
	/// </summary>
	/// <returns>Returns the hash code for this instance.</returns>	
	public override readonly int GetHashCode() => Min.GetHashCode() + Max.GetHashCode();
	

	/// <summary>
	/// Get string representation of the class.
	/// </summary>	 
	/// <returns>Returns string, which contains min/max values of the range in readable form.</returns>	
	public override readonly string ToString() => string.Format( CultureInfo.InvariantCulture, $"{Min}, {Max}" );	
}
