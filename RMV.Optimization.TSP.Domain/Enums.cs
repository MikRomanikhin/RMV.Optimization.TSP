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


