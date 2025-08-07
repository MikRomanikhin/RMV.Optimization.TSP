namespace RMV.Optimization.TSP.Domain;

/// <summary>
/// TSP algorithm types
/// </summary>
public enum TspAlgorithm
{
	Unknown, Random, NearestNeighbour, ShortestEdge, FarthestInsert, Beam, Pilot,
	IteratedLocal, GuidedLocal, VariableNeiborhood, GRASP, Scatter, Taboo, 
	Genetic, ES, DE, EP, Classifier,
	Annealing, BranchAndBound,  
	AntSystem, AntColonySystem, MaxMinAnt, PSO, QLearning
}

#region obsolete
//public enum AcoAlgorithm { AntSystem, AntColonySystem }

/// <summary>
/// The possible states for a genetic algorithm.
/// </summary>
//public enum GeneticAlgorithmState
//{
//	NotStarted, // GA has not started yet	
//	Started, // GA started and is running	
//	Stopped, // GA has been stopped and is not running		
//	Resumed, // GA has been resumed after a stop or termination and is running		
//	TerminationReached // GA reached termination condition and is not running
//}

/// <summary>
/// SelectionTypes
/// </summary>
//public enum SelectionType
//{
//	Unknown, Elite, Rank, Roulette, SUS, Tournament, Truncation
//}

/// <summary>
/// Crossover types
/// </summary>
//public enum CrossoverType
//{
//	Unknown, Cycle, OnePoint, TwoPoint, ThreeParents, OrderBased, Ordered, PartiallyMapped, PositionBased, Uniform, VotingRecombination 
//}

/// <summary>
/// Mutation types
/// </summary>
//public enum MutationType
//{
//	Unknown, Displacement, Insertion, Shuffle, Reverse, Twors//, Uniform
//}

/// <summary>
/// Reinsertion types
/// </summary>
//public enum ReinsertionType
//{
//	Unknown, Elitist, Uniform //Pure,Fitness
//}
#endregion
