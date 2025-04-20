using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulateTurns
{
    void Start() { }

    void Update() { }
}

public class Move
{
    public List<MinCardAction> cards;

    public Move(List<MinCardAction> cards)
    {
        this.cards = cards;
    }
}

public class Node
{
    public Move move;
    public Node parentNode;
    public List<Node> childNodes;
    public float wins;
    public int visits;
    public int avails;
    public int playerJustMoved;

    public Node(Move move = null, Node parent = null, int playerMoved = -1)
    {
        this.move = move;
        this.parentNode = parent;
        this.wins = 0;
        this.visits = 1;
        this.avails = 1;
        this.playerJustMoved = playerMoved;
        this.childNodes = new List<Node>();
    }

    public List<Move> GetUntriedMoves(List<Move> legalMoves)
    {
        List<Move> triedMoves = new List<Move>();
        foreach (Node child in childNodes)
        {
            triedMoves.Add(child.move);
        }
        List<Move> untriedMoves = new List<Move>();

        foreach (Move move in legalMoves)
        {
            if (!triedMoves.Contains(move))
            {
                untriedMoves.Add(move);
            }
        }
        return untriedMoves;
    }

    public Node UCBSelectChild(float exploration = 0.7f)
    {
        // Use the UCB1 formula to select a child node, filtered by the given list of legal moves.
        //             exploration is a constant balancing between exploitation and exploration, with default value 0.7 (approximately sqrt(2) / 2)

        Node bestChild = childNodes[0];
        float s = 0;
        foreach (Node legalChild in childNodes)
        {
            float t_s =
                (float)legalChild.wins / (float)legalChild.visits
                + exploration * Mathf.Sqrt(Mathf.Log(legalChild.avails) / (float)legalChild.visits);
            if (t_s > s)
            {
                s = t_s;
                bestChild = legalChild;
            }
            legalChild.avails++;
        }

        return bestChild;
    }

    public Node AddChild(Move m, int playerMoved)
    {
        Node n = new Node(m, this, playerMoved);
        this.childNodes.Add(n);
        return n;
    }

    public void Update(GameManager terminalState)
    {
        this.visits++;
        if (this.playerJustMoved != -1)
        {
            this.wins += terminalState.simulationController.GetResult(this.playerJustMoved);
        }
    }
}

public class ISMCTS
{
    public static Move Search(
        GameManager rootstate,
        int itermax,
        int maxDepth = 10,
        int explorationLimit = 500
    )
    {
        Node rootnode = new Node();
        for (int i = 0; i < itermax; i++)
        {
            Node node = rootnode;
            GameManager state = rootstate.simulationController.Clone();
            SimulationController simulation = state.simulationController;

            // Select, Replace this conditional with a Limit as we will never fully sample the solution space
            while (node.childNodes.Count > explorationLimit)
            {
                node = node.UCBSelectChild();
                simulation.DoMove(node.move);
            }

            // Simulate
            int iterCount = 0;
            while (iterCount < maxDepth)
            {
                iterCount++;
                Move m = simulation.GetRandomMove();
                simulation.DoMove(m);
                node = node.AddChild(m, state.turn == TurnTypes.Player ? 1 : 0);
            }

            // Backpropagate
            while (node != null)
            {
                node.Update(state);
                node = node.parentNode;
            }
        }
        Node best = rootnode.childNodes[0];
        foreach (Node child in rootnode.childNodes)
        {
            if ((float)best.wins / (float)best.visits < (float)child.wins / (float)child.visits)
            {
                best = child;
            }
        }
        return best.move;
    }
}
