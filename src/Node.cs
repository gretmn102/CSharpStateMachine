namespace StateMachine
{
    class Node<NodeKey, Action, Context, Command>
        where Action : notnull
        where NodeKey : notnull
    {
        public Dictionary<Action, Transition<NodeKey, Action, Context, Command>> Transitions { get; set; }

        public NodeKey Key { get; set; }

        public Node(
            NodeKey key,
            Dictionary<Action, Transition<NodeKey, Action, Context, Command>> transitions)
        {
            Key = key;
            Transitions = transitions;
        }

        public Node(
            NodeKey name,
            IEnumerable<Transition<NodeKey, Action, Context, Command>> transitions
        ) : this(name, new Dictionary<Action, Transition<NodeKey, Action, Context, Command>>())
        {
            Transitions = new Dictionary<Action, Transition<NodeKey, Action, Context, Command>>(
                transitions.Select(x =>
                    new KeyValuePair<Action, Transition<NodeKey, Action, Context, Command>>(x.Key, x))
            );
        }
    }
}
