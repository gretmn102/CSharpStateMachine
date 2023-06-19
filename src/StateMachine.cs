namespace StateMachine
{
    class Machine<NodeKey, Action, Context, Command>
        where Action : notnull
        where NodeKey : notnull
    {
        private Dictionary<NodeKey, Node<NodeKey, Action, Context, Command>> _nodes;

        public Dictionary<NodeKey, Node<NodeKey, Action, Context, Command>> Nodes
        {
            get { return _nodes; }
            set
            {
                _nodes = value;
                SetCurrentNodeKey(CurrentNodeKey);
            }
        }

        Node<NodeKey, Action, Context, Command>? _currentNode;

        private NodeKey? _currentNodeKey;

        public void SetCurrentNodeKey(NodeKey? value)
        {
            _currentNodeKey = value;
            if (value != null)
            {
                _nodes.TryGetValue(value, out var result);
                _currentNode = result;
            }
        }

        public NodeKey? CurrentNodeKey
        {
            get { return _currentNodeKey; }
            set { SetCurrentNodeKey(value); }
        }

        public Context State { get; set; }

        public Action<IEnumerator<Command>> Interpret { get; set; }

        public Machine(
            Dictionary<NodeKey, Node<NodeKey, Action, Context, Command>> nodes,
            NodeKey initNode,
            Action<IEnumerator<Command>> interpret,
            Context context)
        {
            _nodes = nodes; // to avoid csharp(CS8618)
            Nodes = nodes;
            CurrentNodeKey = initNode;
            Interpret = interpret;
            State = context;
        }

        public Machine(
            IEnumerable<Node<NodeKey, Action, Context, Command>> nodes,
            NodeKey initNode,
            Action<IEnumerator<Command>> interpret,
            Context context
        ) : this(new Dictionary<NodeKey, Node<NodeKey, Action, Context, Command>>(), initNode, interpret, context)
        {
            Nodes = new Dictionary<NodeKey, Node<NodeKey, Action, Context, Command>>(
                nodes.Select(x =>
                    new KeyValuePair<NodeKey, Node<NodeKey, Action, Context, Command>>(x.Key, x))
            );
        }

        public void Do(Action action)
        {
            if (_currentNode == null)
            {
                throw new Exception($"_currentNode is null {action}");
            }
            else
            {
                if (!_currentNode.Transitions.TryGetValue(action, out var transition))
                {
                    throw new Exception($"Not found {action}");
                }
                else
                {
                    CurrentNodeKey = transition.To;
                    var act = transition.Act;
                    if (act == null)
                    {
                        return;
                    }
                    else
                    {
                        var res = act.Invoke(State);
                        if (res == null)
                        {
                            return;
                        }
                        else
                        {
                            Interpret(res);
                        }
                    }
                }
            }
        }
    }
}
