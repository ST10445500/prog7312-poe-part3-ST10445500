//ST10445500 - PROG7312 - SmartX POE
//AvlTree

//.....................................o0oSTART OF FILEo0o........................................//

// This collection is written by hand so telemetry can be found by time range without walking every reading.

namespace SmartX.Api.Collections
{
    //keeps readings in a binary search tree that rebalances itself after every insert, so it never leans into a list
    //insert is O(log n) and a range query is O(log n + k) for k results, the height stays under about 1.44 log2(n)
    //it is not thread safe on its own, whoever owns the tree takes the lock
    public class AvlTree<TKey, TValue> where TKey : IComparable<TKey>
    {
        //one key in the tree, readings with the same key share the node
        private class Node
        {
            public TKey Key { get; }

            public List<TValue> Values { get; } = new();

            public Node? Left { get; set; }

            public Node? Right { get; set; }

            // A leaf is height 1, so a missing child counts as 0.
            public int Height { get; set; } = 1;

            public Node(TKey key, TValue value)
            {
                Key = key;
                Values.Add(value);
            }
        }

        private Node? _root;
        private int _count;

        //..............................................................................//

        //retrieves how many values are stored, a repeated key counts every value
        public int Count => _count;

        //retrieves the height of the tree, 0 when it is empty
        public int Height => HeightOf(_root);

        //..............................................................................//

        //adds a value under a key and rebalances on the way back up
        //a key that is already in the tree keeps the old value and gets the new one after it
        public void Insert(TKey key, TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _root = Insert(_root, key, value);
            _count++;
        }

        //..............................................................................//

        //retrieves every value whose key is between from and to, both included, in key order
        //a subtree is only entered if it can hold a key inside the range
        public List<TValue> GetRange(TKey from, TKey to)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            if (to == null)
            {
                throw new ArgumentNullException(nameof(to));
            }

            if (from.CompareTo(to) > 0)
            {
                throw new ArgumentException("The start of the range is after the end.", nameof(from));
            }

            var found = new List<TValue>();
            Collect(_root, from, to, found);
            return found;
        }

        //..............................................................................//

        //inserts into one subtree and gives back the root that subtree has afterwards
        private Node Insert(Node? node, TKey key, TValue value)
        {
            if (node == null)
            {
                return new Node(key, value);
            }

            var order = key.CompareTo(node.Key);

            if (order < 0)
            {
                node.Left = Insert(node.Left, key, value);
            }
            else if (order > 0)
            {
                node.Right = Insert(node.Right, key, value);
            }
            else
            {
                // Same key, so the shape does not change and there is nothing to rebalance.
                node.Values.Add(value);
                return node;
            }

            return Rebalance(node);
        }

        //..............................................................................//

        //fixes a node whose two sides differ in height by more than one, returns the new top of that subtree
        private Node Rebalance(Node node)
        {
            UpdateHeight(node);
            var balance = HeightOf(node.Left) - HeightOf(node.Right);

            if (balance > 1)
            {
                // Left side is too tall. If the extra weight is on its right, turn it first (left right case).
                if (HeightOf(node.Left!.Left) < HeightOf(node.Left.Right))
                {
                    node.Left = RotateLeft(node.Left);
                }

                return RotateRight(node);
            }

            if (balance < -1)
            {
                // Mirror of the above (right left case).
                if (HeightOf(node.Right!.Right) < HeightOf(node.Right.Left))
                {
                    node.Right = RotateRight(node.Right);
                }

                return RotateLeft(node);
            }

            return node;
        }

        //..............................................................................//

        //lifts the right child above the node, the node becomes its left child
        private Node RotateLeft(Node node)
        {
            var top = node.Right!;
            node.Right = top.Left;
            top.Left = node;

            // The lower node has to be updated first because the new top depends on it.
            UpdateHeight(node);
            UpdateHeight(top);
            return top;
        }

        //..............................................................................//

        //lifts the left child above the node, the node becomes its right child
        private Node RotateRight(Node node)
        {
            var top = node.Left!;
            node.Left = top.Right;
            top.Right = node;

            UpdateHeight(node);
            UpdateHeight(top);
            return top;
        }

        //..............................................................................//

        //works out a node's height from its two children
        private static void UpdateHeight(Node node)
        {
            node.Height = 1 + Math.Max(HeightOf(node.Left), HeightOf(node.Right));
        }

        //retrieves a node's height, 0 for a missing one
        private static int HeightOf(Node? node)
        {
            return node == null ? 0 : node.Height;
        }

        //..............................................................................//

        //walks the tree in key order and adds the values inside the range
        private static void Collect(Node? node, TKey from, TKey to, List<TValue> found)
        {
            if (node == null)
            {
                return;
            }

            // Everything to the left is smaller than this key, so skip it if the range starts at or after the key.
            if (from.CompareTo(node.Key) < 0)
            {
                Collect(node.Left, from, to, found);
            }

            if (from.CompareTo(node.Key) <= 0 && node.Key.CompareTo(to) <= 0)
            {
                found.AddRange(node.Values);
            }

            // Same on the right, nothing there can be inside the range if the range ends at or before the key.
            if (node.Key.CompareTo(to) < 0)
            {
                Collect(node.Right, from, to, found);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o........................................//
