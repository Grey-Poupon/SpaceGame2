using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class SimulateTurns 
{
    // Start is called before the first frame update


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class Move {
    public List<MinCardAction> cards; 
    public Move(List<MinCardAction> cards){
        this.cards=cards;
    }
}

public class Node {
    public Move move;
    public Node parentNode;

    public List<Node> childNodes;
    public float wins;
    public int visits;
    public int avails;

    public int playerJustMoved;


    public Node(Move move=null, Node parent = null, int playerMoved=-1 ){
        this.move = move;
        this.parentNode = parent;
        this.wins=0;
        this.visits=1;
        this.avails =1;
        this.playerJustMoved = playerMoved;
        this.childNodes = new List<Node>();
    }

    public List<Move> GetUntriedMoves(List<Move> legalMoves){

        List<Move> triedMoves = new List<Move>();
        foreach(Node child in childNodes){
            triedMoves.Add(child.move);
        }
        List<Move> untriedMoves = new List<Move>();

        foreach(Move move in legalMoves){
            if(!triedMoves.Contains(move)){
                untriedMoves.Add(move);
            }
        }
        return untriedMoves;
    }

    public Node UCBSelectChild(List<Move> legalMoves, float exploration=0.7f){
            // Use the UCB1 formula to select a child node, filtered by the given list of legal moves.
            //             exploration is a constant balancing between exploitation and exploration, with default value 0.7 (approximately sqrt(2) / 2)

        List<Node> legalChildren = new List<Node>();
        foreach(Node child in childNodes){
            if(legalMoves.Contains(child.move)){
                legalChildren.Add(child);
            }
        }
        float s = 0;
        Node bestChild = legalChildren[0];
        foreach(Node legalChild in legalChildren){
            float t_s = (float)legalChild.wins / (float)legalChild.visits + exploration * Mathf.Sqrt(Mathf.Log(legalChild.avails) / (float)legalChild.visits);
            if(t_s>s){
                s=t_s;
                bestChild = legalChild;
            }
            legalChild.avails++;
        }
        

        return bestChild;
    }

    public Node AddChild(Move m, int playerMoved){
        
        Node n = new Node(m,this,playerMoved);
        this.childNodes.Add(n);
        return n;
    }

    public void Update(GameManager terminalState){
        this.visits++;
        if(this.playerJustMoved!=-1){
            this.wins+=terminalState.GetResult(this.playerJustMoved);
        }
        
    }
}

public class ISMCTS{
    public static Move Search(GameManager rootstate, int itermax, int maxDepth = 10){
        Node rootnode = new Node();
        for(int i =0;i<itermax;i++){
            Node node = rootnode;
            GameManager state = rootstate.Clone();
            List<Move> moves = state.GetMoves();
            // Select
            while(moves.Count>0 && node.GetUntriedMoves(moves).Count==0){
                node = node.UCBSelectChild(moves);
                state.DoMove(node.move);
                moves= state.GetMoves();
            }
            List<Move> untriedMoves = node.GetUntriedMoves(moves);
            // Expand
            if(untriedMoves.Count>0){
                Move m = untriedMoves[ UnityEngine.Random.Range(0, untriedMoves.Count)];
                int player = state.turn == TurnTypes.Player ? 1 : 0;
                state.DoMove(m);
                node = node.AddChild(m,player);
            }

            // Simulate 
            int iterCount = 0;
            while(moves.Count>0 && iterCount < maxDepth){
                iterCount++;
                moves = state.GetMoves();
                Move m = moves[UnityEngine.Random.Range(0, moves.Count)];
                state.DoMove(m);
            }
            
            // Backpropagate
            while(node!=null){
                node.Update(state);
                node = node.parentNode;
            }

        }
        Node best = rootnode.childNodes[0];
        foreach(Node child in rootnode.childNodes){
            if((float)best.wins/(float)best.visits<(float)child.wins/(float)child.visits){
                best = child;
            }
        }
        return best.move;

    } 
}


