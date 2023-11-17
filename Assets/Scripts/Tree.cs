using System.Collections;
using System.Collections.Generic;

public class TreeNode
{
    public List<CardAction> data { get; set; }
    public TreeNode left { get; set; }
    public TreeNode right { get; set; }

    public TreeNode(List<CardAction> data)
    {
        this.data = data;
        this.left = null;
        this.right = null;
    }
}
